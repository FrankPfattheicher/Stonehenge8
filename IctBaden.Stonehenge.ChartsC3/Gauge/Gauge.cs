// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace IctBaden.Stonehenge.Extension;

public class Gauge
{
    public string? Label { get; set; }

    public bool MinMaxLabels { get; set; } = true;
    
    public int Min { get; set; }
    public int Max { get; set; }
    public int Value { get; set; }
    
    public string[] ColorPatterns { get; set; } = { "blue" };
    public int[] ColorThresholds { get; set; } = { 0 };
    
    /// <summary>
    /// For adjusting arc thickness
    /// </summary>
    public int Thickness { get; set; } = 32;
    public string? Units { get; set; }

}