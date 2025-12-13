namespace Application.DTOs.Dashboard.Shared;

/// <summary>
/// Currency exchange rates response
/// </summary>
public record CurrencyRatesDto(
    /// <summary>
    /// Base currency code (e.g., "USD")
    /// </summary>
    string BaseCurrency,
    
    /// <summary>
    /// List of all supported currencies with their exchange rates
    /// </summary>
    List<CurrencyRateItemDto> Rates,
    
    /// <summary>
    /// When the rates were last updated
    /// </summary>
    DateTime LastUpdated,
    
    /// <summary>
    /// Data source (e.g., "Frankfurter API (ECB)")
    /// </summary>
    string Source
);

/// <summary>
/// Individual currency rate item
/// </summary>
public record CurrencyRateItemDto(
    /// <summary>
    /// Currency code (e.g., "EGP")
    /// </summary>
    string Code,
    
    /// <summary>
    /// Currency name (e.g., "Egyptian Pound")
    /// </summary>
    string Name,
    
    /// <summary>
    /// Currency symbol (e.g., "£E")
    /// </summary>
    string Symbol,
    
    /// <summary>
    /// Exchange rate relative to base currency
    /// </summary>
    decimal Rate
);
