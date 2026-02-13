using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            string fullText = ExtractWithBestSettings(imagePath);

            results.Add(new OcrResult
            {
                Text = fullText,
                Confidence = 85
            });

            return results;
        }

        private string ExtractWithBestSettings(string imagePath)
        {
            string processedPath = PreprocessImage(imagePath);

            try
            {
                using (var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default))
                {
                    engine.DefaultPageSegMode = PageSegMode.Auto;
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("tessedit_char_whitelist",
                        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz .,/:()-°%");

                    using (var img = Pix.LoadFromFile(processedPath))
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
            finally
            {
                CleanupTempFile(processedPath, imagePath);
            }
        }

        private string PreprocessImage(string imagePath)
        {
            try
            {
                using (Bitmap original = new Bitmap(imagePath))
                {
                    int scale = 4;
                    int newWidth = original.Width * scale;
                    int newHeight = original.Height * scale;

                    using (Bitmap scaled = new Bitmap(original, newWidth, newHeight))
                    {
                        Bitmap processed = new Bitmap(newWidth, newHeight);

                        for (int y = 0; y < newHeight; y++)
                        {
                            for (int x = 0; x < newWidth; x++)
                            {
                                Color pixel = scaled.GetPixel(x, y);
                                int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                                gray = gray < 140 ? 0 : 255;
                                processed.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                            }
                        }

                        string tempPath = Path.Combine(Path.GetTempPath(), "invoice_ocr_" + Path.GetFileName(imagePath));
                        processed.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                        processed.Dispose();

                        return tempPath;
                    }
                }
            }
            catch
            {
                return imagePath;
            }
        }

        private void CleanupTempFile(string processedPath, string originalPath)
        {
            if (processedPath != originalPath && File.Exists(processedPath))
            {
                try { File.Delete(processedPath); } catch { }
            }
        }
    }
}