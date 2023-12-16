using System;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

public class InvalidValuesTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app;

    public InvalidValuesTests()
    {
        _app = new VueTestApp();
    }

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async void SerializationOfInvalidViewModelShouldNotReturnError()
    {
        var response = string.Empty;

        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/ViewModel/InvalidVm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(SerializationOfInvalidViewModelShouldNotReturnError));
        }

        Assert.NotNull(response);
        Assert.DoesNotContain("ValueNotSupported", response);
        Assert.DoesNotContain("Exception", response);
    }
        
        
}
