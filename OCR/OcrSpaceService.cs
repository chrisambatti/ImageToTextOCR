using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageToTextOCR.OCR
{
    internal class OcrSpaceService
    {
        private readonly string _apiKey = "K86721916188957";
        private readonly string _apiUrl = "https://api.ocr.space/parse/image";

        public async Task<List<OcrResult>> ExtractTextAsync(string imagePath)
        {
            var results = new List<OcrResult>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(3);

                    using (var content = new MultipartFormDataContent())
                    {
                        byte[] imageBytes = File.ReadAllBytes(imagePath);
                        var fileContent = new ByteArrayContent(imageBytes);
                        content.Add(fileContent, "file", Path.GetFileName(imagePath));

                        content.Add(new StringContent(_apiKey), "apikey");
                        content.Add(new StringContent("2"), "OCREngine");
                        content.Add(new StringContent("true"), "isTable");
                        content.Add(new StringContent("true"), "detectOrientation");

                        var response = await client.PostAsync(_apiUrl, content);
                        var jsonResult = await response.Content.ReadAsStringAsync();

                        var ocrResponse = JsonConvert.DeserializeObject<OcrApiResponse>(jsonResult);

                        if (ocrResponse?.ParsedResults != null && ocrResponse.ParsedResults.Count > 0)
                        {
                            string extractedText = ocrResponse.ParsedResults[0].ParsedText;
                            results.Add(new OcrResult
                            {
                                Text = extractedText,
                                Confidence = 90
                            });
                        }
                        else if (ocrResponse?.ErrorMessage != null && ocrResponse.ErrorMessage.Count > 0)
                        {
                            throw new Exception($"OCR.space Error: {string.Join(", ", ocrResponse.ErrorMessage)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR API Error: {ex.Message}");
            }

            return results;
        }

        private class OcrApiResponse
        {
            public List<OcrParsedResult> ParsedResults { get; set; }
            public List<string> ErrorMessage { get; set; }
            public bool IsErroredOnProcessing { get; set; }
        }

        private class OcrParsedResult
        {
            public string ParsedText { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}