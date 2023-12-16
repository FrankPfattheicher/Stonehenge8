using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Global

namespace IctBaden.Stonehenge.Test.Serializer;

public class SimpleClass
{
    public int Integer { get; set; }
    public bool Boolean { get; set; }
    public double FloatingPoint { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public DateTimeOffset Timeoffset { get; set; } = DateTimeOffset.Now;

    public TestEnum Wieviel { get; set; } = TestEnum.Fumpf;

    public string PrivateText = string.Empty;
}