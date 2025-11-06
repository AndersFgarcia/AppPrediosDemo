// Infrastructure/NumeroEnteroOpcionalValidationRule.cs
using System.Globalization;
using System.Windows.Controls;

namespace AppPrediosDemo.Infrastructure
{
    public class NumeroEnteroOpcionalValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var texto = value as string ?? string.Empty;

            // vacío = válido (campo opcional)
            if (string.IsNullOrWhiteSpace(texto))
                return ValidationResult.ValidResult;

            // solo enteros
            if (int.TryParse(texto, out _))
                return ValidationResult.ValidResult;

            return new ValidationResult(false, "Solo números enteros.");
        }
    }
}


