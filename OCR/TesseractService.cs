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
            string processedImagePath = PreprocessImageForOCR(imagePath);

            using (var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default))
            {
                engine.DefaultPageSegMode = PageSegMode.Auto;

                using (var img = Pix.LoadFromFile(processedImagePath))
                using (var page = engine.Process(img))
                {
                    var text = page.GetText();
                    results.Add(new OcrResult
                    {
                        Text = text,
                        Confidence = page.GetMeanConfidence() * 100
                    });
                }
            }

            CleanupTempFile(processedImagePath, imagePath);
            return results;
        }

        private string PreprocessImageForOCR(string imagePath)
        {
            try
            {
                using (Bitmap originalImage = new Bitmap(imagePath))
                {
                    int scaledWidth = originalImage.Width * 2;
                    int scaledHeight = originalImage.Height * 2;

                    using (Bitmap scaledImage = new Bitmap(originalImage, scaledWidth, scaledHeight))
                    {
                        Bitmap processedImage = new Bitmap(scaledWidth, scaledHeight);

                        for (int y = 0; y < scaledHeight; y++)
                        {
                            for (int x = 0; x < scaledWidth; x++)
                            {
                                Color pixelColor = scaledImage.GetPixel(x, y);
                                int grayValue = (int)(pixelColor.R * 0.299 + pixelColor.G * 0.587 + pixelColor.B * 0.114);

                                grayValue = grayValue < 160 ? 0 : 255;

                                Color newColor = Color.FromArgb(grayValue, grayValue, grayValue);
                                processedImage.SetPixel(x, y, newColor);
                            }
                        }

                        string tempFilePath = Path.Combine(Path.GetTempPath(), "ocr_processed_" + Path.GetFileName(imagePath));
                        processedImage.Save(tempFilePath, System.Drawing.Imaging.ImageFormat.Png);
                        processedImage.Dispose();

                        return tempFilePath;
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
                try
                {
                    File.Delete(processedPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}