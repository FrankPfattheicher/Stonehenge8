using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

[assembly: InternalsVisibleTo("IctBaden.Stonehenge.Test")]

namespace IctBaden.Stonehenge.Core;

[SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein")]
public class AppSession : INotifyPropertyChanged, IDisposable
{
    public static string AppInstanceId { get; private set; } = Guid.NewGuid().ToString("N");

    public StonehengeHostOptions HostOptions { get; private set; } = new();
    public string HostDomain { get; private set; } = string.Empty;
    public string HostUrl { get; private set; } = string.Empty;
    public bool IsLocal { get; private set; }
    public bool IsDebug { get; private set; }
    public string ClientAddress { get; private set; } = string.Empty;
    public int ClientPort { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;

    public bool CookiesSupported { get; private set; }
    public bool StonehengeCookieSet { get; private set; }
    public Dictionary<string, string> Cookies { get; private set; }
    public Dictionary<string, string> Parameters { get; private set; }

    public DateTime ConnectedSince { get; private set; }
    public DateTime LastAccess { get; private set; }
    public string CurrentRoute => _history.FirstOrDefault() ?? string.Empty;
    public string Context { get; private set; } = string.Empty;


    /// User login is requested on next request 
    public bool RequestLogin;

    /// Redirect URL used to complete authorization 
    public string AuthorizeRedirectUrl = string.Empty;
    /// Access token given from authorization 
    public string AccessToken = string.Empty;
    /// Refresh token given from authorization 
    public string RefreshToken = string.Empty;


    /// Name of user identity 
    public string UserIdentity { get; private set; } = string.Empty;

    /// Name of user identity 
    public string UserIdentityId { get; private set; } = string.Empty;

    /// Name of user identity 
    public string UserIdentityEMail { get; private set; } = string.Empty;

        
    public CultureInfo SessionCulture { get; private set; } = CultureInfo.CurrentUICulture;
        
        
    public DateTime LastUserAction { get; private set; }

    private readonly Guid _id;
    public string Id => $"{_id:N}";

    public string PermanentSessionId { get; private set; } = string.Empty;

    public readonly bool UseBasicAuth;
    public readonly Passwords Passwords = new(string.Empty);
    public string VerifiedBasicAuth = string.Empty;

    private readonly int _eventTimeoutMs;

    private readonly List<string> _events = [];

    private CancellationTokenSource? _eventRelease;
    private bool _forceUpdate;
    private readonly List<string> _history = [];

    public string GetBackRoute()
    {
        var route = "";
        if (_history.Count > 1)
        {
            route = _history.Skip(1).First();
            _history.RemoveAt(0);
        }

        return route;
    }

    public bool IsWaitingForEvents { get; private set; }

    public bool SecureCookies { get; private set; }

    public readonly ILogger Logger;

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    private Task Wait(CancellationTokenSource cts, int milliseconds)
    {
        return Task.Delay(milliseconds, cts.Token)
            .ContinueWith(_ =>
            {
                if (cts is { IsCancellationRequested: true })
                {
                    _forceUpdate = true;
                }
            }, TaskContinuationOptions.None);
    }

    public async Task<string[]> CollectEvents()
    {
        IsWaitingForEvents = true;
        var eventVm = ViewModel;

        _eventRelease = new CancellationTokenSource();

        // wait _eventTimeoutMs for events - if there is one - continue
        var max = _eventTimeoutMs / 100;

        while (!_forceUpdate && max > 0)
        {
            await Wait(_eventRelease, 100);
            max--;
        }

        if (ViewModel == eventVm)
        {
            // wait for maximum 500ms for more events - if there is none within - continue
            max = 50;
            while (!_forceUpdate && max > 0)
            {
                await Wait(_eventRelease, 10);
                max--;
            }
        }
        else
        {
            // VM has changed
            EventsClear(false);
        }

        _forceUpdate = false;
        IsWaitingForEvents = false;

        lock (_events)
        {
            var events = _events.ToArray();
            _events.Clear();
            return events;
        }
    }

    public string GetNextEvent()
    {
        lock (_events)
        {
            var name = _events.FirstOrDefault();
            _events.Clear();
            return name ?? string.Empty;
        }
    }

    public event Action<string>? OnNavigate; 
    
    
    private object? _viewModel;

    public object? ViewModel
    {
        get => _viewModel;
        set
        {
            (_viewModel as IDisposable)?.Dispose();

            _viewModel = value;
            if (value is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (sender, args) =>
                {
                    if (sender is not ActiveViewModel avm) return;

                    lock (avm.Session._events)
                    {
                        if (!string.IsNullOrEmpty(args.PropertyName))
                        {
                            avm.Session.UpdateProperty(args.PropertyName);
                        }
                    }
                };
            }
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void ClientAddressChanged(string address)
    {
        ClientAddress = address;
        NotifyPropertyChanged(nameof(ClientAddress));
    }

    internal object? SetViewModelType(string typeName)
    {
        var oldViewModel = ViewModel;
        if (oldViewModel != null)
        {
            if ((oldViewModel.GetType().FullName == typeName))
            {
                // no change
                return oldViewModel;
            }

            EventsClear(true);
            var disposable = oldViewModel as IDisposable;
            disposable?.Dispose();
        }

        var resourceLoader = _resourceLoader.Providers
            .First(ld => ld.GetType() == typeof(ResourceLoader)) as ResourceLoader;
        if (resourceLoader == null)
        {
            ViewModel = null;
            Logger.LogError("Could not create ViewModel - No resourceLoader specified:" + typeName);
            return null;
        }

        var newViewModelType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(type => type.FullName?.EndsWith($".{typeName}") ?? false);

        if (newViewModelType == null)
        {
            ViewModel = null;
            Logger.LogError("Could not create ViewModel:" + typeName);
            return null;
        }

        ViewModel = CreateType($"ViewModel({typeName})", newViewModelType);

        var viewModelInfo = _resourceLoader.Providers
            .SelectMany(p => p.GetViewModelInfos())
            .FirstOrDefault(vmi => vmi.VmName == typeName);

        var route = viewModelInfo?.Route ?? string.Empty;
        _history.Insert(0, route);
        if (!string.IsNullOrEmpty(route))
        {
            OnNavigate?.Invoke(route);
        }

        return ViewModel;
    }

    public object? CreateType(string context, Type type)
    {
        object? instance = null;
        var typeConstructors = type.GetConstructors();
        if (!typeConstructors.Any())
        {
            Logger.LogError($"AppSession.CreateType({context}, {type.Name}): No public constructors");
        }
        foreach (var constructor in typeConstructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                instance = Activator.CreateInstance(type);
                break;
            }

            var paramValues = new object?[parameters.Length];

            for (var ix = 0; ix < parameters.Length; ix++)
            {
                var parameterInfo = parameters[ix];
                if (parameterInfo.ParameterType == typeof(AppSession))
                {
                    paramValues[ix] = this;
                }
                else
                {
                    paramValues[ix] = _resourceLoader.Services.GetService(parameterInfo.ParameterType)
                                      ?? CreateType($"{context}, CreateType({type.Name})",
                                          parameterInfo.ParameterType);
                }
            }

            try
            {
                instance = Activator.CreateInstance(type, paramValues);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError($"AppSession.CreateType({context}, {type.Name}): " + ex.Message);
                Debugger.Break();
            }
        }

        return instance;
    }


    public string SubDomain
    {
        get
        {
            if (string.IsNullOrEmpty(HostDomain))
                return string.Empty;

            var parts = HostDomain.Split('.');
            if (parts.Length == 1) return string.Empty;

            var isNumeric = int.TryParse(parts[0], out _);
            return isNumeric ? HostDomain : parts[0];
        }
    }

    private readonly Dictionary<string, object?> _userData;

    public object? this[string key]
    {
        get => _userData.ContainsKey(key) ? _userData[key] : null;
        set
        {
            if (this[key] == value)
                return;
            _userData[key] = value;
            NotifyPropertyChanged(key);
        }
    }

    public void Set<T>(string key, T value)
    {
        _userData[key] = value;
    }

    public T? Get<T>(string key)
    {
        if (!_userData.ContainsKey(key))
            return default;

        return (T?)_userData[key];
    }

    public void Remove(string key)
    {
        _userData.Remove(key);
    }

    public TimeSpan ConnectedDuration => DateTime.Now - ConnectedSince;

    public TimeSpan LastAccessDuration => DateTime.Now - LastAccess;

    // ReSharper disable once UnusedMember.Global
    public TimeSpan LastUserActionDuration => DateTime.Now - LastUserAction;

    public event Action? TimedOut;
    private Timer? _pollSessionTimeout;
    public TimeSpan SessionTimeout { get; private set; }
    public bool IsTimedOut => LastAccessDuration > SessionTimeout;

    // ReSharper disable once UnusedMember.Global
    public void SetTimeout(TimeSpan timeout)
    {
        _pollSessionTimeout?.Dispose();
        SessionTimeout = timeout;
        if (Math.Abs(timeout.TotalMilliseconds) > 0.1)
        {
            _pollSessionTimeout = new Timer(CheckSessionTimeout, null, TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
        }
    }

    private void CheckSessionTimeout(object? _)
    {
        if ((LastAccessDuration > SessionTimeout) && (_terminator != null))
        {
            _pollSessionTimeout?.Dispose();
            _terminator.Dispose();
            TimedOut?.Invoke();
        }

        NotifyPropertyChanged(nameof(ConnectedDuration));
        NotifyPropertyChanged(nameof(LastAccessDuration));
    }

    private IDisposable? _terminator;


    // ReSharper disable once UnusedMember.Global
    public void SetTerminator(IDisposable disposable)
    {
        _terminator = disposable;
    }

    private readonly StonehengeResourceLoader _resourceLoader;

    public AppSession()
        : this(null, new StonehengeHostOptions())
    {
    }

    public AppSession(StonehengeResourceLoader? resourceLoader, StonehengeHostOptions options)
    {
        if (resourceLoader == null)
        {
            var assemblies = new List<Assembly?>
                {
                    Assembly.GetEntryAssembly(),
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetAssembly(typeof(ResourceLoader))
                }
                .Where(a => a != null)
                .Distinct()
                .Cast<Assembly>()
                .ToList();

            Logger = StonehengeLogger.DefaultLogger;
            var loader = new ResourceLoader(Logger, assemblies, Assembly.GetCallingAssembly());
            resourceLoader = new StonehengeResourceLoader(Logger, new List<IStonehengeResourceProvider> { loader });
        }
        else
        {
            Logger = resourceLoader.Logger;
        }

        _resourceLoader = resourceLoader;
        _userData = new Dictionary<string, object?>();
        _id = Guid.NewGuid();
        SessionTimeout = TimeSpan.FromMinutes(15);
        Cookies = new Dictionary<string, string>();
        Parameters = new Dictionary<string, string>();
        LastAccess = DateTime.Now;

        UseBasicAuth = options.UseBasicAuth;
        if (UseBasicAuth)
        {
            var htpasswd = Path.Combine(StonehengeApplication.BaseDirectory, ".htpasswd");
            if (File.Exists(htpasswd))
            {
                Passwords = new Passwords(htpasswd);
            }
            else
            {
                Logger.LogError("Option UseBasicAuth requires .htpasswd file " + htpasswd);
            }
        }

        _eventTimeoutMs = options.GetEventTimeoutMs();

        try
        {
            if (Assembly.GetEntryAssembly() == null) return;
            var cfg = Path.Combine(StonehengeApplication.BaseDirectory, "Stonehenge.cfg"); // TODO: doc
            if (!File.Exists(cfg)) return;

            var settings = File.ReadAllLines(cfg);
            var secureCookies = settings.FirstOrDefault(s => s.Contains("SecureCookies"));
            if (secureCookies != null)
            {
                var set = secureCookies.Split('=');
                SecureCookies = (set.Length > 1) && (set[1].Trim() == "1");
            }
        }
        catch
        {
            // ignore
        }
    }

    // ReSharper disable once UnusedMember.Global
    public bool IsInitialized => !string.IsNullOrEmpty(UserAgent);


    private bool IsAssemblyDebugBuild(Assembly assembly)
    {
        return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
    }

    public void Initialize(StonehengeHostOptions hostOptions, string hostUrl, string hostDomain,
        bool isLocal, string clientAddress, int clientPort, string userAgent)
    {
        HostOptions = hostOptions;
        HostUrl = hostUrl;
        HostDomain = hostDomain;
        IsLocal = isLocal;
        ClientAddress = clientAddress;
        ClientPort = clientPort;
        UserAgent = userAgent;
        ConnectedSince = DateTime.Now;

        DetectBrowser(userAgent);
        IsDebug = IsAssemblyDebugBuild(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
    }

    // ReSharper disable once UnusedParameter.Local
    private void DetectBrowser(string userAgent)
    {
        var browserDecoder = new SimpleUserAgentDecoder(userAgent);

        Browser = browserDecoder.BrowserName;
        if (!string.IsNullOrEmpty(browserDecoder.BrowserVersion))
        {
            Browser += $" {browserDecoder.BrowserVersion}";
        }

        Platform = browserDecoder.ClientOsName;
        if (!string.IsNullOrEmpty(browserDecoder.ClientOsVersion))
        {
            Platform += $" {browserDecoder.ClientOsVersion}";
        }
        
        CookiesSupported = true;
    }

    public void Accessed(IDictionary<string, string> cookies, bool userAction)
    {
        foreach (var cookie in cookies)
        {
            Cookies[cookie.Key] = cookie.Value;
        }


        if (string.IsNullOrEmpty(PermanentSessionId) && cookies.TryGetValue("ss-pid", out var ssPid))
        {
            PermanentSessionId = ssPid;
        }

        LastAccess = DateTime.Now;
        NotifyPropertyChanged(nameof(LastAccess));
        if (userAction)
        {
            LastUserAction = DateTime.Now;
            NotifyPropertyChanged(nameof(LastUserAction));
        }

        StonehengeCookieSet = cookies.ContainsKey("stonehenge-id");
        NotifyPropertyChanged(nameof(StonehengeCookieSet));
    }

    public void SetContext(string context)
    {
        Context = context;
    }

    public void EventsClear(bool forceEnd)
    {
        Logger.LogTrace($"Session({Id}).EventsClear({forceEnd})");
        lock (_events)
        {
            //var privateEvents = Events.Where(e => e.StartsWith(AppService.PropertyNameId)).ToList();
            _events.Clear();
            //Events.AddRange(privateEvents);
            if (forceEnd)
            {
                _eventRelease?.Cancel();
            }
        }
    }

    public void UpdatePropertyImmediately(string name)
    {
        UpdateProperty(name);
        UpdatePropertiesImmediately();
    }

    public void UpdatePropertiesImmediately()
    {
        _forceUpdate = true;
    }


    public void UpdateProperty(string name)
    {
        lock (_events)
        {
            if (!_events.Contains(name))
            {
                _events.Add(name);
            }

            _eventRelease?.Cancel();
        }
    }
    
    public static string GetResourceETag(string path) => AppInstanceId + path.GetHashCode().ToString("x8");

    public override string ToString()
    {
        // ReSharper disable once UseStringInterpolation
        return string.Format("[{0}] {1} {2}", Id,
            ConnectedSince.ToShortDateString() + " " + ConnectedSince.ToShortTimeString(), SubDomain);
    }

    public event PropertyChangedEventHandler? PropertyChanged;


    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetParameters(Dictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            Parameters[parameter.Key] = parameter.Value;
        }
    }

    public void SetUser(string identityName, string identityId, string identityEMail)
    {
        UserIdentity = identityName;
        UserIdentityId = identityId;
        UserIdentityEMail = identityEMail;
        RequestLogin = false;
    }

    public void SetSessionCulture(CultureInfo culture)
    {
        SessionCulture = culture;
    }
        
        
    public void UserLogin()
    {
        SetUser(string.Empty, string.Empty, string.Empty);
        AuthorizeRedirectUrl = string.Empty;

        var o = HostOptions.UseKeycloakAuthentication;
        if (o == null) return;
            
        RequestLogin = true;
        AuthorizeRedirectUrl = $"{HostUrl}/index.html?stonehenge-id={Id}&ts={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
        var query = new QueryBuilder
        {
            { "client_id", o.ClientId },
            { "redirect_uri", AuthorizeRedirectUrl },
            { "response_type", "code" },
            { "scope", "openid" },
            { "nonce", Id },
            { "state", Id }
        };
        (ViewModel as ActiveViewModel)?.NavigateTo($"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/auth{query}");
    }

    public bool UserLogout()
    {
        if (HostOptions.UseKeycloakAuthentication == null) return false;

        if (string.IsNullOrEmpty(AuthorizeRedirectUrl) || string.IsNullOrEmpty(RefreshToken)) return false;

        var o = HostOptions.UseKeycloakAuthentication;

        using var client = new HttpClient();
        var data = $"client_id={o.ClientId}&state={Id}&&refresh_token={RefreshToken}&redirect_uri={HttpUtility.UrlEncode(AuthorizeRedirectUrl)}";
            
        var logoutUrl = $"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/logout";
        var result = client.PostAsync(logoutUrl,
                new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded"))
            .Result;

        var text = result.Content.ReadAsStringAsync().Result;
        Debug.WriteLine($"UserLogout {result.StatusCode} : {text}");
            
        SetUser(string.Empty, string.Empty, string.Empty);
        AuthorizeRedirectUrl = string.Empty;

        return result.StatusCode == HttpStatusCode.NoContent;
    }

    public void Dispose()
    {
        // _eventRelease?.Dispose();
        // _eventRelease = null;
        
        _pollSessionTimeout?.Dispose();
        _pollSessionTimeout = null;
        
        _terminator?.Dispose();
        _terminator = null;
        
        OnNavigate = null;
    }
}