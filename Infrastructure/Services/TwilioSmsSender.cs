using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Sends SMS messages using Twilio's REST API.
/// </summary>
public class TwilioSmsSender : ISmsSender
{
    private readonly HttpClient _client;
    private readonly SmsSettings _settings;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(HttpClient client, IOptions<SmsSettings> options, ILogger<TwilioSmsSender> logger)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendSmsAsync(string number, string message)
    {
        if (string.IsNullOrEmpty(_settings.AccountSid) || string.IsNullOrEmpty(_settings.AuthToken))
        {
            _logger.LogWarning("Twilio credentials are missing");
            return;
        }

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = number,
            ["From"] = _settings.From,
            ["Body"] = message
        });
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        try
        {
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Twilio SMS failed: {Status} {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Twilio");
        }
    }
}

