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

    public sealed record PredioListado(
        long IdRegistroProceso,
        string FMI,
        string? NumeroExpediente,
        string? Fuente,
        string? Tipo,
        string? Etapa
    );

    public class PredioFormViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        // ===== Estado =====
        private Predio _predioActual = new();
        public Predio PredioActual
        {
            get => _predioActual;
            set { if (Set(ref _predioActual, value)) ValidateAll(); }
        }
        //nueva
        private static string K(string prop) => $"PredioActual.{prop}";

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

        // ===== Buscador =====
        private string? _filtroId;
        public string? FiltroId { get => _filtroId; set => Set(ref _filtroId, value); }

        private string? _filtroFmi;
        public string? FiltroFmi { get => _filtroFmi; set => Set(ref _filtroFmi, value); }

        private string? _filtroExpediente;
        public string? FiltroExpediente { get => _filtroExpediente; set => Set(ref _filtroExpediente, value); }

        public ObservableCollection<PredioListado> ResultadosBusqueda { get; } = new();

        private PredioListado? _resultadoSeleccionado;
        public PredioListado? ResultadoSeleccionado
        {
            get => _resultadoSeleccionado;
            set
            {
                if (Set(ref _resultadoSeleccionado, value) && value is not null)
                    _ = CargarPredioDesdeRegistroAsync(value.IdRegistroProceso);
            }
        }

        // ===== Comandos =====
        public RelayCommand NuevoCommand { get; }
        public RelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }
        public RelayCommand BuscarRegistrosCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

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
            BuscarRegistrosCommand = new RelayCommand(async () => await BuscarAsync(), () => true);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros, () => true);

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
                .Select(x => new CatalogOption { Id = x.IdTipoProceso, Nombre = x.NombreTipoProceso })
                .ToListAsync();
            Rellenar(TipoProcesos, data);
        }

        private async Task CargarFuentesProcesoAsync()
        {
            using var ctx = new ViabilidadContext();
            var data = await ctx.FuenteProcesos
                .AsNoTracking()
                .OrderBy(x => x.NombreFuenteProceso)
                .Select(x => new CatalogOption { Id = x.IdFuenteProceso, Nombre = x.NombreFuenteProceso })
                .ToListAsync();
            Rellenar(FuentesProceso, data);
        }

        private async Task CargarEtapasProcesalesAsync()
        {
            using var ctx = new ViabilidadContext();
            var data = await ctx.EtapaProcesals
                .AsNoTracking()
                .OrderBy(x => x.NombreEtapaProcesal)
                .Select(x => new CatalogOption { Id = x.IdEtapaProcesal, Nombre = x.NombreEtapaProcesal })
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

                if (departamento is null) { ValidateLocalizacion(); return; }

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

                if (departamento is null || municipio is null) { ValidateLocalizacion(); return; }

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
        public IEnumerable GetErrors(string? propertyName)
            => string.IsNullOrEmpty(propertyName) ? Array.Empty<string>() :
               _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();

        private void AddError(string prop, string message)
        {
            if (!_errors.TryGetValue(prop, out var list)) { list = new List<string>(); _errors[prop] = list; }
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
            // Usamos esta clave asumiendo que el error se enlaza a una propiedad del ViewModel padre.
            const string key = nameof(IdLocalizacionSeleccionada);
            // ✅ CLAVE: Limpiar los errores de la propiedad antes de revalidar
            ClearErrors(key);
            if (SelectedDepartamento is null || SelectedMunicipio is null || SelectedCentro is null)
                AddError(key, "Seleccione departamento, municipio y centro poblado.");
        }

        public void ValidateAll()
        {
            // IdPostulacion
            ValidateRequiredMaxLen(K(nameof(PredioActual.ID)), PredioActual.ID, 30);
            // si es solo numérico:
            //ValidateRegex(K(nameof(PredioActual.ID)), PredioActual.ID, @"^\d{1,30}$", "Solo dígitos.");

            // FMI
            ValidateRequiredMaxLen(K(nameof(PredioActual.FMI)), PredioActual.FMI, 100);

            // Áreas
            ValidateDecimalReq(K(nameof(PredioActual.AreaRegistral)), PredioActual.AreaRegistral);
            ValidateDecimalReq(K(nameof(PredioActual.AreaCalculada)), PredioActual.AreaCalculada);

            // === 4. Catálogos (FKs) ===
            // Criterios: Requerido (ID > 0)
            ValidateCatalogo(K(nameof(PredioActual.IdFuenteProceso)), PredioActual.IdFuenteProceso);
            ValidateCatalogo(K(nameof(PredioActual.IdTipoProceso)), PredioActual.IdTipoProceso);
            ValidateCatalogo(K(nameof(PredioActual.IdEtapaProcesal)), PredioActual.IdEtapaProcesal);

            // Criterio: IdLocalizacionSeleccionada no nulo (Debe invocar a la versión ajustada)
            ValidateLocalizacion();
        }
        private void ValidateRequiredMaxLen(string key, string? value, int maxLen)
        {
            ClearErrors(key);
            if (string.IsNullOrWhiteSpace(value)) { AddError(key, "Campo obligatorio."); return; }
            if (value.Length > maxLen) AddError(key, $"Longitud máxima {maxLen} caracteres.");
        }

        private void ValidateRegex(string key, string? value, string pattern, string msg)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
                AddError(key, msg);
        }

        // 📚 Valida que se haya seleccionado un ítem de un catálogo (ID > 0)
        private void ValidateCatalogo(string key, int? value)
        {
            ClearErrors(key);
            if (!value.HasValue || value.Value <= 0) AddError(key, "Seleccione una opción.");
        }

        private void ValidateDecimalReq(string key, decimal? value)
        {
            ClearErrors(key);
            if (value is null) { AddError(key, "Campo obligatorio."); return; }
            if (value < 0) AddError(key, "El valor no puede ser negativo.");
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
            ResultadosBusqueda.Clear();
            ResultadoSeleccionado = null;
            ValidateLocalizacion();
        }

        private void Guardar()
        {
            ValidateAll();
            if (HasErrors) return;
            // var idLoc = IdLocalizacionSeleccionada!.Value; // usar para EstudioTerreno.IdLocalizacion
            // Medidas.ToEntities(idEstudioTerreno) -> INSERT en Postulacion.MedidaProcesal
        }

        private void Cancelar() => PredioActual = _backup;

        // ===== Buscar y cargar =====
        private async Task BuscarAsync()
        {
            try
            {
                // 1. Validar filtros vacíos (se mantiene igual)
                if (string.IsNullOrWhiteSpace(FiltroId) &&
                    string.IsNullOrWhiteSpace(FiltroFmi) &&
                    string.IsNullOrWhiteSpace(FiltroExpediente))
                {
                    MessageBox.Show("Ingrese al menos un filtro.");
                    return;
                }

                using var ctx = new ViabilidadContext();

                // 2. ✅ CORRECCIÓN: Definición de 'q' sin los Includes (Mejor rendimiento inicial)
                var q = ctx.RegistroProcesos.AsNoTracking().AsQueryable();

                // 3. ✅ CORRECCIÓN ID: Busca por IdPostulacion (VARCHAR) y usa StartsWith.
                if (!string.IsNullOrWhiteSpace(FiltroId))
                    q = q.Where(x => x.IdPostulacion.StartsWith(FiltroId));

                // 4. ✅ CORRECCIÓN FMI: Usa StartsWith.
                if (!string.IsNullOrWhiteSpace(FiltroFmi))
                    q = q.Where(x => x.FMI.StartsWith(FiltroFmi));

                // 5. ✅ CORRECCIÓN EXPEDIENTE: Usa StartsWith.
                if (!string.IsNullOrWhiteSpace(FiltroExpediente))
                    q = q.Where(x => x.NumeroExpediente != null && x.NumeroExpediente.StartsWith(FiltroExpediente));

                // 6. Ejecución de la consulta
                var data = await q
                    // ⚠️ Se reintroducen los Includes aquí para que el Select funcione correctamente,
                    // pero después de haber aplicado los filtros.
                    .Include(x => x.IdFuenteProcesoNavigation)
                    .Include(x => x.IdTipoProcesoNavigation)
                    .Include(x => x.IdEtapaProcesalNavigation)

                    .OrderByDescending(x => x.IdRegistroProceso)
                    .Take(5)
                    .Select(x => new PredioListado(
                        x.IdRegistroProceso,
                        x.FMI,
                        x.NumeroExpediente,
                        x.IdFuenteProcesoNavigation.NombreFuenteProceso,
                        x.IdTipoProcesoNavigation.NombreTipoProceso,
                        x.IdEtapaProcesalNavigation.NombreEtapaProcesal
                    ))
                    .ToListAsync();

                ResultadosBusqueda.Clear();
                foreach (var r in data) ResultadosBusqueda.Add(r);

                if (ResultadosBusqueda.Count == 0)
                    MessageBox.Show("Sin resultados.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("BuscarAsync:\n" + ex.Message);
            }
        }

        private async Task CargarPredioDesdeRegistroAsync(long idRegistroProceso)
        {
            try
            {
                using var ctx = new ViabilidadContext();

                // RegistroProceso + últimos EstudioTerreno y su Localizacion
                var rp = await ctx.RegistroProcesos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdRegistroProceso == idRegistroProceso);

                if (rp is null)
                {
                    MessageBox.Show("No se encontró el registro.");
                    return;
                }

                var et = await ctx.EstudioTerrenos
                    .AsNoTracking()
                    .Where(x => x.IdRegistroProceso == idRegistroProceso)
                    .OrderByDescending(x => x.IdEstudioTerreno)
                    .FirstOrDefaultAsync();

                Localizacion? loc = null;
                if (et is not null)
                {
                    loc = await ctx.Localizacions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.IdLocalizacion == et.IdLocalizacion);
                }

                // Poblar Identificación
                PredioActual.ID = rp.IdRegistroProceso.ToString();
                PredioActual.FMI = rp.FMI;
                PredioActual.NoExpediente = rp.NumeroExpediente;
                PredioActual.IdFuenteProceso = rp.IdFuenteProceso;
                PredioActual.IdTipoProceso = rp.IdTipoProceso;
                PredioActual.IdEtapaProcesal = rp.IdEtapaProcesal;
                PredioActual.RadicadoOrfeo = rp.RadicadoOrfeo;
                PredioActual.Dependencia = rp.Dependencia;

                // Poblar EstudioTerreno básicos
                if (et is not null)
                {
                    PredioActual.AreaRegistral = et.AreaRegistral;
                    PredioActual.AreaCalculada = et.AreaCalculada;
                    PredioActual.CirculoRegistral = et.CirculoRegistral;
                }

                // Seleccionar Localización en cascada
                if (loc is not null)
                {
                    var dep = Departamentos.FirstOrDefault(d => d.Codigo == loc.CodigoDepartamento);
                    SelectedDepartamento = dep;
                    if (dep is not null)
                    {
                        await CargarMunicipiosAsync(dep);
                        var mun = Municipios.FirstOrDefault(m => m.Codigo == loc.CodigoMunicipio);
                        SelectedMunicipio = mun;
                        if (mun is not null)
                        {
                            await CargarCentrosPobladosAsync(dep, mun);
                            var cen = CentrosPoblados.FirstOrDefault(c =>
                                c.Codigo == loc.CodigoCentroPoblado && c.IdLocalizacion == loc.IdLocalizacion);
                            SelectedCentro = cen;
                        }
                    }
                }

                // Cargar Medidas procesales (si existen)
                if (et is not null)
                {
                    var medidas = await ctx.MedidaProcesals
                        .AsNoTracking()
                        .Where(m => m.IdEstudioTerreno == et.IdEstudioTerreno)
                        .ToListAsync();

                    if (medidas.Count > 0)
                        Medidas.LoadFrom(medidas);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("CargarPredioDesdeRegistroAsync:\n" + ex.Message);
            }
        }

        private void LimpiarFiltros()
        {
            FiltroId = null;
            FiltroFmi = null;
            FiltroExpediente = null;
            ResultadosBusqueda.Clear();
            ResultadoSeleccionado = null;
        }
    }
}
