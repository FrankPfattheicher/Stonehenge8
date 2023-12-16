using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension;

public abstract class ChartAxis
{
    [JsonIgnore]
    public string Id { get; init; }

    protected ChartAxis(string id)
    {
        Id = id;
    }

}