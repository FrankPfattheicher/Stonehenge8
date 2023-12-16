// ReSharper disable MemberCanBePrivate.Global

using System;
using System.IO;
using System.Reflection;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Hosting;

public class StonehengeHostOptions
{
    /// <summary>
    /// Title to be shown in the Title bar.
    /// Default is the entry assembly name.
    /// </summary>
    public string Title { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

    /// <summary>
    /// Initial page to be activated.
    /// By default the first page (by sort index) is used. 
    /// </summary>
    public string StartPage { get; set; } = string.Empty;

    /// <summary>
    /// Path to the file based content.
    /// </summary>
    public string AppFilesPath { get; set; } = Path.Combine(StonehengeApplication.BaseDirectory, "app");

    /// <summary>
    /// Method to use for server initiated data transfer to the client.
    /// </summary>
    public ServerPushModes ServerPushMode { get; set; } = ServerPushModes.Automatic;
        
    /// <summary>
    /// Interval for client site polling modes.
    /// [Seconds]
    /// Set to 0 to use system default.
    /// </summary>
    public int PollIntervalSec { get; set; }

    /// <summary>
    /// Count of event poll retries before setting StonehengeIsDisconnected 
    /// </summary>
    public int PollRetries { get; set; } = 1;

    /// <summary>
    /// Forth NTLM authentication using HttpSys.
    /// (Windows host only)
    /// </summary>
    public bool UseNtlmAuthentication { get; set; }
        
    /// <summary>
    /// Internally check basic Authentication.
    /// Place .htpasswd file in application directory.
    /// Encoding apache specific salted MD5 (insecure but common).
    /// htpasswd -nbm myName myPassword
    /// </summary>
    public bool UseBasicAuth { get; set; }

    /// <summary>
    /// If not null, contains all options
    /// to handle Keycloak user authentication
    /// </summary>
    public KeycloakAuthenticationOptions? UseKeycloakAuthentication { get; set; }

    /// <summary>
    /// Path of the pfx certificate to be used with Kestrel.
    /// (not used with HttpSys, you need to "netsh http add sslcert ..." for the the p12 certificate in that case)
    /// On Windows it is better to use IIS as reverse proxy.
    /// </summary>
    public string SslCertificatePath { get; set; } = string.Empty;
    /// <summary>
    /// Password of the pfx certificate to be used with Kestrel.
    /// (not used with HttpSys)
    /// </summary>
    public string SslCertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Host is using the following headers to disable clients
    /// to cache any content.
    ///     Cache-Control: no-cache, no-store, must-revalidate, proxy-revalidate
    ///     Pragma: no-cache
    ///     Expires: 0 
    /// </summary>
    public bool DisableClientCache { get; set; }

    /// <summary>
    /// Allow custom middleware (by type name) to be inserted
    /// before StonehengeContent is called
    /// </summary>
    public string[] CustomMiddleware { get; set; } = Array.Empty<string>();
        
    /// <summary>
    /// Enable firing WindowResized AppCommand  
    /// </summary>
    public bool HandleWindowResized { get; set; }
        
    /// <summary>
    /// Enable detecting client locale (Accept-Language header)
    /// to set session locale
    /// </summary>
    public bool UseClientLocale { get; set; }
        
        
    /// <summary>
    /// Delay [ms] the client should wait for new poll.
    /// </summary>
    /// <returns></returns>
    public int GetPollDelayMs()
    {
        return ServerPushMode switch
        {
            ServerPushModes.LongPolling => 100,
            ServerPushModes.ShortPolling when PollIntervalSec > 1 => (PollIntervalSec * 1000) + 100,
            _ => 5000
        };
    }
    /// <summary>
    /// Timeout the server max waits to respond to event query
    /// if there is no event.
    /// </summary>
    /// <returns></returns>
    public int GetEventTimeoutMs()
    {
        if (ServerPushMode != ServerPushModes.LongPolling) return 100;
        if(PollIntervalSec > 1) return (PollIntervalSec * 1000) + 100;
        return 10000;
    }

}