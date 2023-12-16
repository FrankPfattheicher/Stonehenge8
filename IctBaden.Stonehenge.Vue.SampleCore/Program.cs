using System;
using System.Diagnostics;
using System.Threading;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Extension;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.SampleCore
{
    internal static class Program
    {
        private static IStonehengeHost? _server;

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly ILoggerFactory LoggerFactory = StonehengeLogger.DefaultFactory;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            StonehengeLogger.DefaultLevel = LogLevel.Trace;
            var logger = LoggerFactory.CreateLogger("stonehenge");

            Console.WriteLine(@"");
            Console.WriteLine(@"Stonehenge 4 Sample");
            Console.WriteLine(@"");
            logger.LogInformation("Vue.SampleCore started");

            // ReSharper disable once RedundantAssignment
            KeycloakAuthenticationOptions? keycloak = null;
            keycloak = new KeycloakAuthenticationOptions
            {
                ClientId = "frontend",
                Realm = "liva-pms",
                AuthUrl = "https://auth.liva-cloud.com"
            };

            // select hosting options
            var options = new StonehengeHostOptions
            {
                Title = "VueSample",

                ServerPushMode = ServerPushModes.ServerSentEvents,
                PollIntervalSec = 10,
                HandleWindowResized = true,
                CustomMiddleware = new []{ nameof(StonehengeRawContent) },
                UseClientLocale = true,
                UseKeycloakAuthentication = keycloak
                // SslCertificatePath = Path.Combine(StonehengeApplication.BaseDirectory, "stonehenge.pfx"),
                // SslCertificatePassword = "test"
            };

            // Select client framework
            Console.WriteLine(@"Using client framework vue");
            var vue = new VueResourceProvider(logger);
            var loader = StonehengeResourceLoader.CreateDefaultLoader(logger, vue);
            loader.AddResourceAssembly(typeof(TreeView).Assembly);
            loader.AddResourceAssembly(typeof(ChartsC3).Assembly);
            loader.AddResourceAssembly(typeof(AppDialog).Assembly);
            loader.AddResourceAssembly(typeof(DropEdit.DropEdit).Assembly);
            loader.AddResourceAssembly(typeof(Mermaid).Assembly);
            loader.Services.AddService(typeof(ILogger), logger);
            
            // Select hosting technology
            Console.WriteLine(@"Using Kestrel hosting");
            _server = new KestrelHost(loader, options);

            Console.WriteLine(@"Starting server");
            var terminate = new AutoResetEvent(false);
            Console.CancelKeyPress += (_, _) => { terminate.Set(); };

            var host = Environment.CommandLine.Contains("/localhost") ? "localhost" : "*";
            if (_server.Start(host, 32000))
            {
                Console.WriteLine(@"Server reachable on: " + _server.BaseUrl);

                if (Environment.CommandLine.Contains("/window"))
                {
                    using var wnd = new HostWindow(_server.BaseUrl, options.Title);
                    if (!wnd.Open())
                    {
                        logger.LogError("Failed to open main window");
                        terminate.Set();
                    }
                }
                else
                {
                    terminate.WaitOne();
                }

                Console.WriteLine(@"Server terminated.");
            }
            else
            {
                Console.WriteLine(@"Failed to start server on: " + _server.BaseUrl);
            }

#pragma warning disable 0162
            // ReSharper disable once HeuristicUnreachableCode
            _server.Terminate();

            Console.WriteLine(@"Exit sample app");
            Environment.Exit(0);
        }
    }
}