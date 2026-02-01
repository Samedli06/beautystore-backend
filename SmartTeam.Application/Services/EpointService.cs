using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public class EpointService : IEpointService
{
    private readonly HttpClient _httpClient;
    private readonly EpointSettings _settings;

    public EpointService(HttpClient httpClient, IOptions<EpointSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public string GenerateSignature(string jsonData)
    {
        // Epoint signature generation: SHA1(privateKey + base64(jsonData) + privateKey)
        var jsonBytes = Encoding.UTF8.GetBytes(jsonData);
        var base64Data = Convert.ToBase64String(jsonBytes);
        var signatureString = $"{_settings.PrivateKey}{base64Data}{_settings.PrivateKey}";
        
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<EpointPaymentResponse> CreatePaymentRequestAsync(Guid orderId, decimal amount, string? description, CancellationToken cancellationToken = default)
    {
        try
        {
            var publicKey = _settings.PublicKey?.Trim();
            var privateKey = _settings.PrivateKey?.Trim();

            // Use anonymous object to control formatting exactly as needed (amount as string)
            var requestData = new
            {
                public_key = publicKey,
                amount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                currency = _settings.Currency,
                language = _settings.Language,
                description = description ?? $"Order #{orderId}",
                order_id = orderId.ToString(),
                success_redirect_url = $"https://gunaybeauty.com/api/v1/Payment/success?order_id={orderId}",
                error_redirect_url = $"https://gunaybeauty.com/api/v1/Payment/error?order_id={orderId}"
            };

            // Verify if amount needs to be a string or number. Documentation shows string "30.75" in JSON example.
            // Documentation C# example uses simple default serialization.
            var jsonRequest = JsonSerializer.Serialize(requestData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonRequest);
            var base64Data = Convert.ToBase64String(jsonBytes);
            
            // Generate signature: SHA1(privateKey + base64Data + privateKey)
            var signatureString = $"{privateKey}{base64Data}{privateKey}";
            string signature;
            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                signature = Convert.ToBase64String(hashBytes);
            }
            
            // Console.WriteLine($"DEBUG: Data: {base64Data}");
            // Console.WriteLine($"DEBUG: Signature: {signature}");

            // Prepare FormUrlEncoded content
            var formData = new Dictionary<string, string>
            {
                { "data", base64Data },
                { "signature", signature }
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/api/1/request", content, cancellationToken);
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return new EpointPaymentResponse
                {
                    status = "error",
                    message = $"Epoint API error: {response.StatusCode} - {responseContent}"
                };
            }

            var epointResponse = JsonSerializer.Deserialize<EpointPaymentResponse>(responseContent);
            return epointResponse ?? new EpointPaymentResponse
            {
                status = "error",
                message = "Failed to parse Epoint response"
            };
        }
        catch (Exception ex)
        {
            return new EpointPaymentResponse
            {
                status = "error",
                message = $"Exception: {ex.Message}"
            };
        }
    }

    public bool VerifyCallbackSignature(EpointCallbackDto callback)
    {
        try
        {
            // Create JSON without signature field for verification
            var callbackForVerification = new
            {
                transaction_id = callback.transaction_id,
                order_id = callback.order_id,
                status = callback.status,
                amount = callback.amount,
                currency = callback.currency
            };

            var jsonData = JsonSerializer.Serialize(callbackForVerification);
            var expectedSignature = GenerateSignature(jsonData);

            return callback.signature == expectedSignature;
        }
        catch
        {
            return false;
        }
    }
}
