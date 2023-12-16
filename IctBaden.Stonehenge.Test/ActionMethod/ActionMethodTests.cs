using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Test.Session;
using IctBaden.Stonehenge.ViewModel;
using Xunit;

namespace IctBaden.Stonehenge.Test.ActionMethod;

public class ActionMethodTests : IDisposable
{
    private readonly StonehengeResourceLoader _loader;
    private readonly AppSession _session = new();

    public ActionMethodTests()
    {
        var assemblies = new List<Assembly?>
            {
                Assembly.GetAssembly(typeof(ResourceLoader)),
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly()
            }
            .Where(a => a != null)
            .Distinct()
            .Cast<Assembly>()
            .ToList();

        var resLoader = new ResourceLoader(StonehengeLogger.DefaultLogger, assemblies, Assembly.GetCallingAssembly());
        var fileLoader = new FileLoader(StonehengeLogger.DefaultLogger, Path.GetTempPath());

        var providers = new List<IStonehengeResourceProvider>
        {
            new ViewModelProvider(StonehengeLogger.DefaultLogger),
            resLoader,
            fileLoader
        };
        _loader = new StonehengeResourceLoader(StonehengeLogger.DefaultLogger, providers);
    }

    public void Dispose()
    {
    }

    [Fact]
    public async Task PostWithParameterShouldExecuteActionAndReturnParameter()
    {
        await ExecuteTestAction("test123", "test123");
    }

    [Fact]
    public async Task PostWithEmptyParameterShouldNotExecuteAction()
    {
        await ExecuteTestAction("", "INITIAL VALUE");
    }

    private async Task ExecuteTestAction(string parameterValue, string expectedValue)
    {
        var formData = new Dictionary<string, string>();
        var parameters = string.IsNullOrEmpty(parameterValue)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>
            {
                { "parameter", parameterValue }
            };
        var viewModelType = _session.SetViewModelType("TestVm");
        Assert.NotNull(viewModelType);

        var resource = await _loader.Post(_session, "ViewModel/TestVm/TestAction", parameters, formData);
        Assert.NotNull(resource);

        var vmData = JsonSerializer.Deserialize<TestVm>(resource.Text ?? "{}");
        Assert.Equal(expectedValue, vmData?.ActionParameter);
    }
}