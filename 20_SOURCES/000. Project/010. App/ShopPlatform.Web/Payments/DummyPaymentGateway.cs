namespace ShopPlatform.Payments;

/// <summary>
/// \if KO
/// <para>결제 키 미설정 또는 개발/데모 모드용. 항상 성공.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dummy payment gateway functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DummyPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// \if KO
    /// <para>Start Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="orderNo">
    /// \if KO
    /// <para>order No에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for order no.</para>
    /// \endif
    /// </param>
    /// <param name="amount">
    /// \if KO
    /// <para>amount에 사용할 <c>decimal</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>decimal</c> value used for amount.</para>
    /// \endif
    /// </param>
    /// <param name="customerName">
    /// \if KO
    /// <para>customer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for customer name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Async 작업에서 생성한 <c>Task&lt;PaymentStartResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;PaymentStartResult&gt;</c> result produced by the start async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Confirm Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the confirm async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="paymentKey">
    /// \if KO
    /// <para>payment Key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for payment key.</para>
    /// \endif
    /// </param>
    /// <param name="orderId">
    /// \if KO
    /// <para>order Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for order id.</para>
    /// \endif
    /// </param>
    /// <param name="amount">
    /// \if KO
    /// <para>amount에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for amount.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Confirm Async 작업에서 생성한 <c>Task&lt;PaymentConfirmResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;PaymentConfirmResult&gt;</c> result produced by the confirm async operation.</para>
    /// \endif
    /// </returns>
    public Task<PaymentConfirmResult> ConfirmAsync(string paymentKey, string orderId, int amount)
        => Task.FromResult(new PaymentConfirmResult
        {
            Succeeded = true,
            TransactionId = $"DEMO-{orderId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        });
}
