using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpMultipartParser;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein")]
public class StonehengeContent
{
    private readonly RequestDelegate _next;
    private static readonly object LockViews = new();
    private static readonly object LockEvents = new();

    // ReSharper disable once UnusedMember.Global
    public StonehengeContent(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public Task Invoke(HttpContext context)
    {
        if (context.Request.Path.Value.Contains("/Events"))
        {
            lock (LockEvents)
            {
                return InvokeLocked(context);
            }
        }

        lock (LockViews)
        {
            return InvokeLocked(context);
        }
    }

    private async Task InvokeLocked(HttpContext context)
    {
        var logger = (ILogger)context.Items["stonehenge.Logger"];
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (logger == null)
        {
            Debugger.Break();
            return;
        }
        
        var path = context.Request.Path.Value.Replace("//", "/");
        try
        {
            var response = context.Response.Body;
            var resourceLoader = context.Items["stonehenge.ResourceLoader"] as IStonehengeResourceProvider;
            var resourceName = path.Substring(1);
            var appSession = context.Items["stonehenge.AppSession"] as AppSession;
            var requestVerb = context.Request.Method;
            var cookiesHeader = context.Request.Headers
                .FirstOrDefault(h => h.Key == HeaderNames.Cookie).Value.ToString();
            var requestCookies = cookiesHeader!
                .Split(';')
                .Select(s => s.Trim())
                .Select(s => s.Split('='));
            var cookies = new Dictionary<string, string>();
            foreach (var cookie in requestCookies)
            {
                if (!cookies.ContainsKey(cookie[0]) && (cookie.Length > 1))
                {
                    cookies.Add(cookie[0], cookie[1]);
                }
            }

            var queryString = HttpUtility.ParseQueryString(context.Request.QueryString.ToString() ?? string.Empty);
            var parameters = queryString.AllKeys
                .Where(k => !string.IsNullOrEmpty(k))
                .ToDictionary(key => key!, key => queryString[key]!);
            
            Resource? content = null;

            appSession?.SetParameters(parameters);
            if ((appSession?.UseBasicAuth ?? false) && !CheckBasicAuthFromContext(appSession, context))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Headers.Add("WWW-Authenticate", "Basic");
                return;
            }

            if (appSession?.HostOptions.UseKeycloakAuthentication != null
                && appSession.RequestLogin
                && !context.Request.Path.Value.Contains("/Events"))
            {
                var o = appSession.HostOptions.UseKeycloakAuthentication;
                var requestQuery =
                    HttpUtility.ParseQueryString(context.Request.QueryString.ToString() ?? string.Empty);

                var state = requestQuery["state"] ?? "";
                if (state.StartsWith(appSession.Id))
                {
                    var code = requestQuery["code"];
                    var data =
                        $"grant_type=authorization_code&client_id={o.ClientId}&code={code}&redirect_uri={HttpUtility.UrlEncode(appSession.AuthorizeRedirectUrl)}";

                    using var client = new HttpClient();
                    var tokenUrl = $"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/token";
                    var result = client.PostAsync(tokenUrl,
                            new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded"))
                        .Result;
                    var json = result.Content.ReadAsStringAsync().Result;
                    var authResponse = JsonSerializer.Deserialize<JsonObject>(json);
                    if (authResponse != null)
                    {
                        appSession.AccessToken = authResponse["id_token"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(appSession.AccessToken))
                        {
                            appSession.AccessToken = authResponse["access_token"]?.ToString() ?? string.Empty;
                        }

                        appSession.RefreshToken = authResponse["refresh_token"]?.ToString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(appSession.AccessToken))
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadToken(appSession.AccessToken) as JwtSecurityToken;
                            var identityId = jwtToken?.Subject ?? string.Empty;
                            var identityName = jwtToken?.Payload["name"]?.ToString() ?? string.Empty;
                            var identityMail = jwtToken?.Payload["email"]?.ToString() ?? string.Empty;
                            appSession.SetUser(identityName, identityId, identityMail);
                            (appSession.ViewModel as ActiveViewModel)?.NavigateTo(appSession.AuthorizeRedirectUrl);
                        }
                    }

                    Console.WriteLine(result);
                }
                else
                {
                    var newSession =
                        $"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.Path}?stonehenge-id=new";
                    context.Response.Redirect(newSession);
                    return;
                }
            }

            if (appSession != null
                && appSession.HostOptions.UseKeycloakAuthentication == null
                && string.IsNullOrEmpty(appSession.UserIdentity))
            {
                SetUserNameFromContext(appSession, context);
            }

            if (appSession?.SessionCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = appSession.SessionCulture;
                Thread.CurrentThread.CurrentUICulture = appSession.SessionCulture;
            }

            switch (requestVerb)
            {
                case "GET":
                    appSession?.Accessed(cookies, false);
                    content = resourceLoader != null
                        ? await resourceLoader.Get(appSession, resourceName, parameters)
                        : null;
                    if (content == null && appSession != null &&
                        resourceName.EndsWith("index.html", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.LogError(
                            $"Invalid path in index resource {resourceName} - redirecting to root index");

                        var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString() ??
                                                                 string.Empty);
                        query["stonehenge-id"] = appSession.Id;
                        context.Response.Redirect($"/index.html?{query}");
                        return;
                    }

                    if (content != null &&
                        string.Compare(resourceName, "index.html",
                            StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        HandleIndexContent(context, content);
                    }

                    break;

                case "POST":
                case "PUT":
                case "PATCH":
                case "DELETE":
                    appSession?.Accessed(cookies, true);

                    try
                    {
                        var formData = new Dictionary<string, string>();
                        context.Request.EnableBuffering();
                        var body = new StreamReader(context.Request.Body).ReadToEndAsync().Result;
                        if (body.StartsWith("{"))
                        {
                            try
                            {
                                var jsonObject = JsonSerializer.Deserialize<JsonObject>(body);
                                if (jsonObject != null)
                                {
                                    foreach (var kv in jsonObject.AsObject())
                                    {
                                        if(kv.Value != null)
                                            formData.Add(kv.Key, kv.Value.ToString());
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                logger.LogWarning("Failed to parse post data as json");
                            }
                        }
                        else if (context.Request.ContentType == "application/x-www-form-urlencoded")
                        {
                            var result = HttpUtility.ParseQueryString(body);
                            foreach (string key in result)
                            {
                                var value = result[key];
                                if(value != null) formData.Add(key, value);
                            }
                        }
                        else
                        {
                            try
                            {
                                context.Request.Body.Seek(0, SeekOrigin.Begin);
                                var parser = await MultipartFormDataParser.ParseAsync(context.Request.Body);
                                foreach (var p in parser.Parameters)
                                {
                                    formData.Add(p.Name, p.Data);
                                }

                                foreach (var f in parser.Files)
                                {
                                    // Save temp file
                                    var fileName = Path.GetTempFileName();
                                    await using var file = File.OpenWrite(fileName);
                                    await f.Data.CopyToAsync(file);
                                    file.Close();
                                    formData.Add(f.Name, fileName);
                                    formData.Add(f.Name + ".SourceName", f.FileName);
                                    formData.Add(f.Name + ".ContentType", f.ContentType);
                                }
                            }
                            catch (Exception)
                            {
                                logger.LogWarning("Failed to parse post data as multipart form data");
                            }
                        }

                        if (resourceLoader != null)
                        {
                            switch (requestVerb)
                            {
                                case "PUT":
                                case "PATCH":
                                    content = await resourceLoader.Put(appSession, resourceName, parameters, formData);
                                    break;
                                case "DELETE":
                                    content = await resourceLoader.Delete(appSession, resourceName, parameters, formData);
                                    break;
                                default: // POST
                                    content = await resourceLoader.Post(appSession, resourceName, parameters, formData);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null) ex = ex.InnerException;
                        logger.LogError(ex.Message);
                        logger.LogError(ex.StackTrace);

                        var exResource = new Dictionary<string, string>
                        {
                            { "Message", ex.Message },
                            { "StackTrace", ex.StackTrace ?? string.Empty }
                        };
                        content = new Resource(resourceName, $"StonehengeContent.Invoke.{requestVerb}", ResourceType.Json,
                            JsonSerializer.Serialize(exResource), Resource.Cache.None);
                        
                        Debugger.Break();
                    }

                    break;
            }

            if (content == null)
            {
                await _next.Invoke(context);
                return;
            }

            context.Response.ContentType = content.ContentType;

            if (context.Items["stonehenge.HostOptions"] is StonehengeHostOptions { DisableClientCache: true })
            {
                context.Response.Headers.Add("Cache-Control",
                    new[] { "no-cache", "no-store", "must-revalidate", "proxy-revalidate" });
                context.Response.Headers.Add("Pragma", new[] { "no-cache" });
                context.Response.Headers.Add("Expires", new[] { "0" });
            }
            else
            {
                switch (content.CacheMode)
                {
                    case Resource.Cache.None:
                        context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
                        break;
                    case Resource.Cache.Revalidate:
                        context.Response.Headers.Add("Cache-Control",
                            new[] { "max-age=3600", "must-revalidate", "proxy-revalidate" });
                        var etag = AppSession.GetResourceETag(path);
                        context.Response.Headers.Add(HeaderNames.ETag, new StringValues(etag));
                        break;
                    case Resource.Cache.OneDay:
                        context.Response.Headers.Add("Cache-Control", new[] { "max-age=86400" });
                        break;
                }
            }

            if (appSession != null)
            {
                context.Response.Headers.Add("X-Stonehenge-Id", new[] { appSession.Id });
            }

            if (content.IsNoContent)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else if (content.IsBinary)
            {
                await using var writer = new StreamWriter(response);
                await writer.BaseStream.WriteAsync(content.Data);
            }
            else
            {
                await using var writer = new StreamWriter(response);
                await writer.WriteAsync(content.Text);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"StonehengeContent write response: {ex.Message}" + Environment.NewLine + ex.StackTrace);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                logger.LogError(" + " + ex.Message);
            }
            Debugger.Break();
        }
    }


    private bool CheckBasicAuthFromContext(AppSession appSession, HttpContext context)
    {
        var auth = context.Request.Headers["Authorization"].FirstOrDefault();
        if (auth == null) return false;

        if (auth.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (auth == appSession.VerifiedBasicAuth) return true;

            var userPassword = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6)));
            var usrPwd = userPassword.Split(':');
            if (usrPwd.Length != 2) return false;

            var user = usrPwd[0];
            var pwd = usrPwd[1];
            var isValid = appSession.Passwords.IsValid(user, pwd);
            appSession.VerifiedBasicAuth = isValid ? auth : string.Empty;
            return isValid;
        }

