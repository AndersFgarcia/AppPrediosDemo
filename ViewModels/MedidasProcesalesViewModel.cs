using System.Collections.Generic;
using System.Collections.ObjectModel;
using AppPrediosDemo.Infrastructure;
using AppPrediosDemo.Models;

namespace AppPrediosDemo.ViewModels
{
    public class MedidasProcesalesViewModel : ViewModelBase
    {
        // Opciones de valor general para todos los ComboBox (Sí/No/Pendiente)
        public IReadOnlyList<string> ValoresSN { get; } = new[] { "SI", "NO", "PENDIENTE" };

        // Opciones combinadas para el ComboBox de clasificación de RUPTA (TipoClasificacion)
        public IReadOnlyList<string> RuptaClasificacionOpciones { get; } = new[]
        {
            "COLECTIVO",
            "INDIVIDUAL",
            "SI",
            "NO",
            "PENDIENTE"
        };

        public MedidaItemVM Hipoteca { get; }
        public MedidaItemVM Servidumbres { get; }
        public MedidaItemVM MedidasCautelares { get; }
        public MedidaItemVM Rupta { get; }
        public MedidaItemVM RTDAF { get; }
        public MedidaItemVM Oferta { get; }
        public MedidaItemVM Clarificacion { get; }

        public MedidasProcesalesViewModel()
        {
            Hipoteca = new MedidaItemVM(this, "HIPOTECA");
            Servidumbres = new MedidaItemVM(this, "SERVIDUMBRES");
            MedidasCautelares = new MedidaItemVM(this, "MEDIDAS CAUTELARES");
            Rupta = new MedidaItemVM(this, "RUPTA");
            RTDAF = new MedidaItemVM(this, "RTDAF LEY 1448");
            Oferta = new MedidaItemVM(this, "OFERTA OTRAS ENTIDADES");
            Clarificacion = new MedidaItemVM(this, "PROCESOS DE CLARIFICACIÓN");
        }

        // Método para convertir los datos del ViewModel a entidades (Modelos) para guardar en la DB
        public List<MedidaProcesal> ToEntities(int idEstudioTerreno)
        {
            var list = new List<MedidaProcesal>();

            // Lógica para añadir una medida procesal a la lista solo si tiene datos
            void Add(MedidaItemVM vm, bool incluirTipo = false)
            {
                // Solo retorna si Valor, Anotacion Y TipoClasificacion (si aplica) están vacíos.
                if (string.IsNullOrWhiteSpace(vm.Valor) &&
                    string.IsNullOrWhiteSpace(vm.Anotacion) &&
                    (!incluirTipo || string.IsNullOrWhiteSpace(vm.TipoClasificacion)))
                    return;

                list.Add(new MedidaProcesal
                {
                    IdEstudioTerreno = idEstudioTerreno,
                    Objeto = vm.Objeto,
                    Valor = vm.Valor ?? "",
                    Anotacion = vm.Anotacion,
                    // Incluye TipoClasificacion solo si 'incluirTipo' es true (solo para RUPTA)
                    TipoClasificacion = incluirTipo ? vm.TipoClasificacion : null
                });
            }

            // Llamadas a la función Add
            Add(Hipoteca);
            Add(Servidumbres);
            Add(MedidasCautelares);
            Add(Rupta, incluirTipo: true); // <-- RUPTA incluye TipoClasificacion
            Add(RTDAF);
            Add(Oferta);
            Add(Clarificacion);

            return list;
        }

        // Método para cargar datos desde la DB al ViewModel
        public void LoadFrom(IEnumerable<MedidaProcesal> rows)
        {
            foreach (var r in rows)
            {
                // Usa el campo Objeto para saber a qué propiedad del ViewModel corresponde
                var t = r.Objeto.ToUpperInvariant() switch
                {
                    "HIPOTECA" => Hipoteca,
                    "SERVIDUMBRES" => Servidumbres,
                    "MEDIDAS CAUTELARES" => MedidasCautelares,
                    "RUPTA" => Rupta,
                    "RTDAF LEY 1448" => RTDAF,
                    "OFERTA OTRAS ENTIDADES" => Oferta,
                    "PROCESOS DE CLARIFICACIÓN" => Clarificacion,
                    _ => null
                };

                if (t is null) continue;

                t.Valor = r.Valor;
                t.Anotacion = r.Anotacion;

                // Carga TipoClasificacion solo si es el objeto RUPTA
                if (t == Rupta) t.TipoClasificacion = r.TipoClasificacion;
            }
        }
    }

    // Clase interna para manejar cada elemento de Medida Procesal (ej. Hipoteca, Rupta)
    public class MedidaItemVM : ViewModelBase
    {
        public MedidasProcesalesViewModel Owner { get; }
        public string Objeto { get; }

        private string? _valor;
        // Enlaza al ComboBox de Sí/No/Pendiente
        public string? Valor { get => _valor; set => Set(ref _valor, value); }

        private string? _anotacion;
        // Enlaza al TextBox de anotaciones
        public string? Anotacion { get => _anotacion; set => Set(ref _anotacion, value); }

        private string? _tipoClasificacion;
        // Enlaza al ComboBox de clasificación (solo usado por RUPTA)
        public string? TipoClasificacion { get => _tipoClasificacion; set => Set(ref _tipoClasificacion, value); }

        public MedidaItemVM(MedidasProcesalesViewModel owner, string objeto)
        {
            Owner = owner;
            Objeto = objeto;
        }
    }
}