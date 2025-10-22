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
            using var skBitmap = SKBitmap.Decode(stream);

            if (skBitmap == null)
                return results;

            var reader = new BarcodeReader(); // from ZXing.SkiaSharp
            var decodedResults = reader.DecodeMultiple(skBitmap); // ? Directly pass SKBitmap

            if (decodedResults != null)
            {
                foreach (var r in decodedResults)
                {
                    results.Add(new ScanBarcodeItemViewModel
                    {
                        Value = r.Text,
                        BarcodeType = r.BarcodeFormat.ToString(),
                        Category = BarcodeClassifier.Classify(r.Text),
                        Time = DateTimeOffset.Now
                    });
                }
            }

            return results;
        }
    }

}