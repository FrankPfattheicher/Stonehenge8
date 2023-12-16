using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Vue.TestApp2.ViewModels;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.MultiApp;

public class SecondAppTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new(typeof(SecondAppVm).Assembly);

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task SecondAppShouldContainPagesFromSecondAssemblyOnly()
    {
        var response = string.Empty;
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(SecondAppShouldContainPagesFromSecondAssemblyOnly));
        }

        Assert.NotNull(response);
        Assert.Contains("'secondapp'", response);
        Assert.DoesNotContain("'start'", response);
    }

}