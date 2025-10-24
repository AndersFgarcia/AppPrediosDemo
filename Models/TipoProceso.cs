using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("TipoProceso", Schema = "Postulacion")]
public partial class TipoProceso
{
    [Key]
    public int IdTipoProceso { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreTipoProceso { get; set; } = null!;

    [InverseProperty("IdTipoProcesoNavigation")]
    public virtual ICollection<RegistroProceso> RegistroProcesos { get; set; } = new List<RegistroProceso>();
}
