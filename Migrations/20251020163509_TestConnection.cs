using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppPrediosDemo.Migrations
{
    /// <inheritdoc />
    public partial class TestConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AnalisisJuridico");

            migrationBuilder.EnsureSchema(
                name: "Postulacion");

            migrationBuilder.CreateTable(
                name: "EtapaProcesal",
                schema: "Postulacion",
                columns: table => new
                {
                    IdEtapaProcesal = table.Column<int>(type: "int", nullable: false),
                    NombreEtapaProcesal = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdEtapaProcesal", x => x.IdEtapaProcesal);
                });

            migrationBuilder.CreateTable(
                name: "FuenteProceso",
                schema: "Postulacion",
                columns: table => new
                {
                    IdFuenteProceso = table.Column<int>(type: "int", nullable: false),
                    NombreFuenteProceso = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdFuenteProceso", x => x.IdFuenteProceso);
                });

            migrationBuilder.CreateTable(
                name: "Localizacion",
                schema: "Postulacion",
                columns: table => new
                {
                    IdLocalizacion = table.Column<int>(type: "int", nullable: false),
                    CodigoDepartamento = table.Column<int>(type: "int", nullable: false),
                    NombreDepartamento = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CodigoMunicipio = table.Column<int>(type: "int", nullable: false),
                    NombreMunicipio = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CodigoCentroPoblado = table.Column<int>(type: "int", nullable: false),
                    NombreCentroPoblado = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localizacion", x => x.IdLocalizacion);
                });

            migrationBuilder.CreateTable(
                name: "TipoDocumento",
                columns: table => new
                {
                    IdTipoDocumento = table.Column<byte>(type: "tinyint", nullable: false),
                    NombreTipoDocumento = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PrefijoTipoDocumento = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdTipoDocumento", x => x.IdTipoDocumento);
                });

            migrationBuilder.CreateTable(
                name: "TipoProceso",
                schema: "Postulacion",
                columns: table => new
                {
                    IdTipoProceso = table.Column<int>(type: "int", nullable: false),
                    NombreTipoProceso = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdTipoProceso", x => x.IdTipoProceso);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Identificacion = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    NombreUsuario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ApellidoUsuario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LoginUsuario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    PasswordUsuario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "datetime", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "datetime", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdUsuario", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "RegistroProceso",
                schema: "Postulacion",
                columns: table => new
                {
                    IdRegistroProceso = table.Column<long>(type: "bigint", nullable: false),
                    IdPostulacion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    FMI = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    NumeroExpediente = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IdFuenteProceso = table.Column<int>(type: "int", nullable: false),
                    IdTipoProceso = table.Column<int>(type: "int", nullable: false),
                    IdEtapaProcesal = table.Column<int>(type: "int", nullable: false),
                    Dependencia = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    RadicadoOrfeo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdRegistroProceso", x => x.IdRegistroProceso);
                    table.ForeignKey(
                        name: "FK_EtapaProcesal_IdEtapaProcesal",
                        column: x => x.IdEtapaProcesal,
                        principalSchema: "Postulacion",
                        principalTable: "EtapaProcesal",
                        principalColumn: "IdEtapaProcesal");
                    table.ForeignKey(
                        name: "FK_FuenteProceso_IdFuenteProceso",
                        column: x => x.IdFuenteProceso,
                        principalSchema: "Postulacion",
                        principalTable: "FuenteProceso",
                        principalColumn: "IdFuenteProceso");
                    table.ForeignKey(
                        name: "FK_TipoProceso_IdTipoProceso",
                        column: x => x.IdTipoProceso,
                        principalSchema: "Postulacion",
                        principalTable: "TipoProceso",
                        principalColumn: "IdTipoProceso");
                });

            migrationBuilder.CreateTable(
                name: "ConceptosPrevio",
                schema: "AnalisisJuridico",
                columns: table => new
                {
                    IdGestionJuridica = table.Column<int>(type: "int", nullable: false),
                    IdRegistroProceso = table.Column<long>(type: "bigint", nullable: false),
                    InfomeJuridico = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    FechaInforme = table.Column<DateTime>(type: "datetime", nullable: true),
                    Concepto = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdGestionJuridica", x => x.IdGestionJuridica);
                    table.ForeignKey(
                        name: "FK_IdRegistroProceso_GestionJuridica",
                        column: x => x.IdRegistroProceso,
                        principalSchema: "Postulacion",
                        principalTable: "RegistroProceso",
                        principalColumn: "IdRegistroProceso");
                });

            migrationBuilder.CreateTable(
                name: "EstudioTerreno",
                schema: "Postulacion",
                columns: table => new
                {
                    IdEstudioTerreno = table.Column<int>(type: "int", nullable: false),
                    IdRegistroProceso = table.Column<long>(type: "bigint", nullable: false),
                    IdLocalizacion = table.Column<int>(type: "int", nullable: false),
                    AreaRegistral = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CirculoRegistral = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AreaCalculada = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TipoPersonaTitular = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    NombrePropietario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ApellidoPropietario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Identificacion = table.Column<long>(type: "bigint", nullable: true),
                    NaturalezaJuridica = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AcreditacionPropiedad = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdEstudioTerreno", x => x.IdEstudioTerreno);
                    table.ForeignKey(
                        name: "FK_IdLocalizacion",
                        column: x => x.IdLocalizacion,
                        principalSchema: "Postulacion",
                        principalTable: "Localizacion",
                        principalColumn: "IdLocalizacion");
                    table.ForeignKey(
                        name: "FK_IdRegistroProceso",
                        column: x => x.IdRegistroProceso,
                        principalSchema: "Postulacion",
                        principalTable: "RegistroProceso",
                        principalColumn: "IdRegistroProceso");
                });

            migrationBuilder.CreateTable(
                name: "MedidaProcesal",
                schema: "Postulacion",
                columns: table => new
                {
                    IdMedidasProcesal = table.Column<int>(type: "int", nullable: false),
                    IdEstudioTerreno = table.Column<int>(type: "int", nullable: false),
                    Objeto = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    Valor = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Anotacion = table.Column<string>(type: "varchar(4000)", unicode: false, maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdMedidasProcesal", x => x.IdMedidasProcesal);
                    table.ForeignKey(
                        name: "FK_IdEstudioTerreno_Postulacion_EstudioTerreno",
                        column: x => x.IdEstudioTerreno,
                        principalSchema: "Postulacion",
                        principalTable: "EstudioTerreno",
                        principalColumn: "IdEstudioTerreno");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPrevio_IdRegistroProceso",
                schema: "AnalisisJuridico",
                table: "ConceptosPrevio",
                column: "IdRegistroProceso");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_ApellidoPropietario",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "ApellidoPropietario");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_Identificacion",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "Identificacion");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_IdLocalizacion",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "IdLocalizacion");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_IdRegistroProceso",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "IdRegistroProceso");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_NombrePropietario",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "NombrePropietario");

            migrationBuilder.CreateIndex(
                name: "Idx_EstudioTerreno_TipoPersonaTitular",
                schema: "Postulacion",
                table: "EstudioTerreno",
                column: "TipoPersonaTitular");

            migrationBuilder.CreateIndex(
                name: "IDX_Localizacion_CodigoCentroPoblado",
                schema: "Postulacion",
                table: "Localizacion",
                column: "CodigoCentroPoblado");

            migrationBuilder.CreateIndex(
                name: "IDX_Localizacion_CodigoDepartamento",
                schema: "Postulacion",
                table: "Localizacion",
                column: "CodigoDepartamento");

            migrationBuilder.CreateIndex(
                name: "IDX_Localizacion_CodigoMunicipio",
                schema: "Postulacion",
                table: "Localizacion",
                column: "CodigoMunicipio");

            migrationBuilder.CreateIndex(
                name: "Idx_Postulacion_IdEstudioTerreno",
                schema: "Postulacion",
                table: "MedidaProcesal",
                column: "IdEstudioTerreno");

            migrationBuilder.CreateIndex(
                name: "Idx_ConceptosPrevio_Postulacion_IdRegistroProceso",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "IdRegistroProceso");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_Dependencia",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "Dependencia");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_FMI",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "FMI");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_IdEtapaProcesal",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "IdEtapaProcesal");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_IdFuentePostulacionProceso",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "IdFuenteProceso");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_IdPostulacion",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "IdPostulacion");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_IdTipoProceso",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "IdTipoProceso");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_NumeroExpediente",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "NumeroExpediente");

            migrationBuilder.CreateIndex(
                name: "Idx_RegistroProceso_RadicadoOrfeo",
                schema: "Postulacion",
                table: "RegistroProceso",
                column: "RadicadoOrfeo");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_Activo",
                table: "Usuarios",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_ApellidoUsuario",
                table: "Usuarios",
                column: "ApellidoUsuario");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_FechaRegistro",
                table: "Usuarios",
                column: "FechaRegistro");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_Identificacion",
                table: "Usuarios",
                column: "Identificacion");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_LoginUsuario",
                table: "Usuarios",
                column: "LoginUsuario");

            migrationBuilder.CreateIndex(
                name: "Idx_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConceptosPrevio",
                schema: "AnalisisJuridico");

            migrationBuilder.DropTable(
                name: "MedidaProcesal",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "TipoDocumento");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "EstudioTerreno",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "Localizacion",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "RegistroProceso",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "EtapaProcesal",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "FuenteProceso",
                schema: "Postulacion");

            migrationBuilder.DropTable(
                name: "TipoProceso",
                schema: "Postulacion");
        }
    }
}
