using System.Drawing;
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyLink
{
    [JsonPropertyName("source")]
    public string Source { get; init; }

    [JsonPropertyName("target")]
    public string Target { get; init; }
    
    [JsonPropertyName("value")]
    public long Value { get; set; }

    [JsonIgnore]    
    public Color Color { get; set; } = Color.Silver;
    public string ColorRgb => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

    public string Tooltip { get; set; } = "";

    public SankeyLink(string source, string target)
    {
        Source = source;
        Target = target;
    }

    public override string ToString()
    {
        return $"{Source} -> {Target} ({Value})";
    }
}