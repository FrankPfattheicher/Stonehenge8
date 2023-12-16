using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.ViewModel;

public class ViewModelProvider : IStonehengeResourceProvider
{
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DoubleConverter() }
    };

    public ViewModelProvider(ILogger logger)
    {
        _logger = logger;
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public List<ViewModelInfo> GetViewModelInfos() => new List<ViewModelInfo>();

    public void Dispose()
    {
    }

    public Task<Resource?> Put(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) =>
        Task.FromResult<Resource?>(null);

    public Task<Resource?> Delete(AppSession? session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) =>
        Task.FromResult<Resource?>(null);

    public Task<Resource?> Post(AppSession? session, string resourceName,
        Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        if (resourceName.StartsWith("Command/"))
        {
            var commandName = resourceName.Substring(8);
            var appCommandsType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(type => type.GetInterfaces().Contains(typeof(IStonehengeAppCommands)));
            if (appCommandsType != null)
            {
                var appCommands = session?.CreateType("AppCommands", appCommandsType);

                var commandHandler = appCommands?.GetType().GetMethod(commandName);
                if (commandHandler != null)
                {
                    var cmdParameters = commandHandler.GetParameters()
                        .Select(parameter => parameter.ParameterType == typeof(AppSession)
                            ? session
                            : Convert.ChangeType(parameters.FirstOrDefault(kv => kv.Key == parameter.Name).Value,
                                parameter.ParameterType, CultureInfo.InvariantCulture));

                    commandHandler.Invoke(appCommands, cmdParameters.ToArray());

                    return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json, "{ 'executed': true }",
                        Resource.Cache.None));
                }

                return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json, "{ 'executed': false }",
                    Resource.Cache.None));
            }

            return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json, "{ 'executed': false }",
                Resource.Cache.None));
        }

        if (resourceName.StartsWith("Data/"))
        {
            return PostDataResource(session, resourceName.Substring(5), parameters, formData);
        }

        if (!resourceName.StartsWith("ViewModel/")) return Task.FromResult<Resource?>(null);

        var parts = resourceName.Split('/');
        if (parts.Length != 3) return Task.FromResult<Resource?>(null);

        var vmTypeName = parts[1];
        var methodName = parts[2];

        if (session?.ViewModel == null)
        {
            _logger.LogWarning($"ViewModelProvider: Set VM={vmTypeName}, no current VM");
            session?.SetViewModelType(vmTypeName);
        }

        foreach (var (key, value) in formData)
        {
            _logger.LogDebug($"ViewModelProvider: Set {key}={value}");
            SetPropertyValue(_logger, session?.ViewModel, key, value);
        }

        var vmType = session?.ViewModel?.GetType();
        if (vmType?.Name != vmTypeName)
        {
            _logger.LogWarning($"ViewModelProvider: Request for VM={vmTypeName}, current VM={vmType?.Name}");
            return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
                "{ \"StonehengeContinuePolling\":false }", Resource.Cache.None));
        }

        var method = vmType.GetMethod(methodName);
        if (method == null)
        {
            _logger.LogWarning($"ViewModelProvider: ActionMethod {methodName} not found.");
            return Task.FromResult<Resource?>(null);
        }

        try
        {
            var attribute = method
                .GetCustomAttributes(typeof(ActionMethodAttribute), true)
                .FirstOrDefault() as ActionMethodAttribute;
            var executeAsync = attribute?.ExecuteAsync ?? false;
            var methodParams = method.GetParameters()
                .Zip(
                    parameters.Values,
                    (parameterInfo, postParam) =>
                        new KeyValuePair<Type, object>(parameterInfo.ParameterType, postParam))
                .Select(parameterPair =>
                    Convert.ChangeType(parameterPair.Value, parameterPair.Key, CultureInfo.InvariantCulture))
                .ToArray();
            if (executeAsync)
            {
                Task.Run(() => method.Invoke(session?.ViewModel, methodParams));
                return GetEvents(session, resourceName);
            }

            method.Invoke(session?.ViewModel, methodParams);
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null) ex = ex.InnerException;

            _logger.LogError(
                $"ViewModelProvider: ActionMethod {methodName} has {method.GetParameters().Length} params.");
            _logger.LogError($"ViewModelProvider: Called with {parameters.Count} params.");
            _logger.LogError("ViewModelProvider: " + ex.Message);
            _logger.LogError("ViewModelProvider: " + ex.StackTrace);

            Debugger.Break();

            var exResource = new Dictionary<string, string>
            {
                { "Message", ex.Message },
                { "StackTrace", ex.StackTrace ?? string.Empty }
            };
            return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json, GetViewModelJson(exResource),
                Resource.Cache.None));
        }

        return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
            GetViewModelJson(session?.ViewModel), Resource.Cache.None));
    }

    public Task<Resource?> Get(AppSession? session, string resourceName, Dictionary<string, string> parameters)
    {
        if (resourceName.StartsWith("ViewModel/"))
        {
            if (session != null && SetViewModel(session, resourceName))
            {
                if (session.ViewModel is ActiveViewModel avm)
                {
                    avm.OnLoad();
                }

                return GetViewModel(session, resourceName);
            }
        }
        else if (resourceName.StartsWith("Events/"))
        {
            return GetEvents(session, resourceName);
        }
        else if (session != null && resourceName.StartsWith("Data/"))
        {
            return GetDataResource(session, resourceName.Substring(5), parameters);
        }

        return Task.FromResult<Resource?>(null);
    }

    private bool SetViewModel(AppSession? session, string resourceName)
    {
        if (session == null) return false;
        var vmTypeName = Path.GetFileNameWithoutExtension(resourceName);
        if (session.ViewModel != null)
        {
            ClearStonehengeInternalProperties(session.ViewModel as ActiveViewModel);
            if (session.ViewModel.GetType().Name == vmTypeName)
            {
                return true;
            }
        }
        if (session.SetViewModelType(vmTypeName) != null)
        {
            return true;
        }

        _logger.LogError("Could not set ViewModel type to " + vmTypeName);
        return false;
    }

    private Task<Resource?> GetViewModel(AppSession session, string resourceName)
    {
        session.EventsClear(true);

        return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
            GetViewModelJson(session.ViewModel),
            Resource.Cache.None));
    }

    private static async Task<Resource?> GetEvents(AppSession? session, string resourceName)
    {
        var parts = resourceName.Split('/');
        if (parts.Length < 2) return null;

        var vmTypeName = parts[1];
        var vmType = session?.ViewModel?.GetType();

        string json;
        if (vmTypeName != vmType?.Name)
        {
            // view model changed !
            json = "{ \"StonehengeContinuePolling\":false";
            if (session == null)
            {
                json += ", \"StonehengeEval\":\"window.location.reload();\"";    
            }
            json += " }";
            return new Resource(resourceName, "ViewModelProvider", ResourceType.Json, json, Resource.Cache.None);
        }

        var data = new List<string> { "\"StonehengeContinuePolling\":true" };
        var events = session == null
            ? Array.Empty<string>()  
            : await session.CollectEvents();
        if (session?.ViewModel is ActiveViewModel activeVm)
        {
            try
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var property in events)
                {
                    var value = activeVm.TryGetMember(property);
                    data.Add(
                        $"\"{property}\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions))}");
                }

                AddStonehengeInternalProperties(data, activeVm);
            }
            catch
            {
                // ignore for events
            }
        }

        json = "{" + string.Join(",", data) + "}";
        return new Resource(resourceName, "ViewModelProvider", ResourceType.Json, json, Resource.Cache.None);
    }

    private static void ClearStonehengeInternalProperties(ActiveViewModel? activeVm)
    {
        if (activeVm == null) return;
            
        // clear only if navigation happens
        activeVm.NavigateToRoute = string.Empty;
    }
        
    private static void AddStonehengeInternalProperties(ICollection<string> data, ActiveViewModel activeVm)
    {
        if (!string.IsNullOrEmpty(activeVm.MessageBoxTitle) || !string.IsNullOrEmpty(activeVm.MessageBoxText))
        {
            var title = activeVm.MessageBoxTitle;
            var text = activeVm.MessageBoxText;
            var script = $"alert('{HttpUtility.JavaScriptStringEncode(title)}\\r\\n{HttpUtility.JavaScriptStringEncode(text)}');";
            data.Add($"\"StonehengeEval\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(script, JsonOptions))}");
            activeVm.MessageBoxTitle = string.Empty;
            activeVm.MessageBoxText = string.Empty;
        }

        if (!string.IsNullOrEmpty(activeVm.ClientScript))
        {
            var script = activeVm.ClientScript;
            data.Add($"\"StonehengeEval\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(script, JsonOptions))}");
            activeVm.ClientScript = string.Empty;
        }

        if (!string.IsNullOrEmpty(activeVm.NavigateToRoute))
        {
            var route = activeVm.NavigateToRoute;
            data.Add($"\"StonehengeNavigate\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(route, JsonOptions))}");
        }
    }

    private static Task<Resource?> GetDataResource(AppSession session, string resourceName,
        Dictionary<string, string> parameters)
    {
        var vm = session.ViewModel as ActiveViewModel;
        var method = vm?.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
                string.Compare(m.Name, "GetDataResource", StringComparison.InvariantCultureIgnoreCase) == 0);
        if (method == null || method.ReturnType != typeof(Resource)) return Task.FromResult<Resource?>(null);

        Resource? data;
        if (method.GetParameters().Length == 2)
        {
            data = (Resource?)method.Invoke(vm, new object[] { resourceName, parameters });
        }
        else
        {
            data = (Resource?)method.Invoke(vm, new object[] { resourceName });
        }

        return Task.FromResult(data);
    }

    private static Task<Resource?> PostDataResource(AppSession? session, string resourceName,
        Dictionary<string, string> parameters, Dictionary<string, string> formData)
    {
        var vm = session?.ViewModel as ActiveViewModel;
        var method = vm?.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
                string.Compare(m.Name, "PostDataResource", StringComparison.InvariantCultureIgnoreCase) == 0);
        if (method == null || method.ReturnType != typeof(Resource)) return Task.FromResult<Resource?>(null);

        Resource? data;
        if (method.GetParameters().Length == 3)
        {
            data = (Resource?)method.Invoke(vm, new object[] { resourceName, parameters, formData });
        }
        else if (method.GetParameters().Length == 2)
        {
            data = (Resource?)method.Invoke(vm, new object[] { resourceName, parameters });
        }
        else
        {
            data = (Resource?)method.Invoke(vm, new object[] { resourceName });
        }

        return Task.FromResult(data);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void DeserializeStructValue(ILogger logger, ref object? structObj, 
        string? structValue, Type structType)
    {
        try
        {
            if (string.IsNullOrEmpty(structValue))
            {
                structObj = null;
                return;
            }

            if (structValue.StartsWith("["))
            {
                var arrayObjects = JsonSerializer.Deserialize<JsonObject[]>(structValue);
                if (arrayObjects == null)
                {
                    structObj = null;
                    return;
                }

                var elementType = structType.GenericTypeArguments.FirstOrDefault();
                if (elementType == null)
                {
                    structObj = null;
                    return;
                }

                var addMethod = structObj?.GetType().GetMethod("Add");
                if (addMethod == null)
                {
                    structObj = null;
                    return;
                }

                foreach (var member in arrayObjects)
                {
                    var element = Activator.CreateInstance(elementType);
                    if (element == null) continue;

                    if (member is { } objMembers)
                    {
                        SetMembers(logger, ref element, elementType, objMembers);
                    }

                    addMethod.Invoke(structObj, new[] { element });
                }

                return;
            }

            if (JsonSerializer.Deserialize<JsonObject>(structValue) is { } members)
            {
                SetMembers(logger, ref structObj, structType, members);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("DeserializeStructValue: " + ex.Message);
            Debugger.Break();
        }
    }

    private static void SetMembers(ILogger logger, ref object? structObj, Type structType, JsonObject members)
    {
        foreach (var member in members)
        {
            var mProp = structType.GetProperty(member.Key);
            if (mProp != null && member.Value != null)
            {
                var val = DeserializePropertyValue(logger, member.Value.ToString(), mProp.PropertyType);
                mProp.SetValue(structObj, val, null);
            }
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static object? DeserializePropertyValue(ILogger logger, string? propValue, Type propType)
    {
        try
        {
            if (propType == typeof(string))
                return propValue;
            if (propType == typeof(bool) && !string.IsNullOrEmpty(propValue))
                return bool.Parse(propValue);
            if (propType == typeof(float))
            {
                if (float.TryParse(propValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var fVal))
                    return fVal;
                if (float.TryParse(propValue, NumberStyles.Float, CultureInfo.InvariantCulture, out fVal))
                    return fVal;
            }

            if (propType == typeof(double))
            {
                if (double.TryParse(propValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var dVal))
                    return dVal;
                if (double.TryParse(propValue, NumberStyles.Float, CultureInfo.InvariantCulture, out dVal))
                    return dVal;
            }

            if (propType == typeof(DateTime))
            {
                if (DateTime.TryParse(propValue, out var dt))
                    return dt;
                if (DateTime.TryParse(propValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;
            }

            if (propType == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(propValue, out var dt))
                    return dt;
                if (DateTimeOffset.TryParse(propValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;
            }

            if (propType is { IsClass: true, IsArray: false })
            {
                var structObj = Activator.CreateInstance(propType);
                if (structObj != null)
                {
                    DeserializeStructValue(logger, ref structObj, propValue, propType);
                }

                return structObj;
            }

            if (propValue != null)
            {
                return JsonSerializer.Deserialize(propValue, propType);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("DeserializePropertyValue: " + ex.Message);
            Debugger.Break();
        }

        return null;
    }

    private static void SetPropertyValue(ILogger logger, object? vm, string propName, string newValue)
    {
        try
        {
            if (vm is ActiveViewModel activeVm)
            {
                var pi = activeVm.GetPropertyInfo(propName);
                if ((pi == null) || !pi.CanWrite)
                    return;

                if (pi.PropertyType is { IsValueType: true, IsPrimitive: false } &&
                    (pi.PropertyType.Namespace != "System")) // struct
                {
                    var structObj = activeVm.TryGetMember(propName);
                    if (structObj != null && !string.IsNullOrEmpty(newValue) && newValue.Trim().StartsWith("{"))
                    {
                        DeserializeStructValue(logger, ref structObj, newValue, pi.PropertyType);
                        activeVm.TrySetMember(propName, structObj);
                    }
                }
                else if (pi.PropertyType.IsGenericType && pi.PropertyType.Name.StartsWith("Notify`"))
                {
                    var val = DeserializePropertyValue(logger, newValue, pi.PropertyType.GenericTypeArguments[0]);
                    var type = typeof(Notify<>).MakeGenericType(pi.PropertyType.GenericTypeArguments[0]);
                    var notify = Activator.CreateInstance(type, new[] { activeVm, pi.Name, val });
                    var valueField = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
                    valueField?.SetValue(notify, val);
                    activeVm.TrySetMember(propName, notify);
                }
                else
                {
                    var val = DeserializePropertyValue(logger, newValue, pi.PropertyType);
                    activeVm.TrySetMember(propName, val);
                }
            }
            else
            {
                var pi = vm?.GetType().GetProperty(propName);
                if ((pi == null) || !pi.CanWrite)
                    return;

                var val = DeserializePropertyValue(logger, newValue, pi.PropertyType);
                pi.SetValue(vm, val, null);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"SetPropertyValue({propName}): " + ex.Message);
            Debugger.Break();
        }
    }

    private string GetViewModelJson(object? viewModel)
    {
        if (viewModel == null) return string.Empty;
        
        var watch = new Stopwatch();
        watch.Start();

        var ty = viewModel.GetType();
        _logger.LogDebug("ViewModelProvider: ViewModel=" + ty.Name);

        var data = new List<string>();
        var context = "";
        try
        {
            // ensure view model data available before executing client scripts
            context = "view model";
            var vm = JsonSerializer.SerializeToDocument(viewModel, JsonOptions);
            foreach (var jsonElement in vm.RootElement.EnumerateObject())
            {
                data.Add(jsonElement.ToString());
            }

            if (viewModel is ActiveViewModel activeVm)
            {
                context = "internal properties";
                AddStonehengeInternalProperties(data, activeVm);

                context = "dictionary names";
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var name in activeVm.GetDictionaryNames())
                {
                    // ReSharper disable once UseStringInterpolation
                    data.Add(string.Format("\"{0}\":{1}", name,
                        JsonSerializer.SerializeToElement(activeVm.TryGetMember(name), JsonOptions)));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception serializing ViewModel({ty.Name}) : {context}");
            _logger.LogError(ex.Message);
            _logger.LogError(ex.StackTrace);

            Debugger.Break();

            var exResource = new Dictionary<string, string>
            {
                { "Message", ex.Message },
                { "StackTrace", ex.StackTrace ?? string.Empty }
            };
            return JsonSerializer.SerializeToElement(exResource, JsonOptions).ToString();
        }

        var json = "{" + string.Join(",", data) + "}";

        watch.Stop();
        _logger.LogTrace($"GetViewModelJson: {watch.ElapsedMilliseconds}ms");
        return json;
    }
}