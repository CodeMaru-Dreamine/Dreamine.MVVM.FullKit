using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShopPlatform.Models;
using ShopPlatform.Services;

namespace ShopPlatform.Payments;

/// <summary>토스페이먼츠 V2 구현. 테넌트별 키 사용.</summary>
public sealed class TossPaymentGateway : IPaymentGateway
{
    private const string ApiBase = "https://api.tosspayments.com/v1";

    private readonly HttpClient _http;
    private readonly ShopPaymentConfig _config;
    private readonly PaymentKeyProtector _protector;

    public TossPaymentGateway(HttpClient http, ShopPaymentConfig config, PaymentKeyProtector protector)
    {
        _http = http;
        _config = config;
        _protector = protector;
    }

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
