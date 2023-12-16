using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Resources;

public class StonehengeResourceLoader : IStonehengeResourceProvider
{
    public readonly ILogger Logger;
        
    public List<IStonehengeResourceProvider> Providers { get; }
    public readonly ServiceContainer Services;

    public StonehengeResourceLoader(ILogger logger, List<IStonehengeResourceProvider> loaders)
    {
        Logger = logger;
        Providers = loaders;
        Services = new ServiceContainer();
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
        foreach (var provider in Providers)
        {
            provider.InitProvider(loader, options);
        }
    }

    public List<ViewModelInfo> GetViewModelInfos() => new List<ViewModelInfo>();

    public void Dispose()
    {
        Providers.ForEach(l => l.Dispose());
        Providers.Clear();
    }

    public async Task<Resource?> Get(AppSession? session, string resourceName, Dictionary<string, string> parameters)
    {
        var disableCache = false;

        if (resourceName.Contains("${") || resourceName.Contains("{{"))
        {
            resourceName = ReplaceFields(session, resourceName);
            disableCache = true;
        }

        Resource? loadedResource = null;
        foreach (var loader in Providers)
        {
            try
            {
                loadedResource = await loader.Get(session, resourceName, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError($"StonehengeResourceLoader.{loader.GetType().Name}({resourceName}) exception: {ex.Message}" + 
                                Environment.NewLine + ex.StackTrace);
                Debugger.Break();
            }
            if (loadedResource != null) break;
        }

        if (disableCache)
        {
            loadedResource?.SetCacheMode(Resource.Cache.None);
        }

        return loadedResource;
    }
        
    private string ReplaceFields(AppSession? session, string resourceName)
    {
        // support es6 format "${}"
        var replaced = string.Empty;
        while (resourceName.Length > 0)
        {
            var start = resourceName.IndexOf("${", StringComparison.InvariantCulture);
            var closing = 1;
            if (start == -1)
            {
                start = resourceName.IndexOf("{{", StringComparison.InvariantCulture);
                closing = 2;
            }
            if (start == -1)
            {
                replaced += resourceName;
                break;
            }
            replaced += resourceName.Substring(0, start);
            var field = resourceName.Substring(start + 2);
            resourceName = resourceName.Substring(start + 2);

            var end = field.IndexOf('}');
            field = field.Substring(0, end);

            if (session != null && session.Cookies.TryGetValue(field, out var fieldCookie))
            {
                replaced += fieldCookie;
            }

            resourceName = resourceName.Substring(end + closing);
        }
        return replaced;
    }

    public static StonehengeResourceLoader CreateDefaultLoader(ILogger logger, IStonehengeResourceProvider? provider)
    {
        return CreateDefaultLoader(logger, provider, Assembly.GetCallingAssembly());
    }
    public static StonehengeResourceLoader CreateDefaultLoader(ILogger logger, IStonehengeResourceProvider? provider, Assembly appAssembly)
    {
        var assemblies = new List<Assembly?>
            {
                appAssembly,
                Assembly.GetEntryAssembly(),
                Assembly.GetExecutingAssembly(),
                Assembly.GetAssembly(typeof(ResourceLoader))
            }
            .Where(a => a != null)
            .Distinct()
            .Cast<Assembly>()
            .ToList();

        var resLoader = new ResourceLoader(logger, assemblies, appAssembly);
        if (provider != null)
        {
            resLoader.AddAssembly(provider.GetType().Assembly);
        }

        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Directory.GetCurrentDirectory();
        var fileLoader = new FileLoader(logger, Path.Combine(path, "app"));

        var viewModelCreator = new ViewModelProvider(logger);

        var loader = new StonehengeResourceLoader(logger, new List<IStonehengeResourceProvider> { fileLoader, resLoader, viewModelCreator });
        if (provider != null)
        {
            loader.Providers.Add(provider);
        }
        return loader;
    }

    public void AddResourceAssembly(Assembly assembly)
    {
        var resourceLoader = Providers.FirstOrDefault(p => p.GetType() == typeof(ResourceLoader)) as ResourceLoader;
        resourceLoader?.AddAssembly(assembly);
    }

    public async Task<Resource?> Post(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Post(session, resourceName, parameters, formData);
            if (resource != null) return resource;
        }
        return null;
    }
    public async Task<Resource?> Put(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Put(session, resourceName, parameters, formData);
            if (resource != null) return resource;
        }
        return null;
    }
    public async Task<Resource?> Delete(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Delete(session, resourceName, parameters, formData);
            if (resource != null) return resource;
        }
        return null;
    }

}