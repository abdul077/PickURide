using System.Text;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PickURide.Infrastructure.Services;

public interface IPushNotificationService
{
    Task SendToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task SendToMultipleAsync(IReadOnlyList<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly bool _firebaseReady;
    private static readonly object InitLock = new();

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _logger = logger;
        _firebaseReady = TryInitializeFirebase(configuration);
    }

    private bool TryInitializeFirebase(IConfiguration configuration)
    {
        lock (InitLock)
        {
            if (FirebaseApp.DefaultInstance != null)
                return true;

            try
            {
                var json = configuration["Firebase:CredentialJson"];
                var path = configuration["Firebase:CredentialPath"];

                GoogleCredential? credential = null;
                if (!string.IsNullOrWhiteSpace(json))
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    credential = GoogleCredential.FromStream(stream);
                }
                else if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    using var stream = File.OpenRead(path);
                    credential = GoogleCredential.FromStream(stream);
                }

                if (credential == null)
                {
                    _logger.LogWarning(
                        "Firebase credentials not configured (Firebase:CredentialJson or Firebase:CredentialPath). Push notifications are disabled.");
                    return false;
                }

                FirebaseApp.Create(new AppOptions { Credential = credential });
                _logger.LogInformation("Firebase Admin SDK initialized.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK. Push notifications are disabled.");
                return false;
            }
        }
    }

    public async Task SendToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_firebaseReady || string.IsNullOrWhiteSpace(fcmToken))
            return;

        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>(),
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps { Sound = "default" }
                }
            };

            var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Push sent: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push notification failed for token: {TokenPrefix}...", fcmToken[..Math.Min(8, fcmToken.Length)]);
        }
    }

    public async Task SendToMultipleAsync(IReadOnlyList<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_firebaseReady || fcmTokens == null || fcmTokens.Count == 0)
            return;

        var tokens = fcmTokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (tokens.Count == 0)
            return;

        try
        {
            var multicastMessage = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new Notification { Title = title, Body = body },
                Data = data ?? new Dictionary<string, string>()
            };

            var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage);
            _logger.LogInformation("Push multicast: success {Success}, failure {Failure}", result.SuccessCount, result.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push multicast failed");
        }
    }
}
