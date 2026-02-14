using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using ImageToTextOCR.OCR;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageToTextOCR
{
    public partial class MainWindow : Window
    {
        private readonly OcrSpaceService _ocrService;

        public MainWindow()
        {
            InitializeComponent();
            _ocrService = new OcrSpaceService();
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Invoice Files|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tiff|PDF Files|*.pdf|Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff",
                Title = "Select Invoice (PDF or Image)"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            await ProcessInvoiceAsync(fileDialog.FileName);
        }

        private async Task ProcessInvoiceAsync(string filePath)
        {
            try
            {
                ShowLoadingState();

                var ocrResults = await _ocrService.ExtractTextAsync(filePath);

                if (ocrResults == null || ocrResults.Count == 0)
                {
                    MessageBox.Show("Failed to extract text from the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetAllFields();
                    return;
                }

                string text = ocrResults.First().Text;

                // Extract header fields
                CompanyNameBox.Text = ExtractCompanyName(text);
                InvoiceNoBox.Text = ExtractInvoiceNumber(text);
                DateBox.Text = ExtractDate(text);
                TRNBox.Text = ExtractTRN(text);

                // Extract new fields
                SalesPersonBox.Text = ExtractSalesPerson(text);
                PaymentTermsBox.Text = ExtractPaymentTerms(text);
                ShipDateBox.Text = ExtractShipDate(text);
                DONumberBox.Text = ExtractDONumber(text);
                SONumberBox.Text = ExtractSONumber(text);

                // Extract line items
                var items = ExtractLineItems(text);
                InvoiceItemsGrid.ItemsSource = items;

                if (items.Count == 0)
                {
                    MessageBox.Show("No line items found in the invoice.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetAllFields();
            }
        }

        private void ShowLoadingState()
        {
            CompanyNameBox.Text = "Processing...";
            InvoiceNoBox.Text = "Processing...";
            DateBox.Text = "Processing...";
            TRNBox.Text = "Processing...";
            SalesPersonBox.Text = "Processing...";
            PaymentTermsBox.Text = "Processing...";
            ShipDateBox.Text = "Processing...";
            DONumberBox.Text = "Processing...";
            SONumberBox.Text = "Processing...";
            InvoiceItemsGrid.ItemsSource = null;
        }

        private void ResetAllFields()
        {
            CompanyNameBox.Text = "N/A";
            InvoiceNoBox.Text = "N/A";
            DateBox.Text = "N/A";
            TRNBox.Text = "N/A";
            SalesPersonBox.Text = "N/A";
            PaymentTermsBox.Text = "N/A";
            ShipDateBox.Text = "N/A";
            DONumberBox.Text = "N/A";
            SONumberBox.Text = "N/A";
            InvoiceItemsGrid.ItemsSource = null;
        }

        private string ExtractCompanyName(string text)
        {
            var match = Regex.Match(text, @"GF\s+Corys\s+Piping\s+Systems\s+LLC\s*-?\s*Duba[il]", RegexOptions.IgnoreCase);
            if (match.Success)
                return "GF Corys Piping Systems LLC - Dubai";

            return "N/A";
        }

        private string ExtractInvoiceNumber(string text)
        {
            var match = Regex.Match(text, @"\b(26\d{7,8})\b");
            if (match.Success)
                return match.Groups[1].Value;

            match = Regex.Match(text, @"Invoice.*?(\d{8,10})", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            return "N/A";
        }

        private string ExtractDate(string text)
        {
            var match = Regex.Match(text, @"\b(\d{1,2}[-/](?:JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[-/]\d{2,4})\b", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.ToUpper();

            return "N/A";
        }

        private string ExtractTRN(string text)
        {
            var match = Regex.Match(text, @"\b(100\d{12,15})\b");
            if (match.Success)
                return match.Groups[1].Value;

            match = Regex.Match(text, @"TRN\s*[:\s]*(\d{12,16})", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            return "N/A";
        }

        private string ExtractSalesPerson(string text)
        {
            // Look for "BIJU V. PILLAI" or similar pattern (name with initials)
            var match = Regex.Match(text, @"\b([A-Z]{3,}\s+[A-Z]\.\s+[A-Z]{3,})\b");
            if (match.Success)
            {
                string name = match.Groups[1].Value.Trim();
                // Make sure it's not a common false positive
                if (!name.Contains("LLC") && !name.Contains("BOX"))
                    return name;
            }

            // Alternative: Look after "Sales Person" label
            match = Regex.Match(text, @"Sales\s+Person[:\s]*\r?\n?\s*([A-Z][A-Z\s.]+?)(?:\r|\n|Payment|Ship)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "N/A";
        }

        private string ExtractPaymentTerms(string text)
        {
            // Look for "90 Days" pattern
            var match = Regex.Match(text, @"(\d+\s+Days\.?)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Alternative: Look after "Payment Terms" label
            match = Regex.Match(text, @"Payment\s+Terms[:\s]*(.+?)(?:\r|\n|Ship)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string terms = match.Groups[1].Value.Trim();
                if (terms.Length > 0 && terms.Length < 50)
                    return terms;
            }

            return "N/A";
        }

        private string ExtractShipDate(string text)
        {
            // Pattern 1: Look for "Shilp Date" or "Ship Date" with date
            var match = Regex.Match(text, @"(?:Ship|Shilp)\s+Date[:\s]*(\d{1,2}[A-Z]{3}-?\d{2,4})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string date = match.Groups[1].Value.Trim().ToUpper();
                // Ensure hyphen format: 11-JAN-2026
                return FormatDateWithHyphens(date);
            }

            // Pattern 2: Look for "11JAN-2026" format anywhere in text
            match = Regex.Match(text, @"\b(\d{1,2}[A-Z]{3}-?\d{4})\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string date = match.Groups[1].Value.Trim().ToUpper();
                // Make sure it's not the invoice date we already extracted
                if (date != DateBox.Text)
                    return FormatDateWithHyphens(date);
            }

            // Pattern 3: Look for date near the sales section
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool foundSalesPerson = false;

            foreach (var line in lines)
            {
                if (line.Contains("Sales Person", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Payment Terms", StringComparison.OrdinalIgnoreCase))
                {
                    foundSalesPerson = true;
                }

                if (foundSalesPerson)
                {
                    match = Regex.Match(line, @"\b(\d{1,2}[A-Z]{3}-?\d{4})\b", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string date = match.Groups[1].Value.Trim().ToUpper();
                        return FormatDateWithHyphens(date);
                    }
                }
            }

            return "N/A";
        }

        private string FormatDateWithHyphens(string date)
        {
            // Convert "11JAN2026" or "11JAN-2026" to "11-JAN-2026"
            if (string.IsNullOrEmpty(date))
                return date;

            // Pattern: ddMMMyyyyd or dd-MMM-yyyy
            var match = Regex.Match(date, @"^(\d{1,2})([A-Z]{3})-?(\d{4})$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string day = match.Groups[1].Value;
                string month = match.Groups[2].Value.ToUpper();
                string year = match.Groups[3].Value;

                return $"{day}-{month}-{year}";
            }

            return date;
        }

        private string ExtractDONumber(string text)
        {
            // Pattern 1: Look for "D.O. Number" followed by 8 digits
            var match = Regex.Match(text, @"D\.?\s*O\.?\s*Number[:\s]*(\d{7,9})", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Pattern 2: Look for 8-digit number starting with 3 or 4 (typical D.O. format)
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool inHeaderSection = false;

            foreach (var line in lines)
            {
                if (line.Contains("Payment Terms", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Ship Date", StringComparison.OrdinalIgnoreCase))
                {
                    inHeaderSection = true;
                }

                if (line.Contains("Item Code", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Item Description", StringComparison.OrdinalIgnoreCase))
                {
                    break; // Stop at table section
                }

                if (inHeaderSection)
                {
                    // Look for 8-digit number
                    match = Regex.Match(line, @"\b([34]\d{7})\b");
                    if (match.Success)
                        return match.Groups[1].Value.Trim();
                }
            }

            return "N/A";
        }

        private string ExtractSONumber(string text)
        {
            // Pattern 1: Look for "S.O. Number" followed by 10 digits
            var match = Regex.Match(text, @"S\.?\s*O\.?\s*Number[:\s]*(\d{9,11})", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Pattern 2: Look for 10-digit number starting with 25 (typical S.O. format)
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool inHeaderSection = false;

            foreach (var line in lines)
            {
                if (line.Contains("Payment Terms", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Ship Date", StringComparison.OrdinalIgnoreCase))
                {
                    inHeaderSection = true;
                }

                if (line.Contains("Item Code", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Item Description", StringComparison.OrdinalIgnoreCase))
                {
                    break; // Stop at table section
                }

                if (inHeaderSection)
                {
                    // Look for 10-digit number starting with 25
                    match = Regex.Match(line, @"\b(25\d{8})\b");
                    if (match.Success)
                        return match.Groups[1].Value.Trim();
                }
            }

            return "N/A";
        }

        private List<InvoiceLineItem> ExtractLineItems(string text)
        {
            var items = new List<InvoiceLineItem>();
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            bool inTableSection = false;
            int itemCounter = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (Regex.IsMatch(line, @"S\.?no|S\.?\s*No|Item\s+Code|Item\s+Description|UOM|QTY", RegexOptions.IgnoreCase))
                {
                    inTableSection = true;
                    continue;
                }

                if (Regex.IsMatch(line, @"^Total\s+Number|^Freight|^Total\s+In\s+Words|^Misc", RegexOptions.IgnoreCase))
                    break;

                if (inTableSection)
                {
                    if (Regex.IsMatch(line, @"\b[A-Z]\d{9,10}\b"))
                    {
                        string combinedLine = line;
                        if (i + 1 < lines.Length)
                        {
                            combinedLine += " " + lines[i + 1].Trim();
                        }

                        var item = ParseLineItem(combinedLine, itemCounter);
                        if (item != null)
                        {
                            items.Add(item);
                            itemCounter++;
                        }
                    }
                }
            }

            return items;
        }

        private InvoiceLineItem ParseLineItem(string line, int counter)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var codeMatch = Regex.Match(line, @"\b([A-Z]\d{9,10})\b");
            if (!codeMatch.Success)
                return null;

            var item = new InvoiceLineItem();
            item.ItemCode = codeMatch.Groups[1].Value;
            item.SrNo = counter.ToString();

            var descMatch = Regex.Match(line, @"(Duct\s+Bend[^\d]+?)(?:\s+EA|\s+PC|\s+UNIT|\s+\d{3,})", RegexOptions.IgnoreCase);
            if (descMatch.Success)
            {
                item.ItemDescription = descMatch.Groups[1].Value.Trim();
            }

            var uomMatch = Regex.Match(line, @"\b(EA|PC|UNIT|KG|MTR|SET|BOX)\b", RegexOptions.IgnoreCase);
            if (uomMatch.Success)
                item.UOM = uomMatch.Groups[1].Value.ToUpper();

            var vatMatch = Regex.Match(line, @"\b(\d{1,2})%");
            if (vatMatch.Success)
            {
                item.VATPercent = vatMatch.Groups[1].Value + "%";
            }

            var allNumbers = Regex.Matches(line, @"\d+\.\d+");
            var numbers = new List<string>();

            foreach (Match m in allNumbers)
            {
                numbers.Add(m.Value);
            }

            if (numbers.Count >= 5)
            {
                item.Quantity = numbers[0];
                item.UnitRate = numbers[1];
                item.TotalExclVAT = numbers[2];
                item.VATAmount = numbers[3];
                item.TotalInclVAT = numbers[4];
            }
            else if (numbers.Count >= 2)
            {
                item.Quantity = numbers[0];
                item.UnitRate = numbers[1];
            }

            if (!string.IsNullOrEmpty(item.ItemCode))
                return item;

            return null;
        }
    }
}