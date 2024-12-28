using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace LumiSky.Core.Profile;

public interface ISettings : INotifyPropertyChanged { }

public abstract class Settings : ObservableObject
{
    protected Settings()
    {
        Reset();
        HookEvents();
    }

    ~Settings()
    {
        UnhookEvents();
    }

    protected abstract void Reset();
    protected virtual void HookEvents() { }
    protected virtual void UnhookEvents() { }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context)
    {
        UnhookEvents();
        Reset();
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        HookEvents();
    }

    protected void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is INotifyPropertyChanged obj)
        {
            if (e.Action is NotifyCollectionChangedAction.Remove
                or NotifyCollectionChangedAction.Reset
                or NotifyCollectionChangedAction.Replace)
            {
                Unhook();
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Hook();
            }
        }

        var name = this
            .GetType()
            .GetProperties()
            .First(x => x.PropertyType.IsInstanceOfType(sender))
            .Name;
        OnPropertyChanged(name);
        return;

        void Hook()
        {
            obj.PropertyChanged += OnPropertyChanged;
            if (e.NewItems is null) return;
            foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged += OnPropertyChanged;
            }
            foreach (var item in e.NewItems.OfType<INotifyPropertyChanging>())
            {
                item.PropertyChanging += OnPropertyChanging;
            }

            // Attach the value object (settings) for each collection
            foreach (var item in e.NewItems)
            {
                if (item.GetType().Name.StartsWith("KeyValuePair"))
                {
                    var value = item.GetType().GetProperty("Value")!.GetValue(item);
                    if (value is Settings notify)
                    {
                        notify.PropertyChanged += OnPropertyChanged;
                        notify.PropertyChanging += OnPropertyChanging;
                    }
                }
            }
        }

        void Unhook()
        {
            obj.PropertyChanged -= OnPropertyChanged;
            if (e.OldItems is null) return;
            foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= OnPropertyChanged;
            }
            foreach (var item in e.OldItems.OfType<INotifyPropertyChanging>())
            {
                item.PropertyChanging -= OnPropertyChanging;
            }

            // Detach the value object (settings) for each collection
            foreach (var item in e.OldItems)
            {
                if (item.GetType().Name.StartsWith("KeyValuePair"))
                {
                    var value = item.GetType().GetProperty("Value")!.GetValue(item);
                    if (value is Settings notify)
                    {
                        notify.PropertyChanged -= OnPropertyChanged;
                        notify.PropertyChanging -= OnPropertyChanging;
                    }
                }
            }
        }
    }

    protected void HookPropertyEvents(object obj)
    {
        if (obj is INotifyPropertyChanging notify1)
            notify1.PropertyChanging += OnPropertyChanging;
        if (obj is INotifyPropertyChanged notify2)
            notify2.PropertyChanged += OnPropertyChanged;
    }

    protected void UnhookPropertyEvents(object obj)
    {
        if (obj is INotifyPropertyChanging notify1)
            notify1.PropertyChanging -= OnPropertyChanging;
        if (obj is INotifyPropertyChanged notify2)
            notify2.PropertyChanged -= OnPropertyChanged;
    }

    protected void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    protected void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        OnPropertyChanging(e.PropertyName);
    }
}
