using System;
using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Test.Serializer
{
    public class NestedClass
    {
        public string Name { get; set; } = string.Empty;
        public List<NestedClass2> Nested { get; set; } = new();
    }

    public class NestedClass2
    {
        public SimpleClass[] NestedSimple { get; set; } = Array.Empty<SimpleClass>();
    }
}