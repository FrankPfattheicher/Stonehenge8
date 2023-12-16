using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

public class NotifyTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task ModifyNotifyPropertyShouldCreateEvent()
    {
        var response = string.Empty;

        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app.BaseUrl + "/ViewModel/StartVm");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(ModifyNotifyPropertyShouldCreateEvent));
            Debugger.Break();
        }

        Assert.NotNull(response);


        _app.Data.ExecAction("Notify");
            
        // rising event not yet tested ...
    }
        
}