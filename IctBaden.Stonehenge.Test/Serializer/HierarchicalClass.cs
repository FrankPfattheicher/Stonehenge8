using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Test.Serializer
{
    public class HierarchicalClass
    {
        public string Name { get; set; } = string.Empty;
        public List<HierarchicalClass> Children { get; set; } = new();
    }
}