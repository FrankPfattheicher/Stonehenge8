using System.Collections.Generic;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class Continent
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Countries { get; set; }
    /// <summary>
    /// 1000 square km
    /// </summary>
    public int Area { get; set; }
    public bool IsChild { get; set; }

    public List<Continent> Children = new();
}