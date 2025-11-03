using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppPrediosDemo.Models;

namespace AppPrediosDemo.Services
{
    public sealed class RegistroService
    {
        private readonly ViabilidadContext _ctx;
        public RegistroService(ViabilidadContext ctx) => _ctx = ctx;

        public async Task<long> GuardarNuevoAsync(
            RegistroProceso rp,
            EstudioTerreno et,
            IReadOnlyCollection<MedidaProcesal>? medidas = null)
        {
            // 1) Validaciones de negocio antes de tocar la BD
            await ValidarReglasAsync(rp, et);

            // 2) Estrategia resiliente + transacción
            var strategy = _ctx.Database.CreateExecutionStrategy();
            long nuevoIdRp = 0;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _ctx.Database.BeginTransactionAsync();

                // 3) Enlaza por NAVEGACIONES para permitir SaveChanges() único
                et.IdRegistroProcesoNavigation = rp; // EF insertará RP primero y propagará FK
                if (medidas is not null && medidas.Count > 0)
                {
                    foreach (var m in medidas)
                        m.IdEstudioTerrenoNavigation = et; // EF insertará ET y luego MP
                    _ctx.MedidaProcesals.AddRange(medidas);
                }

                _ctx.RegistroProcesos.Add(rp);
                _ctx.EstudioTerrenos.Add(et);

                await _ctx.SaveChangesAsync(); // genera Ids vía SEQUENCE y resuelve FKs

                nuevoIdRp = rp.IdRegistroProceso;

                await tx.CommitAsync();
            });

            return nuevoIdRp;
        }

        private async Task ValidarReglasAsync(RegistroProceso rp, EstudioTerreno et)
        {
            if (string.IsNullOrWhiteSpace(rp.IdPostulacion) || rp.IdPostulacion.Length > 30)
                throw new InvalidOperationException("IdPostulacion requerido y ? 30 caracteres.");
            if (string.IsNullOrWhiteSpace(rp.FMI) || rp.FMI.Length > 100)
                throw new InvalidOperationException("FMI requerido y ? 100 caracteres.");
            if (et.AreaRegistral < 0 || et.AreaCalculada < 0)
                throw new InvalidOperationException("Áreas requeridas y ? 0.");

            // Unicidad (refuerza índice único si lo agregaste en SQL)
            var existe = await _ctx.RegistroProcesos
                .AsNoTracking()
                .AnyAsync(x => x.IdPostulacion == rp.IdPostulacion);
            if (existe)
                throw new InvalidOperationException("IdPostulacion ya existe.");

            // Catálogos y FK de localización
            if (!await _ctx.FuenteProcesos.AnyAsync(x => x.IdFuenteProceso == rp.IdFuenteProceso))
                throw new InvalidOperationException("FuenteProceso inválido.");
            if (!await _ctx.TipoProcesos.AnyAsync(x => x.IdTipoProceso == rp.IdTipoProceso))
                throw new InvalidOperationException("TipoProceso inválido.");
            if (!await _ctx.EtapaProcesals.AnyAsync(x => x.IdEtapaProcesal == rp.IdEtapaProcesal))
                throw new InvalidOperationException("EtapaProcesal inválida.");
            if (!await _ctx.Localizacions.AnyAsync(x => x.IdLocalizacion == et.IdLocalizacion))
                throw new InvalidOperationException("Localización inválida.");
        }
    }
}
