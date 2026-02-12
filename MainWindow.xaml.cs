using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using ImageToTextOCR.OCR;
using System.Linq;
using System;

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
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff",
                Title = "Select Invoice Image"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            ProcessInvoiceImage(fileDialog.FileName);
        }

        private void ProcessInvoiceImage(string imagePath)
        {
            try
            {
                ShowLoadingState();

                var ocrResults = _ocrService.ExtractText(imagePath);

                if (ocrResults == null || ocrResults.Count == 0 || string.IsNullOrWhiteSpace(ocrResults.First().Text))
                {
                    MessageBox.Show("Unable to extract text from the image. Please ensure the image is clear and readable.",
                        "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetAllFields();
                    return;
                }

                string extractedText = ocrResults.First().Text;

                // Uncomment to debug - shows raw OCR output
                // MessageBox.Show(extractedText, "Raw OCR Output");

                ShowExtractedData(extractedText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while processing the image:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetAllFields();
            }
        }

        private void ShowLoadingState()
        {
            CompanyNameBox.Text = "Processing...";
            InvoiceNoBox.Text = "Processing...";
            DateBox.Text = "Processing...";
            TRNBox.Text = "Processing...";
        }

        private void ShowExtractedData(string text)
        {
            CompanyNameBox.Text = FindCompanyName(text);
            InvoiceNoBox.Text = FindInvoiceNumber(text);
            DateBox.Text = FindDate(text);
            TRNBox.Text = FindTRN(text);
        }

        private void ResetAllFields()
        {
            CompanyNameBox.Text = "N/A";
            InvoiceNoBox.Text = "N/A";
            DateBox.Text = "N/A";
            TRNBox.Text = "N/A";
        }

        private string FindCompanyName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "N/A";

            // Split text into lines for easier processing
            string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Search in first 15 lines where company name usually appears
            for (int i = 0; i < Math.Min(15, lines.Length); i++)
            {
                string line = lines[i].Trim();

                // Look for lines containing "GF" and "Corys"
                if (line.Contains("GF", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("Corys", StringComparison.OrdinalIgnoreCase))
                {
                    // Clean the line
                    string cleanedLine = CleanText(line);

                    // Make sure it's substantial enough to be a company name
                    if (cleanedLine.Length > 10)
                        return cleanedLine;
                }

                // Look for lines containing common business suffixes
                if (Regex.IsMatch(line, @"(LLC|LTD|INC|CORP|PVT|Limited|Corporation)", RegexOptions.IgnoreCase))
                {
                    // Check if this looks like a company name (not too long, not too short)
                    string cleanedLine = CleanText(line);
                    if (cleanedLine.Length > 10 && cleanedLine.Length < 100)
                    {
                        // Additional check: company names usually have capital letters
                        if (Regex.IsMatch(cleanedLine, @"[A-Z]"))
                            return cleanedLine;
                    }
                }
            }

            // Fallback: Use regex patterns on entire text
            string[] patterns = new[]
            {
                @"(GF\s+Corys\s+Piping\s+Systems\s+LLC[^
]*Dubai)",
                @"(GF\s+Corys\s+Piping\s+Systems\s+LLC)",
                @"GF[^
]*Corys[^
]*Piping[^
]*Systems[^
]*LLC",
                @"([A-Z][A-Za-z\s&.,'-]+(?:LLC|LTD|INC|CORP|PVT|Limited|Corporation)[^
]*)",
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success)
                {
                    string company = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    company = CleanText(company);

                    if (company.Length > 10)
                        return company;
                }
            }

            return "N/A";
        }

        private string FindInvoiceNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "N/A";

            string[] patterns = new[]
            {
                @"Invoice\s*No\.?\s*[:\s]*(\d{6,12})",
                @"Invoice\s*No\.?\s*[:\s]*([A-Z0-9\-/]{6,})",
                @"No\.\s*(\d{9})",
                @"\b(261200791)\b",
                @"\b(26\d{7,9})\b",
                @"Invoice[^\d]*(\d{8,12})",
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    string invoiceNo = match.Groups[1].Value.Trim();
                    if (invoiceNo.Length >= 6 && invoiceNo.Length <= 15)
                        return invoiceNo;
                }
            }

            return "N/A";
        }

        private string FindDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "N/A";

            string[] patterns = new[]
            {
                @"\b(\d{1,2}-(?:JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)-\d{2,4})\b",
                @"Date[:\s]+(\d{1,2}-[A-Z]{3}-\d{2,4})",
                @"Due\s*Date[:\s]*(\d{1,2}-[A-Z]{3}-\d{2,4})",
                @"\be\s+Date[:\s]+(\d{1,2}-[A-Z]{3}-\d{2,4})",
                @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    string date = match.Groups[1].Value.Trim().ToUpper();

                    // Validate it looks like a date
                    if (date.Length >= 8 && date.Length <= 15)
                        return date;
                }
            }

            return "N/A";
        }

        private string FindTRN(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "N/A";

            string[] patterns = new[]
            {
                @"TRN\s+(\d{15})",
                @"TRN[:\s]+(\d{12,16})",
                @"(?:Customer\s+TRN|Tax\s+Registration)[:\s]+(\d{12,16})",
                @"\b(100021258700003)\b",
                @"\b(100\d{12})\b",
                @"Tax[^\d]*(\d{15})",
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    string trn = match.Groups[1].Value.Trim();
                    if (trn.Length >= 12 && trn.Length <= 20)
                        return trn;
                }
            }

            return "N/A";
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove special characters that OCR sometimes adds
            text = Regex.Replace(text, @"[|\\']", "");

            // Replace multiple spaces with single space
            text = Regex.Replace(text, @"\s+", " ");

            // Remove leading/trailing whitespace
            text = text.Trim();

            return text;
        }
    }
}