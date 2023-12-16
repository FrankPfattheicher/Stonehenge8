using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;

namespace IctBaden.Stonehenge.Resources;

public interface IStonehengeResourceProvider : IDisposable
{
    void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options);
       
    List<ViewModelInfo> GetViewModelInfos();

    Task<Resource?> Get(AppSession? session, string resourceName, Dictionary<string, string> parameters);
    Task<Resource?> Post(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData);
    Task<Resource?> Put(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData);
    Task<Resource?> Delete(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData);
}