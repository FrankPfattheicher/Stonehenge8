using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class StonehengeRoot(RequestDelegate next)
{
    // ReSharper disable once UnusedMember.Global

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value.Replace("//", "/");
        if (path == "/")
        {
            var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString() ?? string.Empty);
            context.Response.Redirect($"/index.html?{query}");
            return;
        }

        await next.Invoke(context);
    }
}