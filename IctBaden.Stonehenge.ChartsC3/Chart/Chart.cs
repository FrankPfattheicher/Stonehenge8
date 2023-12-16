using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

// ReSharper disable FieldCanBeMadeReadOnly.Global

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension;

public class Chart
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Show series points
    /// </summary>
    public bool ShowPoints = true;
    
    /// <summary>
    /// Enable zooming of chart
    /// </summary>
    public bool EnableZoom = false;

    /// <summary>
    /// Define the chart's category axis
    /// </summary>
    public ChartCategoryTimeseriesAxis? CategoryAxis = null;

    /// <summary>
    /// Define the chart's values axes (maximum two)
    /// </summary>
    public ChartValueAxis[] ValueAxes;

    /// <summary>
    /// The chart's data series
    /// </summary>
    public ChartSeries[] Series;

    /// <summary>
    /// Define chart's additionally grid lines
    /// </summary>
    public ChartGridLine[] GridLines;

    /// <summary>
    /// Define chart's additionally axes and series regions
    /// </summary>
    public ChartDataRegion[] DataRegions;

    /// <summary>
    /// Define chart's title
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public ChartTitle? Title { get; set; }

    private object[] Columns
    {
        get
        {
            var columns = new List<object>();
            if (CategoryAxis != null)
            {
                var colData = new List<object> { CategoryAxis.Id };
                colData.AddRange(CategoryAxis.Values.Cast<object>());
                columns.Add(colData.ToArray());
            }

            foreach (var serie in Series)
            {
                var colData = new List<object?> { serie.Label };
                colData.AddRange(serie.Data);
                columns.Add(colData.ToArray());
            }

            return columns.ToArray();
        }
    }
    
    private object[][] Groups
    {
        get
        {
            var groupNames = Series
                .Select(s => s.Group)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct();

            var groups = groupNames
                    .Select(n => Series.Where(s => s.Group == n).Select(s => s.Label).Cast<object>().ToArray());

            return groups.ToArray();
        }
    }

    private object Colors
    {
        get
        {
            var colors = new Dictionary<string, object>();
            foreach (var series in Series.Where(s => s.Color != Color.Transparent))
            {
                var c = series.Color;
                colors.Add(series.Label, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
            }
            return colors;
        }
    }

    private object Types
    {
        get
        {
            var types = new Dictionary<string, object>();
            foreach (var series in Series.Where(s => s.Type != ChartDataType.Line))
            {
                types.Add(series.Label, series.Type.ToString().ToLower());
            }
            return types;
        }
    }

    private Dictionary<string, object> Regions
    {
        get
        {
            var regions = new Dictionary<string, object>();
            foreach (var region in DataRegions.GroupBy(r => r.Series))
            {
                if (string.IsNullOrEmpty(region.Key)) continue;

                var regionSpec = new List<object>();
                foreach (var dataRegion in region)
                {
                    var dataSpec = new Dictionary<string, object>();
                    if (dataRegion.StartValue != null) dataSpec.Add("start", dataRegion.StartValue);
                    if (dataRegion.EndValue != null) dataSpec.Add("end", dataRegion.EndValue);
                    if (dataRegion.Style != null) dataSpec.Add("style", dataRegion.Style);
                    regionSpec.Add(dataSpec);
                }
                regions.Add(region.Key, regionSpec.ToArray());
            }
            return regions;
        }
    }

    public Dictionary<string, object> Point => new()
    {
        { "show", ShowPoints }
    };

    public Dictionary<string, object> Zoom => new()
    {
        { "enabled", EnableZoom }
    };

    public Dictionary<string, object> Axis
    {
        get
        {
            var axis = new Dictionary<string, object>();
            if (CategoryAxis != null)
            {
                axis[CategoryAxis.Id] = CategoryAxis;
            }

            foreach (var ax in ValueAxes)
            {
                axis[ax.Id] = ax;
            }

            return axis;
        }
    }

    public Dictionary<string, Dictionary<string, object>> Grid
    {
        get
        {
            var gridLines = new Dictionary<string, Dictionary<string, object>>();
            var options = new Dictionary<string, object>
            {
                { "front", false }
            };
            gridLines.Add("lines", options);

            var lines = new Dictionary<string, object>
            {
                { "lines", GridLines.ToArray() }
            };
            gridLines.Add(ValueAxisId.y.ToString(), lines);

            return gridLines;
        }
    }

    /// <summary>
    /// Use column name as key, axis id as object.
    /// By default all columns are mapped to axis 'y'
    /// </summary>
    public Dictionary<string, object> Axes
    {
        get
        {
            var axes = new Dictionary<string, object>();
            foreach (var serie in Series)
            {
                axes[serie.Label] = serie.ValueAxis.ToString();
            }

            return axes;
        }
    }

    public Dictionary<string, object> Data
    {
        get
        {
            var data = new Dictionary<string, object>();
            if (CategoryAxis != null)
            {
                data[CategoryAxis.Id] = CategoryAxis.Id;
            }

            data["regions"] = Regions;
            data["axes"] = Axes;
            data["columns"] = Columns;
            data["groups"] = Groups;
            data["colors"] = Colors;
            data["types"] = Types;
           
            return data;
        }
    }


    public Chart()
    {
        ValueAxes = new[] { new ChartValueAxis(ValueAxisId.y) };
        Series = Array.Empty<ChartSeries>();
        GridLines = Array.Empty<ChartGridLine>();
        DataRegions = Array.Empty<ChartDataRegion>();
    }

    public void SetSeriesData(string series, object?[] data)
    {
        var serie = Series.FirstOrDefault(s => s.Label == series);
        if (serie != null) serie.Data = data;
    }
}