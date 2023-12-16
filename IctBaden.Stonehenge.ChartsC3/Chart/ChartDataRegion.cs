namespace IctBaden.Stonehenge.Extension;

public class ChartDataRegion
{
    public string Series { get; init; }
    public object? StartValue { get; init; } = null;
    public object? EndValue { get; init; } = null;
    public string? Style { get; init; } = null;

    public ChartDataRegion(string series)
    {
        Series = series;
    }
}