using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Services.Tests;

[TestFixture]
public class CurrencyServiceTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private CurrencyService _currencyService;

    [SetUp]
    public void SetUp()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var loggerMock = new Mock<ILogger<CurrencyService>>();
        _currencyService = new CurrencyService(_httpClientFactoryMock.Object, loggerMock.Object);
    }

    [Test]
    public async Task GetLatestRatesAsync_ShouldReturnExpectedRates()
    {
        var baseCurrency = "EUR";
        var expectedRates = new ExchangeRatesResponse
        {
            Base = "EUR",
            Rates = new Dictionary<string, decimal> { { "USD", 1.1m } }
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(expectedRates));
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _currencyService.GetLatestRatesAsync(baseCurrency);
        result.Should().BeEquivalentTo(expectedRates);
    }

    [Test]
    public async Task ConvertCurrencyAsync_ShouldReturnExpectedConversion()
    {
        var from = "EUR";
        var to = "USD";
        var amount = 100m;
        
        var expectedConversion = new ConversionResult
        {
            Amount = 110.0m,
            Base = from,
            Rates = new Dictionary<string, decimal> { { to, 1.1m } }
        };

        var expectedRates = new ExchangeRatesResponse
        {
            Amount = 110.0m,
            Base = from,
            Rates = new Dictionary<string, decimal> { { to, 1.1m } }
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(expectedRates));
        _httpClientFactoryMock.Setup(factory => factory.CreateClient("frankfurter")).Returns(httpClient);

        var result = await _currencyService.ConvertCurrencyAsync(from, to, amount);
        result.Should().BeEquivalentTo(expectedConversion);
    }

    [Test]
    public async Task ConvertCurrencyAsync_ShouldThrowException_ForRestrictedCurrencies()
    {
        const string from = "TRY";
        const string to = "USD";
        const decimal amount = 100m;

        Func<Task> action = async () => await _currencyService.ConvertCurrencyAsync(from, to, amount);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Currency conversion is not allowed for TRY, PLN, THB, and MXN.");
    }

    [Test]
    public async Task GetHistoricalRatesAsync_ShouldReturnExpectedHistoricalRates()
    {
        const string baseCurrency = "EUR";
        DateTime startDate = new DateTime(2020, 1, 1);
        DateTime endDate = new DateTime(2020, 1, 31);
        
        var expectedHistoricalRates = new HistoricalRatesResponse
        {
            Base = "EUR",
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2020-01-01", new Dictionary<string, decimal> { { "USD", 1.1m } } },
                { "2020-01-02", new Dictionary<string, decimal> { { "USD", 1.2m } } }
            },
            TotalCount = 2
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(expectedHistoricalRates));
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, 1, 10);
        result.Should().BeEquivalentTo(expectedHistoricalRates);
    }

    [Test]
    public async Task GetLatestRatesAsync_ShouldThrowException_OnApiError()
    {
        const string baseCurrency = "EUR";
        
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "");
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        Func<Task> action = async () => await _currencyService.GetLatestRatesAsync(baseCurrency);
        await action.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Failed to fetch data from the Frankfurter API.");
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null && req.RequestUri.ToString().StartsWith("https://api.frankfurter.app")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app/")
        };
    }
}