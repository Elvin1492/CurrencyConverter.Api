using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace Services;

public interface ICurrencyService
{
    Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency);
    Task<ConversionResult> ConvertCurrencyAsync(string from, string to, decimal amount);

    Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate,
        int page, int pageSize);
}

public class CurrencyService : ICurrencyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CurrencyService> _logger;
    private static readonly HashSet<string> RestrictedCurrencies = new HashSet<string> { "TRY", "PLN", "THB", "MXN" };
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public CurrencyService(IHttpClientFactory httpClientFactory, ILogger<CurrencyService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<ExchangeRatesResponse> GetLatestRatesAsync(string baseCurrency)
    {
        var client = _httpClientFactory.CreateClient("frankfurter");
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync($"latest?base={baseCurrency}"));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch latest rates: {response.ReasonPhrase}");
            throw new HttpRequestException("Failed to fetch data from the Frankfurter API.");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ExchangeRatesResponse>(content);
    }

    public async Task<ConversionResult> ConvertCurrencyAsync(string from, string to, decimal amount)
    {
        if (RestrictedCurrencies.Contains(from) || RestrictedCurrencies.Contains(to))
        {
            throw new ArgumentException("Currency conversion is not allowed for TRY, PLN, THB, and MXN.");
        }

        var client = _httpClientFactory.CreateClient("frankfurter");
        var response =
            await _retryPolicy.ExecuteAsync(() => client.GetAsync($"latest?from={from}&to={to}&amount={amount}"));

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ArgumentException($"Conversion rate from {from} to {to} not found.");
            }

            _logger.LogError($"Failed to fetch conversion rate: {response.ReasonPhrase}");
            throw new HttpRequestException("Failed to fetch data from the Frankfurter API.");
        }

        var content = await response.Content.ReadAsStringAsync();
        var rate = JsonConvert.DeserializeObject<ExchangeRatesResponse>(content);


        if (rate != null)
        {
            return new ConversionResult
            {
                Amount = rate.Amount,
                Base = from,
                Rates = rate.Rates
            };
        }

        throw new ArgumentException($"Conversion rate from {from} to {to} not found.");
    }

    public async Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate,
        DateTime endDate, int page, int pageSize)
    {
        var client = _httpClientFactory.CreateClient("frankfurter");
        var response = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?base={baseCurrency}&pageSize=10&page=1"));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch historical rates: {response.ReasonPhrase}");
            throw new HttpRequestException("Failed to fetch data from the Frankfurter API.");
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<HistoricalRatesResponse>(content);
        result.TotalCount = result.Rates.Count;
        result.Rates = result.Rates.Skip((page - 1) * pageSize).Take(pageSize).ToDictionary();
        
        return result;
    }
}