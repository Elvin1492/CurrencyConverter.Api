namespace Services;

public class ConversionResult
{
    public decimal Amount { get; set; }
    public string Base { get; set; }
    public Dictionary<string, decimal> Rates { get; set; }
}