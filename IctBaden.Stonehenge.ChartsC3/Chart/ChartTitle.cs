using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension;

public class ChartTitle
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    public ChartTitle(string text)
    {
        Text = text;
    }


}