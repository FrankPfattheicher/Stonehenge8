// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Resources
{
    internal class AssemblyResource
    {
        public string FullName { get; private set; }

        public string ShortName { get; private set; }

        public Assembly Assembly { get; private set; }

        public AssemblyResource(string fullName, string shortName, Assembly assembly)
        {
            FullName = fullName;
            ShortName = shortName;
            Assembly = assembly;
        }
    }
} 
