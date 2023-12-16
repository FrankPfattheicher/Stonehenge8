using System;
using System.Collections.Generic;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.Test.ViewModels
{
    // ReSharper disable once UnusedType.Global
    public class InvalidVm : ActiveViewModel
    {
        public int VmPropInteger { get; set; }
        public string VmPropText { get; set; } = string.Empty;
        public List<string> VmPropList { get; set; } = new();
        public Notify<string>? VmPropNotify { get; set; }

        private readonly VueTestData _data;
        
        public double FloatNaN { get; set; }
        public double FloatNegativeInfinity { get; set; }
        public double FloatPositiveInfinity { get; set; }
        
        public double DoubleNaN { get; set; }
        public double DoubleNegativeInfinity { get; set; }
        public double DoublePositiveInfinity { get; set; }
        
        
        public InvalidVm(AppSession session, VueTestData data)
        : base(session)
        {
            _data = data;
            _data.DoAction += OnDoAction;
        }

        private string OnDoAction(string action)
        {
            if (action == "Notify")
            {
                VmPropNotify?.Update(Guid.NewGuid().ToString());
            }
            return "";
        }

        public override void OnLoad()
        {
            _data.StartVmParameters = Session.Parameters;
            _data.StartVmOnLoadCalled++;
            _data.CurrentRoute = Session.CurrentRoute;

            
            FloatNaN = float.NaN;
            FloatNegativeInfinity = float.NegativeInfinity;
            FloatPositiveInfinity = float.PositiveInfinity;
            
            DoubleNaN = double.NaN;
            DoubleNegativeInfinity = double.NegativeInfinity;
            DoublePositiveInfinity = double.PositiveInfinity;
            
        }
    }
}
