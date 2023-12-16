using System;
using System.Threading;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CA2254

namespace IctBaden.Stonehenge.App;

// ReSharper disable once UnusedType.Global
public class StonehengeUiWindow : IDisposable
{
    private readonly ILogger _logger;
    private readonly StonehengeHostOptions _options;
    private readonly StonehengeUi _ui;
    private HostWindow? _wnd;

    public StonehengeUiWindow(ILogger logger, StonehengeHostOptions options)
    {
        _logger = logger;
        _options = options;
        _ui = new StonehengeUi(logger, options);
    }
    
    /// <summary>
    /// Start window process with
    /// random free port, not public reachable
    /// and default size 800 x 600
    /// </summary>
    /// <returns>false if failed to start window process</returns>
    public bool Start() => Start(0, false, new Point(800, 600));

    /// <summary>
    /// Start window process
    /// does not return until window is closed or Ctrl+C pressed 
    /// </summary>
    /// <param name="port">port to listen on</param>
    /// <param name="publicReachable">enables public reachable hosting</param>
    /// <param name="windowSize">definition of initial window size</param>
    /// <returns>false if failed to start window process</returns>
    public bool Start(int port, bool publicReachable, Point windowSize)
    {
        if (!_ui.Start(port, publicReachable))
        {
            _logger.LogCritical($"StonehengeUiWindow failed to start on port {port}, ({(publicReachable ? "" : "not ")}public reachable)");
            return false;
        }
        if (string.IsNullOrEmpty(_ui.Server?.BaseUrl))
        {
            _logger.LogCritical($"StonehengeUiWindow failed to start: No base URL given");
            return false;
        }

        _wnd = new HostWindow(_ui.Server?.BaseUrl!, _options.Title, windowSize);
        if(!_wnd.Open()) return false;
        
        var terminate = new AutoResetEvent(false);
        Console.CancelKeyPress += (_, _) => { terminate.Set(); };
        terminate.WaitOne();
        return true;
    }
    
    public void Dispose()
    {
        _wnd?.Dispose();
        _ui.Dispose();
    }
}
