using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppPrediosDemo.Infrastructure
{
    public static class AlphanumericInput
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(AlphanumericInput),
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
            var proposed = GetProposedText(tb, e.Text);

            e.Handled = !IsValidAlphanumeric(proposed);
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }

            var tb = (TextBox)sender;
            var text = (string)e.SourceDataObject.GetData(DataFormats.Text);
            var proposed = GetProposedText(tb, text);

            if (!IsValidAlphanumeric(proposed))
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

        // Permite letras, números y espacios (alfanumérico)
        private static bool IsValidAlphanumeric(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            // Permite letras (mayúsculas y minúsculas), números y espacios
            return Regex.IsMatch(text, @"^[a-zA-Z0-9\s]*$");
        }
    }
}

