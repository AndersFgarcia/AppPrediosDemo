using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppPrediosDemo.Infrastructure
{
    /// Base MVVM: notifica cambios a la UI.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? prop = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            return true;
        }

        protected void Raise([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

