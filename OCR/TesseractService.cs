using System.Collections.Generic;
using System.Drawing;
using Tesseract;
using ImageToTextOCR.OCR;

namespace ImageToTextOCR.OCR
{
    internal class TesseractService
    {
        private readonly string _tessdataPath = "tessdata";

        public List<OcrResult> ExtractText(string imagePath)
        {
            var results = new List<OcrResult>();

            using (var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default))
            using (var img = Pix.LoadFromFile(imagePath))
            using (var page = engine.Process(img))
            {
                var text = page.GetText();
                results.Add(new OcrResult
                {
                    Text = text.Trim(),
                    Confidence = page.GetMeanConfidence() * 100
                });
            }

            return results;
        }
    }
}
