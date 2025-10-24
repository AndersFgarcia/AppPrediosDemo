using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("MedidaProcesal", Schema = "Postulacion")]
[Index("IdEstudioTerreno", Name = "Idx_Postulacion_IdEstudioTerreno")]
public partial class MedidaProcesal
{
    [Key]
    public int IdMedidasProcesal { get; set; }

    public int IdEstudioTerreno { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string Objeto { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string Valor { get; set; } = null!;

    [StringLength(4000)]
    [Unicode(false)]
    public string? Anotacion { get; set; }

    [ForeignKey("IdEstudioTerreno")]
    [InverseProperty("MedidaProcesals")]
    public virtual EstudioTerreno IdEstudioTerrenoNavigation { get; set; } = null!;
}
