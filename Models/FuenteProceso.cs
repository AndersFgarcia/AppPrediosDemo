using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("FuenteProceso", Schema = "Postulacion")]
public partial class FuenteProceso
{
    [Key]
    public int IdFuenteProceso { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreFuenteProceso { get; set; } = null!;

    [InverseProperty("IdFuenteProcesoNavigation")]
    public virtual ICollection<RegistroProceso> RegistroProcesos { get; set; } = new List<RegistroProceso>();
}
