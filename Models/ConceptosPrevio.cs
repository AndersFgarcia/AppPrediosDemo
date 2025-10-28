using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppPrediosDemo.Models;

[Table("ConceptosPrevio", Schema = "AnalisisJuridico")]
public partial class ConceptosPrevio
{
    // En C# usaremos el nombre lógico IdGestionJuridica,
    // mapeado a la columna física IdConceptoPrevio (con SEQUENCE).
    [Key]
    [Column("IdConceptoPrevio")]
    public int IdGestionJuridica { get; set; }

    public long IdRegistroProceso { get; set; }

    public DateTime? FechaInforme { get; set; }

    [Column(TypeName = "varchar(max)")]
    public string? Concepto { get; set; }

    [ForeignKey(nameof(IdRegistroProceso))]
    public virtual RegistroProceso? IdRegistroProcesoNavigation { get; set; }
}
