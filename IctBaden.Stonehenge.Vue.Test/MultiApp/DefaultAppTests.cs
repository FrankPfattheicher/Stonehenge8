using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.MultiApp;

public class DefaultAppTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task DefaultAppShouldContainPagesFromCurrentAssemblyOnly()
    {
        var response = string.Empty;
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(DefaultAppShouldContainPagesFromCurrentAssemblyOnly));
        }

        Assert.NotNull(response);
        Assert.Contains("'start'", response);
        Assert.DoesNotContain("'secondapp'", response);
    }

}