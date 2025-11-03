using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AppPrediosDemo.Infrastructure
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<Exception>? _onError;
        private bool _isExecuting;

        // Conveniencia: sin parámetro
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
            : this(_ => execute(), _ => canExecute?.Invoke() ?? true, onError) { }

        // Versión con parámetro
        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, Action<Exception>? onError = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onError = onError;
        }

        public bool CanExecute(object? parameter) =>
            !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                // Mantén el contexto de WPF (sin ConfigureAwait(false))
                await _execute(parameter);
            }
            catch (Exception ex)
            {
                // Superficie el error de forma clara
                if (_onError != null)
                    _onError(ex);
                else
                    Application.Current?.Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            ex.GetBaseException().Message,
                            "Error en comando",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error));
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            var disp = Application.Current?.Dispatcher;
            if (disp is null || disp.CheckAccess())
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            else
                disp.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}


