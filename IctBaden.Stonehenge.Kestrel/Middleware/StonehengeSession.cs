using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Kestrel.Middleware;

internal static class HttpContextExtensions
{
    public static bool IsLocal(this HttpContext ctx)
    {
        var connection = ctx.Connection;
        if (connection.RemoteIpAddress != null)
        {
            if (connection.LocalIpAddress != null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }
            else
            {
                return IPAddress.IsLoopback(connection.RemoteIpAddress);
            }
        }

        // for in memory TestServer or when dealing with default connection info
        if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
        {
            return true;
        }

        return false;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class StonehengeSession
{
    private readonly RequestDelegate _next;

    // ReSharper disable once UnusedMember.Global
    public StonehengeSession(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var logger = (ILogger)context.Items["stonehenge.Logger"];

        var timer = new Stopwatch();
        timer.Start();

        var path = context.Request.Path.ToString() ?? string.Empty;

        if (path.ToLower().Contains("/user/"))
        {
            logger.LogTrace($"Kestrel Begin USER {context.Request.Method} {path}");
            await _next.Invoke(context);
            logger.LogTrace($"Kestrel End USER {context.Request.Method} {path}");
            return;
        }

        var appSessions = context.Items["stonehenge.AppSessions"] as List<AppSession>;
        Debug.Assert(appSessions != null);

        // Header id has first priority
        var stonehengeId = context.Request.Headers["X-Stonehenge-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(stonehengeId))
        {
            // URL id has second priority
            stonehengeId = context.Request.Query["stonehenge-id"];
        }

        if (string.IsNullOrEmpty(stonehengeId))
        {
            // see referer
            var referer = context.Request.Headers["Referer"].FirstOrDefault() ?? "";
            stonehengeId = new Regex("stonehenge-id=([a-f0-9A-F]+)")
                .Matches(referer)
                .Select(m => m.Groups[1].Value)
                .FirstOrDefault();
        }

        if (string.IsNullOrEmpty(stonehengeId))
        {
            var cookie = context.Request.Headers.FirstOrDefault(h => h.Key == "Cookie");
            if (!string.IsNullOrEmpty(cookie.Value.ToString()))
            {
                // workaround for double stonehenge-id values in cookie - take the last one
                var ids = new Regex("stonehenge-id=([a-f0-9A-F]+)")
                    .Matches(cookie.Value.ToString() ?? string.Empty)
                    .Select(m => m.Groups[1].Value).ToArray();
                if (ids.Length > 1)
                {
                    logger.LogError("Multiple Stonehenge Ids in cookie: " + string.Join(", ", ids));
                }

                if (ids.Length > 0)
                {
                    stonehengeId = ids.LastOrDefault(id => appSessions.Any(s => s.Id == id));
                }
            }
        }

        logger.LogTrace(
            $"Kestrel[{stonehengeId}] Begin {context.Request.Method} {path}{context.Request.QueryString}");

        CleanupTimedOutSessions(logger, appSessions);
        var session = appSessions.FirstOrDefault(s => s.Id == stonehengeId);
        if (session == null)
        {
            // session not found
            var resourceLoader = context.Items["stonehenge.ResourceLoader"] as StonehengeResourceLoader;
            var directoryName = Path.GetDirectoryName(path) ?? "/";
            var resource = resourceLoader != null
                ? await resourceLoader.Get(null, path.Substring(1).Replace("/", "."),
                    new Dictionary<string, string>())
                : null;
            if (directoryName.Length > 1 && resource == null && stonehengeId != null)
            {
                logger.LogTrace(
                    $"Kestrel[{stonehengeId}] Abort {context.Request.Method} {path}{context.Request.QueryString}");
                return;
            }

            if (directoryName.Length <= 1 || resource == null)
            {
                // redirect to new session
                session = NewSession(logger, appSessions, context, resourceLoader);
                context.Response.Headers.Add("X-Stonehenge-id", new StringValues(session.Id));

                var redirectUrl = "/index.html";
                var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString() ?? string.Empty);
                query["stonehenge-id"] = session.Id;
                redirectUrl += $"?{query}";

                context.Response.Redirect(redirectUrl);

                var remoteIp = context.Connection.RemoteIpAddress;
                var remotePort = context.Connection.RemotePort;
                logger.LogTrace(
                    $"Kestrel[{stonehengeId}] From IP {remoteIp}:{remotePort} - redirect to {session.Id}");
                return;
            }
        }

        var etag = context.Request.Headers["If-None-Match"];
        if (context.Request.Method == "GET" && !string.IsNullOrEmpty(etag) &&
            etag == AppSession.GetResourceETag(path))
        {
            logger.LogTrace("ETag match");
            context.Response.StatusCode = (int)HttpStatusCode.NotModified;
        }
        else
        {
            context.Items.Add("stonehenge.AppSession", session);
            await _next.Invoke(context);
        }

        timer.Stop();

        if (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogTrace(
                $"Kestrel[{stonehengeId}] Canceled {context.Request.Method}={context.Response.StatusCode} {path}, {timer.ElapsedMilliseconds}ms");
            throw new TaskCanceledException();
        }

        logger.LogTrace(
            $"Kestrel[{stonehengeId}] End {context.Request.Method}={context.Response.StatusCode} {path}, {timer.ElapsedMilliseconds}ms");
    }

    private static void CleanupTimedOutSessions(ILogger logger, ICollection<AppSession> appSessions)
    {
        var timedOutSessions = appSessions.Where(s => s.IsTimedOut).ToArray();
        foreach (var session in timedOutSessions)
        {
            var vm = session.ViewModel as IDisposable;
            vm?.Dispose();
            session.ViewModel = null;
            appSessions.Remove(session);
            logger.LogInformation($"Kestrel Session timed out {session.Id}.");
            session.Dispose();
        }

        if (timedOutSessions.Any())
        {
            logger.LogInformation($"Kestrel {appSessions.Count} sessions.");
        }
    }

    private static AppSession NewSession(ILogger logger, ICollection<AppSession> appSessions, HttpContext context,
        StonehengeResourceLoader? resourceLoader)
    {
        var options = (StonehengeHostOptions)context.Items["stonehenge.HostOptions"];
        var session = new AppSession(resourceLoader, options);
        var isLocal = context.IsLocal();
        var userAgent = context.Request.Headers["User-Agent"];
        var userLanguages = context.Request.Headers["Accept-Language"];
        if (options.UseClientLocale)
        {
            session.SetSessionCulture(GetCulture(userLanguages));
        }

        var httpContext = context.Request?.HttpContext;
        var clientAddress = httpContext?.Connection.RemoteIpAddress.ToString() ?? string.Empty;
        var clientPort = httpContext?.Connection.RemotePort ?? 0;
        var hostDomain = context.Request?.Host.Value ?? string.Empty;
        var hostUrl = $"{context.Request?.Scheme ?? "http"}://{hostDomain}";
        session.Initialize(options, hostUrl, hostDomain, isLocal, clientAddress, clientPort, userAgent);
        appSessions.Add(session);
        logger.LogInformation($"Kestrel New session {session.Id}. {appSessions.Count} sessions.");
        return session;
    }

    private static CultureInfo GetCulture(string languages)
    {
        if (string.IsNullOrEmpty(languages)) return CultureInfo.CurrentCulture;

        foreach (var language in languages.Split(';'))
        {
            var realLanguage = Regex.Replace(language, "[;q=(0-9).]", "");
            var locale = realLanguage.Split(',').FirstOrDefault();
            //first one should be the used language that is set for a browser (if user did not change it their self).
            if (locale != null)
            {
                return new CultureInfo(locale);
            }
        }

        return CultureInfo.CurrentCulture;
    }
}