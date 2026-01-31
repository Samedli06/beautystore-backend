namespace SmartTeam.Application.DTOs;

public class EpointSettings
{
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Currency { get; set; } = "AZN";
    public string Language { get; set; } = "az";
}

public class InitiatePaymentDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class EpointPaymentRequest
{
    public string public_key { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string currency { get; set; } = "AZN";
    public string language { get; set; } = "az";
    public string? description { get; set; }
    public string? order_id { get; set; }
    public string? success_redirect_url { get; set; }
    public string? error_redirect_url { get; set; }
}

public class EpointPaymentResponse
{
    public string status { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("transaction")]
    public string? transaction_id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("redirect_url")]
    public string? payment_url { get; set; }
    
    public string? message { get; set; }
}

public class EpointCallbackDto
{
    public string transaction_id { get; set; } = string.Empty;
    public string order_id { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string currency { get; set; } = "AZN";
    public string? signature { get; set; }
    public string? message { get; set; }
}
