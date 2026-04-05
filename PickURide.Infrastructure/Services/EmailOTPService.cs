using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System.Text;
using System.Text.Json;

public class EmailOTPService : IEmailOTPService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public EmailOTPService(IMemoryCache cache, IConfiguration config)
    {
        _cache = cache;
        _config = config;
    }

    public async Task SendOtpAsync(string email, string otp)
    {
        var apiKey = _config["Email:BrevoApiKey"];
        var senderEmail = _config["Email:From"];

        var payload = new
        {
            sender = new { name = "PickURide", email = senderEmail },
            to = new[] { new { email = email } },
            subject = "Your OTP Code",
            htmlContent = $"<p>Your OTP is <strong>{otp}</strong>. It will expire in 5 minutes.</p>"
        };

        var json = JsonSerializer.Serialize(payload);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send email: {error}");
        }

        var otpEntry = new OtpEntry
        {
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Used = false
        };

        _cache.Set(email, otpEntry, TimeSpan.FromMinutes(5));
    }


    public bool VerifyOtp(string email, string otp)
    {
        if (!_cache.TryGetValue(email, out OtpEntry? entry))
            return false;

        if (entry == null || entry.Used || entry.ExpiresAt < DateTime.UtcNow)
            return false;

        if (entry.Otp != otp)
            return false;

        entry.Used = true;
        _cache.Set(email, entry, entry.ExpiresAt - DateTime.UtcNow);

        return true;
    }
}
