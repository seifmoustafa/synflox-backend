using Domain.Enums;

namespace Application.Services_Interfaces;

/// <summary>
/// Service for real-time currency exchange rate conversion
/// </summary>
public interface ICurrencyExchangeService
{
    /// <summary>
    /// Get the current exchange rate between two currencies
    /// </summary>
    /// <param name="from">Source currency</param>
    /// <param name="to">Target currency</param>
    /// <returns>Exchange rate (multiply by this to convert)</returns>
    Task<decimal> GetExchangeRateAsync(Currency from, Currency to);

    /// <summary>
    /// Convert an amount from one currency to another
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="from">Source currency</param>
    /// <param name="to">Target currency</param>
    /// <returns>Converted amount</returns>
    Task<decimal> ConvertAsync(decimal amount, Currency from, Currency to);

    /// <summary>
    /// Get all exchange rates for a base currency
    /// </summary>
    /// <param name="baseCurrency">Base currency</param>
    /// <returns>Dictionary of currency to rate</returns>
    Task<Dictionary<Currency, decimal>> GetAllRatesAsync(Currency baseCurrency);

    /// <summary>
    /// Get the timestamp of the last rate update
    /// </summary>
    Task<DateTime?> GetLastUpdateTimeAsync();

    /// <summary>
    /// Force refresh exchange rates from external API
    /// </summary>
    Task RefreshRatesAsync();
}
