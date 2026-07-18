namespace ShopPlatform.Payments;

/// <summary>
/// \if KO
/// <para>Payment Start Result 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates payment start result functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PaymentStartResult
{
    /// <summary>
    /// \if KO
    /// <para>Order Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the order id value.</para>
    /// \endif
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Order No 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the order no value.</para>
    /// \endif
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Amount 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the amount value.</para>
    /// \endif
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Client Key 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the client key value.</para>
    /// \endif
    /// </summary>
    public string ClientKey { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Success Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the success url value.</para>
    /// \endif
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Fail Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the fail url value.</para>
    /// \endif
    /// </summary>
    public string FailUrl { get; set; } = string.Empty;
}

/// <summary>
/// \if KO
/// <para>Payment Confirm Result 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates payment confirm result functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PaymentConfirmResult
{
    /// <summary>
    /// \if KO
    /// <para>Succeeded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the succeeded value.</para>
    /// \endif
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Transaction Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the transaction id value.</para>
    /// \endif
    /// </summary>
    public string? TransactionId { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Error 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the error value.</para>
    /// \endif
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// \if KO
/// <para>I Payment Gateway 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i payment gateway functionality and related state.</para>
/// \endif
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// \if KO
    /// <para>결제 시작. PG에 주문번호 등록하고 프론트에 전달할 정보 반환.</para>
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
    Task<PaymentStartResult> StartAsync(string orderNo, decimal amount, string customerName);

    /// <summary>
    /// \if KO
    /// <para>결제 승인 (리디렉션 콜백에서 호출).</para>
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
    Task<PaymentConfirmResult> ConfirmAsync(string paymentKey, string orderId, int amount);
}
