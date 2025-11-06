using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("EstudioTerreno", Schema = "Postulacion")]
[Index("ApellidoPropietario", Name = "Idx_EstudioTerreno_ApellidoPropietario")]
[Index("IdLocalizacion", Name = "Idx_EstudioTerreno_IdLocalizacion")]
[Index("IdRegistroProceso", Name = "Idx_EstudioTerreno_IdRegistroProceso")]
[Index("Identificacion", Name = "Idx_EstudioTerreno_Identificacion")]
[Index("NombrePropietario", Name = "Idx_EstudioTerreno_NombrePropietario")]
[Index("TipoPersonaTitular", Name = "Idx_EstudioTerreno_TipoPersonaTitular")]
public partial class EstudioTerreno
{
    [Key]
    public int IdEstudioTerreno { get; set; }

    public long IdRegistroProceso { get; set; }

    public int IdLocalizacion { get; set; }

    [Column(TypeName = "numeric(18, 4)")]
    public decimal? AreaRegistral { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? CirculoRegistral { get; set; }

    [Column(TypeName = "numeric(18, 4)")]
    public decimal AreaCalculada { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? TipoPersonaTitular { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? NombrePropietario { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? ApellidoPropietario { get; set; }

    public long? Identificacion { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? NaturalezaJuridica { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? AcreditacionPropiedad { get; set; }

    [ForeignKey("IdLocalizacion")]
    [InverseProperty("EstudioTerrenos")]
    public virtual Localizacion IdLocalizacionNavigation { get; set; } = null!;

    [ForeignKey("IdRegistroProceso")]
    [InverseProperty("EstudioTerrenos")]
    public virtual RegistroProceso IdRegistroProcesoNavigation { get; set; } = null!;

    [InverseProperty("IdEstudioTerrenoNavigation")]
    public virtual ICollection<MedidaProcesal> MedidaProcesals { get; set; } = new List<MedidaProcesal>();
}
