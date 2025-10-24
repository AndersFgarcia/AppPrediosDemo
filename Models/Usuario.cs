using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models;

[Index("Activo", Name = "Idx_Usuarios_Activo")]
[Index("ApellidoUsuario", Name = "Idx_Usuarios_ApellidoUsuario")]
[Index("FechaRegistro", Name = "Idx_Usuarios_FechaRegistro")]
[Index("Identificacion", Name = "Idx_Usuarios_Identificacion")]
[Index("LoginUsuario", Name = "Idx_Usuarios_LoginUsuario")]
[Index("NombreUsuario", Name = "Idx_Usuarios_NombreUsuario")]
public partial class Usuario
{
    [Key]
    public int IdUsuario { get; set; }

    [Column(TypeName = "numeric(18, 0)")]
    public decimal Identificacion { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string NombreUsuario { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string ApellidoUsuario { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string LoginUsuario { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string PasswordUsuario { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime FechaRegistro { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime FechaInicioVigencia { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaFinVigencia { get; set; }

    public bool Activo { get; set; }
}
