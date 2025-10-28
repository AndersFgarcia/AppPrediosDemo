using AppPrediosDemo.Infrastructure;
using AppPrediosDemo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AppPrediosDemo.ViewModels
{
    public sealed class CatalogOption
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public override string ToString() => Nombre;
    }

    public sealed record ItemCatalogo(int Codigo, string Nombre);
    public sealed record CentroItem(int Codigo, string Nombre, int IdLocalizacion);

    public class PredioFormViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        // ===== Estado =====
        private Predio _predioActual = new();
        public Predio PredioActual
        {
            get => _predioActual;
            set { if (Set(ref _predioActual, value)) ValidateAll(); }
        }

        // Sub-VM para “Gravámenes y afectaciones”
        public MedidasProcesalesViewModel Medidas { get; } = new();

        // ===== Catálogos (BD) =====
        public ObservableCollection<CatalogOption> TipoProcesos { get; } = new();
        public ObservableCollection<CatalogOption> FuentesProceso { get; } = new();
        public ObservableCollection<CatalogOption> EtapasProcesales { get; } = new();

        // ===== Cascada ubicación (desde Localizacion) =====
        public ObservableCollection<ItemCatalogo> Departamentos { get; } = new();
        public ObservableCollection<ItemCatalogo> Municipios { get; } = new();
        public ObservableCollection<CentroItem> CentrosPoblados { get; } = new();

        private ItemCatalogo? _selectedDepartamento;
        public ItemCatalogo? SelectedDepartamento
        {
            get => _selectedDepartamento;
            set
            {
                if (Set(ref _selectedDepartamento, value))
                {
                    PredioActual.Departamento = value?.Nombre;
                    PredioActual.Municipio = null;
                    PredioActual.CentroPoblado = null;
                    SelectedMunicipio = null;
                    SelectedCentro = null;
                    _ = CargarMunicipiosAsync(value);
                    ValidateLocalizacion();
                }
            }
        }

        private ItemCatalogo? _selectedMunicipio;
        public ItemCatalogo? SelectedMunicipio
        {
            get => _selectedMunicipio;
            set
            {
                if (Set(ref _selectedMunicipio, value))
                {
                    PredioActual.Municipio = value?.Nombre;
                    PredioActual.CentroPoblado = null;
                    SelectedCentro = null;
                    _ = CargarCentrosPobladosAsync(SelectedDepartamento, value);
                    ValidateLocalizacion();
                }
            }
        }

        private CentroItem? _selectedCentro;
        public CentroItem? SelectedCentro
        {
            get => _selectedCentro;
            set
            {
                if (Set(ref _selectedCentro, value))
                {
                    PredioActual.CentroPoblado = value?.Nombre;
                    ValidateLocalizacion();
                }
            }
        }

        // Para guardar luego en EstudioTerreno.IdLocalizacion
        public int? IdLocalizacionSeleccionada => SelectedCentro?.IdLocalizacion;

        // ===== Listas mock varias =====
        public ObservableCollection<string> CirculosRegistrales { get; } = new();
        public ObservableCollection<string> AbogadosSustanciadores { get; } = new();
        public ObservableCollection<string> AbogadosRevisores { get; } = new();
        public ObservableCollection<string> EstadosRevision { get; } = new();
        public ObservableCollection<string> EstadosAprobacion { get; } = new();
        public ObservableCollection<string> OpcionesViabilidad { get; } =
            new() { "Viable", "No viable", "Pendiente" };

        // ===== Comandos =====
        public RelayCommand NuevoCommand { get; }
        public RelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public PredioFormViewModel()
        {
            // Mocks
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

            _ = LoadAsync();
            ValidateAll();
        }

        private async Task LoadAsync()
        {
            try
            {
                await CargarCatalogosAsync();
                await CargarDepartamentosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init:\n" + ex.Message);
            }
        }

        // ===== Carga Catálogos =====
        private async Task CargarCatalogosAsync()
        {
            await CargarTipoProcesosAsync();
            await CargarFuentesProcesoAsync();
            await CargarEtapasProcesalesAsync();
        }

        private static void Rellenar<T>(ObservableCollection<T> target, IEnumerable<T> data)
        {
            target.Clear();
            foreach (var x in data) target.Add(x);
        }

        private async Task CargarTipoProcesosAsync()
        {
            using var ctx = new ViabilidadContext();
            var data = await ctx.TipoProcesos
                .AsNoTracking()
                .OrderBy(x => x.NombreTipoProceso)
                .Select(x => new CatalogOption
                {
                    Id = x.IdTipoProceso,
                    Nombre = x.NombreTipoProceso
                })
                .ToListAsync();

            Rellenar(TipoProcesos, data);
        }

        private async Task CargarFuentesProcesoAsync()
        {
            using var ctx = new ViabilidadContext();
            var data = await ctx.FuenteProcesos
                .AsNoTracking()
                .OrderBy(x => x.NombreFuenteProceso)
                .Select(x => new CatalogOption
                {
                    Id = x.IdFuenteProceso,
                    Nombre = x.NombreFuenteProceso
                })
                .ToListAsync();

            Rellenar(FuentesProceso, data);
        }

        private async Task CargarEtapasProcesalesAsync()
        {
            using var ctx = new ViabilidadContext();
            var data = await ctx.EtapaProcesals
                .AsNoTracking()
                .OrderBy(x => x.NombreEtapaProcesal)
                .Select(x => new CatalogOption
                {
                    Id = x.IdEtapaProcesal,
                    Nombre = x.NombreEtapaProcesal
                })
                .ToListAsync();

            Rellenar(EtapasProcesales, data);
        }

        // ===== Localización en cascada =====
        private async Task CargarDepartamentosAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();

                var raw = await ctx.Localizacions
                    .AsNoTracking()
                    .Select(x => new { x.CodigoDepartamento, x.NombreDepartamento })
                    .Distinct()
                    .OrderBy(x => x.NombreDepartamento)
                    .ToListAsync();

                Departamentos.Clear();
                foreach (var r in raw)
                    Departamentos.Add(new ItemCatalogo(r.CodigoDepartamento, r.NombreDepartamento));

                Municipios.Clear();
                CentrosPoblados.Clear();
                SelectedMunicipio = null;
                SelectedCentro = null;
                ValidateLocalizacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("CargarDepartamentosAsync:\n" + ex.Message);
            }
        }

        private async Task CargarMunicipiosAsync(ItemCatalogo? departamento)
        {
            try
            {
                Municipios.Clear();
                CentrosPoblados.Clear();
                SelectedMunicipio = null;
                SelectedCentro = null;

                if (departamento is null)
                {
                    ValidateLocalizacion();
                    return;
                }

                using var ctx = new ViabilidadContext();

                var raw = await ctx.Localizacions
                    .AsNoTracking()
                    .Where(x => x.CodigoDepartamento == departamento.Codigo)
                    .Select(x => new { x.CodigoMunicipio, x.NombreMunicipio })
                    .Distinct()
                    .OrderBy(x => x.NombreMunicipio)
                    .ToListAsync();

                foreach (var r in raw)
                    Municipios.Add(new ItemCatalogo(r.CodigoMunicipio, r.NombreMunicipio));

                ValidateLocalizacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("CargarMunicipiosAsync:\n" + ex.Message);
            }
        }

        private async Task CargarCentrosPobladosAsync(ItemCatalogo? departamento, ItemCatalogo? municipio)
        {
            try
            {
                CentrosPoblados.Clear();
                SelectedCentro = null;

                if (departamento is null || municipio is null)
                {
                    ValidateLocalizacion();
                    return;
                }

                using var ctx = new ViabilidadContext();

                var raw = await ctx.Localizacions
                    .AsNoTracking()
                    .Where(x => x.CodigoDepartamento == departamento.Codigo &&
                                x.CodigoMunicipio == municipio.Codigo)
                    .Select(x => new { x.CodigoCentroPoblado, x.NombreCentroPoblado, x.IdLocalizacion })
                    .OrderBy(x => x.NombreCentroPoblado)
                    .ToListAsync();

                foreach (var r in raw)
                    CentrosPoblados.Add(new CentroItem(r.CodigoCentroPoblado, r.NombreCentroPoblado, r.IdLocalizacion));

                ValidateLocalizacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("CargarCentrosPobladosAsync:\n" + ex.Message);
            }
        }

        // ===== Validación =====
        private readonly Dictionary<string, List<string>> _errors = new();
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? Array.Empty<string>()
                : _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();

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

        private void ValidateLocalizacion()
        {
            const string key = nameof(IdLocalizacionSeleccionada);
            ClearErrors(key);
            if (SelectedDepartamento is null || SelectedMunicipio is null || SelectedCentro is null)
                AddError(key, "Seleccione departamento, municipio y centro poblado.");
        }

        public void ValidateAll()
        {
            ValidateRequired(nameof(PredioActual.ID), PredioActual.ID);
            ValidateNumeric(nameof(PredioActual.ID), PredioActual.ID);

            ValidateRequired(nameof(PredioActual.FMI), PredioActual.FMI);
            ValidateNumeric(nameof(PredioActual.FMI), PredioActual.FMI);

            ValidateDecimal(nameof(PredioActual.AreaRegistral), PredioActual.AreaRegistral);
            ValidateDecimal(nameof(PredioActual.AreaCalculada), PredioActual.AreaCalculada);

            ValidateLocalizacion();
        }

        private void ValidateRequired(string prop, string? value)
        {
            ClearErrors(prop);
            if (string.IsNullOrWhiteSpace(value)) AddError(prop, "Campo obligatorio.");
        }

        private void ValidateNumeric(string prop, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!long.TryParse(value, out _)) AddError(prop, "Ingrese un número válido.");
        }

        private void ValidateDecimal(string prop, decimal? value)
        {
            ClearErrors(prop);
            if (value is null) return;
            if (value < 0) AddError(prop, "El valor no puede ser negativo.");
        }

        // ===== Comandos =====
        private Predio _backup = new();

        private void Nuevo()
        {
            _backup = PredioActual;
            PredioActual = new Predio();
            SelectedDepartamento = null;
            SelectedMunicipio = null;
            SelectedCentro = null;
            Municipios.Clear();
            CentrosPoblados.Clear();
            ValidateLocalizacion();
        }

        private void Guardar()
        {
            ValidateAll();
            if (HasErrors) return;

            // Ejemplo al guardar:
            // var idLoc = IdLocalizacionSeleccionada!.Value;
            // usar idLoc para EstudioTerreno.IdLocalizacion
            // Medidas.ToEntities(idEstudioTerreno) -> INSERT en Postulacion.MedidaProcesal
        }

        private void Cancelar() => PredioActual = _backup;
    }
}
