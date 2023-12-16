using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

// ReSharper disable ClassNeverInstantiated.Global

namespace IctBaden.Stonehenge.Vue.SampleCore;

public class StonehengeRawContent(RequestDelegate next)
{
    // ReSharper disable once UnusedMember.Global

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/metrics"))
        {
            var response = context.Response.Body;

            context.Response.Headers.Append("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Append("Content-Type", MediaTypeNames.Text.Plain);

            await using var writer = new StreamWriter(response);
            await writer.WriteAsync("test test test test test test");

            return;
        }

        await next.Invoke(context);
    }
}