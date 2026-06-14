namespace ShopPlatform.Payments;

/// <summary>결제 키 미설정 또는 개발/데모 모드용. 항상 성공.</summary>
public sealed class DummyPaymentGateway : IPaymentGateway
{
    public Task<PaymentStartResult> StartAsync(string orderNo, decimal amount, string customerName)
        => Task.FromResult(new PaymentStartResult
        {
            OrderId = orderNo,
            OrderNo = orderNo,
            Amount = amount,
            ClientKey = "demo",
            SuccessUrl = string.Empty,
            FailUrl = string.Empty
        });

    public Task<PaymentConfirmResult> ConfirmAsync(string paymentKey, string orderId, int amount)
        => Task.FromResult(new PaymentConfirmResult
        {
            Succeeded = true,
            TransactionId = $"DEMO-{orderId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        });
}
