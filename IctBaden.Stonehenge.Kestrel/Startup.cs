using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel.Middleware;
using IctBaden.Stonehenge.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Kestrel
{
    public class Startup : IStartup
    {
        private readonly string _appTitle;
        private readonly ILogger _logger;
        private readonly IStonehengeResourceProvider _resourceLoader;
        public readonly List<AppSession> AppSessions = new();
        private readonly StonehengeHostOptions? _options;

        // ReSharper disable once UnusedMember.Global
        public Startup(ILogger logger, IConfiguration configuration, IStonehengeResourceProvider resourceLoader)
        {
            Configuration = configuration;
            _logger = logger;
            _resourceLoader = resourceLoader;
            _appTitle = Configuration["AppTitle"];
            _options = JsonSerializer.Deserialize<StonehengeHostOptions>(Configuration["HostOptions"]);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // ReSharper disable once UnusedMember.Global
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddCors();
            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<ServerExceptionLogger>();
            app.UseMiddleware<StonehengeAcme>();
            app.Use((context, next) =>
            {
                context.Items.Add("stonehenge.Logger", _logger);
                context.Items.Add("stonehenge.AppTitle", _appTitle);
                context.Items.Add("stonehenge.HostOptions", _options);
                context.Items.Add("stonehenge.ResourceLoader", _resourceLoader);
                context.Items.Add("stonehenge.AppSessions", AppSessions);
                return next.Invoke();
            });
            if (_options?.CustomMiddleware != null)
            {
                foreach (var cm in _options.CustomMiddleware)
                {
                    var cmType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(type => type.Name == cm);
                    if (cmType != null)
                    {
                        app.UseMiddleware(cmType);
                    }    
                }
            }
            app.UseResponseCompression();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
            app.UseMiddleware<StonehengeSession>();
            app.UseMiddleware<StonehengeHeaders>();
            app.UseMiddleware<StonehengeRoot>();
            
            app.UseMiddleware<ServerSentEvents>();
            app.UseMiddleware<StonehengeContent>();
        }
    }
}
