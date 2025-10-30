using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AppPrediosDemo.Models
{
    public partial class ViabilidadContext : DbContext
    {
        public ViabilidadContext() { }
        public ViabilidadContext(DbContextOptions<ViabilidadContext> options) : base(options) { }

        public virtual DbSet<ConceptosPrevio> ConceptosPrevios { get; set; }
        public virtual DbSet<EstudioTerreno> EstudioTerrenos { get; set; }
        public virtual DbSet<EtapaProcesal> EtapaProcesals { get; set; }
        public virtual DbSet<FuenteProceso> FuenteProcesos { get; set; }
        public virtual DbSet<Localizacion> Localizacions { get; set; }
        public virtual DbSet<MedidaProcesal> MedidaProcesals { get; set; }
        public virtual DbSet<RegistroProceso> RegistroProcesos { get; set; }
        public virtual DbSet<TipoProceso> TipoProcesos { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var cs = ConfigurationManager.ConnectionStrings["ViabilidadJuridica"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Falta cadena de conexión 'ViabilidadJuridica'.");
                optionsBuilder.UseSqlServer(cs);
            }
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ----- Catálogos (PK manual, sin secuencia) -----
            mb.Entity<TipoProceso>(e =>
            {
                e.ToTable("TipoProceso", "Postulacion");
                e.HasKey(x => x.IdTipoProceso).HasName("PK_IdTipoProceso");
                e.Property(x => x.IdTipoProceso).ValueGeneratedNever();
            });

            mb.Entity<FuenteProceso>(e =>
            {
                e.ToTable("FuenteProceso", "Postulacion");
                e.HasKey(x => x.IdFuenteProceso).HasName("PK_IdFuenteProceso");
                e.Property(x => x.IdFuenteProceso).ValueGeneratedNever();
            });

            mb.Entity<EtapaProcesal>(e =>
            {
                e.ToTable("EtapaProcesal", "Postulacion");
                e.HasKey(x => x.IdEtapaProcesal).HasName("PK_IdEtapaProcesal");
                e.Property(x => x.IdEtapaProcesal).ValueGeneratedNever();
            });

            mb.Entity<Localizacion>(e =>
            {
                e.ToTable("Localizacion", "Postulacion");
                e.HasKey(x => x.IdLocalizacion).HasName("PK_Localizacion");
                e.Property(x => x.IdLocalizacion).ValueGeneratedNever();
            });

            // ----- Transaccionales con SEQUENCE -----
            mb.Entity<RegistroProceso>(e =>
            {
                e.ToTable("RegistroProceso", "Postulacion");
                e.HasKey(x => x.IdRegistroProceso).HasName("PK_IdRegistroProceso");
                e.Property(x => x.IdRegistroProceso)
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR [Postulacion].[Seq_RegistroProceso]");
                // INTEGRACIÓN DE HASMAXLENGTH PARA RegistroProceso:
                e.Property(x => x.IdPostulacion).HasMaxLength(30).IsRequired();
                e.Property(x => x.FMI).HasMaxLength(100).IsRequired();
                e.Property(x => x.NumeroExpediente).HasMaxLength(100);
                e.Property(x => x.Dependencia).HasMaxLength(10);
                e.Property(x => x.RadicadoOrfeo).HasMaxLength(50);

                //posible error
               // e.HasIndex(x => x.IdPostulacion)
                 //.IsUnique() // 👈 Esto es lo que fuerza la unicidad
                 //.HasConstraintName("UX_RegistroProceso_IdPostulacion"); // Nombre del índice único

                e.HasOne(x => x.IdEtapaProcesalNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdEtapaProcesal)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_EtapaProcesal_IdEtapaProcesal");

                e.HasOne(x => x.IdFuenteProcesoNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdFuenteProceso)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_FuenteProceso_IdFuenteProceso");

                e.HasOne(x => x.IdTipoProcesoNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdTipoProceso)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_TipoProceso_IdTipoProceso");
            });

            mb.Entity<EstudioTerreno>(e =>
            {
                e.ToTable("EstudioTerreno", "Postulacion");
                e.HasKey(x => x.IdEstudioTerreno).HasName("PK_IdEstudioTerreno");
                e.Property(x => x.IdEstudioTerreno)
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR [Postulacion].[Seq_EstudioTerreno]");

                e.Property(x => x.AreaRegistral).HasColumnType("numeric(18,4)");
                e.Property(x => x.AreaCalculada).HasColumnType("numeric(18,4)");

                // INTEGRACIÓN DE HASMAXLENGTH EstudioTerreno
                e.Property(x => x.CirculoRegistral).HasMaxLength(100);
                e.Property(x => x.TipoPersonaTitular).HasMaxLength(50);
                e.Property(x => x.NombrePropietario).HasMaxLength(100);
                e.Property(x => x.ApellidoPropietario).HasMaxLength(100);
                e.Property(x => x.NaturalezaJuridica).HasMaxLength(100);
                e.Property(x => x.AcreditacionPropiedad).HasMaxLength(100);

                e.HasOne(x => x.IdLocalizacionNavigation)
                 .WithMany(p => p.EstudioTerrenos)
                 .HasForeignKey(x => x.IdLocalizacion)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_IdLocalizacion");

                e.HasOne(x => x.IdRegistroProcesoNavigation)
                 .WithMany(p => p.EstudioTerrenos)
                 .HasForeignKey(x => x.IdRegistroProceso)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_IdRegistroProceso");
            });

            mb.Entity<MedidaProcesal>(e =>
            {
                e.ToTable("MedidaProcesal", "Postulacion");
                e.HasKey(x => x.IdMedidasProcesal).HasName("PK_IdMedidasProcesal");
                e.Property(x => x.IdMedidasProcesal)
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR [Postulacion].[Seq_MedidaProcesal]");

                // MedidaProcesal
                e.Property(x => x.Objeto).HasMaxLength(1000).IsRequired();
                e.Property(x => x.Valor).HasMaxLength(10).IsRequired();
                e.Property(x => x.Anotacion).HasMaxLength(4000);
                e.Property(x => x.TipoClasificacion).HasMaxLength(100);

                e.HasOne(x => x.IdEstudioTerrenoNavigation)
                 .WithMany(p => p.MedidaProcesals)
                 .HasForeignKey(x => x.IdEstudioTerreno)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_IdEstudioTerreno_Postulacion_EstudioTerreno");
            });

            mb.Entity<ConceptosPrevio>(e =>
            {
                e.ToTable("ConceptosPrevio", "AnalisisJuridico");
                // Tu propiedad se llama IdGestionJuridica. Mapea a la columna real IdConceptoPrevio.
                e.HasKey(x => x.IdGestionJuridica).HasName("PK_IdGestionJuridica");
                e.Property(x => x.IdGestionJuridica)
                 .HasColumnName("IdConceptoPrevio")
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR [AnalisisJuridico].[Seq_ConceptosPrevio]");
                e.Property(x => x.Concepto).HasMaxLength(500); // Puedes ajustar el valor según tu necesidad

                e.HasOne(x => x.IdRegistroProcesoNavigation)
                 .WithMany(p => p.ConceptosPrevios)
                 .HasForeignKey(x => x.IdRegistroProceso)
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasConstraintName("FK_IdRegistroProceso_GestionJuridica");
            });

            OnModelCreatingPartial(mb);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}