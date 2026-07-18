using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShopPlatform.Models;
using ShopPlatform.Services;

namespace ShopPlatform.Payments;

/// <summary>
/// \if KO
/// <para>토스페이먼츠 V2 구현. 테넌트별 키 사용.</para>
/// \endif
/// \if EN
/// <para>Encapsulates toss payment gateway functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TossPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// \if KO
    /// <para>Api Base 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the api base value.</para>
    /// \endif
    /// </summary>
    private const string ApiBase = "https://api.tosspayments.com/v1";

    /// <summary>
    /// \if KO
    /// <para>http 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the http value.</para>
    /// \endif
    /// </summary>
    private readonly HttpClient _http;
    /// <summary>
    /// \if KO
    /// <para>config 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the config value.</para>
    /// \endif
    /// </summary>
    private readonly ShopPaymentConfig _config;
    /// <summary>
    /// \if KO
    /// <para>protector 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the protector value.</para>
    /// \endif
    /// </summary>
    private readonly PaymentKeyProtector _protector;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="TossPaymentGateway"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TossPaymentGateway"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="http">
    /// \if KO
    /// <para>http에 사용할 <c>HttpClient</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HttpClient</c> value used for http.</para>
    /// \endif
    /// </param>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>ShopPaymentConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopPaymentConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="protector">
    /// \if KO
    /// <para>protector에 사용할 <c>PaymentKeyProtector</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PaymentKeyProtector</c> value used for protector.</para>
    /// \endif
    /// </param>
    public TossPaymentGateway(HttpClient http, ShopPaymentConfig config, PaymentKeyProtector protector)
    {
        _http = http;
        _config = config;
        _protector = protector;
    }

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
    {
        return Task.FromResult(new PaymentStartResult
        {
            OrderId = orderNo,
            OrderNo = orderNo,
            Amount = amount,
            ClientKey = _config.TossClientKey,
            SuccessUrl = _config.SuccessReturnUrl,
            FailUrl = _config.FailReturnUrl
        });
    }

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
    public async Task<PaymentConfirmResult> ConfirmAsync(string paymentKey, string orderId, int amount)
    {
        var secretKey = _protector.Unprotect(_config.TossSecretKeyEncrypted);
        if (string.IsNullOrEmpty(secretKey))
            return new PaymentConfirmResult { Succeeded = false, Error = "결제 키가 설정되지 않았습니다." };

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/payments/confirm");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { paymentKey, orderId, amount }),
            Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return new PaymentConfirmResult { Succeeded = false, Error = body };

        using var doc = JsonDocument.Parse(body);
        var txId = doc.RootElement.TryGetProperty("paymentKey", out var pk) ? pk.GetString() : null;
        return new PaymentConfirmResult { Succeeded = true, TransactionId = txId };
    }
}
