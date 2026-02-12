namespace ImageToTextOCR.OCR
{
    internal class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}