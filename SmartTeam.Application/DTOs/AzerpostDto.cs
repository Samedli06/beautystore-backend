namespace SmartTeam.Application.DTOs;

// ── Settings ──────────────────────────────────────────────────────────────────

public class AzerpostSettings
{
    public string BaseUrl { get; set; } = "https://api-dev.azerpost.az";
    public string ApiKey { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    /// <summary>System ID sent with every request (2 = dashboard/vendor system)</summary>
    public int SystemId { get; set; } = 2;
}

// ── Create / Bulk Create ──────────────────────────────────────────────────────

public class AzerpostCreateOrderRequest
{
    public string vendor_id { get; set; } = string.Empty;
    public string package_id { get; set; } = string.Empty;
    public string delivery_post_code { get; set; } = string.Empty;
    public double package_weight { get; set; }
    public string customer_address { get; set; } = string.Empty;
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string? phone_no { get; set; }
    public string? user_passport { get; set; }
    /// <summary>0 = standard delivery</summary>
    public string delivery_type { get; set; } = "0";
    /// <summary>Amount the vendor will collect from the customer (COD). 0 if pre-paid.</summary>
    public decimal vendor_payment { get; set; }
    public int fragile { get; set; }
    public int vendor_payment_status { get; set; }
    public int system_id { get; set; } = 2;
}

public class AzerpostBulkCreateOrderRequest
{
    public string vendor_id { get; set; } = string.Empty;
    public List<AzerpostBulkPackage> package_list { get; set; } = new();
}

public class AzerpostBulkPackage
{
    public string package_id { get; set; } = string.Empty;
    public string delivery_post_code { get; set; } = string.Empty;
    public double package_weight { get; set; }
    public string customer_address { get; set; } = string.Empty;
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string? phone_no { get; set; }
    public string? user_passport { get; set; }
    public string delivery_type { get; set; } = "0";
    public decimal vendor_payment { get; set; }
    public int fragile { get; set; }
    public int vendor_payment_status { get; set; }
}

public class AzerpostCreateOrderResponse
{
    public int code { get; set; }
    public AzerpostOrderData? data { get; set; }
    public bool IsSuccess => code == 200 && data?.status == true;
}

public class AzerpostOrderData
{
    /// <summary>Azerpost tracking ID e.g. "SX001-AZ1045-000107-0426"</summary>
    public string order_Id { get; set; } = string.Empty;
    /// <summary>Delivery charge calculated by Azerpost</summary>
    public string? charge { get; set; }
    public bool status { get; set; }
}

// ── Vendor Payment Status ─────────────────────────────────────────────────────

public class AzerpostVpStatusRequest
{
    public string vendor_id { get; set; } = string.Empty;
    public string package_id { get; set; } = string.Empty;
    /// <summary>1 = paid, 0 = unpaid</summary>
    public int vendor_payment_status { get; set; }
    /// <summary>1 = paid, 0 = unpaid (mirrors vendor_payment_status)</summary>
    public int pym_state { get; set; }
}

public class AzerpostVpStatusResponse
{
    public int code { get; set; }
    public bool IsSuccess => code == 200;
}

// ── View / Track ──────────────────────────────────────────────────────────────

public class AzerpostViewRequest
{
    public string vendor_id { get; set; } = string.Empty;
    public string package_id { get; set; } = string.Empty;
}

public class AzerpostViewBulkRequest
{
    public string vendor_id { get; set; } = string.Empty;
    public List<string> package_ids { get; set; } = new();
}

public class AzerpostViewResponse
{
    public int code { get; set; }
    public AzerpostPackageStatus? data { get; set; }
    public bool IsSuccess => code == 200;
}

public class AzerpostPackageStatus
{
    public string? package_id { get; set; }
    public string? order_id { get; set; }
    public string? status { get; set; }
    public string? status_description { get; set; }
    public string? delivery_post_code { get; set; }
    public string? customer_address { get; set; }
}