        return false;
    }

    private void SetUserNameFromContext(AppSession appSession, HttpContext context)
    {
        var identityId = context.User.Identity?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(identityId)) return;

        var identityName = "";
        var identityMail = "";

        var auth = context.Request.Headers["Authorization"].FirstOrDefault();
        if (auth != null)
        {
            if (auth.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
            {
                var userPassword = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6)));
                identityId = userPassword.Split(':').FirstOrDefault() ?? string.Empty;
            }
            else if (auth.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
            {
                var token = auth.Substring(7);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                identityId = jwtToken?.Subject ?? string.Empty;
                identityName = jwtToken?.Payload["name"]?.ToString() ?? string.Empty;
                identityMail = jwtToken?.Payload["email"]?.ToString() ?? string.Empty;
            }

            appSession.SetUser(identityName, identityId, identityMail);
        }

        var isLocal = context.IsLocal();
        if (!isLocal) return;

        var explorers = Process.GetProcessesByName("explorer");
        if (explorers.Length == 1)
        {
            identityId = $"{Environment.UserDomainName}\\{Environment.UserName}";
            appSession.SetUser(identityId, "", "");
        }

        // RDP with more than one session: How to find app and session using request's client IP port
    }

    private void HandleIndexContent(HttpContext context, Resource content)
    {
        const string placeholderAppTitle = "stonehengeAppTitle";
        var appTitle = context.Items["stonehenge.AppTitle"].ToString();
        content.Text = content.Text?.Replace(placeholderAppTitle, appTitle);
    }
}