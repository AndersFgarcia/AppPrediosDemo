using AppPrediosDemo.Infrastructure;
using AppPrediosDemo.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;   // INotifyDataErrorInfo, DataErrorsChangedEventArgs


namespace AppPrediosDemo.ViewModels
{
    /// VM principal del formulario: estado, validación y comandos.
    public class PredioFormViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private Predio _predioActual = new();
        public Predio PredioActual
        {
            get => _predioActual;
            set { if (Set(ref _predioActual, value)) ValidateAll(); }
        }

        // Colecciones para ComboBox (cárgalas desde BD en la siguiente fase)
        public ObservableCollection<string> TiposProceso { get; } = new();
        public ObservableCollection<string> Dependencias { get; } = new();
        public ObservableCollection<string> Departamentos { get; } = new();
        public ObservableCollection<string> Municipios { get; } = new();
        public ObservableCollection<string> CirculosRegistrales { get; } = new();
        public ObservableCollection<string> AbogadosSustanciadores { get; } = new();
        public ObservableCollection<string> AbogadosRevisores { get; } = new();
        public ObservableCollection<string> EstadosRevision { get; } = new();
        public ObservableCollection<string> EstadosAprobacion { get; } = new();
        public ObservableCollection<string> OpcionesViabilidad { get; } = new() { "Viable", "No viable", "Pendiente" };
        public ObservableCollection<string> OpcionesColectivo { get; } = new() { "Colectivo", "Individual" };

        // Comandos
        public RelayCommand NuevoCommand { get; }
        public RelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public PredioFormViewModel()
        {
            // Demo: carga mínima para probar UI (luego se traerá de BD)
            TiposProceso.Add("Clarificación");
            TiposProceso.Add("Restitución");
            Dependencias.Add("Jurídica A");
            Dependencias.Add("Jurídica B");
            Departamentos.Add("Antioquia");
            Departamentos.Add("Cundinamarca");
            Municipios.Add("Medellín");
            Municipios.Add("Bogotá");
            CirculosRegistrales.Add("Círculo 01");
            CirculosRegistrales.Add("Círculo 02");
            AbogadosSustanciadores.Add("Abogada 1");
            AbogadosSustanciadores.Add("Abogado 2");
            AbogadosRevisores.Add("Revisor 1");
            AbogadosRevisores.Add("Revisor 2");
            EstadosRevision.Add("Aprobado");
            EstadosRevision.Add("Devuelto");
            EstadosRevision.Add("En revisión");
            EstadosAprobacion.Add("Aprobado");
            EstadosAprobacion.Add("Observado");
            EstadosAprobacion.Add("Rechazado");

            NuevoCommand = new RelayCommand(Nuevo, () => true);
            GuardarCommand = new RelayCommand(Guardar, () => !HasErrors);
            CancelarCommand = new RelayCommand(Cancelar, () => true);

            ValidateAll();
        }

        // ===== Validación con INotifyDataErrorInfo =====
        private readonly Dictionary<string, List<string>> _errors = new();
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public IEnumerable GetErrors(string? propertyName)
            => string.IsNullOrEmpty(propertyName) ? Array.Empty<string>() :
               _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();

        private void AddError(string prop, string message)
        {
            if (!_errors.TryGetValue(prop, out var list))
            {
                list = new List<string>();
                _errors[prop] = list;
            }
            if (!list.Contains(message)) list.Add(message);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
            GuardarCommand.RaiseCanExecuteChanged();
        }

        private void ClearErrors(string prop)
        {
            if (_errors.Remove(prop))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
                GuardarCommand.RaiseCanExecuteChanged();
            }
        }

        /// Valida todas las propiedades clave (extiende con tus reglas).
        public void ValidateAll()
        {
            // Ejemplos: obligatorios y numéricos
            ValidateRequired(nameof(PredioActual.ID), PredioActual.ID);
            ValidateNumeric(nameof(PredioActual.ID), PredioActual.ID);

            ValidateRequired(nameof(PredioActual.FMI), PredioActual.FMI);
            ValidateNumeric(nameof(PredioActual.FMI), PredioActual.FMI);

            ValidateDecimal(nameof(PredioActual.AreaRegistral), PredioActual.AreaRegistral);
            ValidateDecimal(nameof(PredioActual.AreaCalculada), PredioActual.AreaCalculada);

            // Fechas (si hay reglas de rango, aplícalas aquí)
            // Ej: FechaInforme <= hoy, etc. (opcional)
        }

        private void ValidateRequired(string prop, string? value)
        {
            ClearErrors(prop);
            if (string.IsNullOrWhiteSpace(value))
                AddError(prop, "Campo obligatorio.");
        }

        private void ValidateNumeric(string prop, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return; // ya lo cubre Required
            if (!long.TryParse(value, out _))
                AddError(prop, "Ingrese un número válido.");
        }

        private void ValidateDecimal(string prop, decimal? value)
        {
            ClearErrors(prop);
            if (value is null) return; // permitir nulo si no es obligatorio
            if (value < 0) AddError(prop, "El valor no puede ser negativo.");
        }

        // ===== Comandos =====
        private Predio _backup = new();
        private void Nuevo()
        {
            _backup = PredioActual;            // guarda referencia para cancelar
            PredioActual = new Predio();       // formulario limpio
        }

        private void Guardar()
        {
            ValidateAll();
            if (HasErrors) return;

            // Aquí NO tocamos BD aún: solo punto de extensión.
            // TODO: llamar a servicio/repositorio para guardar PredioActual.
        }

        private void Cancelar()
        {
            PredioActual = _backup; // restaurar
        }
    }
}
