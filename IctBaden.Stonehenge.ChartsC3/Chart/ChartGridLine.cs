using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartGridLine
{
    [JsonIgnore] public ValueAxisId Axis { get; init; } = ValueAxisId.y;

    [JsonPropertyName("axis")] public string AxisName => Axis.ToString();  
    [JsonPropertyName("position")] public string Position { get; init; } = GridLineTextPosition.End;  
    [JsonPropertyName("value")] public double Value { get; init; }
    [JsonPropertyName("text")] public string Text { get; init; } = "";
    [JsonPropertyName("class")] public string Class { get; init; } = "";
}