# ImageToTextOCR

ImageToTextOCR is a **Windows WPF application** built with **.NET 8.0** that extracts key invoice information from images using **Tesseract OCR**. It is aimed at automating invoice data extraction for finance, accounting, and operations teams.

## Features
- Upload invoice images in PNG, JPG, or JPEG format.
- Extract essential fields:
  - Company Name
  - Invoice Number
  - Date
  - TRN
  - Item Code, Description, Quantity, Unit Rate, Total Value
- Confidence-based highlights for verification.
- Executive-friendly output display.
- Windows-only WPF application for responsive and clean UI.

## Technologies Used
- **.NET 8.0 (Windows-only)**  
- **WPF** for UI  
- **Tesseract OCR** for text extraction  
- **System.Drawing.Common** for image processing  

## Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/chrisambatti/ImageToTextOCR.git
Open the solution in Visual Studio 2022/2023.

Restore NuGet packages:

dotnet restore


Build and run the application:

dotnet build
dotnet run

Usage

Click Upload Invoice.

Select the invoice image file.

The extracted text will appear in the UI fields.

Fields with low confidence are highlighted (pink for review, green for high confidence).

Notes

The OCR works best on clearly printed invoices.

Handwritten, blurry, or poorly scanned invoices may result in lower accuracy.

Only fields that can be reliably extracted are included.

License

This project is open-source under the MIT License.

Acknowledgments

Special thanks to MauiOcrPluginSample

and Gerald Versluis for the detailed video on OCR integration.
