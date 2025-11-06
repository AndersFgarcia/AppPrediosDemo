// Models/Predio.cs
using AppPrediosDemo.Infrastructure;
using System;

namespace AppPrediosDemo.Models
{
    public class Predio : ViewModelBase
    {
        // Identificación
        private string? id;
        public string? ID { get => id; set => Set(ref id, value); }

        private string? fmi;
        public string? FMI { get => fmi; set => Set(ref fmi, value); }

        private string? noExp;
        public string? NoExpediente { get => noExp; set => Set(ref noExp, value); }

        private string? rad;
        public string? RadicadoOrfeo { get => rad; set => Set(ref rad, value); }

        private string? dep;
        public string? Dependencia { get => dep; set => Set(ref dep, value); }

        // Catálogos
        private int? idFu;
        public int? IdFuenteProceso { get => idFu; set => Set(ref idFu, value); }

        private int? idTp;
        public int? IdTipoProceso { get => idTp; set => Set(ref idTp, value); }

        private int? idEt;
        public int? IdEtapaProcesal { get => idEt; set => Set(ref idEt, value); }

        // Ubicación
        private string? dpto;
        public string? Departamento { get => dpto; set => Set(ref dpto, value); }

        private string? mun;
        public string? Municipio { get => mun; set => Set(ref mun, value); }

        private string? centro;
        public string? CentroPoblado { get => centro; set => Set(ref centro, value); }

        private string? cir;
        public string? CirculoRegistral { get => cir; set => Set(ref cir, value); }

        private decimal? ar;
        public decimal? AreaRegistral { get => ar; set => Set(ref ar, value); }

        private decimal? ac;
        public decimal? AreaCalculada { get => ac; set => Set(ref ac, value); }

        // Titularidad
        private string? pt;
        public string? PersonaTitular { get => pt; set => Set(ref pt, value); }

        private string? np;
        public string? NombrePropietarios { get => np; set => Set(ref np, value); }

        private string? ap;
        public string? ApellidoPropietario { get => ap; set => Set(ref ap, value); }

        private string? ni;
        public string? NumeroIdentificacion { get => ni; set => Set(ref ni, value); }

        private string? to;
        public string? TituloOriginario { get => to; set => Set(ref to, value); }

        private string? an;
        public string? AnalisisNaturalezaUltimaTradicion { get => an; set => Set(ref an, value); }

        // Concepto jurídico
        private bool tienePrevio;
        public bool CuentaConInformeJuridicoPrevio { get => tienePrevio; set => Set(ref tienePrevio, value); }

        private DateTime? fechaPrevio;
        public DateTime? FechaInformePrevioReportada { get => fechaPrevio; set => Set(ref fechaPrevio, value); }

        private string? conceptoAntiguo;
        public string? ConceptoAntiguo { get => conceptoAntiguo; set => Set(ref conceptoAntiguo, value); }

        private string? analisisFinal;
        public string? AnalisisJuridicoFinal { get => analisisFinal; set => Set(ref analisisFinal, value); }

        private DateTime? fechaInf;
        public DateTime? FechaInforme { get => fechaInf; set => Set(ref fechaInf, value); }

        private string? viab;
        public string? Viabilidad { get => viab; set => Set(ref viab, value); }

        // si ya no usas texto libre, puedes borrar esto

        private string? causalNV;
        public string? CausalNoViabilidad { get => causalNV; set => Set(ref causalNV, value); }

        private string? insPend;
        public string? InsumosPendientes { get => insPend; set => Set(ref insPend, value); }

        // === Concepto final (FKs a catálogos) ===
        private int? idTipoInforme;
        public int? IdTipoInforme
        {
            get => idTipoInforme;
            set => Set(ref idTipoInforme, value);
        }

        private int? idTipoEstadoRevision;
        public int? IdTipoEstadoRevision
        {
            get => idTipoEstadoRevision;
            set => Set(ref idTipoEstadoRevision, value);
        }


        // Asignación y revisión
        private DateTime? fEntRev;
        public DateTime? FechaEntregaARevisor { get => fEntRev; set => Set(ref fEntRev, value); }

        private string? sust;
        public string? AbogadoSustanciadorAsignado { get => sust; set => Set(ref sust, value); }

        private string? rev;
        public string? AbogadoRevisorAsignado { get => rev; set => Set(ref rev, value); }

        private string? nroRep;
        public string? NumeroReparto { get => nroRep; set => Set(ref nroRep, value); }

        private DateTime? fAsigRep;
        public DateTime? FechaAsignacionReparto { get => fAsigRep; set => Set(ref fAsigRep, value); }

        // NUEVO: coincide con la BD (datetime) y con el ViewModel
        private DateTime? fPlazoRev;
        public DateTime? FechaPlazoEntregaARevisor
        {
            get => fPlazoRev;
            set => Set(ref fPlazoRev, value);
        }

        private string? obsRev;
        public string? ObservacionesRevisor { get => obsRev; set => Set(ref obsRev, value); }

        // Gestión documental

        private string? entregoCarpetaSoportes;
        public string? EntregoCarpetaSoportes
        {
            get => entregoCarpetaSoportes;
            set => Set(ref entregoCarpetaSoportes, value);
        }

        private DateTime? fCoord;
        public DateTime? FechaEnvioACoordinacion { get => fCoord; set => Set(ref fCoord, value); }

        private string? estAprob;
        public string? EstadoAprobacionCoordinadora { get => estAprob; set => Set(ref estAprob, value); }

        private DateTime? fRemSoportes;
        public DateTime? FechaRemisionSoportesGestoraDocumental { get => fRemSoportes; set => Set(ref fRemSoportes, value); }

        private DateTime? fRemInf;
        public DateTime? FechaRemisionInformeGestoraDocumental { get => fRemInf; set => Set(ref fRemInf, value); }

        private DateTime? fCargInf;
        public DateTime? FechaCargueInformeJuridicoEnExpteOrfeo { get => fCargInf; set => Set(ref fCargInf, value); }

        private DateTime? fCargDocs;
        public DateTime? FechaCargueDocumentosYSoportesEnExpdteOrfeo { get => fCargDocs; set => Set(ref fCargDocs, value); }

        private DateTime? fSit;
        public DateTime? FechaGestionEtapaSIT { get => fSit; set => Set(ref fSit, value); }
    }
}

