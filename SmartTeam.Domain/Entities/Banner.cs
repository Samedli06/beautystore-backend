namespace SmartTeam.Domain.Entities;

public class Banner
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool TitleVisible { get; set; } = true;
    public string? Description { get; set; }
    public bool DescriptionVisible { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public bool ButtonVisible { get; set; } = true;
    public BannerType Type { get; set; } = BannerType.Hero;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // --- NEW: Title positioning & style ---
    public int TitlePositionX { get; set; } = 50;        // % from left (0-100)
    public int TitlePositionY { get; set; } = 20;        // % from top (0-100)
    public int TitleFontSize { get; set; } = 32;         // px
    public string TitleColor { get; set; } = "#ffffff";
    public string TitleAlign { get; set; } = "center";   // left | center | right

    // --- NEW: Description positioning & style ---
    public int DescriptionPositionX { get; set; } = 50;
    public int DescriptionPositionY { get; set; } = 40;
    public int DescriptionFontSize { get; set; } = 16;
    public string DescriptionColor { get; set; } = "#eeeeee";

    // --- NEW: Button positioning & style ---
    public int ButtonPositionX { get; set; } = 50;
    public int ButtonPositionY { get; set; } = 65;
    public string ButtonColor { get; set; } = "#ffffff";     // background
    public string ButtonTextColor { get; set; } = "#000000";
    public int ButtonBorderRadius { get; set; } = 8;         // px
    public int ButtonPaddingX { get; set; } = 24;          // px
    public int ButtonPaddingY { get; set; } = 10;          // px
    public int ButtonFontSize { get; set; } = 14;          // px
}

public enum BannerType
{
    Hero = 0,        // Main hero banner (Top)
    Top = 1,         // Additional top banners
    Middle = 2,      // Between sections
    Bottom = 3,      // Near footer
    Popup = 4        // Promotional popups/modals
}
