namespace ShopPlatform.Payments;

public sealed class PaymentStartResult
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ClientKey { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailUrl { get; set; } = string.Empty;
}

public sealed class PaymentConfirmResult
{
    public bool Succeeded { get; set; }
    public string? TransactionId { get; set; }
    public string? Error { get; set; }
}

public interface IPaymentGateway
{
    /// <summary>결제 시작. PG에 주문번호 등록하고 프론트에 전달할 정보 반환.</summary>
    Task<PaymentStartResult> StartAsync(string orderNo, decimal amount, string customerName);

    /// <summary>결제 승인 (리디렉션 콜백에서 호출).</summary>
    Task<PaymentConfirmResult> ConfirmAsync(string paymentKey, string orderId, int amount);
}
