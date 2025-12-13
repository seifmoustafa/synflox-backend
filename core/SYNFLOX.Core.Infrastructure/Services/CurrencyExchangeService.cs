using System.Text.Json;
using Application.Services_Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Real-time currency exchange service using ExchangeRate-API (free, no API key required)
/// https://open.er-api.com/ - Supports 160+ currencies including EGP, SAR, AED
/// Updates daily with real market rates
/// </summary>
public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyExchangeService> _logger;

    // ExchangeRate-API - FREE, supports EGP, SAR, AED, and 160+ currencies
    // Updates DAILY - we cache until the next update time
    private const string EXCHANGE_RATE_API = "https://open.er-api.com/v6/latest";
    private const string CACHE_KEY_RATES = "currency_exchange_rates";
    private const string CACHE_KEY_TIMESTAMP = "currency_exchange_timestamp";
    private const string CACHE_KEY_NEXT_UPDATE = "currency_exchange_next_update";
    private static readonly TimeSpan DEFAULT_CACHE_DURATION = TimeSpan.FromHours(24); // Default: 24 hours (API updates daily)

    // Fallback rates (USD as base) - Only used if API is completely down
    private static readonly Dictionary<string, decimal> FallbackRates = new()
    {
        { "USD", 1.0m },
        { "EUR", 0.95m },
        { "EGP", 50.85m },
        { "SAR", 3.75m },
        { "AED", 3.6725m },
        { "GBP", 0.79m },
        { "JPY", 149.50m },
        { "CNY", 7.25m }
    };

    public CurrencyExchangeService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CurrencyExchangeService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<decimal> GetExchangeRateAsync(Currency from, Currency to)
    {
        if (from == to || from == Currency.Free || to == Currency.Free)
            return 1.0m;

        var rates = await GetAllRatesAsync(from);
        return rates.TryGetValue(to, out var rate) ? rate : 1.0m;
    }

    public async Task<decimal> ConvertAsync(decimal amount, Currency from, Currency to)
    {
        if (amount == 0 || from == to || from == Currency.Free || to == Currency.Free)
            return amount;

        var rate = await GetExchangeRateAsync(from, to);
        return Math.Round(amount * rate, 2);
    }

    public async Task<Dictionary<Currency, decimal>> GetAllRatesAsync(Currency baseCurrency)
    {
        var cacheKey = $"{CACHE_KEY_RATES}_{baseCurrency}";

        if (_cache.TryGetValue(cacheKey, out Dictionary<Currency, decimal>? cachedRates) && cachedRates != null)
        {
            return cachedRates;
        }

        var (rates, cacheDuration) = await FetchRatesFromApiAsync(baseCurrency);
        
        // Cache until the next API update time (smart caching based on time_next_update_utc)
        _cache.Set(cacheKey, rates, cacheDuration);
        _cache.Set(CACHE_KEY_TIMESTAMP, DateTime.UtcNow, cacheDuration);

        return rates;
    }

    public Task<DateTime?> GetLastUpdateTimeAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY_TIMESTAMP, out DateTime timestamp))
        {
            return Task.FromResult<DateTime?>(timestamp);
        }
        return Task.FromResult<DateTime?>(null);
    }

    public async Task RefreshRatesAsync()
    {
        // Clear cache for all currencies
        foreach (Currency currency in Enum.GetValues<Currency>())
        {
            if (currency != Currency.Free)
            {
                _cache.Remove($"{CACHE_KEY_RATES}_{currency}");
            }
        }

        // Pre-fetch USD rates (most common base)
        await GetAllRatesAsync(Currency.USD);

        _logger.LogInformation("Currency exchange rates refreshed at {Time}", DateTime.UtcNow);
    }

    private async Task<(Dictionary<Currency, decimal> Rates, TimeSpan CacheDuration)> FetchRatesFromApiAsync(Currency baseCurrency)
    {
        var result = new Dictionary<Currency, decimal>();
        var baseCurrencyCode = GetCurrencyCode(baseCurrency);
        var cacheDuration = DEFAULT_CACHE_DURATION;

        try
        {
            // ExchangeRate-API format: https://open.er-api.com/v6/latest/{BASE}
            var url = $"{EXCHANGE_RATE_API}/{baseCurrencyCode}";

            _logger.LogDebug("Fetching exchange rates from ExchangeRate-API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ExchangeRateApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Result == "success" && data.Rates != null)
                {
                    foreach (var (currencyCode, rate) in data.Rates)
                    {
                        var currency = ParseCurrencyCode(currencyCode);
                        if (currency.HasValue)
                        {
                            result[currency.Value] = rate;
                        }
                    }

                    // Add base currency rate as 1.0
                    result[baseCurrency] = 1.0m;

                    // Calculate cache duration based on next update time from API
                    if (data.TimeNextUpdateUnix.HasValue)
                    {
                        var nextUpdate = DateTimeOffset.FromUnixTimeSeconds(data.TimeNextUpdateUnix.Value).UtcDateTime;
                        var timeUntilNextUpdate = nextUpdate - DateTime.UtcNow;
                        if (timeUntilNextUpdate > TimeSpan.Zero)
                        {
                            // Add 5 minutes buffer after next update
                            cacheDuration = timeUntilNextUpdate + TimeSpan.FromMinutes(5);
                            _logger.LogInformation(
                                "📅 Next rate update at {NextUpdate} UTC. Caching for {Hours:F1} hours",
                                nextUpdate, cacheDuration.TotalHours);
                        }
                    }

                    _logger.LogInformation(
                        "✅ Successfully fetched {Count} REAL-TIME exchange rates for base {Base} from ExchangeRate-API (Last update: {UpdateTime})",
                        result.Count, baseCurrencyCode, data.TimeLastUpdateUtc);

                    return (result, cacheDuration);
                }
                else
                {
                    _logger.LogError(
                        "ExchangeRate-API returned error result: {Result}. Real-time rates unavailable!",
                        data?.Result ?? "null");
                    // TESTING: Throw exception to ensure we're using real-time API
                    throw new InvalidOperationException($"ExchangeRate-API returned error result: {data?.Result ?? "null"}");
                }
            }
            else
            {
                _logger.LogError(
                    "ExchangeRate-API returned {StatusCode}. Real-time rates unavailable!",
                    response.StatusCode);
                // TESTING: Throw exception to ensure we're using real-time API
                throw new InvalidOperationException($"ExchangeRate-API returned {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching exchange rates from ExchangeRate-API.");
            throw; // Re-throw to make it clear real-time API failed
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to fetch exchange rates from ExchangeRate-API.");
            throw; // Re-throw to make it clear real-time API failed
        }

        // FALLBACK DISABLED FOR TESTING - Uncomment below to enable fallback
        // return GetFallbackRates(baseCurrency);
        throw new InvalidOperationException("Real-time exchange rate fetch failed and fallback is disabled for testing.");
    }

    private Dictionary<Currency, decimal> GetFallbackRates(Currency baseCurrency)
    {
        var result = new Dictionary<Currency, decimal>();
        var baseCurrencyCode = GetCurrencyCode(baseCurrency);

        if (!FallbackRates.TryGetValue(baseCurrencyCode, out var baseRate))
        {
            baseRate = 1.0m;
        }

        foreach (Currency currency in Enum.GetValues<Currency>())
        {
            if (currency == Currency.Free) continue;

            var currencyCode = GetCurrencyCode(currency);
            if (FallbackRates.TryGetValue(currencyCode, out var rate))
            {
                // Convert rate relative to base currency
                result[currency] = Math.Round(rate / baseRate, 6);
            }
            else
            {
                result[currency] = 1.0m;
            }
        }

        _logger.LogWarning("Using fallback exchange rates for base {Base}", baseCurrencyCode);
        return result;
    }

    private static string GetCurrencyCode(Currency currency) => currency switch
    {
        Currency.Free => "USD", // Treat free as USD for calculation purposes
        Currency.USD => "USD",
        Currency.EUR => "EUR",
        Currency.EGP => "EGP",
        Currency.SAR => "SAR",
        Currency.AED => "AED",
        Currency.GBP => "GBP",
        Currency.JPY => "JPY",
        Currency.CNY => "CNY",
        _ => "USD"
    };

    private static Currency? ParseCurrencyCode(string code) => code.ToUpperInvariant() switch
    {
        "USD" => Currency.USD,
        "EUR" => Currency.EUR,
        "EGP" => Currency.EGP,
        "SAR" => Currency.SAR,
        "AED" => Currency.AED,
        "GBP" => Currency.GBP,
        "JPY" => Currency.JPY,
        "CNY" => Currency.CNY,
        _ => null
    };

    // Response model for ExchangeRate-API
    // Sample: {"result":"success","base_code":"USD","time_last_update_utc":"Sat, 07 Dec 2024...","rates":{"EGP":50.85,"EUR":0.95,...}}
    private class ExchangeRateApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public string? Result { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        public string? Documentation { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("terms_of_use")]
        public string? TermsOfUse { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("time_last_update_unix")]
        public long? TimeLastUpdateUnix { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("time_last_update_utc")]
        public string? TimeLastUpdateUtc { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("time_next_update_unix")]
        public long? TimeNextUpdateUnix { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("time_next_update_utc")]
        public string? TimeNextUpdateUtc { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("base_code")]
        public string? BaseCode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
