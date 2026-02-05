using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using ImageToTextOCR.OCR;
using System.Linq;

namespace ImageToTextOCR
{
    public partial class MainWindow : Window
    {
        private readonly TesseractService _ocrService;

        public MainWindow()
        {
            InitializeComponent();
            _ocrService = new TesseractService();
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() != true)
                return;

            var ocrResults = _ocrService.ExtractText(dialog.FileName);
            if (ocrResults.Count == 0) return;

            string text = ocrResults.First().Text;
            text = CleanText(text);

            CompanyNameBox.Text = ExtractCompany(text);
            InvoiceNoBox.Text = ExtractInvoiceNo(text);
            DateBox.Text = ExtractDate(text);
            TRNBox.Text = ExtractTRN(text);
        }

        private string CleanText(string input)
        {
            // Remove strange characters and normalize spaces
            string cleaned = Regex.Replace(input, @"[^\w\s\-/.:]", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.ToUpper();
        }

        private string ExtractCompany(string text)
        {
            // Match "GF CORYS PIPING SYSTEMS LLC" type
            var match = Regex.Match(text, @"GF\s+CORYS\s+PIPING\s+SYSTEMS\s+LLC");
            return match.Success ? match.Value : "N/A";
        }

        private string ExtractInvoiceNo(string text)
        {
            var match = Regex.Match(text, @"\b\d{10,}\b"); // 10+ digit numbers
            return match.Success ? match.Value : "N/A";
        }

        private string ExtractDate(string text)
        {
            var match = Regex.Match(text, @"\b\d{2}-[A-Z]{3}-\d{2,4}\b"); // e.g., 11-APR-26
            return match.Success ? match.Value : "N/A";
        }

        private string ExtractTRN(string text)
        {
            var match = Regex.Match(text, @"\b\d{15}\b"); // 15-digit TRN
            return match.Success ? match.Value : "N/A";
        }
    }
}
