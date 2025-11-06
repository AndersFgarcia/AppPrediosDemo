using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppPrediosDemo.Infrastructure
{
    public static class DecimalInput
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(DecimalInput),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(TextBox element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(TextBox element) => (bool)element.GetValue(EnableProperty);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnPreviewTextInput;
                DataObject.AddPastingHandler(tb, OnPaste);
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            // Acepta tanto "." como "," y las normaliza a la cultura actual
            var input = e.Text.Replace(",", dec).Replace(".", dec);
            var proposed = GetProposedText(tb, input);

            e.Handled = !IsValidPartialDecimal(proposed, dec);
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }

            var tb = (TextBox)sender;
            var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            var text = (string)e.SourceDataObject.GetData(DataFormats.Text);
            text = text.Replace(",", dec).Replace(".", dec);
            var proposed = GetProposedText(tb, text);

            if (!IsValidPartialDecimal(proposed, dec))
                e.CancelCommand();
        }

        private static string GetProposedText(TextBox tb, string input)
        {
            var start = tb.SelectionStart;
            var len = tb.SelectionLength;
            var baseText = tb.Text ?? "";
            if (len > 0 && start < baseText.Length)
                baseText = baseText.Remove(start, len);
            return baseText.Insert(start, input);
        }

        // Permite "", "123", "123,", "123,45" según la cultura
        private static bool IsValidPartialDecimal(string text, string dec)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var pattern = @"^\d*(\" + Regex.Escape(dec) + @"?\d*)?$";
            return Regex.IsMatch(text, pattern);
        }
    }
}
