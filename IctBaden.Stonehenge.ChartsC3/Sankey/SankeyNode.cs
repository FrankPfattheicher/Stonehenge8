using System.Drawing;
using System.Text.Json.Serialization;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyNode
{
    [JsonPropertyName("id")] 
    public string Id { get; init; }

    private string _name = string.Empty;
    
    [JsonPropertyName("name")] 
    public string Name
    {
        get => string.IsNullOrEmpty(_name) ? Id : _name;
        set => _name = value;
    }

    [JsonIgnore]    
    public Color Color { get; set; } = Color.LightSkyBlue;
    public string ColorRgb => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

    public string? NodeStroke { get; set; }

    public SankeyNode(string id)
    {
        Id = id;
    }
    
    public override string ToString()
    {
        return Name;
    }
}