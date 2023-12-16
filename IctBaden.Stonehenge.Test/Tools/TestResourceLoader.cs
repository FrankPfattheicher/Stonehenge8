using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;

namespace IctBaden.Stonehenge.Test.Tools;

public class TestResourceLoader : IStonehengeResourceProvider
{
    private readonly string _content;

    public TestResourceLoader(string content)
    {
        _content = content;
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public List<ViewModelInfo> GetViewModelInfos() => new();

    public void Dispose()
    {
    }

    public Task<Resource?> Post(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        var data = parameters;
        data.Add("method", "POST");
        var json = JsonSerializer.Serialize(data);
        return Task.FromResult<Resource?>(new Resource(resourceName, "test://TestResourceLoader.POST", ResourceType.Json, json, Resource.Cache.None));
    }

    public Task<Resource?> Put(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) 
    {
        var data = parameters;
        data.Add("method", "PUT");
        var json = JsonSerializer.Serialize(data);
        return Task.FromResult<Resource?>(new Resource(resourceName, "test://TestResourceLoader.POST", ResourceType.Json, json, Resource.Cache.None));
    }

    public Task<Resource?> Delete(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        var data = parameters;
        data.Add("method", "DELETE");
        var json = JsonSerializer.Serialize(data);
        return Task.FromResult<Resource?>(new Resource(resourceName, "test://TestResourceLoader.POST", ResourceType.Json, json, Resource.Cache.None));
    }

    public Task<Resource?> Get(AppSession? session, string resourceName, Dictionary<string, string> parameters)
    {
        var resourceExtension = Path.GetExtension(resourceName);
        return Task.FromResult<Resource?>(new Resource(resourceName, "test://TestResourceLoader.content", ResourceType.GetByExtension(resourceExtension), _content, Resource.Cache.None));
    }
}