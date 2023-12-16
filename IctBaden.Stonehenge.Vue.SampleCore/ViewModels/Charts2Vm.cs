using System;
using System.Drawing;
using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.Extension.Sankey;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

// ReSharper disable once UnusedType.Global
public class Charts2Vm : ActiveViewModel
{
    public int RangeMin { get; } = 0;
    public int RangeMax { get; } = 100;

    public bool FullTimeRange { get; set; } = true;

    public Chart? LineChart { get; private set; }
    public Sankey? SankeyChart { get; private set; }

    public int Speed { get; private set; }
    private int _start;

    public Charts2Vm(AppSession session) : base(session)
    {
        Speed = 500;
    }

    public override void OnLoad()
    {
        var time = DateTime.Now;
        var timeStamps = Enumerable.Range(0, 60)
            .Select(t => time + TimeSpan.FromMinutes(t))
            .ToArray();

        var timeSeriesAxis = new ChartCategoryTimeseriesAxis("%H:%M", 50, timeStamps);
        
        LineChart = new Chart
        {
            Title = new ChartTitle("Test"),
            Series = new[] { new ChartSeries("Sinus") },
            CategoryAxis = timeSeriesAxis
        };
        SankeyChart = new Sankey
        {
            Nodes = new SankeyNode[]
            {
                new("Alice"),
                new("Bert"),
                new("Bob") { Color = Color.Coral },
                new("Carol"),
                new("Doris")
            },
            Links = new SankeyLink[]
            {
                new("Alice", "Bob") { Value = 10 },
                new("Bert", "Bob") { Value = 5 },
                new("Bob", "Carol") { Value = 95 },
                new("Bob", "Doris") { Value = 5 }
            }
        };
        foreach (var link in SankeyChart.Links)
        {
            link.Tooltip = $"{link.Source} -> {link.Target}\n{link.Value} units";
        }
            
        UpdateData();

        SetUpdateTimer(Speed);
    }

    private void UpdateData()
    {
        if(SankeyChart == null || LineChart == null) return;
        
        var data = new object?[60];
        for (var ix = 0; ix < 60; ix++)
        {
            data[ix] = FullTimeRange || ix >= 30
                ? (int)(Math.Sin((ix * 2 + _start) * Math.PI / 36) * 40) + 50
                : null;
        }

        SankeyChart.Links[0].Value = (int)(Math.Sin((50 * 2 + _start) * Math.PI / 36) * 40) + 50;
        SankeyChart.Links[1].Value = 100 - SankeyChart.Links[0].Value;

        _start++;

        LineChart.SetSeriesData("Sinus", data);
    }

    public override void OnUpdateTimer()
    {
        UpdateData();
            
        NotifyPropertiesChanged(new []
        {
            nameof(LineChart),
            nameof(SankeyChart)
        });
        Session.UpdatePropertiesImmediately();
    }


    [ActionMethod]
    public void ChangeFullTimeRange()
    {
            
    }
        
    [ActionMethod]
    public void ToggleSpeed()
    {
        Speed = 600 - Speed;

        SetUpdateTimer(Speed);
    }
}