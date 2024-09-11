using Newtonsoft.Json;

namespace Services;

public class ExchangeRatesResponse
{
    [JsonProperty("amount")] public decimal Amount { get; set; }
    [JsonProperty("base")] public string Base { get; set; }
    [JsonProperty("date")] public string Date { get; set; }
    [JsonProperty("rates")] public Dictionary<string, decimal> Rates { get; set; }
}