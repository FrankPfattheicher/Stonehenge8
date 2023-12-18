using System.Net;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class ServerSentEvents(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (context.Request.Method == "GET" && path.StartsWith("/EventSource/"))
        {
            var appSession = context.Items["stonehenge.AppSession"] as AppSession;
            if (appSession == null)
            {
                await next.Invoke(context);
                return;
            }
            var rqVmName = path.Substring(13);
            var appVmName = appSession.ViewModel?.GetType().Name ?? string.Empty;
            string? json;
            if (rqVmName != appVmName)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK; 
                json = "data: { \"StonehengeContinuePolling\": false }\r\r";
                await context.Response.WriteAsync(json);
                await context.Response.Body.FlushAsync();
                context.Response.Body.Close();
                return;
            }

            var viewModel = appSession.ViewModel as ActiveViewModel;
            if (viewModel == null)
            {
                await next.Invoke(context);
                return;
            }
            
            context.Response.Headers.Add("Content-Type", "text/event-stream");
            await viewModel.SendPropertiesChanged(context);

            json = "data: { \"StonehengeContinuePolling\": false }\r\r";
            await context.Response.WriteAsync(json);
            await context.Response.Body.FlushAsync();
            context.Response.Body.Close();
            return;
        }

        await next.Invoke(context);
    }

}