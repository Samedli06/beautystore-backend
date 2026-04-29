using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public class AzerpostService : IAzerpostService
{
    private readonly HttpClient _httpClient;
    private readonly AzerpostSettings _settings;
    private readonly ILogger<AzerpostService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AzerpostService(HttpClient httpClient, IOptions<AzerpostSettings> settings, ILogger<AzerpostService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Set base address and default headers once
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Fail fast so a hung test API doesn't block user checkout flow
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
    }

    // ── Create single order ───────────────────────────────────────────────────

    public async Task<string?> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildCreateRequest(order);
            var response = await PostAsync<AzerpostCreateOrderResponse>("order/create", request, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Azerpost CreateOrder returned null response for order {OrderNumber}", order.OrderNumber);
                return null;
            }

            if (!response.IsSuccess)
            {
                _logger.LogWarning("Azerpost CreateOrder failed (code={Code}) for order {OrderNumber}", response.code, order.OrderNumber);
                return null;
            }

            _logger.LogInformation(
                "Azerpost order created: AzerpostId={AzerpostId}, Charge={Charge}, for order {OrderNumber}",
                response.data!.order_Id, response.data.charge, order.OrderNumber);

            return response.data!.order_Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azerpost CreateOrder for order {OrderNumber}", order.OrderNumber);
            return null;
        }
    }

    public async Task<(string? TrackingId, decimal DeliveryFee, string? ErrorMessage)> CreateOrderWithFeeAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildCreateRequest(order);
            var response = await PostAsync<AzerpostCreateOrderResponse>("order/create", request, cancellationToken);

            if (response == null || !response.IsSuccess || response.data == null)
            {
                _logger.LogWarning("Azerpost CreateOrderWithFee failed or returned null data for order {OrderNumber}", order.OrderNumber);
                return (null, 0m, $"API returned unsuccessful code or null data.");
            }

            _logger.LogInformation(
                "Azerpost order created: AzerpostId={AzerpostId}, Charge={Charge}, for order {OrderNumber}",
                response.data.order_Id, response.data.charge, order.OrderNumber);

            // Attempt to parse the charge string (e.g. "2.50") into a decimal
            decimal fee = 0m;
            if (!string.IsNullOrWhiteSpace(response.data.charge))
            {
                // Ensure parsing works with standard decimal formats (en-US typical for APIs)
                _ = decimal.TryParse(response.data.charge, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out fee);
            }

            return (response.data.order_Id, fee, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azerpost CreateOrderWithFeeAsync for order {OrderNumber}", order.OrderNumber);
            return (null, 0m, ex.Message);
        }
    }

    // ── Create bulk orders ────────────────────────────────────────────────────

    public async Task<Dictionary<string, string>> CreateBulkOrdersAsync(IEnumerable<Order> orders, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var orderList = orders.ToList();
            var bulkRequest = new AzerpostBulkCreateOrderRequest
            {
                vendor_id = _settings.VendorId,
                package_list = orderList.Select(o => new AzerpostBulkPackage
                {
                    package_id       = o.OrderNumber,
                    delivery_post_code = o.DeliveryPostCode ?? string.Empty,
                    package_weight   = (double)(o.PackageWeight > 0 ? o.PackageWeight : 0.1m),
                    customer_address = o.ShippingAddress ?? string.Empty,
                    first_name       = o.CustomerName,
                    last_name        = string.Empty,
                    email            = o.CustomerEmail,
                    phone_no         = string.IsNullOrWhiteSpace(o.CustomerPhone) ? null : o.CustomerPhone,
                    user_passport    = o.UserPassport,
                    delivery_type    = ((int)o.DeliveryType).ToString(),
                    vendor_payment   = 0,
                    fragile          = o.Fragile ? 1 : 0,
                    vendor_payment_status = 0
                }).ToList()
            };

            var response = await PostAsync<AzerpostCreateOrderResponse>("order/create_bulk", bulkRequest, cancellationToken);

            if (response?.IsSuccess == true && response.data != null)
            {
                // Bulk API returns a single response for all packages — map back by checking data
                // (Azerpost bulk currently returns a single status; log and store the first returned ID)
                _logger.LogInformation("Azerpost bulk create succeeded. Response ID: {Id}", response.data.order_Id);
            }
            else
            {
                _logger.LogWarning("Azerpost bulk create returned non-success for {Count} orders", orderList.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azerpost CreateBulkOrders");
        }

        return result;
    }

    // ── Update vendor payment status ──────────────────────────────────────────

    public async Task<bool> UpdateVendorPaymentStatusAsync(string packageId, bool isPaid, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AzerpostVpStatusRequest
            {
                vendor_id             = _settings.VendorId,
                package_id            = packageId,
                vendor_payment_status = isPaid ? 1 : 0,
                pym_state             = isPaid ? 1 : 0
            };

            var response = await PostAsync<AzerpostVpStatusResponse>("order/vp-status", request, cancellationToken);

            if (response?.IsSuccess == true)
            {
                _logger.LogInformation("Azerpost payment status updated for package {PackageId}, isPaid={IsPaid}", packageId, isPaid);
                return true;
            }

            _logger.LogWarning("Azerpost vp-status failed (code={Code}) for package {PackageId}", response?.code, packageId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azerpost UpdateVendorPaymentStatus for package {PackageId}", packageId);
            return false;
        }
    }

    // ── Get package tracking status ───────────────────────────────────────────

    public async Task<AzerpostPackageStatus?> GetPackageStatusAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AzerpostViewRequest
            {
                vendor_id  = _settings.VendorId,
                package_id = packageId
            };

            var response = await PostAsync<AzerpostViewResponse>("order/view", request, cancellationToken);

            if (response?.IsSuccess == true)
            {
                return response.data;
            }

            _logger.LogWarning("Azerpost view failed (code={Code}) for package {PackageId}", response?.code, packageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azerpost GetPackageStatus for package {PackageId}", packageId);
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AzerpostCreateOrderRequest BuildCreateRequest(Order order)
    {
        // Split CustomerName into first/last parts (best-effort)
        var nameParts = (order.CustomerName ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : order.CustomerName ?? string.Empty;
        var lastName  = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        return new AzerpostCreateOrderRequest
        {
            vendor_id             = _settings.VendorId,
            package_id            = order.OrderNumber,
            delivery_post_code    = order.DeliveryPostCode ?? string.Empty,
            package_weight        = (double)(order.PackageWeight > 0 ? order.PackageWeight : 0.1m),
            customer_address      = order.ShippingAddress ?? string.Empty,
            first_name            = firstName,
            last_name             = lastName,
            email                 = order.CustomerEmail,
            phone_no              = string.IsNullOrWhiteSpace(order.CustomerPhone) ? null : order.CustomerPhone,
            user_passport         = order.UserPassport,
            delivery_type         = ((int)order.DeliveryType).ToString(),
            vendor_payment        = 0,   // pre-paid via Epoint; no COD
            fragile               = order.Fragile ? 1 : 0,
            vendor_payment_status = 0,
            system_id             = _settings.SystemId
        };
    }

    private async Task<T?> PostAsync<T>(string endpoint, object body, CancellationToken cancellationToken)
    {
        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var raw      = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("Azerpost [{Endpoint}] Status={Status} Body={Body}", endpoint, response.StatusCode, raw);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azerpost [{Endpoint}] HTTP {Status}: {Body}", endpoint, (int)response.StatusCode, raw);
            throw new Exception($"HTTP Error {(int)response.StatusCode}: {raw}");
        }

        return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
    }
}
