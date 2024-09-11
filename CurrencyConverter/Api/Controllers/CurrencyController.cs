using Services;

namespace Api.Controllers;

using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency)
    {
        var result = await _currencyService.GetLatestRatesAsync(baseCurrency);
        return Ok(result);
    }

    [HttpGet("convert")]
    public async Task<IActionResult> ConvertCurrency([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount)
    {
        var result = await _currencyService.ConvertCurrencyAsync(from, to, amount);
        return Ok(result);
    }

    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalRates([FromQuery] string baseCurrency, [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int page, [FromQuery] int pageSize)
    {
        var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize);
        return Ok(result);
    }
}
