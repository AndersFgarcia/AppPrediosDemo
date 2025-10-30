using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AppPrediosDemo.Infrastructure
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        // ?? CLAVE: El comando SOLO puede ejecutarse si NO está corriendo (_isExecuting) 
        // Y si la lógica de negocio (_canExecute) lo permite (ej., !HasErrors).
        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true; RaiseCanExecuteChanged();
            try { await _execute().ConfigureAwait(true); }
            finally { _isExecuting = false; RaiseCanExecuteChanged(); }// Vuelve a habilitar el botón
        }
    }
}
