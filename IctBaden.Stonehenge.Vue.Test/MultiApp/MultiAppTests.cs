using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Vue.TestApp2.ViewModels;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.MultiApp;

public class MultiAppTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app1 = new();
    private readonly VueTestApp _app2 = new(typeof(SecondAppVm).Assembly);

    public void Dispose()
    {
        _app1.Dispose();
        _app2.Dispose();
    }

    [Fact]
    public async Task RunningMultipleAppsShouldNotMixUpContent()
    {
        var response = string.Empty;

        // app1
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app1.BaseUrl + "/app.js");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(RunningMultipleAppsShouldNotMixUpContent));
        }

        Assert.NotNull(response);
        Assert.Contains("'start'", response);
        Assert.DoesNotContain("'secondapp'", response);

        // app2
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app2.BaseUrl + "/app.js");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(RunningMultipleAppsShouldNotMixUpContent));
        }

        Assert.NotNull(response);
        Assert.Contains("'secondapp'", response);
        Assert.DoesNotContain("'start'", response);
    }

}