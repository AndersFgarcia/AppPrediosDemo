using System;

namespace AppPrediosDemo.Models
{
    public partial class ConceptoFinal
    {

        public int IdConceptoFinal { get; set; }
        public long IdRegistroProceso { get; set; }
        public virtual RegistroProceso IdRegistroProcesoNavigation { get; set; } = null!;
        public virtual TipoInforme? IdTipoInformeNavigation { get; set; }
        public virtual TipoEstadoRevision? IdTipoEstadoRevisionNavigation { get; set; }

        public string? ConceptoActualDeViabilidadJuridica { get; set; }
        public DateTime? FechaInforme { get; set; }
        public string? Viabilidad { get; set; }

        public byte? IdTipoInforme { get; set; }

        public string? CausalNoViabilidad { get; set; }
        public string? InsumosPendientes { get; set; }

        public DateTime? FechaEntregaARevisor { get; set; }
        public string? AbogadoSustanciadorAsignado { get; set; }
        public string? AbogadoRevisorAsignado { get; set; }

        public int? NroReparto { get; set; }
        public DateTime? FechaAsignacionReparto { get; set; }
        public DateTime? FechaPlazoEntregaARevisor { get; set; }

        public byte? IdTipoEstadoRevision { get; set; }
        public string? ObservacionesRevisor { get; set; }

        // En BD es varchar(2) "SI"/"NO". Luego en VM lo convertimos a bool.
        public string? EntregoCarpetaSoportes { get; set; }

        public DateTime? FechaEnvioACoordinacion { get; set; }
        public string? EstadoAprobacionCoordinadora { get; set; }

        public DateTime? FechaRemisionSoportesAGestoraDocumental { get; set; }

        // En BD el nombre tiene tilde: FechaRemisiónInformeAGestoraDocumental
        public DateTime? FechaRemisionInformeAGestoraDocumental { get; set; }

        // En BD: FechaCargueInformeJurídicoExpOrfeo
        public DateTime? FechaCargueInformeJuridicoExpOrfeo { get; set; }

        public DateTime? FechaDeCargueDocsYSoportesExpOrfeo { get; set; }

        // En BD: FechaGestionEtapaSIT
        public DateTime? FechaGestionEtapaSit { get; set; }
    }
}
