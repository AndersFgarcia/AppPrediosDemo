using AppPrediosDemo.Infrastructure;
using AppPrediosDemo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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

    public enum ModoFormulario
    {
        Ninguno,   // solo lectura, campos deshabilitados
        Nuevo,     // creando registro, campos habilitados, Guardar ejecuta INSERT
        Edicion    // editando uno existente, campos habilitados, Guardar ejecuta UPDATE
    }

    public sealed record ItemCatalogo(int Codigo, string Nombre);
    public sealed record CentroItem(int Codigo, string Nombre, int IdLocalizacion);

    public sealed record PredioListado(
        long IdRegistroProceso,          // interno, para cargar el registro
        string IdPostulacion,            // este es el ID que verás en la grilla
        string FMI,
        string? NumeroExpediente,
        string? AbogadoSustanciador,
        string? AbogadoRevisor,
        DateTime? FechaAsignacionReparto,
        DateTime? FechaEntregaARevisor
    );


    public class PredioFormViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        // ===== Estado =====
        private Predio? _prevPredio;
        private Predio _predioActual = new();

        private long? _idRegistroActual; // ID del registro que se edita.

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

        private ModoFormulario _modo = ModoFormulario.Ninguno;
        public ModoFormulario Modo
        {
            get => _modo;
            private set
            {
                if (Set(ref _modo, value))
                {
                    OnPropertyChanged(nameof(PuedeEditarCampos));
                    GuardarCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Para bindings de IsEnabled / IsReadOnly
        public bool PuedeEditarCampos =>
            Modo == ModoFormulario.Nuevo || Modo == ModoFormulario.Edicion;

        private static string K(string prop) => prop;

        // Sub-VM
        public MedidasProcesalesViewModel Medidas { get; } = new();

        // ===== Busy =====
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

        // ===== Listas varias =====
        public ObservableCollection<string> OpcionesViabilidad { get; } =
            new() { "Sin definir", "Pendiente", "Viable", "Viabilidad parcial", "No viable" };

        // Catálogos ConceptoFinal
        public ObservableCollection<CatalogOption> TiposInforme { get; } = new();
        public ObservableCollection<CatalogOption> TiposEstadoRevision { get; } = new();

        public ObservableCollection<string> OpcionesEntregaCarpeta { get; } =
            new() { "", "SI", "NO", "PENDIENTE" };

        // ===== Buscador =====
        public ObservableCollection<PredioListado> ResultadosBusqueda { get; } = new();

        private PredioListado? _resultadoSeleccionado;
        public PredioListado? ResultadoSeleccionado
        {
            get => _resultadoSeleccionado;
            set
            {
                if (Set(ref _resultadoSeleccionado, value))
                {
                    EditarRegistroSeleccionadoCommand.RaiseCanExecuteChanged();
                    // Ya no cargamos automáticamente al seleccionar, solo cuando se presiona Editar
                }
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
        public RelayCommand EditarRegistroSeleccionadoCommand { get; }
        
        // Evento para notificar cambio de pestaña
        public event Action? CambiarAPestañaNuevoRegistro;

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
            NuevoCommand = new RelayCommand(Nuevo, () => true);
            GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar);
            CancelarCommand = new RelayCommand(Cancelar, () => true);
            BuscarRegistrosCommand = new RelayCommand(async () => await BuscarAsync(), () => true);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros, () => true);
            EditarRegistroSeleccionadoCommand = new RelayCommand(EditarRegistroSeleccionado, () => ResultadoSeleccionado != null);
            PredioActual = new Predio();

            Modo = ModoFormulario.Ninguno;

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
            // Si no estamos en Nuevo o Edición, no se puede guardar
            if (Modo == ModoFormulario.Ninguno)
                return false;

            // CLAVE: si hay cualquier error de validación (borde rojo), NO guardar
            if (HasErrors)
                return false;

            // Requisitos mínimos de negocio
            return
                // Identificación
                !string.IsNullOrWhiteSpace(PredioActual.ID) &&
                !string.IsNullOrWhiteSpace(PredioActual.FMI) &&

                // Localización
                IdLocalizacionSeleccionada.HasValue &&

                // Área calculada obligatoria
                PredioActual.AreaCalculada.HasValue &&

                // Asignación y revisión obligatorios
                !string.IsNullOrWhiteSpace(PredioActual.AbogadoSustanciadorAsignado) &&
                !string.IsNullOrWhiteSpace(PredioActual.AbogadoRevisorAsignado) &&
                PredioActual.FechaEntregaARevisor.HasValue &&
                PredioActual.FechaAsignacionReparto.HasValue &&
                PredioActual.FechaPlazoEntregaARevisor.HasValue;
        }



        private async Task LoadAsync()
        {
            try
            {
                await CargarCatalogosAsync();
                await CargarDepartamentosAsync();
            }
            catch (SqlException sqlEx)
            {
                string mensaje = "No se pudo conectar a la base de datos.\n\n" +
                               "Verifique:\n" +
                               "1. Que el servidor SQL Server esté accesible\n" +
                               "2. Que la cadena de conexión sea correcta\n" +
                               "3. Que tenga permisos de acceso\n\n" +
                               "Puede configurar la conexión editando el archivo 'connectionstring.txt' en el directorio de la aplicación.\n\n" +
                               $"Error: {sqlEx.Message}";
                MessageBox.Show(mensaje, "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (System.Net.Sockets.SocketException)
            {
                string mensaje = "No se pudo conectar al servidor de base de datos.\n\n" +
                               "Verifique que:\n" +
                               "- El servidor esté en ejecución\n" +
                               "- La red esté disponible\n" +
                               "- La dirección del servidor sea correcta\n\n" +
                               "Puede configurar la conexión editando el archivo 'connectionstring.txt' en el directorio de la aplicación.";
                MessageBox.Show(mensaje, "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cadena de conexión"))
            {
                string mensaje = "No se encontró la cadena de conexión.\n\n" +
                               "Cree un archivo 'connectionstring.txt' en el directorio de la aplicación con la siguiente línea:\n\n" +
                               "Server=nombre_servidor;Database=ViabilidadJuridica;User Id=usuario;Password=contraseña;Encrypt=False;TrustServerCertificate=True";
                MessageBox.Show(mensaje, "Configuración Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string mensaje = "Error al inicializar la aplicación:\n\n" + ex.Message;
                MessageBox.Show(mensaje, "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== Carga Catálogos =====
        private async Task CargarCatalogosAsync()
        {
            await CargarTipoProcesosAsync();
            await CargarFuentesProcesoAsync();
            await CargarEtapasProcesalesAsync();

            await CargarTiposInformeAsync();
            await CargarTiposEstadoRevisionAsync();
        }

        private static void Rellenar<T>(ObservableCollection<T> target, IEnumerable<T> data)
        {
            target.Clear();
            foreach (var x in data) target.Add(x);
        }

        private async Task CargarTipoProcesosAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();
                var data = await ctx.TipoProcesos
                    .AsNoTracking()
                    .OrderBy(x => x.NombreTipoProceso)
                    .Select(x => new CatalogOption { Id = x.IdTipoProceso, Nombre = x.NombreTipoProceso })
                    .ToListAsync();
                Rellenar(TipoProcesos, data);
            }
            catch
            {
            }
        }

        private async Task CargarFuentesProcesoAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();
                var data = await ctx.FuenteProcesos
                    .AsNoTracking()
                    .OrderBy(x => x.NombreFuenteProceso)
                    .Select(x => new CatalogOption { Id = x.IdFuenteProceso, Nombre = x.NombreFuenteProceso })
                    .ToListAsync();
                Rellenar(FuentesProceso, data);
            }
            catch
            {
            }
        }

        private async Task CargarEtapasProcesalesAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();
                var data = await ctx.EtapaProcesals
                    .AsNoTracking()
                    .OrderBy(x => x.NombreEtapaProcesal)
                    .Select(x => new CatalogOption { Id = x.IdEtapaProcesal, Nombre = x.NombreEtapaProcesal })
                    .ToListAsync();
                Rellenar(EtapasProcesales, data);
            }
            catch
            {
            }
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
            catch
            {
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
            catch
            {
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
            catch
            {
            }
        }

        private async Task CargarTiposInformeAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();
                var data = await ctx.TipoInformes
                    .AsNoTracking()
                    .OrderBy(x => x.NombreTipoInforme)
                    .Select(x => new CatalogOption { Id = x.IdTipoInforme, Nombre = x.NombreTipoInforme })
                    .ToListAsync();

                TiposInforme.Clear();
                foreach (var item in data) TiposInforme.Add(item);
            }
            catch
            {
            }
        }

        private async Task CargarTiposEstadoRevisionAsync()
        {
            try
            {
                using var ctx = new ViabilidadContext();
                var data = await ctx.TipoEstadoRevisions
                    .AsNoTracking()
                    .OrderBy(x => x.NombreTipoEstadoRevision)
                    .Select(x => new CatalogOption { Id = x.IdTipoEstadoRevision, Nombre = x.NombreTipoEstadoRevision })
                    .ToListAsync();

                TiposEstadoRevision.Clear();
                foreach (var item in data) TiposEstadoRevision.Add(item);
            }
            catch
            {
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
            // ID y FMI
            ValidateRequiredMaxLen(K(nameof(PredioActual.ID)), PredioActual.ID, 30);
            ValidateRequiredMaxLen(K(nameof(PredioActual.FMI)), PredioActual.FMI, 100);

            // Identificación del titular (sigue igual, opcional)
            if (!string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion))
            {
                ValidateRegex(
                    K(nameof(PredioActual.NumeroIdentificacion)),
                    PredioActual.NumeroIdentificacion,
                    @"^\d{1,19}([.-]?\d{1,19})*$",
                    "Ingrese solo números (opcional . o -).");
            }

            // Áreas:
            // - Registral: opcional, pero no negativa
            // - Calculada: obligatoria
            ValidateDecimalOptional(K(nameof(PredioActual.AreaRegistral)), PredioActual.AreaRegistral);
            ValidateDecimalReq(K(nameof(PredioActual.AreaCalculada)), PredioActual.AreaCalculada);

            // Catálogos de proceso: ya no obligatorios por regla de negocio nueva
            // (si quieres seguir exigiéndolos, vuelve a activar estas líneas)
            //ValidateCatalogo(K(nameof(PredioActual.IdFuenteProceso)), PredioActual.IdFuenteProceso);
            //ValidateCatalogo(K(nameof(PredioActual.IdTipoProceso)), PredioActual.IdTipoProceso);
            //ValidateCatalogo(K(nameof(PredioActual.IdEtapaProcesal)), PredioActual.IdEtapaProcesal);

            // Localización
            ValidateLocalizacion();

            // Asignación y revisión: nuevos obligatorios
            ValidateRequiredString(K(nameof(PredioActual.AbogadoSustanciadorAsignado)), PredioActual.AbogadoSustanciadorAsignado);
            ValidateRequiredString(K(nameof(PredioActual.AbogadoRevisorAsignado)), PredioActual.AbogadoRevisorAsignado);
            ValidateDateReq(K(nameof(PredioActual.FechaEntregaARevisor)), PredioActual.FechaEntregaARevisor);
            ValidateDateReq(K(nameof(PredioActual.FechaAsignacionReparto)), PredioActual.FechaAsignacionReparto);
            ValidateDateReq(K(nameof(PredioActual.FechaPlazoEntregaARevisor)), PredioActual.FechaPlazoEntregaARevisor);

            ValidateNumeroReparto();

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
            ClearErrors(key);

            if (string.IsNullOrWhiteSpace(value))
                return;

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
        private void ValidateDecimalOptional(string key, decimal? value)
        {
            ClearErrors(key);
            if (value < 0)
                AddError(key, "El valor no puede ser negativo.");
        }

        private void ValidateRequiredString(string key, string? value)
        {
            ClearErrors(key);
            if (string.IsNullOrWhiteSpace(value))
                AddError(key, "Campo obligatorio.");
        }

        private void ValidateDateReq(string key, DateTime? value)
        {
            ClearErrors(key);
            if (!value.HasValue)
                AddError(key, "Campo obligatorio.");
        }
        private void ValidateNumeroReparto()
        {
            string key = K(nameof(PredioActual.NumeroReparto)); // "NumeroReparto"
            ClearErrors(key);

            var v = PredioActual.NumeroReparto;

            // Vacío es válido (campo opcional)
            if (string.IsNullOrWhiteSpace(v))
                return;

            if (!int.TryParse(v, out _))
                AddError(key, "# Reparto debe ser un número entero.");
        }
        private bool ValidarCamposCriticosAntesDeGuardar()
        {
            // 1) Número de identificación (CC/NIT):
            if (!string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        PredioActual.NumeroIdentificacion,
                        @"^\d{1,19}([.-]?\d{1,19})*$"))
                {
                    MessageBox.Show(
                        "Número de identificación inválido. Use solo números (opcionalmente . o -).",
                        "Validación");
                    return false;
                }
            }

            // 2) # Reparto: solo enteros
            if (!string.IsNullOrWhiteSpace(PredioActual.NumeroReparto) &&
                !int.TryParse(PredioActual.NumeroReparto, out _))
            {
                MessageBox.Show("# Reparto debe ser un número entero.", "Validación");
                return false;
            }

            return true;
        }


        // ===== Comandos =====
        private Predio _backup = new();
        
        // Método público para habilitar modo nuevo sin limpiar datos
        public void HabilitarModoNuevo()
        {
            if (Modo == ModoFormulario.Ninguno)
            {
                Modo = ModoFormulario.Nuevo;
            }
        }
        
        private void Nuevo()
        {

            PredioActual = new Predio();
            SelectedDepartamento = null;
            SelectedMunicipio = null;
            SelectedCentro = null;
            Municipios.Clear();
            CentrosPoblados.Clear();
            IdLocalizacionSeleccionada = null;
            ResultadosBusqueda.Clear();
            ResultadoSeleccionado = null;
            LimpiarDatosFormulario();    // deja todo en blanco
            Modo = ModoFormulario.Nuevo; // habilita campos

            ValidateAll();
            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();

        }
        private void LimpiarDatosFormulario()
        {
            // 1) Modelo principal: limpia Identificación, Ubicación, Concepto, Asignación, etc.
            PredioActual = new Predio();

            // 2) Cascada de localización (combo de depto/muni/centro)
            SelectedDepartamento = null;
            SelectedMunicipio = null;
            SelectedCentro = null;
            Municipios.Clear();
            CentrosPoblados.Clear();
            IdLocalizacionSeleccionada = null;

            // 3) Medidas procesales (Gravámenes y afectaciones)
            //    Esto deja todos los combos/caixas de esa pestaña en blanco.
            Medidas.LoadFrom(Enumerable.Empty<MedidaProcesal>());

            // 4) Búsqueda / registro actual
            _idRegistroActual = null;
            ResultadoSeleccionado = null;

            // 5) Validación y debug
            _errors.Clear();
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(string.Empty));

            ValidateAll();
            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }
        // Limpia TODOS los datos de la pantalla y deja el formulario en solo lectura
        private void ResetDespuesDeGuardar()
        {
            // Modelo principal
            PredioActual = new Predio();

            // Id del registro en edición
            _idRegistroActual = null;

            // Localización y combos en cascada
            SelectedDepartamento = null;
            SelectedMunicipio = null;
            SelectedCentro = null;
            Municipios.Clear();
            CentrosPoblados.Clear();
            IdLocalizacionSeleccionada = null;

            // Medidas procesales (pestaña Gravámenes y afectaciones)
            Medidas.Limpiar();   // método nuevo que vas a crear en el VM de medidas

            // Conceptos / fechas / etc. también quedan en blanco porque PredioActual es nuevo

            // Estado del formulario
            Modo = ModoFormulario.Ninguno;

            // Recalcular validaciones y estado del botón Guardar
            ValidateAll();
            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }

        private async Task GuardarAsync()
        {
            ValidateAll();

            // Bloqueo duro para identificación y # Reparto
            if (!ValidarCamposCriticosAntesDeGuardar())
                return;

            if (HasErrors || IdLocalizacionSeleccionada is null)
            {
                MessageBox.Show("Revise los datos obligatorios antes de guardar.");
                return;
            }

            if (Modo == ModoFormulario.Ninguno)
            {
                MessageBox.Show("El formulario no está en modo de Edición o Creación.");
                return;
            }

            try
            {
                IsBusy = true;

                if (Modo == ModoFormulario.Nuevo)
                {
                    await GuardarNuevoAsync();   // INSERT
                    MessageBox.Show("Registro creado correctamente.");
                }
                else if (Modo == ModoFormulario.Edicion)
                {
                    await ActualizarAsync();     // UPDATE
                    MessageBox.Show("Registro actualizado correctamente.");
                }

                ResetDespuesDeGuardar();
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


        private async Task GuardarNuevoAsync()
        {
            IsBusy = true;
            try
            {
                var medidas = Medidas.ToEntities(0);

                await using var ctx = new ViabilidadContext();
                await using var tx = await ctx.Database.BeginTransactionAsync();

                // ===== RegistroProceso =====
                var rp = new RegistroProceso
                {
                    IdPostulacion = PredioActual.ID!,
                    FMI = PredioActual.FMI!,
                    NumeroExpediente = PredioActual.NoExpediente,

                    // OPCIONALES: sin .Value
                    IdFuenteProceso = PredioActual.IdFuenteProceso,
                    IdTipoProceso = PredioActual.IdTipoProceso,
                    IdEtapaProcesal = PredioActual.IdEtapaProcesal,

                    RadicadoOrfeo = PredioActual.RadicadoOrfeo,
                    Dependencia = PredioActual.Dependencia
                };
                ctx.RegistroProcesos.Add(rp);
                await ctx.SaveChangesAsync();

                // ===== EstudioTerreno =====
                var et = new EstudioTerreno
                {
                    IdRegistroProceso = rp.IdRegistroProceso,
                    IdLocalizacion = IdLocalizacionSeleccionada!.Value,
                    AreaRegistral = PredioActual.AreaRegistral,        // opcional
                    AreaCalculada = PredioActual.AreaCalculada!.Value, // obligatoria

                    CirculoRegistral = PredioActual.CirculoRegistral,
                    TipoPersonaTitular = PredioActual.PersonaTitular,
                    NombrePropietario = PredioActual.NombrePropietarios,
                    ApellidoPropietario = PredioActual.ApellidoPropietario,
                    Identificacion = string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion)
                        ? (long?)null
                        : long.Parse(new string(PredioActual.NumeroIdentificacion.Where(char.IsDigit).ToArray())),
                    NaturalezaJuridica = PredioActual.AnalisisNaturalezaUltimaTradicion,
                    AcreditacionPropiedad = PredioActual.TituloOriginario
                };

                ctx.EstudioTerrenos.Add(et);
                await ctx.SaveChangesAsync();

                // ===== Medidas =====
                foreach (var m in medidas)
                {
                    m.IdEstudioTerreno = et.IdEstudioTerreno;
                    ctx.MedidaProcesals.Add(m);
                }
                await ctx.SaveChangesAsync();

                // ===== Concepto previo (si aplica) =====
                if (PredioActual.CuentaConInformeJuridicoPrevio)
                {
                    var cp = new ConceptosPrevio
                    {
                        IdRegistroProceso = rp.IdRegistroProceso,
                        FechaInforme = PredioActual.FechaInformePrevioReportada,
                        Concepto = PredioActual.ConceptoAntiguo
                    };
                    ctx.ConceptosPrevios.Add(cp);
                    await ctx.SaveChangesAsync();
                }

                int? nroReparto = null;
                if (!string.IsNullOrWhiteSpace(PredioActual.NumeroReparto))
                {
                    if (!int.TryParse(PredioActual.NumeroReparto, out var tmp))
                    {
                        MessageBox.Show("El # Reparto debe ser un número entero.", "Validación");
                        await tx.RollbackAsync();
                        return;
                    }
                    nroReparto = tmp;
                }

                // Buscar ConceptoFinal (siempre será null al guardar nuevo, pero usamos la lógica de upsert)
                var cf = await ctx.ConceptoFinals
                    .FirstOrDefaultAsync(c => c.IdRegistroProceso == rp.IdRegistroProceso);

                if (cf is null)
                {
                    cf = new ConceptoFinal { IdRegistroProceso = rp.IdRegistroProceso };
                    ctx.ConceptoFinals.Add(cf);
                }

                // Asignar todos los campos del Concepto Final
                cf.ConceptoActualDeViabilidadJuridica = PredioActual.AnalisisJuridicoFinal;
                cf.FechaInforme = PredioActual.FechaInforme;
                cf.Viabilidad = string.IsNullOrWhiteSpace(PredioActual.Viabilidad)
                    ? "Sin definir"
                    : PredioActual.Viabilidad;
                cf.IdTipoInforme = PredioActual.IdTipoInforme.HasValue
                    ? (byte?)PredioActual.IdTipoInforme.Value
                    : null;
                cf.CausalNoViabilidad = PredioActual.CausalNoViabilidad;
                cf.InsumosPendientes = PredioActual.InsumosPendientes;

                // Reparto/Revisión
                cf.FechaEntregaARevisor = PredioActual.FechaEntregaARevisor;
                cf.AbogadoSustanciadorAsignado = PredioActual.AbogadoSustanciadorAsignado;
                cf.AbogadoRevisorAsignado = PredioActual.AbogadoRevisorAsignado;
                cf.NroReparto = nroReparto;
                cf.FechaAsignacionReparto = PredioActual.FechaAsignacionReparto;
                cf.FechaPlazoEntregaARevisor = PredioActual.FechaPlazoEntregaARevisor;

                // Cierre/Coordinación
                cf.IdTipoEstadoRevision = PredioActual.IdTipoEstadoRevision.HasValue
                    ? (byte?)PredioActual.IdTipoEstadoRevision.Value
                    : null;
                cf.ObservacionesRevisor = PredioActual.ObservacionesRevisor;
                cf.EntregoCarpetaSoportes = string.IsNullOrWhiteSpace(PredioActual.EntregoCarpetaSoportes)
                    ? null
                    : PredioActual.EntregoCarpetaSoportes;
                cf.FechaEnvioACoordinacion = PredioActual.FechaEnvioACoordinacion;
                cf.EstadoAprobacionCoordinadora = PredioActual.EstadoAprobacionCoordinadora;
                cf.FechaRemisionSoportesAGestoraDocumental = PredioActual.FechaRemisionSoportesGestoraDocumental;
                cf.FechaRemisionInformeAGestoraDocumental = PredioActual.FechaRemisionInformeGestoraDocumental;
                cf.FechaCargueInformeJuridicoExpOrfeo = PredioActual.FechaCargueInformeJuridicoEnExpteOrfeo;
                cf.FechaDeCargueDocsYSoportesExpOrfeo = PredioActual.FechaCargueDocumentosYSoportesEnExpdteOrfeo;
                cf.FechaGestionEtapaSit = PredioActual.FechaGestionEtapaSIT;

                await ctx.SaveChangesAsync(); // Guardar el ConceptoFinal
            
                await tx.CommitAsync();

            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ActualizarAsync()
        {
            if (!_idRegistroActual.HasValue)
            {
                MessageBox.Show("No hay un registro cargado para actualizar.");
                return;
            }

            IsBusy = true;
            try
            {
                var id = _idRegistroActual.Value;
                var medidas = Medidas.ToEntities(0);

                await using var ctx = new ViabilidadContext();
                await using var tx = await ctx.Database.BeginTransactionAsync();

                // ===== RegistroProceso =====
                var rp = await ctx.RegistroProcesos
                    .FirstOrDefaultAsync(x => x.IdRegistroProceso == id);

                if (rp is null)
                {
                    MessageBox.Show("No se encontró el registro a actualizar.");
                    await tx.RollbackAsync();
                    return;
                }

                rp.IdPostulacion = PredioActual.ID!;
                rp.FMI = PredioActual.FMI!;
                rp.NumeroExpediente = PredioActual.NoExpediente;

                // OPCIONALES: sin .Value
                rp.IdFuenteProceso = PredioActual.IdFuenteProceso;
                rp.IdTipoProceso = PredioActual.IdTipoProceso;
                rp.IdEtapaProcesal = PredioActual.IdEtapaProcesal;

                rp.RadicadoOrfeo = PredioActual.RadicadoOrfeo;
                rp.Dependencia = PredioActual.Dependencia;


                await ctx.SaveChangesAsync();

                // ===== EstudioTerreno =====
                var et = await ctx.EstudioTerrenos
                    .Where(x => x.IdRegistroProceso == id)
                    .OrderByDescending(x => x.IdEstudioTerreno)
                    .FirstOrDefaultAsync();

                if (et is null)
                {
                    et = new EstudioTerreno { IdRegistroProceso = id };
                    ctx.EstudioTerrenos.Add(et);
                    await ctx.SaveChangesAsync();
                }

                et.IdLocalizacion = IdLocalizacionSeleccionada!.Value;
                et.AreaRegistral = PredioActual.AreaRegistral;        // opcional, sin .Value
                et.AreaCalculada = PredioActual.AreaCalculada!.Value; // obligatoria

                et.CirculoRegistral = PredioActual.CirculoRegistral;
                et.TipoPersonaTitular = PredioActual.PersonaTitular;
                et.NombrePropietario = PredioActual.NombrePropietarios;
                et.ApellidoPropietario = PredioActual.ApellidoPropietario;
                et.Identificacion = string.IsNullOrWhiteSpace(PredioActual.NumeroIdentificacion)
                    ? (long?)null
                    : long.Parse(new string(PredioActual.NumeroIdentificacion.Where(char.IsDigit).ToArray()));
                et.NaturalezaJuridica = PredioActual.AnalisisNaturalezaUltimaTradicion;
                et.AcreditacionPropiedad = PredioActual.TituloOriginario;


                await ctx.SaveChangesAsync();

                // ===== Medidas (reemplazar todas) =====
                var existentes = ctx.MedidaProcesals
                    .Where(m => m.IdEstudioTerreno == et.IdEstudioTerreno);
                ctx.MedidaProcesals.RemoveRange(existentes);
                await ctx.SaveChangesAsync();

                foreach (var m in medidas)
                {
                    m.IdEstudioTerreno = et.IdEstudioTerreno;
                    ctx.MedidaProcesals.Add(m);
                }
                await ctx.SaveChangesAsync();

                // ===== Concepto PREVIO =====
                var cp = await ctx.ConceptosPrevios
                    .FirstOrDefaultAsync(x => x.IdRegistroProceso == id);

                if (PredioActual.CuentaConInformeJuridicoPrevio)
                {
                    if (cp is null)
                    {
                        cp = new ConceptosPrevio { IdRegistroProceso = id };
                        ctx.ConceptosPrevios.Add(cp);
                    }

                    cp.FechaInforme = PredioActual.FechaInformePrevioReportada;
                    cp.Concepto = PredioActual.ConceptoAntiguo;
                }
                else if (cp is not null)
                {
                    ctx.ConceptosPrevios.Remove(cp);
                }

                await ctx.SaveChangesAsync();

                // ===== Concepto FINAL =====
                bool tieneConceptoFinal =
                    !string.IsNullOrWhiteSpace(PredioActual.AnalisisJuridicoFinal) ||
                    PredioActual.FechaInforme.HasValue ||
                    PredioActual.Viabilidad != null ||
                    PredioActual.IdTipoInforme.HasValue ||
                    !string.IsNullOrWhiteSpace(PredioActual.CausalNoViabilidad) ||
                    !string.IsNullOrWhiteSpace(PredioActual.InsumosPendientes) ||
                    PredioActual.FechaEntregaARevisor.HasValue ||
                    PredioActual.IdTipoEstadoRevision.HasValue ||
                    !string.IsNullOrWhiteSpace(PredioActual.ObservacionesRevisor) ||
                    !string.IsNullOrWhiteSpace(PredioActual.EntregoCarpetaSoportes) ||
                    PredioActual.FechaEnvioACoordinacion.HasValue ||
                    !string.IsNullOrWhiteSpace(PredioActual.EstadoAprobacionCoordinadora) ||
                    PredioActual.FechaRemisionSoportesGestoraDocumental.HasValue ||
                    PredioActual.FechaRemisionInformeGestoraDocumental.HasValue ||
                    PredioActual.FechaCargueInformeJuridicoEnExpteOrfeo.HasValue ||
                    PredioActual.FechaCargueDocumentosYSoportesEnExpdteOrfeo.HasValue ||
                    PredioActual.FechaGestionEtapaSIT.HasValue;

                var cf = await ctx.ConceptoFinals
                    .FirstOrDefaultAsync(c => c.IdRegistroProceso == id);

                if (tieneConceptoFinal)
                {
                    int? nroReparto = null;
                    if (!string.IsNullOrWhiteSpace(PredioActual.NumeroReparto))
                    {
                        if (!int.TryParse(PredioActual.NumeroReparto, out var tmp))
                        {
                            MessageBox.Show("El # Reparto debe ser un número entero.", "Validación");
                            await tx.RollbackAsync();
                            return;
                        }
                        nroReparto = tmp;
                    }

                    if (cf is null)
                    {
                        cf = new ConceptoFinal { IdRegistroProceso = id };
                        ctx.ConceptoFinals.Add(cf);
                    }

                    cf.ConceptoActualDeViabilidadJuridica = PredioActual.AnalisisJuridicoFinal;
                    cf.FechaInforme = PredioActual.FechaInforme;
                    cf.Viabilidad = string.IsNullOrWhiteSpace(PredioActual.Viabilidad)
                        ? "Sin definir"
                        : PredioActual.Viabilidad;

                    cf.IdTipoInforme = PredioActual.IdTipoInforme.HasValue
                        ? (byte?)PredioActual.IdTipoInforme.Value
                        : null;
                    cf.CausalNoViabilidad = PredioActual.CausalNoViabilidad;
                    cf.InsumosPendientes = PredioActual.InsumosPendientes;

                    cf.FechaEntregaARevisor = PredioActual.FechaEntregaARevisor;
                    cf.AbogadoSustanciadorAsignado = PredioActual.AbogadoSustanciadorAsignado;
                    cf.AbogadoRevisorAsignado = PredioActual.AbogadoRevisorAsignado;
                    cf.NroReparto = nroReparto;
                    cf.FechaAsignacionReparto = PredioActual.FechaAsignacionReparto;
                    cf.FechaPlazoEntregaARevisor = PredioActual.FechaPlazoEntregaARevisor;

                    cf.IdTipoEstadoRevision = PredioActual.IdTipoEstadoRevision.HasValue
                        ? (byte?)PredioActual.IdTipoEstadoRevision.Value
                        : null;
                    cf.ObservacionesRevisor = PredioActual.ObservacionesRevisor;

                    cf.EntregoCarpetaSoportes = string.IsNullOrWhiteSpace(PredioActual.EntregoCarpetaSoportes)
                        ? null
                        : PredioActual.EntregoCarpetaSoportes;

                    cf.FechaEnvioACoordinacion = PredioActual.FechaEnvioACoordinacion;
                    cf.EstadoAprobacionCoordinadora = PredioActual.EstadoAprobacionCoordinadora;
                    cf.FechaRemisionSoportesAGestoraDocumental = PredioActual.FechaRemisionSoportesGestoraDocumental;
                    cf.FechaRemisionInformeAGestoraDocumental = PredioActual.FechaRemisionInformeGestoraDocumental;
                    cf.FechaCargueInformeJuridicoExpOrfeo = PredioActual.FechaCargueInformeJuridicoEnExpteOrfeo;
                    cf.FechaDeCargueDocsYSoportesExpOrfeo = PredioActual.FechaCargueDocumentosYSoportesEnExpdteOrfeo;
                    cf.FechaGestionEtapaSit = PredioActual.FechaGestionEtapaSIT;
                }
                else if (cf is not null)
                {
                    ctx.ConceptoFinals.Remove(cf);
                }

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void Cancelar()
        {
            PredioActual = _backup;
            Modo = ModoFormulario.Ninguno;
        }

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
                .OrderByDescending(x => x.IdRegistroProceso)
                .Take(5)
                .Select(x => new PredioListado(
                    x.IdRegistroProceso,
                    x.IdPostulacion,  // ID que se mostrará

                    x.FMI,
                    x.NumeroExpediente,

                    // desde ConceptoFinal (si existe)
                    x.ConceptoFinals
                        .Select(cf => cf.AbogadoSustanciadorAsignado)
                        .FirstOrDefault(),
                    x.ConceptoFinals
                        .Select(cf => cf.AbogadoRevisorAsignado)
                        .FirstOrDefault(),
                    x.ConceptoFinals
                        .Select(cf => cf.FechaAsignacionReparto)
                        .FirstOrDefault(),
                    x.ConceptoFinals
                        .Select(cf => cf.FechaEntregaARevisor)
                        .FirstOrDefault()
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

                    PredioActual.PersonaTitular = et.TipoPersonaTitular;
                    PredioActual.NombrePropietarios = et.NombrePropietario;
                    PredioActual.ApellidoPropietario = et.ApellidoPropietario;
                    PredioActual.NumeroIdentificacion = et.Identificacion?.ToString();

                    PredioActual.TituloOriginario = et.AcreditacionPropiedad;
                    PredioActual.AnalisisNaturalezaUltimaTradicion = et.NaturalezaJuridica;

                    var medidas = await ctx.MedidaProcesals
                        .AsNoTracking()
                        .Where(m => m.IdEstudioTerreno == et.IdEstudioTerreno)
                        .ToListAsync();

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

                // === Concepto PREVIO ===
                var cp = await ctx.ConceptosPrevios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdRegistroProceso == idRegistroProceso);

                if (cp is not null)
                {
                    PredioActual.CuentaConInformeJuridicoPrevio = true;
                    PredioActual.FechaInformePrevioReportada = cp.FechaInforme;
                    PredioActual.ConceptoAntiguo = cp.Concepto;
                }
                else
                {
                    PredioActual.CuentaConInformeJuridicoPrevio = false;
                    PredioActual.FechaInformePrevioReportada = null;
                    PredioActual.ConceptoAntiguo = null;
                }

                // === Concepto FINAL ===
                var cf = await ctx.ConceptoFinals
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdRegistroProceso == idRegistroProceso);

                if (cf is not null)
                {
                    PredioActual.AnalisisJuridicoFinal = cf.ConceptoActualDeViabilidadJuridica;
                    PredioActual.FechaInforme = cf.FechaInforme;
                    PredioActual.Viabilidad = cf.Viabilidad;

                    PredioActual.IdTipoInforme = cf.IdTipoInforme;
                    PredioActual.CausalNoViabilidad = cf.CausalNoViabilidad;
                    PredioActual.InsumosPendientes = cf.InsumosPendientes;

                    PredioActual.FechaEntregaARevisor = cf.FechaEntregaARevisor;
                    PredioActual.AbogadoSustanciadorAsignado = cf.AbogadoSustanciadorAsignado;
                    PredioActual.AbogadoRevisorAsignado = cf.AbogadoRevisorAsignado;

                    PredioActual.NumeroReparto = cf.NroReparto?.ToString();

                    PredioActual.FechaAsignacionReparto = cf.FechaAsignacionReparto;
                    PredioActual.FechaPlazoEntregaARevisor = cf.FechaPlazoEntregaARevisor;

                    PredioActual.IdTipoEstadoRevision = cf.IdTipoEstadoRevision;
                    PredioActual.ObservacionesRevisor = cf.ObservacionesRevisor;

                    PredioActual.EntregoCarpetaSoportes = cf.EntregoCarpetaSoportes;

                    PredioActual.FechaEnvioACoordinacion = cf.FechaEnvioACoordinacion;
                    PredioActual.EstadoAprobacionCoordinadora = cf.EstadoAprobacionCoordinadora;
                    PredioActual.FechaRemisionSoportesGestoraDocumental = cf.FechaRemisionSoportesAGestoraDocumental;
                    PredioActual.FechaRemisionInformeGestoraDocumental = cf.FechaRemisionInformeAGestoraDocumental;
                    PredioActual.FechaCargueInformeJuridicoEnExpteOrfeo = cf.FechaCargueInformeJuridicoExpOrfeo;
                    PredioActual.FechaCargueDocumentosYSoportesEnExpdteOrfeo = cf.FechaDeCargueDocsYSoportesExpOrfeo;
                    PredioActual.FechaGestionEtapaSIT = cf.FechaGestionEtapaSit;
                }

                _idRegistroActual = idRegistroProceso;
                Modo = ModoFormulario.Edicion;

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

        private async void EditarRegistroSeleccionado()
        {
            if (ResultadoSeleccionado is null)
                return;

            await CargarPredioDesdeRegistroAsync(ResultadoSeleccionado.IdRegistroProceso);
            
            // Notificar para cambiar de pestaña
            CambiarAPestañaNuevoRegistro?.Invoke();
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
                    ValidateDecimalOptional(K(nameof(PredioActual.AreaRegistral)), PredioActual.AreaRegistral);
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


                case nameof(Predio.AbogadoSustanciadorAsignado):
                    ValidateRequiredString(
                        K(nameof(PredioActual.AbogadoSustanciadorAsignado)),
                        PredioActual.AbogadoSustanciadorAsignado);
                    break;

                case nameof(Predio.AbogadoRevisorAsignado):
                    ValidateRequiredString(
                        K(nameof(PredioActual.AbogadoRevisorAsignado)),
                        PredioActual.AbogadoRevisorAsignado);
                    break;

                case nameof(Predio.FechaEntregaARevisor):
                    ValidateDateReq(
                        K(nameof(PredioActual.FechaEntregaARevisor)),
                        PredioActual.FechaEntregaARevisor);
                    break;

                case nameof(Predio.FechaAsignacionReparto):
                    ValidateDateReq(
                        K(nameof(PredioActual.FechaAsignacionReparto)),
                        PredioActual.FechaAsignacionReparto);
                    break;
                case nameof(Predio.NumeroReparto):
                    ValidateNumeroReparto();
                    break;

                default:
                    break;

            }

            GuardarCommand.RaiseCanExecuteChanged();
            UpdateDebug();
        }

    }
}
