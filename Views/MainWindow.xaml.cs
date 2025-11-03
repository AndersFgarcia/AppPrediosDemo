using System;
using System.Linq;
using System.Windows;
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

            // Instancia del VM (el constructor del VM NO toca la BD)
            _vm = new PredioFormViewModel();
            DataContext = _vm;

            // Carga de catálogos/cascadas SOLO en tiempo de ejecución (no en diseñador)
            Loaded += async (_, __) =>
            {
                try
                {
                    await _vm.InitializeAsync(); // adentro llama LoadAsync() y luego ValidateAll()
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Init:\n" + ex.Message, "Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        // ====================== UTILIDADES DE PRUEBA (OPCIONAL, NO SE INVOCAN) ======================
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

                // 1) RegistroProceso -> PK por SEQUENCE
                var rp = new RegistroProceso
                {
                    IdPostulacion = "P_TEST",
                    FMI = "FMI-TEST",
                    IdFuenteProceso = fuenteId,
                    IdTipoProceso = tipoId,
                    IdEtapaProcesal = etapaId
                };
                ctx.RegistroProcesos.Add(rp);
                ctx.SaveChanges(); // genera rp.IdRegistroProceso

                // 2) EstudioTerreno -> PK por SEQUENCE
                var et = new EstudioTerreno
                {
                    IdRegistroProceso = rp.IdRegistroProceso,
                    IdLocalizacion = locId,
                    AreaRegistral = 1m,
                    AreaCalculada = 1m
                };
                ctx.EstudioTerrenos.Add(et);
                ctx.SaveChanges();

                // 3) MedidaProcesal -> PK por SEQUENCE
                var mp = new MedidaProcesal
                {
                    IdEstudioTerreno = et.IdEstudioTerreno,
                    Objeto = "Prueba",
                    Valor = "A"
                };
                ctx.MedidaProcesals.Add(mp);

                // 4) ConceptosPrevio -> PK por SEQUENCE (propiedad = IdGestionJuridica)
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
    }
}
