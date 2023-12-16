using System.Collections.Generic;
using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
public class TreeVm : ActiveViewModel
{
    public readonly List<Continent> Continents;
    public readonly int TotalArea;
    public readonly int TotalCountries;

    public TreeView WorldTree { get; } = new();
    public string SelectedContinent { get; private set; } = string.Empty;
    public string CheckedContinents { get; private set; } = string.Empty;
    public Gauge Area { get; private init; }
    public Gauge Countries { get; private init; }

    // ReSharper disable once UnusedMember.Global
    public TreeVm(AppSession session) : base (session)
    {
        Continents = new List<Continent>
        {
            new() { Icon = "fa fa-flag", Name = "Asia", Area = 44579, Countries = 50, IsChild = true },
            new() { Icon = "fa fa-flag", Name = "Africa <i class=\"fa-solid fa-square-full\" style=\"color: blue;\"></i>", Area = 30370, Countries = 54 },
            new() { Icon = "fa fa-flag", Name = "North America", Area = 24709, Countries = 23, IsChild = true },
            new() { Icon = "fa fa-flag", Name = "South America", Area = 17840, Countries = 12, IsChild = true },
            new() { Icon = "fa fa-flag", Name = "Antarctica", Area = 14000, Countries = 0 },
            new() { Icon = "fa fa-flag", Name = "Europe", Area = 10180, Countries = 51, IsChild = true },
            new() { Icon = "fa fa-flag", Name = "Australia", Area = 8600, Countries = 14 }
        };

        TotalArea = Continents.Select(c => c.Area).Sum();
        TotalCountries = Continents.Select(c => c.Countries).Sum();

        Continents.Add(new Continent
        {
            Name = "America",
            Icon = "fa fa-folder",
            Area = Continents.Where(c => c.Name.Contains("America")).Select(c => c.Area).Sum(),
            Countries = Continents.Where(c => c.Name.Contains("America")).Select(c => c.Countries).Sum(),
            Children = Continents.Where(c => c.Name.Contains("America")).ToList()
        });
        Continents.Add(new Continent
        {
            Name = "Eurasia",
            Icon = "fa fa-folder",
            Area = Continents.Where(c => c.Name.Contains("Eur") || c.Name.Contains("sia")).Select(c => c.Area).Sum(),
            Countries = Continents.Where(c => c.Name.Contains("Eur") || c.Name.Contains("sia")).Select(c => c.Countries).Sum(),
            Children = Continents.Where(c => c.Name.Contains("Eur") || c.Name.Contains("sia")).ToList()
        });

        var world = new Continent
        {
            Name = "World",
            Icon = "fa fa-earth-americas",
            Area = Continents.Select(c => c.Area).Sum(),
            Countries = Continents.Select(c => c.Countries).Sum(),
            Children = Continents.ToList()
        };
        var worldNode = new TreeNode(null, world, new SessionStateProvider(session)); 
            
        WorldTree.SetRootNodes(new []{ worldNode }, true);
        WorldTree.SelectionChanged += WorldTreeOnSelectionChanged;

        foreach (var continent in Continents.Where(c => !c.IsChild))
        {
            worldNode.Children.Add(CreateTreeNode(worldNode, continent));
        }

        Area = new Gauge
        {
            Label = "Area",
            Value = 0,
            Max = TotalArea,
            Units = "[1000 km²]"
        };
        Countries = new Gauge
        {
            Label = "Countries",
            Value = 0,
            Max = TotalCountries,
            Units = ""
        };

        TreeSelect(worldNode.Id);
        TreeChange(string.Empty);
    }

    private TreeNode CreateTreeNode(TreeNode parent, Continent continent)
    {
        var node = new TreeNode(parent, continent)
        {
            Name = continent.Name,
            Checkbox = true 
        };
        node.SetChecked(continent.Name == "Eurasia");
        node.Children = continent.Children
            .Select(c => new TreeNode(node, c) { Name = c.Name })
            .ToList();
        return node;
    }

    [ActionMethod]
    // ReSharper disable once UnusedMember.Global
    public void TreeToggle(string nodeId) => WorldTree.TreeToggle(nodeId);
    [ActionMethod]
    // ReSharper disable once UnusedMember.Global
    public void TreeSelect(string nodeId) => WorldTree.TreeSelect(nodeId);
    [ActionMethod]
    public void TreeChange(string nodeId)
    {
        WorldTree.TreeChange(nodeId);
        CheckedContinents = string.Join(", ", 
            WorldTree.AllNodes().Where(n => n.IsChecked).Select(n => n.Name));
    }

    private void WorldTreeOnSelectionChanged(TreeNode node)
    {
        SelectedContinent = node.Name;

        if (node.Item is Continent continent)
        {
            Area.Value = continent.Area;
            Countries.Value = continent.Countries;
        }
        else
        {
            Area.Value = TotalArea;
            Countries.Value = TotalCountries;
        }

        NotifyPropertiesChanged(new []
        {
            nameof(SelectedContinent),
            nameof(Area),
            nameof(Countries)
        });
    }

}