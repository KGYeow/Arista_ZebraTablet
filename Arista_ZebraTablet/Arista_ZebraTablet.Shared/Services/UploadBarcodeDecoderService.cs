using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;
using ZXing.Net.Maui;

namespace Arista_ZebraTablet.Shared.Services
{
    public class UploadBarcodeDecoderService
    {
        public List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes)
        {
            var results = new List<ScanBarcodeItemViewModel>();

            using var stream = new MemoryStream(imageBytes);
            using var originalBitmap = SKBitmap.Decode(stream);

            if (originalBitmap == null)
                return results;


            // Resize to reduce processing time (optional)
            var resizedBitmap = originalBitmap.Resize(new SKImageInfo(800, 600), SKFilterQuality.Medium);

            // Create a grayscale version using SKColorFilter
            using var surface = SKSurface.Create(new SKImageInfo(resizedBitmap.Width, resizedBitmap.Height));
            var canvas = surface.Canvas;

            using var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
            0.3f, 0.3f, 0.3f, 0, 0,
            0.59f, 0.59f, 0.59f, 0, 0,
            0.11f, 0.11f, 0.11f, 0, 0,
            0,    0,    0,    1, 0
                })
            };

            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(resizedBitmap, 0, 0, paint);
            canvas.Flush();

            // Convert SKSurface to SKBitmap
            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            var processedBitmap = new SKBitmap(image.Width, image.Height);
            pixmap?.ReadPixels(processedBitmap.Info, processedBitmap.GetPixels(), processedBitmap.RowBytes, 0, 0);



            //var reader = new BarcodeReader(); // from ZXing.SkiaSharp
            //var decodedResults = reader.DecodeMultiple(skBitmap); // ? Directly pass SKBitmap

            //if (decodedResults != null)
            //{
            //    foreach (var r in decodedResults)
            //    {
            //        results.Add(new ScanBarcodeItemViewModel
            //        {
            //            Value = r.Text,
            //            BarcodeType = r.BarcodeFormat.ToString(),
            //            Category = BarcodeClassifier.Classify(r.Text),
            //            Time = DateTimeOffset.Now
            //        });
            //    }
            //}

            //return results;

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.AZTEC, ZXing.BarcodeFormat.DATA_MATRIX, ZXing.BarcodeFormat.PDF_417 }
                }
            };

            var decodedResults = reader.DecodeMultiple(processedBitmap);

            if (decodedResults != null)
            {
                foreach (var r in decodedResults)
                {
                    results.Add(new ScanBarcodeItemViewModel
                    {
                        Value = r.Text,
                        BarcodeType = r.BarcodeFormat.ToString(),
                        Category = BarcodeClassifier.Classify(r.Text),
                        ScannedTime = DateTime.Now
                    });
                }
            }

            return results;

        }
    }

}