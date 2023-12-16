using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Content;

public class UserContentTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task IndexShouldContainUserStylesRef()
    {
        var response = string.Empty;
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app.BaseUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(IndexShouldContainUserStylesRef));
        }

        Assert.NotNull(response);
        Assert.Contains("'styles/userstyles.css'", response);
    }

    [Fact]
    public async Task IndexShouldContainUserScriptsRef()
    {
        var response = string.Empty;
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadString(_app.BaseUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(IndexShouldContainUserScriptsRef));
        }

        Assert.NotNull(response);
        Assert.Contains("'scripts/userscripts.js'", response);
    }

    [Fact]
    public async Task StartJsShouldContainStartUserScript()
    {
        var response = string.Empty;
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app.BaseUrl + "/start.js");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(StartJsShouldContainStartUserScript));
        }

        Assert.NotNull(response);
        Assert.Contains("'start_user_InitialLoaded'", response);
    }

}