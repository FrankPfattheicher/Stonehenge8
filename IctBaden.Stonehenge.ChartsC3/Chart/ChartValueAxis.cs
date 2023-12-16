using System.Text.Json.Serialization;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartValueAxis : ChartAxis
{
    [JsonPropertyName("label")]
    public string Label { get; set; }
    
    [JsonPropertyName("show")]
    public bool Show { get; set; }
    
    [JsonPropertyName("min")]
    public double Min { get; set; }
    [JsonPropertyName("center")]
    public double? Center { get; set; }
    
    [JsonPropertyName("max")]
    public double Max { get; set; }

    public ChartValueAxis(ValueAxisId id)
        : base(id.ToString())
    {
        Label = "";
        Show = true;
        Min = 0;
        Max = 100;
    }
}