using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;

    public QuizService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─── Public ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<QuizQuestionDto>> GetAllQuestionsAsync(CancellationToken cancellationToken = default)
    {
        // Load questions with their answer options (shallow include, supported by IRepository)
        var questions = await _unitOfWork.Repository<QuizQuestion>()
            .GetAllWithIncludesAsync(q => q.AnswerOptions);

        return questions
            .OrderBy(q => q.SortOrder)
            .Select(MapQuestionToDto);
    }

    public async Task<QuizResultDto> SubmitQuizAsync(QuizSubmitDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.SelectedAnswerIds == null || dto.SelectedAnswerIds.Count == 0)
            throw new ArgumentException("Bütün sualları cavablandırın.");

        // Load all questions with their options to validate one answer per question
        var questions = await _unitOfWork.Repository<QuizQuestion>()
            .GetAllWithIncludesAsync(q => q.AnswerOptions);

        var orderedQuestions = questions.OrderBy(q => q.SortOrder).ToList();

        foreach (var question in orderedQuestions)
        {
            var questionAnswerIds = question.AnswerOptions.Select(a => a.Id).ToHashSet();
            var userAnswersForQuestion = dto.SelectedAnswerIds
                .Where(id => questionAnswerIds.Contains(id))
                .ToList();

            if (userAnswersForQuestion.Count == 0)
                throw new ArgumentException($"'{question.QuestionText}' sualına cavab verilmədi.");

            if (userAnswersForQuestion.Count > 1)
                throw new ArgumentException($"'{question.QuestionText}' sualına yalnız bir cavab seçin.");
        }

        var selectedAnswerIdSet = dto.SelectedAnswerIds.ToHashSet();

        // Load active rules with their answer option IDs
        var rules = await _unitOfWork.Repository<QuizRule>()
            .GetAllWithIncludesAsync(r => r.Answers, r => r.Products);

        var activeRules = rules.Where(r => r.IsActive).ToList();

        // A rule matches only if ALL its required answer IDs are in the user's selections
        var matchedProductIds = new HashSet<Guid>();

        foreach (var rule in activeRules)
        {
            if (rule.Answers.Count == 0) continue;

            var ruleAnswerIds = rule.Answers.Select(a => a.AnswerOptionId).ToHashSet();
            bool allMatch = ruleAnswerIds.All(id => selectedAnswerIdSet.Contains(id));

            if (allMatch)
            {
                foreach (var rp in rule.Products)
                    matchedProductIds.Add(rp.ProductId);
            }
        }

        if (matchedProductIds.Count == 0)
            return new QuizResultDto { RecommendedProducts = new List<ProductDto>() };

        // Load matched active products with their related data
        var allProducts = await _unitOfWork.Repository<Product>()
            .GetAllWithIncludesAsync(p => p.Category, p => p.Brand, p => p.Images);

        var matchedProducts = allProducts
            .Where(p => matchedProductIds.Contains(p.Id) && p.IsActive)
            .ToList();

        return new QuizResultDto
        {
            RecommendedProducts = matchedProducts.Select(MapProductToDto).ToList()
        };
    }

    // ─── Admin ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<QuizRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _unitOfWork.Repository<QuizRule>()
            .GetAllWithIncludesAsync(r => r.Answers, r => r.Products);

        var ruleDtos = rules.OrderByDescending(r => r.CreatedAt).ToList();

        // Enrich answer and product names with a second query
        await EnrichRuleCollections(ruleDtos, cancellationToken);

        return ruleDtos.Select(MapRuleToDto);
    }

    public async Task<QuizRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _unitOfWork.Repository<QuizRule>()
            .GetByIdWithIncludesAsync(id, r => r.Answers, r => r.Products);

        if (rule == null) return null;

        await EnrichRuleCollections(new[] { rule }, cancellationToken);
        return MapRuleToDto(rule);
    }

    public async Task<QuizRuleDto> CreateRuleAsync(CreateQuizRuleDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.AnswerOptionIds == null || dto.AnswerOptionIds.Count == 0)
            throw new ArgumentException("Ən az bir cavab seçimi tələb olunur.");

        if (dto.ProductIds == null || dto.ProductIds.Count == 0)
            throw new ArgumentException("Ən az bir məhsul tələb olunur.");

        // Validate answer options exist
        var answerCount = await _unitOfWork.Repository<QuizAnswerOption>()
            .CountAsync(a => dto.AnswerOptionIds.Contains(a.Id), cancellationToken);

        if (answerCount != dto.AnswerOptionIds.Count)
            throw new ArgumentException("Bir və ya bir neçə cavab seçimi tapılmadı.");

        // Validate products exist
        var productCount = await _unitOfWork.Repository<Product>()
            .CountAsync(p => dto.ProductIds.Contains(p.Id), cancellationToken);

        if (productCount != dto.ProductIds.Count)
            throw new ArgumentException("Bir və ya bir neçə məhsul tapılmadı.");

        var ruleId = Guid.NewGuid();
        var rule = new QuizRule
        {
            Id = ruleId,
            RuleDescription = dto.RuleDescription,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Answers = dto.AnswerOptionIds.Select(aid => new QuizRuleAnswer
            {
                RuleId = ruleId,
                AnswerOptionId = aid
            }).ToList(),
            Products = dto.ProductIds.Select(pid => new QuizRuleProduct
            {
                RuleId = ruleId,
                ProductId = pid
            }).ToList()
        };

        await _unitOfWork.Repository<QuizRule>().AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetRuleByIdAsync(ruleId, cancellationToken)
               ?? throw new InvalidOperationException("Qayda yaradılarkən xəta baş verdi.");
    }

    public async Task<QuizRuleDto> UpdateRuleAsync(Guid id, UpdateQuizRuleDto dto, CancellationToken cancellationToken = default)
    {
        var rule = await _unitOfWork.Repository<QuizRule>()
            .GetByIdWithIncludesAsync(id, r => r.Answers, r => r.Products)
            ?? throw new ArgumentException($"ID '{id}' olan qayda tapılmadı.");

        if (dto.AnswerOptionIds == null || dto.AnswerOptionIds.Count == 0)
            throw new ArgumentException("Ən az bir cavab seçimi tələb olunur.");

        if (dto.ProductIds == null || dto.ProductIds.Count == 0)
            throw new ArgumentException("Ən az bir məhsul tələb olunur.");

        var answerCount = await _unitOfWork.Repository<QuizAnswerOption>()
            .CountAsync(a => dto.AnswerOptionIds.Contains(a.Id), cancellationToken);

        if (answerCount != dto.AnswerOptionIds.Count)
            throw new ArgumentException("Bir və ya bir neçə cavab seçimi tapılmadı.");

        var productCount = await _unitOfWork.Repository<Product>()
            .CountAsync(p => dto.ProductIds.Contains(p.Id), cancellationToken);

        if (productCount != dto.ProductIds.Count)
            throw new ArgumentException("Bir və ya bir neçə məhsul tapılmadı.");

        // Update scalar fields
        rule.RuleDescription = dto.RuleDescription;
        rule.IsActive = dto.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        // Replace answer mappings
        _unitOfWork.Repository<QuizRuleAnswer>().RemoveRange(rule.Answers);
        rule.Answers = dto.AnswerOptionIds.Select(aid => new QuizRuleAnswer
        {
            RuleId = rule.Id,
            AnswerOptionId = aid
        }).ToList();

        // Replace product mappings
        _unitOfWork.Repository<QuizRuleProduct>().RemoveRange(rule.Products);
        rule.Products = dto.ProductIds.Select(pid => new QuizRuleProduct
        {
            RuleId = rule.Id,
            ProductId = pid
        }).ToList();

        _unitOfWork.Repository<QuizRule>().Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetRuleByIdAsync(rule.Id, cancellationToken)
               ?? throw new InvalidOperationException("Qayda yenilənərkən xəta baş verdi.");
    }

    public async Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _unitOfWork.Repository<QuizRule>().GetByIdAsync(id, cancellationToken);
        if (rule == null) return false;

        _unitOfWork.Repository<QuizRule>().Remove(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads answer texts and product names for rule collections since the repository
    /// doesn't support nested/chained includes (ThenInclude).
    /// </summary>
    private async Task EnrichRuleCollections(IEnumerable<QuizRule> rules, CancellationToken cancellationToken)
    {
        var allAnswerIds = rules.SelectMany(r => r.Answers.Select(a => a.AnswerOptionId)).Distinct().ToList();
        var allProductIds = rules.SelectMany(r => r.Products.Select(p => p.ProductId)).Distinct().ToList();

        var answerOptions = (await _unitOfWork.Repository<QuizAnswerOption>()
            .FindAsync(a => allAnswerIds.Contains(a.Id), cancellationToken))
            .ToDictionary(a => a.Id);

        var products = (await _unitOfWork.Repository<Product>()
            .FindAsync(p => allProductIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var rule in rules)
        {
            foreach (var ra in rule.Answers)
            {
                if (answerOptions.TryGetValue(ra.AnswerOptionId, out var ao))
                    ra.AnswerOption = ao;
            }

            foreach (var rp in rule.Products)
            {
                if (products.TryGetValue(rp.ProductId, out var p))
                    rp.Product = p;
            }
        }
    }

    // ─── Mappers ───────────────────────────────────────────────────────────

    private static QuizQuestionDto MapQuestionToDto(QuizQuestion q) => new()
    {
        Id = q.Id,
        QuestionText = q.QuestionText,
        StepKey = q.StepKey,
        SortOrder = q.SortOrder,
        AnswerOptions = q.AnswerOptions
            .OrderBy(a => a.SortOrder)
            .Select(a => new QuizAnswerOptionDto
            {
                Id = a.Id,
                AnswerCode = a.AnswerCode,
                AnswerText = a.AnswerText,
                SubText = a.SubText,
                SortOrder = a.SortOrder
            }).ToList()
    };

    private static QuizRuleDto MapRuleToDto(QuizRule r) => new()
    {
        Id = r.Id,
        RuleDescription = r.RuleDescription,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Answers = r.Answers.Select(a => new QuizRuleAnswerDto
        {
            AnswerOptionId = a.AnswerOptionId,
            AnswerCode = a.AnswerOption?.AnswerCode ?? string.Empty,
            AnswerText = a.AnswerOption?.AnswerText ?? string.Empty
        }).ToList(),
        Products = r.Products.Select(p => new QuizRuleProductDto
        {
            ProductId = p.ProductId,
            ProductName = p.Product?.Name ?? string.Empty
        }).ToList()
    };

    private static ProductDto MapProductToDto(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            IsActive = p.IsActive,
            IsHotDeal = p.IsHotDeal,
            StockQuantity = p.StockQuantity,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            CategorySlug = p.Category?.Slug ?? string.Empty,
            ParentCategoryName = p.Category?.ParentCategory?.Name,
            ParentCategorySlug = p.Category?.ParentCategory?.Slug,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name,
            ImageUrl = p.ImageUrl,
            DetailImageUrl = p.DetailImageUrl,
            Price = p.Price,
            DiscountedPrice = p.DiscountedPrice,
            CreatedAt = p.CreatedAt,
            Images = p.Images?.Select(img => new ProductImageDto
            {
                Id = img.Id,
                ImageUrl = img.ImageUrl,
                ThumbnailUrl = img.ThumbnailUrl,
                MediumUrl = img.MediumUrl,
                AltText = img.AltText,
                IsPrimary = img.IsPrimary,
                SortOrder = img.SortOrder
            }).ToList() ?? new List<ProductImageDto>()
        };
    }
}
