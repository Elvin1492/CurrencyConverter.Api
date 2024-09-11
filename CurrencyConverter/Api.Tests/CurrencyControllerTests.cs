using Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;

namespace Api.Tests;

[TestFixture]
public class CurrencyControllerTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private CurrencyController _currencyController;

    [SetUp]
    public void Setup()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();

        _currencyController = new CurrencyController(_currencyServiceMock.Object);
    }

    [Test]
    public async Task GetLatestRates_ShouldReturnOkResult_WithExpectedData()
    {
        var baseCurrency = "EUR";
        var exchangeRates = new ExchangeRatesResponse
        {
            Base = "EUR",
            Date = "2024-09-11",
            Rates = new Dictionary<string, decimal> { { "USD", 1.1m }, { "GBP", 0.85m } }
        };

        _currencyServiceMock.Setup(service => service.GetLatestRatesAsync(baseCurrency))
            .ReturnsAsync(exchangeRates);

        var result = await _currencyController.GetLatestRates(baseCurrency);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(exchangeRates);
    }

    [Test]
    public async Task ConvertCurrency_ShouldReturnOkResult_WithConvertedAmount()
    {
        //Arrange
        var from = "USD";
        var to = "EUR";
        var amount = 100m;
        var conversionResult = new ConversionResult
        {
            Amount = 100m,
            Base = "USD",
            Rates = new Dictionary<string, decimal> { { "EUR", 85m } }
        };

        _currencyServiceMock.Setup(service => service.ConvertCurrencyAsync(from, to, amount))
            .ReturnsAsync(conversionResult);

        //Act
        var result = await _currencyController.ConvertCurrency(from, to, amount);

        //Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(conversionResult);
    }

    [Test]
    public async Task ConvertCurrency_ShouldReturnBadRequest_ForRestrictedCurrencies()
    {
        //Arrange
        var from = "TRY";
        var to = "USD";
        var amount = 100m;

        _currencyServiceMock.Setup(service => service.ConvertCurrencyAsync(from, to, amount))
            .ThrowsAsync(new ArgumentException("Currency conversion is not allowed for TRY, PLN, THB, and MXN."));
        
        //Act
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _currencyController.ConvertCurrency(from, to, amount));
        
        //Assert
        Assert.That(ex?.Message, Is.EqualTo("Currency conversion is not allowed for TRY, PLN, THB, and MXN.")); 
    }

    [Test]
    public async Task GetHistoricalRates_ShouldReturnOkResult_WithExpectedData()
    {
        //Arrange
        var baseCurrency = "EUR";
        var startDate = "2020-01-01";
        var endDate = "2020-01-31";
        var page = 1;
        var pageSize = 10;
        var historicalRates = new HistoricalRatesResponse
        {
            Base = "EUR",
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2020-01-01", new Dictionary<string, decimal> { { "USD", 1.1m } } },
                { "2020-01-02", new Dictionary<string, decimal> { { "USD", 1.2m } } }
            }
        };

        _currencyServiceMock.Setup(service => service.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize))
            .ReturnsAsync(historicalRates);

        //Act
        var result = await _currencyController.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

        //Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(historicalRates);
    }

    [Test]
    public void GetLatestRates_ShouldReturnBadRequest_OnError()
    {
        //Arrange
        var baseCurrency = "EUR";

        _currencyServiceMock.Setup(service => service.GetLatestRatesAsync(baseCurrency))
            .ThrowsAsync(new HttpRequestException("Failed to fetch data from the Frankfurter API."));
        
        //Act
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await _currencyController.GetLatestRates(baseCurrency));
        
        //Assert
        Assert.That(ex?.Message, Is.EqualTo("Failed to fetch data from the Frankfurter API."));
    }
}