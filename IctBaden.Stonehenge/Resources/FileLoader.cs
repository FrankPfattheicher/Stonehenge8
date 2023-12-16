using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.Resources;

public class FileLoader : IStonehengeResourceProvider
{
    private readonly ILogger _logger;
    public string RootPath { get; private set; }

    public FileLoader(ILogger logger, string path)
    {
        _logger = logger;
        RootPath = path;
    }
        
    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public List<ViewModelInfo> GetViewModelInfos() => new List<ViewModelInfo>();

    public void Dispose()
    {
    }

    public Task<Resource?> Get(AppSession? session, string resourceName, Dictionary<string, string> parameters)
    {
        var fullFileName = Path.Combine(RootPath, resourceName);
        if(!File.Exists(fullFileName)) return Task.FromResult<Resource?>(null);

        var resourceExtension = Path.GetExtension(resourceName);
        var resourceType = ResourceType.GetByExtension(resourceExtension);

        _logger.LogTrace($"FileLoader({resourceName}): {fullFileName}");
        return Task.FromResult<Resource?>(resourceType.IsBinary 
            ? new Resource(resourceName, "file://" + fullFileName, resourceType, File.ReadAllBytes(fullFileName), Resource.Cache.OneDay) 
            : new Resource(resourceName, "file://" + fullFileName, resourceType, File.ReadAllText(fullFileName), Resource.Cache.OneDay));
    }

    public Task<Resource?> Post(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Put(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Delete(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => Task.FromResult<Resource?>(null);
}