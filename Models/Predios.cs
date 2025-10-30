using System;

namespace AppPrediosDemo.Models
{
    public class Predio
    {
        // Identificación
        public string? ID { get; set; }
        public string? FMI { get; set; }
        public string? NoExpediente { get; set; }

        // Catálogos (guardamos Ids)
        public int? IdFuenteProceso { get; set; }
        public int? IdTipoProceso { get; set; }
        public int? IdEtapaProcesal { get; set; }

        // Libres
        public string? RadicadoOrfeo { get; set; }   // ID ORFEO
        public string? Dependencia { get; set; }     // Dependencia

        // Ubicación (cascada)
        public string? Departamento { get; set; }
        public string? Municipio { get; set; }
        public string? CentroPoblado { get; set; }

        public string? CirculoRegistral { get; set; }
        public decimal? AreaRegistral { get; set; }
        public decimal? AreaCalculada { get; set; }

        // Titularidad
        public string? PersonaTitular { get; set; }
        public string? NombrePropietarios { get; set; }
        public string? NumeroIdentificacion { get; set; }
        public string? TituloOriginario { get; set; }
        public string? AnalisisNaturalezaUltimaTradicion { get; set; }

        // Gravámenes
        public bool Hipoteca_SiNo { get; set; }
        public string? Hipoteca_Anotacion { get; set; }
        public bool Servidumbres_SiNo { get; set; }
        public string? Servidumbres_Anotacion { get; set; }
        public bool MedidasCautelares_SiNo { get; set; }
        public string? MedidasCautelares_Anotacion { get; set; }

        // RUPTA
        public bool Rupta_SiNo { get; set; }
        public string? Rupta_Anotacion { get; set; }
        public string? Rupta_ColectivoIndividual { get; set; }

        // Otras afectaciones
        public bool RTDAF_Ley1448_SiNo { get; set; }
        public string? RTDAF_Anotacion { get; set; }
        public bool OfertaOtrasEntidades_SiNo { get; set; }
        public string? OfertaOtrasEntidades_Anotacion { get; set; }
        public bool ProcesosClarificacion_SiNo { get; set; }
        public string? ProcesosClarificacion_Anotacion { get; set; }

        // Concepto jurídico
        public bool CuentaConInformeJuridicoPrevio { get; set; }
        public DateTime? FechaInformePrevioReportada { get; set; }
        public string? ConceptoAntiguo { get; set; }
        public string? AnalisisJuridicoFinal { get; set; }
        public DateTime? FechaInforme { get; set; }
        public string? Viabilidad { get; set; }
        public string? TipoInforme { get; set; }
        public string? CausalNoViabilidad { get; set; }
        public string? InsumosPendientes { get; set; }

        // Asignación y revisión
        public DateTime? FechaEntregaARevisor { get; set; }
        public string? AbogadoSustanciadorAsignado { get; set; }
        public string? AbogadoRevisorAsignado { get; set; }
        public string? NumeroReparto { get; set; }
        public DateTime? FechaAsignacionReparto { get; set; }
        public string? PlazoParaEntregaARevisor { get; set; }
        public string? EstadoRevision { get; set; }
        public string? ObservacionesRevisor { get; set; }

        // Gestión documental / ORFEO
        public bool EntregoCarpetaSoportes { get; set; }
        public DateTime? FechaEnvioACoordinacion { get; set; }
        public string? EstadoAprobacionCoordinadora { get; set; }
        public DateTime? FechaRemisionSoportesGestoraDocumental { get; set; }
        public DateTime? FechaRemisionInformeGestoraDocumental { get; set; }
        public DateTime? FechaCargueInformeJuridicoEnExpteOrfeo { get; set; }
        public DateTime? FechaCargueDocumentosYSoportesEnExpdteOrfeo { get; set; }
        public DateTime? FechaGestionEtapaSIT { get; set; }
    }
}
