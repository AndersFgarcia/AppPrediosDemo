using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("EtapaProcesal", Schema = "Postulacion")]
public partial class EtapaProcesal
{
    [Key]
    public int IdEtapaProcesal { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreEtapaProcesal { get; set; } = null!;

    [InverseProperty("IdEtapaProcesalNavigation")]
    public virtual ICollection<RegistroProceso> RegistroProcesos { get; set; } = new List<RegistroProceso>();
}
