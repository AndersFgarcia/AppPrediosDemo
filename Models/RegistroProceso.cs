using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AppPrediosDemo.Models;

namespace AppPrediosDemo.Models;

[Table("RegistroProceso", Schema = "Postulacion")]
[Index("IdRegistroProceso", Name = "Idx_ConceptosPrevio_Postulacion_IdRegistroProceso")]
[Index("Dependencia", Name = "Idx_RegistroProceso_Dependencia")]
[Index("FMI", Name = "Idx_RegistroProceso_FMI")]
[Index("IdEtapaProcesal", Name = "Idx_RegistroProceso_IdEtapaProcesal")]
[Index("IdFuenteProceso", Name = "Idx_RegistroProceso_IdFuentePostulacionProceso")]
[Index("IdPostulacion", Name = "Idx_RegistroProceso_IdPostulacion")]
[Index("IdTipoProceso", Name = "Idx_RegistroProceso_IdTipoProceso")]
[Index("NumeroExpediente", Name = "Idx_RegistroProceso_NumeroExpediente")]
[Index("RadicadoOrfeo", Name = "Idx_RegistroProceso_RadicadoOrfeo")]
public partial class RegistroProceso
{

    [Key]
    public long IdRegistroProceso { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string IdPostulacion { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string FMI { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? NumeroExpediente { get; set; }

    // 🎯 CORRECCIÓN: Tipos de dato anulables (int?) para coincidir con la BD
    public int? IdFuenteProceso { get; set; }

    public int? IdTipoProceso { get; set; }

    public int? IdEtapaProcesal { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? Dependencia { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? RadicadoOrfeo { get; set; }

    [InverseProperty("IdRegistroProcesoNavigation")]
    public virtual ICollection<ConceptosPrevio> ConceptosPrevios { get; set; } = new List<ConceptosPrevio>();

    [InverseProperty("IdRegistroProcesoNavigation")]
    public virtual ICollection<EstudioTerreno> EstudioTerrenos { get; set; } = new List<EstudioTerreno>();

    // ===== NUEVA colección =====
    public virtual ICollection<ConceptoFinal> ConceptoFinals { get; set; } = new HashSet<ConceptoFinal>();

    [ForeignKey("IdEtapaProcesal")]
    [InverseProperty("RegistroProcesos")]
    public virtual EtapaProcesal IdEtapaProcesalNavigation { get; set; } = null!;

    [ForeignKey("IdFuenteProceso")]
    [InverseProperty("RegistroProcesos")]
    public virtual FuenteProceso IdFuenteProcesoNavigation { get; set; } = null!;

    [ForeignKey("IdTipoProceso")]
    [InverseProperty("RegistroProcesos")]
    public virtual TipoProceso IdTipoProcesoNavigation { get; set; } = null!;
}
