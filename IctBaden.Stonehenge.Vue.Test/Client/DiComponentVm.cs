using System.Collections.Generic;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.Test.Client
{
    public class DiComponentVm : ActiveViewModel
    {
        public int VmPropInteger { get; set; }
        public string VmPropText { get; set; } = string.Empty;
        public List<string> VmPropList { get; set; } = new();

        private readonly DiDependency _dependency;

        public DiComponentVm(AppSession session, DiDependency dependency)
            : base(session)
        {
            _dependency = dependency;
        }
    }
}