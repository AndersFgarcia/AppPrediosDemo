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
        private Predio? _prevPredio;
        private Predio _predioActual;

        public Predio PredioActual
        {
            get => _predioActual;
            set
            {
                if (Set(ref _predioActual, value))
                {
                    if (_prevPredio is INotifyPropertyChanged oldObs)
                        oldObs.PropertyChanged -= OnPredioChanged;

                    if (_predioActual is INotifyPropertyChanged newObs)
                        newObs.PropertyChanged += OnPredioChanged;

                    _prevPredio = _predioActual;

                    ValidateAll();
                    GuardarCommand.RaiseCanExecuteChanged();
                    UpdateDebug();
                }
            }
        }

        private static string K(string prop) => $"PredioActual.{prop}";

        // Sub-VM
        public MedidasProcesalesViewModel Medidas { get; } = new();

        // ===== Busy =====
        // 1) setter de IsBusy
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (Set(ref _isBusy, value))
                    GuardarCommand.RaiseCanExecuteChanged();
            }
        }

        // ===== Catálogos =====
        public ObservableCollection<CatalogOption> TipoProcesos { get; } = new();
        public ObservableCollection<CatalogOption> FuentesProceso { get; } = new();
        public ObservableCollection<CatalogOption> EtapasProcesales { get; } = new();

        // ===== Cascada ubicación =====
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
                    SelectedMunicipio = null;
                    SelectedCentro = null;
                    IdLocalizacionSeleccionada = null;
                    _ = CargarMunicipiosAsync(value);
                    ValidateLocalizacion();
                    GuardarCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(DebugInfo));
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
                    SelectedCentro = null;
                    IdLocalizacionSeleccionada = null;
                    _ = CargarCentrosPobladosAsync(SelectedDepartamento, value);
                    ValidateLocalizacion();
                    GuardarCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(DebugInfo));
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
                    IdLocalizacionSeleccionada = value?.IdLocalizacion;
                    ValidateLocalizacion();
                    GuardarCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(DebugInfo));
                }
            }
        }

        // Antes era solo getter. Ahora con setter para notificar y habilitar Guardar.
        private int? _idLocalizacionSeleccionada;
        public int? IdLocalizacionSeleccionada
        {
            get => _idLocalizacionSeleccionada;
            private set
            {
                if (Set(ref _idLocalizacionSeleccionada, value))
                {
                    GuardarCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(DebugInfo));
                }
            }
        }

        // ===== Listas mock varias =====
        public ObservableCollection<string> CirculosRegistrales { get; } = new();
        public ObservableCollection<string> AbogadosSustanciadores { get; } = new();
        public ObservableCollection<string> AbogadosRevisores { get; } = new();
        public ObservableCollection<string> EstadosRevision { get; } = new();
        public ObservableCollection<string> EstadosAprobacion { get; } = new();
        public ObservableCollection<string> OpcionesViabilidad { get; } =
            new() { "Viable", "No viable", "Pendiente" };

        // ===== Buscador =====
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

        public string? FiltroId { get => _filtroId; set { Set(ref _filtroId, value); } }
        public string? FiltroFmi { get => _filtroFmi; set { Set(ref _filtroFmi, value); } }
        public string? FiltroExpediente { get => _filtroExpediente; set { Set(ref _filtroExpediente, value); } }
        private string? _filtroId, _filtroFmi, _filtroExpediente;

        // ===== Comandos =====
        public RelayCommand NuevoCommand { get; }
        public AsyncRelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }
        public RelayCommand BuscarRegistrosCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

        // ===== Debug =====
        private string _debugInfo = "";
        public string DebugInfo
        {
            get => _debugInfo;
            private set => Set(ref _debugInfo, value);
        }
        private void UpdateDebug()
        {
            DebugInfo =
                $"HasErrors= {HasErrors}  |  IdLoc= {(IdLocalizacionSeleccionada?.ToString() ?? "-")}  |  " +
                $"Fuente= {PredioActual.IdFuenteProceso?.ToString() ?? "-"}  |  " +
                $"Tipo= {PredioActual.IdTipoProceso?.ToString() ?? "-"}  |  " +
                $"Etapa= {PredioActual.IdEtapaProcesal?.ToString() ?? "-"}";
        }

        // ===== ctor =====
        public PredioFormViewModel()
        {
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
            GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar);
            CancelarCommand = new RelayCommand(Cancelar, () => true);
            BuscarRegistrosCommand = new RelayCommand(async () => await BuscarAsync(), () => true);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros, () => true);
            PredioActual = new Predio();

            ErrorsChanged += (_, __) => { GuardarCommand.RaiseCanExecuteChanged(); UpdateDebug(); };
            UpdateDebug();
        }

        // Llamar desde MainWindow.Loaded
        public async Task InitializeAsync()
        {
            await LoadAsync();
            ValidateAll();
            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }

        // ===== Guardar: requisitos mínimos =====
        private bool PuedeGuardar()
        {
            // No dependas de HasErrors para habilitar botón. Solo los mínimos.
            bool ok =
                !string.IsNullOrWhiteSpace(PredioActual.ID) &&
                !string.IsNullOrWhiteSpace(PredioActual.FMI) &&
                PredioActual.IdFuenteProceso.GetValueOrDefault() > 0 &&
                PredioActual.IdTipoProceso.GetValueOrDefault() > 0 &&
                PredioActual.IdEtapaProcesal.GetValueOrDefault() > 0 &&
                IdLocalizacionSeleccionada.HasValue &&
                PredioActual.AreaRegistral.HasValue &&
                PredioActual.AreaCalculada.HasValue;

            return ok;
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

        // ===== Localización =====
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
                IdLocalizacionSeleccionada = null;
                ValidateLocalizacion();
                UpdateDebug();
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
                IdLocalizacionSeleccionada = null;

                if (departamento is null) { ValidateLocalizacion(); UpdateDebug(); return; }

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
                UpdateDebug();
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
                IdLocalizacionSeleccionada = null;

                if (departamento is null || municipio is null) { ValidateLocalizacion(); UpdateDebug(); return; }

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
                UpdateDebug();
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
        }
        private void ClearErrors(string prop)
        {
            if (_errors.Remove(prop))
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
        }

        private void ValidateLocalizacion()
        {
            const string key = nameof(IdLocalizacionSeleccionada);
            ClearErrors(key);
            if (!IdLocalizacionSeleccionada.HasValue)
                AddError(key, "Seleccione departamento, municipio y centro poblado.");
            GuardarCommand.RaiseCanExecuteChanged();
        }

        public void ValidateAll()
        {
            ValidateRequiredMaxLen(K(nameof(PredioActual.ID)), PredioActual.ID, 30);
            ValidateRequiredMaxLen(K(nameof(PredioActual.FMI)), PredioActual.FMI, 100);

            if (!string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion))
            {
                ValidateRegex(
                    K(nameof(PredioActual.NumeroIdentificacion)),
                    PredioActual.NumeroIdentificacion,
                    @"^\d{1,19}([.-]?\d{1,19})*$",
                    "Ingrese solo números (opcional . o -).");
            }

            ValidateDecimalReq(K(nameof(PredioActual.AreaRegistral)), PredioActual.AreaRegistral);
            ValidateDecimalReq(K(nameof(PredioActual.AreaCalculada)), PredioActual.AreaCalculada);

            ValidateCatalogo(K(nameof(PredioActual.IdFuenteProceso)), PredioActual.IdFuenteProceso);
            ValidateCatalogo(K(nameof(PredioActual.IdTipoProceso)), PredioActual.IdTipoProceso);
            ValidateCatalogo(K(nameof(PredioActual.IdEtapaProcesal)), PredioActual.IdEtapaProcesal);

            ValidateLocalizacion();
            UpdateDebug();
        }

        private void ValidateRequiredMaxLen(string key, string? value, int maxLen)
        {
            ClearErrors(key);
            if (string.IsNullOrWhiteSpace(value)) { AddError(key, "Campo obligatorio."); return; }
            if (value.Length > maxLen) AddError(key, $"Longitud máxima {maxLen}.");
        }

        private void ValidateRegex(string key, string? value, string pattern, string msg)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
                AddError(key, msg);
        }

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
            IdLocalizacionSeleccionada = null;
            ResultadosBusqueda.Clear();
            ResultadoSeleccionado = null;
            ValidateAll();
            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }

        private async Task GuardarAsync()
        {
            MessageBox.Show("Entró a GuardarAsync"); // probe 1
            ValidateAll();
            var diag = $"HasErrors={HasErrors}  IdLoc={IdLocalizacionSeleccionada}  " +
                       $"Fuente={PredioActual.IdFuenteProceso}  Tipo={PredioActual.IdTipoProceso}  Etapa={PredioActual.IdEtapaProcesal}  " +
                       $"AreaReg={PredioActual.AreaRegistral}  AreaCalc={PredioActual.AreaCalculada}";
            MessageBox.Show(diag); // probe 2

            if (HasErrors || IdLocalizacionSeleccionada is null)
            {
                MessageBox.Show("No guarda: validación.");
                return;
            }

            IsBusy = true;
            try
            {
                // BYPASS del servicio para aislar problemas.
                var medidas = Medidas.ToEntities(0);

                await using var ctx = new ViabilidadContext();
                await using var tx = await ctx.Database.BeginTransactionAsync();

                // RegistroProceso
                var rp = new RegistroProceso
                {
                    IdPostulacion = PredioActual.ID!,
                    FMI = PredioActual.FMI!,
                    NumeroExpediente = PredioActual.NoExpediente,
                    IdFuenteProceso = PredioActual.IdFuenteProceso!.Value,
                    IdTipoProceso = PredioActual.IdTipoProceso!.Value,
                    IdEtapaProcesal = PredioActual.IdEtapaProcesal!.Value,
                    RadicadoOrfeo = PredioActual.RadicadoOrfeo,
                    Dependencia = PredioActual.Dependencia
                };
                ctx.RegistroProcesos.Add(rp);
                var n1 = await ctx.SaveChangesAsync(); // probe 3
                MessageBox.Show($"Save1 cambios={n1}  IdRegistro={rp.IdRegistroProceso}");

                // EstudioTerreno
                var et = new EstudioTerreno
                {
                    IdRegistroProceso = rp.IdRegistroProceso,
                    IdLocalizacion = IdLocalizacionSeleccionada.Value,
                    AreaRegistral = PredioActual.AreaRegistral!.Value,
                    AreaCalculada = PredioActual.AreaCalculada!.Value,
                    CirculoRegistral = PredioActual.CirculoRegistral,

                    TipoPersonaTitular = PredioActual.PersonaTitular,
                    NombrePropietario = PredioActual.NombrePropietarios,
                    ApellidoPropietario = PredioActual.ApellidoPropietario,
                    Identificacion = string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion)
                                          ? null
                                          : long.Parse(new string(PredioActual
                                                                    .NumeroIdentificacion
                                                                    .Where(char.IsDigit)
                                                                    .ToArray())),

                    // NUEVO: mapeo a columnas de BD
                    NaturalezaJuridica = PredioActual.AnalisisNaturalezaUltimaTradicion,
                    AcreditacionPropiedad = PredioActual.TituloOriginario
                };
                ctx.EstudioTerrenos.Add(et);
                var n2 = await ctx.SaveChangesAsync(); // probe 4
                MessageBox.Show($"Save2 cambios={n2}  IdEstudio={et.IdEstudioTerreno}");

                // Medidas
                foreach (var m in medidas)
                {
                    m.IdEstudioTerreno = et.IdEstudioTerreno;
                    ctx.MedidaProcesals.Add(m);
                }
                var n3 = await ctx.SaveChangesAsync(); // probe 5

                // Concepto previo si aplica
                if (PredioActual.CuentaConInformeJuridicoPrevio)
                {
                    var cp = new ConceptosPrevio
                    {
                        IdRegistroProceso = rp.IdRegistroProceso,
                        FechaInforme = PredioActual.FechaInformePrevioReportada,
                        Concepto = PredioActual.ConceptoAntiguo
                    };
                    ctx.ConceptosPrevios.Add(cp);
                    var n4 = await ctx.SaveChangesAsync(); // probe 6
                    MessageBox.Show($"Save3 medidas={n3}  concepto={n4}");
                }

                await tx.CommitAsync();

                MessageBox.Show($"OK. IdRegistroProceso={rp.IdRegistroProceso}");
                Nuevo();
                ValidateAll();
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show("Error de BD:\n" + dbEx.GetBaseException().Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error no controlado:\n" + ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
        

        private void Cancelar() => PredioActual = _backup;

        // ===== Buscar y cargar =====
        private async Task BuscarAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FiltroId) &&
                    string.IsNullOrWhiteSpace(FiltroFmi) &&
                    string.IsNullOrWhiteSpace(FiltroExpediente))
                {
                    MessageBox.Show("Ingrese al menos un filtro.");
                    return;
                }

                using var ctx = new ViabilidadContext();
                var q = ctx.RegistroProcesos.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(FiltroId))
                    q = q.Where(x => x.IdPostulacion.StartsWith(FiltroId));
                if (!string.IsNullOrWhiteSpace(FiltroFmi))
                    q = q.Where(x => x.FMI.StartsWith(FiltroFmi));
                if (!string.IsNullOrWhiteSpace(FiltroExpediente))
                    q = q.Where(x => x.NumeroExpediente != null && x.NumeroExpediente.StartsWith(FiltroExpediente));

                var data = await q
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

                // === RegistroProceso -> Predio ===
                PredioActual.ID = rp.IdPostulacion;
                PredioActual.FMI = rp.FMI;
                PredioActual.NoExpediente = rp.NumeroExpediente;
                PredioActual.IdFuenteProceso = rp.IdFuenteProceso;
                PredioActual.IdTipoProceso = rp.IdTipoProceso;
                PredioActual.IdEtapaProcesal = rp.IdEtapaProcesal;
                PredioActual.RadicadoOrfeo = rp.RadicadoOrfeo;
                PredioActual.Dependencia = rp.Dependencia;

                // === EstudioTerreno -> Predio ===
                if (et is not null)
                {
                    PredioActual.AreaRegistral = et.AreaRegistral;
                    PredioActual.AreaCalculada = et.AreaCalculada;
                    PredioActual.CirculoRegistral = et.CirculoRegistral;

                    // 🔹 Nuevos mapeos:
                    PredioActual.TituloOriginario = et.AcreditacionPropiedad;
                    PredioActual.AnalisisNaturalezaUltimaTradicion = et.NaturalezaJuridica;

                    // Medidas asociadas
                    var medidas = await ctx.MedidaProcesals
                        .AsNoTracking()
                        .Where(m => m.IdEstudioTerreno == et.IdEstudioTerreno)
                        .ToListAsync();

                    if (medidas.Count > 0)
                        Medidas.LoadFrom(medidas);
                }

                // === Localización en cascada ===
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
                                c.Codigo == loc.CodigoCentroPoblado &&
                                c.IdLocalizacion == loc.IdLocalizacion);
                            SelectedCentro = cen;
                        }
                    }
                }

                ValidateAll();
                GuardarCommand.RaiseCanExecuteChanged();
                UpdateDebug();
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

        // ===== Predio change hook =====
        private void OnPredioChanged(object? s, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Predio.ID):
                    ValidateRequiredMaxLen(K(nameof(PredioActual.ID)), PredioActual.ID, 30);
                    break;

                case nameof(Predio.FMI):
                    ValidateRequiredMaxLen(K(nameof(PredioActual.FMI)), PredioActual.FMI, 100);
                    break;

                case nameof(Predio.AreaRegistral):
                    ValidateDecimalReq(K(nameof(PredioActual.AreaRegistral)), PredioActual.AreaRegistral);
                    break;

                case nameof(Predio.AreaCalculada):
                    ValidateDecimalReq(K(nameof(PredioActual.AreaCalculada)), PredioActual.AreaCalculada);
                    break;

                case nameof(Predio.NumeroIdentificacion):
                    if (!string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion))
                        ValidateRegex(
                            K(nameof(PredioActual.NumeroIdentificacion)),
                            PredioActual.NumeroIdentificacion,
                            @"^\d{1,19}([.-]?\d{1,19})*$",
                            "Ingrese solo números (opcional . o -).");
                    else
                        ClearErrors(K(nameof(PredioActual.NumeroIdentificacion)));
                    break;

                case nameof(Predio.IdFuenteProceso):
                case nameof(Predio.IdTipoProceso):
                case nameof(Predio.IdEtapaProcesal):
                    ValidateCatalogo(K(e.PropertyName!), (int?)typeof(Predio).GetProperty(e.PropertyName!)?.GetValue(PredioActual));
                    break;

                default:
                    break;
            }

            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }
    }
}
