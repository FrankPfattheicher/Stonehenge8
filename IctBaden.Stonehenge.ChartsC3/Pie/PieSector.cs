// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.Drawing;

namespace IctBaden.Stonehenge.Extension.Pie;

public class PieSector
{
    /// <summary>
    /// Text to be used on sector
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Value for this sector
    /// All values in sum represents 360 degrees
    /// </summary>
    public int Value { get; set; }
    /// <summary>
    /// Optional color for this sector
    /// </summary>
    public Color? Color { get; set; }
}