namespace IctBaden.Stonehenge.ViewModel;

/// <summary>
/// Add view model property support
/// for NotifyPropertyChanged.
/// Works with ActiveViewModel classes only !
/// Use property.Update(newValue) to use.
/// </summary>
/// <typeparam name="T">Type of the property.</typeparam>
//[JsonConverter(typeof(NotifyJsonConverter))]
public class Notify<T>
{
    private readonly ActiveViewModel _viewModel;
    private readonly string _name;
    private T _value;

    // ReSharper disable once UnusedMember.Global
    public Notify(ActiveViewModel viewModel, string name)
        // ReSharper disable once IntroduceOptionalParameters.Global
        : this(viewModel, name, default!)
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public Notify(ActiveViewModel viewModel, string name, T value)
    {
        _viewModel = viewModel;
        _name = name;
        _value = value;
    }

    public void Update(T value)
    {
        _value = value;
        _viewModel.NotifyPropertyChanged(_name);
    }

    public static implicit operator T(Notify<T> value)
    {
        return value._value;
    }
}