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
        public virtual DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public virtual DbSet<TipoProceso> TipoProcesos { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }

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
                 .HasDefaultValueSql("NEXT VALUE FOR Postulacion.Seq_RegistroProceso");

                e.HasOne(x => x.IdEtapaProcesalNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdEtapaProcesal)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_EtapaProcesal_IdEtapaProcesal");

                e.HasOne(x => x.IdFuenteProcesoNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdFuenteProceso)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_FuenteProceso_IdFuenteProceso");

                e.HasOne(x => x.IdTipoProcesoNavigation)
                 .WithMany(p => p.RegistroProcesos)
                 .HasForeignKey(x => x.IdTipoProceso)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_TipoProceso_IdTipoProceso");
            });

            mb.Entity<EstudioTerreno>(e =>
            {
                e.ToTable("EstudioTerreno", "Postulacion");
                e.HasKey(x => x.IdEstudioTerreno).HasName("PK_IdEstudioTerreno");
                e.Property(x => x.IdEstudioTerreno)
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR Postulacion.Seq_EstudioTerreno");

                e.Property(x => x.AreaRegistral).HasColumnType("numeric(18,4)");
                e.Property(x => x.AreaCalculada).HasColumnType("numeric(18,4)");

                e.HasOne(x => x.IdLocalizacionNavigation)
                 .WithMany(p => p.EstudioTerrenos)
                 .HasForeignKey(x => x.IdLocalizacion)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_IdLocalizacion");

                e.HasOne(x => x.IdRegistroProcesoNavigation)
                 .WithMany(p => p.EstudioTerrenos)
                 .HasForeignKey(x => x.IdRegistroProceso)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_IdRegistroProceso");
            });

            mb.Entity<MedidaProcesal>(e =>
            {
                e.ToTable("MedidaProcesal", "Postulacion");
                e.HasKey(x => x.IdMedidasProcesal).HasName("PK_IdMedidasProcesal");
                e.Property(x => x.IdMedidasProcesal)
                 .ValueGeneratedOnAdd()
                 .HasDefaultValueSql("NEXT VALUE FOR Postulacion.Seq_MedidaProcesal");

                e.HasOne(x => x.IdEstudioTerrenoNavigation)
                 .WithMany(p => p.MedidaProcesals)
                 .HasForeignKey(x => x.IdEstudioTerreno)
                 .OnDelete(DeleteBehavior.ClientSetNull)
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
                 .HasDefaultValueSql("NEXT VALUE FOR AnalisisJuridico.Seq_ConceptosPrevio");

                e.HasOne(x => x.IdRegistroProcesoNavigation)
                 .WithMany(p => p.ConceptosPrevios)
                 .HasForeignKey(x => x.IdRegistroProceso)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_IdRegistroProceso_GestionJuridica");
            });

            // ----- dbo (si los usas) -----
            mb.Entity<Usuario>(e =>
            {
                e.ToTable("Usuarios", "dbo");
                e.HasKey(x => x.IdUsuario).HasName("PK_IdUsuario");
                e.Property(x => x.IdUsuario).ValueGeneratedNever();
            });

            mb.Entity<TipoDocumento>(e =>
            {
                e.ToTable("TipoDocumento", "dbo");
                e.HasKey(x => x.IdTipoDocumento).HasName("PK_IdTipoDocumento");
                e.Property(x => x.IdTipoDocumento).ValueGeneratedNever();
            });

            OnModelCreatingPartial(mb);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}