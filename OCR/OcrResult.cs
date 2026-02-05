namespace ImageToTextOCR.OCR
{
    internal class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; } = 0;
    }
}
