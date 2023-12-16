using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Test.DiContainer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestActiveVmWithDependency : ActiveViewModel
    {
        public readonly ResolveVmDependenciesTest Test;

        public TestActiveVmWithDependency(AppSession session, ResolveVmDependenciesTest test)
            : base(session)
        {
            Test = test;
        }

    }
}
