using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using AppPrediosDemo.Models;
using AppPrediosDemo.ViewModels;

namespace AppPrediosDemo
{
    public partial class MainWindow : Window
    {
        private readonly PredioFormViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            UserNameTextBlock.Text = Environment.UserName;

            _vm = new PredioFormViewModel();
            DataContext = _vm;

            // Suscribirse al evento para cambiar a "Nuevo Registro"
            _vm.CambiarAPestañaNuevoRegistro += () =>
            {
                MostrarNuevoRegistro();
            };
            
            // Suscribirse a cambios en Modo para controlar la visibilidad en Consultar
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_vm.Modo))
                {
                    ActualizarVistaConsultar();
                }
            };

            Loaded += async (sender, e) =>
            {
                await _vm.InitializeAsync();
                
                // Por defecto mostrar "Nuevo Registro" y habilitar campos
                MostrarNuevoRegistro();
                if (_vm.Modo == ViewModels.ModoFormulario.Ninguno)
                {
                    _vm.HabilitarModoNuevo();
                }
            };
        }

        private void ProbarConexionBD()
        {
            try
            {
                using var db = new ViabilidadContext();
                var tipos = db.TipoProcesos.Take(5).ToList();

                string msg = tipos.Count > 0
                    ? "Conexión OK.\n\nTipos de proceso:\n" + string.Join("\n", tipos.Select(t => t.NombreTipoProceso))
                    : "Conexión OK, pero TipoProceso está vacío.";
                MessageBox.Show(msg, "Prueba de conexión");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión:\n{ex.Message}", "Prueba de conexión");
            }
        }

        private void ProbarInsercionesEF()
        {
            try
            {
                using var ctx = new ViabilidadContext();

                int fuenteId = ctx.FuenteProcesos.Select(x => x.IdFuenteProceso).FirstOrDefault();
                int tipoId = ctx.TipoProcesos.Select(x => x.IdTipoProceso).FirstOrDefault();
                int etapaId = ctx.EtapaProcesals.Select(x => x.IdEtapaProcesal).FirstOrDefault();
                int locId = ctx.Localizacions.Select(x => x.IdLocalizacion).FirstOrDefault();

                if (fuenteId == 0 || tipoId == 0 || etapaId == 0 || locId == 0)
                {
                    MessageBox.Show("Faltan datos en catálogos o Localizacion. Inserta registros base antes de la prueba.");
                    return;
                }

                var rp = new RegistroProceso
                {
                    IdPostulacion = "P_TEST",
                    FMI = "FMI-TEST",
                    IdFuenteProceso = fuenteId,
                    IdTipoProceso = tipoId,
                    IdEtapaProcesal = etapaId
                };
                ctx.RegistroProcesos.Add(rp);
                ctx.SaveChanges();

                var et = new EstudioTerreno
                {
                    IdRegistroProceso = rp.IdRegistroProceso,
                    IdLocalizacion = locId,
                    AreaRegistral = 1m,
                    AreaCalculada = 1m
                };
                ctx.EstudioTerrenos.Add(et);
                ctx.SaveChanges();

                var mp = new MedidaProcesal
                {
                    IdEstudioTerreno = et.IdEstudioTerreno,
                    Objeto = "Prueba",
                    Valor = "A"
                };
                ctx.MedidaProcesals.Add(mp);

                var cp = new ConceptosPrevio
                {
                    IdRegistroProceso = rp.IdRegistroProceso,
                    FechaInforme = DateTime.Now,
                    Concepto = "Prueba EF Core"
                };
                ctx.ConceptosPrevios.Add(cp);

                ctx.SaveChanges();

                MessageBox.Show(
                    $"IDs generados:\n" +
                    $"RegistroProceso = {rp.IdRegistroProceso}\n" +
                    $"EstudioTerreno  = {et.IdEstudioTerreno}\n" +
                    $"MedidaProcesal  = {mp.IdMedidasProcesal}\n" +
                    $"ConceptosPrevio = {cp.IdGestionJuridica}",
                    "Prueba EF + SEQUENCE");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en prueba EF:\n{ex}", "Prueba EF + SEQUENCE");
            }
        }

        private void BtnNuevoRegistro_Click(object sender, RoutedEventArgs e)
        {
            MostrarNuevoRegistro();
        }

        private void BtnConsultar_Click(object sender, RoutedEventArgs e)
        {
            MostrarConsultar();
        }

        private void MostrarNuevoRegistro()
        {
            if (NuevoRegistroContent != null && ConsultarContent != null)
            {
                NuevoRegistroContent.Visibility = Visibility.Visible;
                ConsultarContent.Visibility = Visibility.Collapsed;
                
                // Mostrar solo el botón "Consultar", ocultar "Nuevo Registro"
                if (BtnNuevoRegistro != null && BtnConsultar != null)
                {
                    BtnNuevoRegistro.Visibility = Visibility.Collapsed;
                    BtnConsultar.Visibility = Visibility.Visible;
                }
                
                // Mostrar título correspondiente
                if (TituloConsultar != null && TituloNuevoRegistro != null)
                {
                    TituloConsultar.Visibility = Visibility.Visible;
                    TituloNuevoRegistro.Visibility = Visibility.Collapsed;
                }
                
                // Habilitar campos si es necesario
                if (_vm.Modo == ViewModels.ModoFormulario.Ninguno)
                {
                    _vm.HabilitarModoNuevo();
                }
            }
        }

        private void MostrarConsultar()
        {
            if (NuevoRegistroContent != null && ConsultarContent != null)
            {
                NuevoRegistroContent.Visibility = Visibility.Collapsed;
                ConsultarContent.Visibility = Visibility.Visible;
                
                // Mostrar solo el botón "Nuevo Registro", ocultar "Consultar"
                if (BtnNuevoRegistro != null && BtnConsultar != null)
                {
                    BtnNuevoRegistro.Visibility = Visibility.Visible;
                    BtnConsultar.Visibility = Visibility.Collapsed;
                }
                
                // Mostrar título correspondiente
                if (TituloConsultar != null && TituloNuevoRegistro != null)
                {
                    TituloConsultar.Visibility = Visibility.Collapsed;
                    TituloNuevoRegistro.Visibility = Visibility.Visible;
                }
                
                // Actualizar la vista de consultar según el modo
                ActualizarVistaConsultar();
            }
        }
        
        private void ActualizarVistaConsultar()
        {
            if (ConsultarBusquedaView == null || ConsultarEdicionView == null)
                return;
                
            // Si estamos en modo Edicion, mostrar el formulario de edición, sino mostrar la búsqueda
            if (_vm.Modo == ViewModels.ModoFormulario.Edicion)
            {
                ConsultarBusquedaView.Visibility = Visibility.Collapsed;
                ConsultarEdicionView.Visibility = Visibility.Visible;
            }
            else
            {
                ConsultarBusquedaView.Visibility = Visibility.Visible;
                ConsultarEdicionView.Visibility = Visibility.Collapsed;
            }
        }
    }
}
