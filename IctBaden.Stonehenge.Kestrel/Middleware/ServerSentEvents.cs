using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class ServerSentEvents(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DoubleConverter() }
    };

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (context.Request.Method == "GET" && path.StartsWith("/EventSource/"))
        {
            var appSession = context.Items["stonehenge.AppSession"] as AppSession;
            var rqVmName = path.Substring(13);
            var appVmName = appSession?.ViewModel?.GetType().Name ?? string.Empty;
            if (rqVmName != appVmName)
            {
                await next.Invoke(context);
                return;
            }

            var response = context.Response;
            response.Headers.Add("Content-Type", "text/event-stream");

            for(var i = 0; true; ++i)
            {
                var name = appSession!.GetNextEvent();
                if (string.IsNullOrEmpty(name))
                {
                    await response.WriteAsync($"data: {{ }}\r\r");
                }
                else
                {
                    var av = appSession.ViewModel as ActiveViewModel;
                    var value = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(av?.TryGetMember(name), JsonOptions));
                    var json = $"data: {{ \"{name}\":{value} }}\r\r";
                    await response.WriteAsync(json);
                }

                await response.Body.FlushAsync();
                await Task.Delay(5 * 1000);
            }
        }

        await next.Invoke(context);
    }

}