using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Table("TipoDocumento")]
public partial class TipoDocumento
{
    [Key]
    public byte IdTipoDocumento { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? NombreTipoDocumento { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string? PrefijoTipoDocumento { get; set; }
}
