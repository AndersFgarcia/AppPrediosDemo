using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("ConceptosPrevio", Schema = "AnalisisJuridico")]
public partial class ConceptosPrevio
{
    [Key]
    [Column("IdConceptoPrevio")]
    public int IdGestionJuridica { get; set; }

    public long IdRegistroProceso { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaInforme { get; set; }

    [Unicode(false)]
    [Column("Concepto")]
    public string? Concepto { get; set; }

    [ForeignKey(nameof(IdRegistroProceso))]
    [InverseProperty(nameof(RegistroProceso.ConceptosPrevios))]
    public virtual RegistroProceso IdRegistroProcesoNavigation { get; set; } = null!;
}
