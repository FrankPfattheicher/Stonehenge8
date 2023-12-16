using System;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Resources;

public class StonehengeResourceProviderTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async void PostRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Post(_app.BaseUrl + "/user/request?p1=11&p2=22", "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response);
        Assert.Contains("22", response);
        Assert.Contains("POST", response);
    }

    [Fact]
    public async void PutRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Put(_app.BaseUrl + "/user/request?p1=11&p2=22", "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response);
        Assert.Contains("22", response);
        Assert.Contains("PUT", response);
    }
    
    [Fact]
    public async void DeleteRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Delete(_app.BaseUrl + "/user/request?p1=11&p2=22");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response);
        Assert.Contains("22", response);
        Assert.Contains("DELETE", response);
    }

}
