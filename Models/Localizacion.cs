using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("Localizacion", Schema = "Postulacion")]
[Index("CodigoCentroPoblado", Name = "IDX_Localizacion_CodigoCentroPoblado")]
[Index("CodigoDepartamento", Name = "IDX_Localizacion_CodigoDepartamento")]
[Index("CodigoMunicipio", Name = "IDX_Localizacion_CodigoMunicipio")]
public partial class Localizacion
{
    [Key]
    public int IdLocalizacion { get; set; }

    public int CodigoDepartamento { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreDepartamento { get; set; } = null!;

    public int CodigoMunicipio { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreMunicipio { get; set; } = null!;

    public int CodigoCentroPoblado { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreCentroPoblado { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string Tipo { get; set; } = null!;

    [InverseProperty("IdLocalizacionNavigation")]
    public virtual ICollection<EstudioTerreno> EstudioTerrenos { get; set; } = new List<EstudioTerreno>();
}
