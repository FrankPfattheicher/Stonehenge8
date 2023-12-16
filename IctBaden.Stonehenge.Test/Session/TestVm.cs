using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Test.Session;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestVm
{
    public string ActionParameter { get; set; } = "INITIAL VALUE";

    [ActionMethod]
    public void TestAction(string parameter)
    {
        ActionParameter = parameter;
    }
}
