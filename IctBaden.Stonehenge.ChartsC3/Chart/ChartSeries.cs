using System.Drawing;

namespace IctBaden.Stonehenge.Extension;

public class ChartSeries
{
    public string Label { get; set; }
    public string Group { get; set; } = "";
    
    public ChartDataType Type { get; set; }
    
    public ValueAxisId ValueAxis { get; set; }

    public Color Color { get; set; }
    
    public object?[] Data { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Type = ChartDataType.Line;
        ValueAxis = ValueAxisId.y;
        Color = Color.Transparent;
        Data = new object[] { 0 };
    }
}