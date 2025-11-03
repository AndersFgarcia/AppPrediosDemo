using System;
using System.Collections.Generic;
using System.Linq;
using AppPrediosDemo.Models;
using AppPrediosDemo.ViewModels;
using System.Text.RegularExpressions; // ?? Necesario para Regex.Replace

namespace AppPrediosDemo.Services // O AppPrediosDemo.Mappers
{
    public static class PredioMapeador
    {
        public static (RegistroProceso rp, EstudioTerreno et, List<MedidaProcesal> mp)
        AEntidades(Predio vm, int idLocalizacion, IEnumerable<MedidaProcesal>? medidas)
        {
            // --- Mapeo a RegistroProceso (RP) ---
            var rp = new RegistroProceso
            {
                IdPostulacion = Trunc(Trim(vm.ID), 30)!,
                FMI = Trunc(Trim(vm.FMI), 100)!,
                IdFuenteProceso = vm.IdFuenteProceso!.Value,
                IdTipoProceso = vm.IdTipoProceso!.Value,
                IdEtapaProcesal = vm.IdEtapaProcesal!.Value,
                NumeroExpediente = NullIfEmpty(Trim(vm.NoExpediente)),
                RadicadoOrfeo = NullIfEmpty(Trim(vm.RadicadoOrfeo)),
                Dependencia = NullIfEmpty(Trim(vm.Dependencia)),
            };

            // --- Mapeo a EstudioTerreno (ET) ---
            var et = new EstudioTerreno
            {
                IdLocalizacion = idLocalizacion,
                AreaRegistral = vm.AreaRegistral!.Value,
                AreaCalculada = vm.AreaCalculada!.Value,
                CirculoRegistral = NullIfEmpty(Trim(vm.CirculoRegistral)),

                // Mapeos con limpieza y truncamiento
                TipoPersonaTitular = Trunc(NullIfEmpty(Trim(vm.PersonaTitular)), 50),
                NombrePropietario = Trunc(NullIfEmpty(Trim(vm.NombrePropietarios)), 100),
                ApellidoPropietario = Trunc(NullIfEmpty(Trim(vm.ApellidoPropietario)), 100),

                // ?? APLICACIÓN DE LA RECOMENDACIÓN: Parseo Robusto de Identificación
                Identificacion = ParseLongOrNull(vm.NumeroIdentificacion),

                NaturalezaJuridica = Trunc(NullIfEmpty(Trim(vm.AnalisisNaturalezaUltimaTradicion)), 100),
                AcreditacionPropiedad = Trunc(NullIfEmpty(Trim(vm.TituloOriginario)), 100)
            };

            // --- Mapeo a MedidaProcesal (MP) ---
            var mpList = new List<MedidaProcesal>();
            if (medidas != null)
            {
                foreach (var m in medidas)
                {
                    m.Objeto = Trunc(Trim(m.Objeto), 1000)!;
                    m.Valor = Trunc(Trim(m.Valor), 10)!;
                    m.Anotacion = Trunc(Trim(m.Anotacion), 4000);
                    m.TipoClasificacion = Trunc(Trim(m.TipoClasificacion), 100);
                    mpList.Add(m);
                }
            }
            return (rp, et, mpList);
        }

        // =========================================================================
        // UTILIDADES DE LIMPIEZA Y PARSEO
        // =========================================================================

        /// <summary>
        /// Normaliza la cadena de identificación (quita no-dígitos) y la parsea a long, 
        /// validando longitud y formato.
        /// </summary>
        private static long? ParseLongOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // 1. Normalizar: Quita cualquier carácter que NO sea un dígito (\D)
            var digits = Regex.Replace(s, @"\D", "");

            if (digits.Length == 0)
                return null;

            // 2. Validar Longitud: Máximo 19 (tamaño de long / BigInt)
            if (digits.Length > 19)
                throw new InvalidOperationException("Identificación: El valor ingresado excede el máximo permitido de 19 dígitos.");

            // 3. Intentar Parsear
            if (!long.TryParse(digits, out var v))
                throw new InvalidOperationException("Identificación inválida: Contiene caracteres no numéricos o está fuera de rango.");

            return v;
        }

        private static string? Trim(string? s) => s?.Trim();

        private static string? Trunc(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s!.Length <= max ? s : s.Substring(0, max));

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}

