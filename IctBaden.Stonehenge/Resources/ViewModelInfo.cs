using System.Collections.Generic;

namespace IctBaden.Stonehenge.Resources;

public class ViewModelInfo
{
    // CustomComponent
    public string ElementName { get; set; } = string.Empty;
    public List<string> Bindings { get; set; } = new();

    // ViewModel
    public string Route { get; set; }
    public string VmName { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortIndex { get; set; }
    public bool Visible { get; set; }

    public ViewModelInfo(string route, string name)
    {
        Route = route;
        VmName = name;
        SortIndex = 1;    // ensure visible
    }


}