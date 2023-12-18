// ActiveViewModel.cs
//
// Author:
//  Frank Pfattheicher <fpf@ict-baden.de>
//
// Copyright (C)2011-2022 ICT Baden GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable VirtualMemberNeverOverridden.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.ViewModel;

public class ActiveViewModel : DynamicObject, ICustomTypeDescriptor, INotifyPropertyChanged, IDisposable
{
    #region helper classes

    class GetMemberBinderEx : GetMemberBinder
    {
        private GetMemberBinderEx(string name) : base(name, false)
        {
        }

        public static GetMemberBinderEx CreateInstance(string name)
        {
            return new GetMemberBinderEx(name);
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target,
            DynamicMetaObject? errorSuggestion)
        {
            return null!;
        }
    }

    class SetMemberBinderEx(string name) : SetMemberBinder(name, false)
    {
        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject? errorSuggestion)
        {
            return null!;
        }
    }

    class PropertyDescriptorEx : PropertyDescriptor
    {
        private readonly string _propertyName;
        private readonly PropertyDescriptor? _originalDescriptor;
        private readonly bool _readOnly;

        internal PropertyDescriptorEx(string name, PropertyInfo? info, bool readOnly)
            : base(name, null)
        {
            _propertyName = name;
            _originalDescriptor = FindOrigPropertyDescriptor(info);
            _readOnly = readOnly;
        }

        public override AttributeCollection Attributes => _originalDescriptor?.Attributes ?? base.Attributes;

        public override object? GetValue(object? component)
        {
            if (!(component is DynamicObject dynComponent))
                return _originalDescriptor?.GetValue(component);

            return dynComponent.TryGetMember(GetMemberBinderEx.CreateInstance(_propertyName), out var result)
                ? result
                : _originalDescriptor?.GetValue(component);
        }

        public override void SetValue(object? component, object? value)
        {
            if (component is DynamicObject dynComponent)
            {
                if (dynComponent.TrySetMember(new SetMemberBinderEx(_propertyName), value))
                    return;
            }

            _originalDescriptor?.SetValue(component, value);
        }

        public override bool IsReadOnly => _readOnly || (_originalDescriptor is { IsReadOnly: true });

        public override Type PropertyType
            => _originalDescriptor == null ? typeof(object) : _originalDescriptor.PropertyType;

        public override bool CanResetValue(object component) 
            => _originalDescriptor != null && _originalDescriptor.CanResetValue(component);

        public override Type ComponentType
            => _originalDescriptor == null ? typeof(object) : _originalDescriptor.ComponentType;

        public override void ResetValue(object component) 
            => _originalDescriptor?.ResetValue(component);

        public override bool ShouldSerializeValue(object component) 
            => _originalDescriptor != null && _originalDescriptor.ShouldSerializeValue(component);

        private static PropertyDescriptor? FindOrigPropertyDescriptor(PropertyInfo? propertyInfo)
        {
            return propertyInfo == null || propertyInfo.DeclaringType == null
                ? null
                : TypeDescriptor.GetProperties(propertyInfo.DeclaringType)
                    .Cast<PropertyDescriptor>()
                    .FirstOrDefault(propertyDescriptor => propertyDescriptor.Name.Equals(propertyInfo.Name));
        }
    }

    class PropertyInfoEx(PropertyInfo pi, object obj, bool readOnly)
    {
        public PropertyInfo Info { get; private set; } = pi;
        public object Obj { get; private set; } = obj;
        public bool ReadOnly { get; private set; } = readOnly;
    }

    #endregion

    #region properties

    private readonly Dictionary<string, List<string>> _dependencies = new();
    private readonly Dictionary<string, object?> _dictionary = new();

    [Browsable(false)] internal int Count => GetProperties().Count;

    [Browsable(false)] public AppSession Session;
    [Browsable(false)] public bool SupportsEvents;

    // ReSharper disable InconsistentNaming
    [Bindable(false)] public string _stonehenge_CommandSenderName_ { get; set; } = string.Empty;

    private Timer? _updateTimer;

    public string GetCommandSenderName()
    {
        return _stonehenge_CommandSenderName_;
    }

    #endregion

    public ActiveViewModel()
        : this(null)
    {
    }

    public ActiveViewModel(AppSession? session)
    {
        SupportsEvents = (session != null);
        Session = session ?? new AppSession();

        foreach (var prop in GetType().GetProperties())
        {
            if (prop.PropertyType.IsGenericType && prop.PropertyType.Name.StartsWith("Notify`"))
            {
                var type = typeof(Notify<>).MakeGenericType(prop.PropertyType.GenericTypeArguments[0]);
                var property = prop.GetValue(this);
                if (property != null) continue;
                var ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .First(c => c.GetParameters().Length == 2);
                property = ctor.Invoke(new object[] { this, prop.Name });
                prop.SetValue(this, property);
            }
        }

        GetProperties();
    }

    /// <summary>
    /// Called when application navigates to this view model.
    /// This is an equivalent to a client site onload event.
    /// </summary>
    public virtual void OnLoad()
    {
    }

    protected void SetParent(ActiveViewModel parent)
    {
        PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.PropertyName))
            {
                parent.NotifyPropertyChanged(args.PropertyName);
            }
        };
    }

    public object? TryGetMember(string name)
    {
        TryGetMember(GetMemberBinderEx.CreateInstance(name), out var result);
        return result;
    }

    public void TrySetMember(string name, object? value)
    {
        TrySetMember(new SetMemberBinderEx(name), value);
    }

    [Browsable(false)]
    protected object? this[string name]
    {
        get => TryGetMember(name);
        set => TrySetMember(name, value);
    }

    #region DynamicObject

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        var names = new List<string>();
        names.AddRange(from elem in _dictionary select elem.Key);
        return names;
    }

    public IEnumerable<string> GetDictionaryNames()
    {
        return _dictionary.Select(e => e.Key);
    }

    private PropertyInfoEx? GetPropertyInfoEx(string name)
    {
        var pi = GetType().GetProperty(name);
        return pi != null ? new PropertyInfoEx(pi, this, false) : null;
    }

    public PropertyInfo? GetPropertyInfo(string name)
    {
        var infoEx = GetPropertyInfoEx(name);
        return infoEx?.Info;
    }

    public bool IsPropertyReadOnly(string name)
    {
        var infoEx = GetPropertyInfoEx(name);
        return (infoEx == null) || infoEx.ReadOnly;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var pi = GetPropertyInfoEx(binder.Name);
        if (pi != null)
        {
            var val = pi.Info.GetValue(pi.Obj, null);
            result = val;
            return true;
        }

        return _dictionary.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var pi = GetPropertyInfoEx(binder.Name);
        if (pi != null)
        {
            pi.Info.SetValue(pi.Obj, value, null);
            NotifyPropertyChanged(binder.Name);
            return true;
        }

        _dictionary[binder.Name] = value;
        NotifyPropertyChanged(binder.Name);
        return true;
    }

    #endregion

    #region ICustomTypeDescriptor

    public AttributeCollection GetAttributes()
    {
        return TypeDescriptor.GetAttributes(this, true);
    }

    public string GetClassName()
    {
        return TypeDescriptor.GetClassName(this, true) ?? string.Empty;
    }

    public string GetComponentName()
    {
        return TypeDescriptor.GetComponentName(this, true) ?? string.Empty;
    }

    public TypeConverter GetConverter()
    {
        return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptor? GetDefaultEvent()
    {
        return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public PropertyDescriptor? GetDefaultProperty()
    {
        return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public object? GetEditor(Type editorBaseType)
    {
        return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[]? attributes)
    {
        return TypeDescriptor.GetEvents(this, attributes, true);
    }

    public EventDescriptorCollection GetEvents()
    {
        return TypeDescriptor.GetEvents(this, true);
    }

    private PropertyDescriptorCollection? properties;

    public PropertyDescriptorCollection GetProperties()
    {
        if (properties != null)
            return properties;

        properties = new PropertyDescriptorCollection(Array.Empty<PropertyDescriptor>());
        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, true))
        {
            var pi = GetType().GetProperty(prop.Name);
            var desc = new PropertyDescriptorEx(prop.Name, pi, false);
            properties.Add(desc);
        }

        foreach (var elem in _dictionary)
        {
            var desc = new PropertyDescriptorEx(elem.Key, null, false);
            properties.Add(desc);
        }

        foreach (PropertyDescriptorEx prop in properties)
        {
            foreach (Attribute attribute in prop.Attributes)
            {
                if (attribute.GetType() != typeof(DependsOnAttribute))
                    continue;
                var da = (DependsOnAttribute)attribute;
                if (!_dependencies.ContainsKey(da.Name))
                    _dependencies[da.Name] = new List<string>();

                _dependencies[da.Name].Add(prop.Name);
            }
        }

        var myMethods =
            GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                 BindingFlags.Instance);
        foreach (var method in myMethods)
        {
            var dependsOnAttributes = method.GetCustomAttributes(typeof(DependsOnAttribute), true);
            foreach (DependsOnAttribute attribute in dependsOnAttributes)
            {
                if (!_dependencies.ContainsKey(attribute.Name))
                    _dependencies[attribute.Name] = new List<string>();

                _dependencies[attribute.Name].Add(method.Name);
            }
        }

        return properties;
    }

    private PropertyDescriptorCollection? propertiesAttribute;

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        if (propertiesAttribute != null)
            return propertiesAttribute;

        propertiesAttribute = new PropertyDescriptorCollection(Array.Empty<PropertyDescriptor>());
        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, true))
            propertiesAttribute.Add(prop);
        return propertiesAttribute;
    }

    public object GetPropertyOwner(PropertyDescriptor? pd)
    {
        return this;
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ExecuteHandler(PropertyChangedEventHandler handler, string name)
    {
        var args = new PropertyChangedEventArgs(name);
        handler(this, args);
    }

    protected internal void NotifyPropertyChanged(string name)
    {
#if DEBUG
        //TODO: AppService.PropertyNameId
        Debug.Assert(name.StartsWith("_stonehenge_")
                     || (GetPropertyInfo(name) != null)
                     || _dictionary.ContainsKey(name)
            , "NotifyPropertyChanged for unknown property " + name);
#endif
        Task.Run(() => SendPropertyChanged(name)).Wait();
        
        var handler = PropertyChanged;
        if (handler != null)
        {
            ExecuteHandler(handler, name);
        }

        if (!_dependencies.ContainsKey(name))
            return;

        foreach (var dependentName in _dependencies[name])
        {
            if (handler != null)
            {
                ExecuteHandler(handler, dependentName);
            }
        }
    }

    protected void NotifyPropertiesChanged(string[] names)
    {
        foreach (var name in names)
        {
            NotifyPropertyChanged(name);
        }
    }

    public void NotifyAllPropertiesChanged()
    {
        if(properties != null)
        {
            foreach (PropertyDescriptorEx prop in properties)
            {
                NotifyPropertyChanged(prop.Name);
            }
        }

        Session.UpdatePropertiesImmediately();
    }

    #endregion

    #region ServerSentEvents

    private HttpContext? _serverSentContext;
    private CancellationTokenSource? _serverSentCancel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DoubleConverter() }
    };

    public async Task SendPropertiesChanged(HttpContext context)
    {
        _serverSentContext = context;
        _serverSentCancel = new CancellationTokenSource();
        await Task.WhenAny(Task.Delay(Timeout.Infinite, _serverSentCancel.Token));
    }
    private async Task SendPropertyChanged(string name)
    {
        if (_serverSentContext == null) return;
        
        var value = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(TryGetMember(name), JsonOptions));
        var json = $"data: {{ \"{name}\":{value} }}\r\r";
        await _serverSentContext.Response.WriteAsync(json);
        await _serverSentContext.Response.Body.FlushAsync();
    }
    
    #endregion
    
    #region MessageBox

    public string MessageBoxTitle = string.Empty;
    public string MessageBoxText = string.Empty;

    public void MessageBox(string title, string text)
    {
        MessageBoxTitle = title;
        MessageBoxText = text;
        NotifyPropertyChanged("_stonehenge_StonehengeEval");
    }

    #endregion

    #region Server side page enabling

    public void EnableRoute(string route, bool enabled)
    {
        route = route.Replace("-", "_");
        Session.Logger.LogInformation($"ActiveViewModel.EnableRoute({route}) = {enabled}");
        ExecuteClientScript($"stonehengeEnableRoute('{route}', {enabled.ToString().ToLower()})");
    }

    #endregion

    #region Server side navigation

    public string NavigateToRoute = string.Empty;

    public void NavigateTo(string route)
    {
        if (Session.CurrentRoute == route) return;
        Session.Logger.LogInformation("ActiveViewModel.NavigateTo: " + route);
        NavigateToRoute = route.StartsWith("http")
            ? route
            : route.Replace("-", "_");
    }
    public void NavigateBack()
    {
        var route = Session.GetBackRoute();
        if (string.IsNullOrEmpty(route))
        {
            Session.Logger.LogWarning("ActiveViewModel.NavigateBack: No back route");
            return;
        }
        Session.Logger.LogInformation("ActiveViewModel.NavigateBack: " + route);
        NavigateToRoute = route.Replace("-", "_");
    }

    public void ReloadPage() => ExecuteClientScript("window.location.reload();");

    #endregion

    #region Client site scripting

    public string ClientScript = string.Empty;

    public void ExecuteClientScript(string script)
    {
        script = script.Trim();
        if (!script.EndsWith(";"))
        {
            script += "; ";
        }

        if (string.IsNullOrEmpty(ClientScript))
        {
            ClientScript = script;
            return;
        }

        ClientScript += script;
    }

    #endregion

    #region Clipboard support

    public void CopyToClipboard(string text)
    {
        text = text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        ExecuteClientScript($"stonehengeCopyToClipboard('{text}')");
    }

    #endregion

    public virtual Resource? GetDataResource(string resourceName)
    {
        return null;
    }

    public virtual Resource? GetDataResource(string resourceName, Dictionary<string, string> parameters)
    {
        return null;
    }

    public virtual Resource? PostDataResource(string resourceName, Dictionary<string, string> parameters,
        Dictionary<string, string> formData)
    {
        return null;
    }

    protected void SetUpdateTimer(int updateMs) => SetUpdateTimer(TimeSpan.FromMilliseconds(updateMs));

    protected void SetUpdateTimer(TimeSpan update)
    {
        if (_updateTimer == null)
        {
            _updateTimer = new Timer(update.TotalMilliseconds);
            _updateTimer.Elapsed += UpdateTimerOnElapsed;
            _updateTimer.Enabled = true;
        }
        else
        {
            _updateTimer.Interval = update.TotalMilliseconds;
        }
    }

    protected void StopUpdateTimer()
    {
        if (_updateTimer == null) return;
            
        _updateTimer.Stop();
        _updateTimer.Dispose();
        _updateTimer = null;
    }

    private void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        Thread.CurrentThread.CurrentCulture = Session.SessionCulture;
        Thread.CurrentThread.CurrentUICulture = Session.SessionCulture;
        OnUpdateTimer();
    }
        
    public virtual void OnUpdateTimer()
    {
    }

    public void Dispose()
    {
        //_serverSentContext?.Abort();
        _serverSentCancel?.Cancel();
        
        StopUpdateTimer();
        OnDispose();
    }
        
    public virtual void OnDispose()
    {
    }
        
}