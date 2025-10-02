using System;
using System.Windows.Input;

namespace AppPrediosDemo.Infrastructure
{
	/// Comando simple para botones (Guardar, Nuevo, etc.)
	public class RelayCommand : ICommand
	{
		private readonly Action _exec;
		private readonly Func<bool>? _canExec;

		public RelayCommand(Action exec, Func<bool>? canExec = null)
		{ _exec = exec; _canExec = canExec; }

		public bool CanExecute(object? parameter) => _canExec?.Invoke() ?? true;
		public void Execute(object? parameter) => _exec();

		public event EventHandler? CanExecuteChanged;
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
