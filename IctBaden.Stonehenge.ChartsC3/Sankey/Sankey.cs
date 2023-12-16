// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;

namespace IctBaden.Stonehenge.Extension.Sankey;

public class Sankey
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public int NodeWidth { get; set; } = 20;


    public SankeyNode[] Nodes { get; set; } = Array.Empty<SankeyNode>();
    public SankeyLink[] Links { get; set; } = Array.Empty<SankeyLink>();
}