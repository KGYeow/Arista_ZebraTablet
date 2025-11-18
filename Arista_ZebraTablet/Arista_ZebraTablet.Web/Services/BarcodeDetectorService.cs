using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Arista_ZebraTablet.Web.Services
{
    public sealed class BarcodeDetectorService : IBarcodeDetectorService
    {

        public BarcodeDetectorService()
        {
        }

        /// <summary>
        /// No-op on web: does not navigate to any scanner UI.
        /// </summary>
        public Task NavigateToScannerAsync(BarcodeMode mode) => Task.CompletedTask;

        /// <summary>
        /// Decodes barcodes from an image byte array.
        /// </summary>
        public List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes)
        {
            var results = new List<ScanBarcodeItemViewModel>();

            using var stream = new MemoryStream(imageBytes);
            using var originalBitmap = SKBitmap.Decode(stream);

            if (originalBitmap == null)
                return results;

            var resizedBitmap = originalBitmap.Resize(new SKImageInfo(800, 600), SKFilterQuality.Medium);

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

            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            var processedBitmap = new SKBitmap(image.Width, image.Height);
            pixmap?.ReadPixels(processedBitmap.Info, processedBitmap.GetPixels(), processedBitmap.RowBytes, 0, 0);

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat>
                    {
                        ZXing.BarcodeFormat.CODE_128,
                        ZXing.BarcodeFormat.QR_CODE,
                        ZXing.BarcodeFormat.AZTEC,
                        ZXing.BarcodeFormat.DATA_MATRIX,
                        ZXing.BarcodeFormat.PDF_417
                    }
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
        public List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode)
        {
            var results = new List<ScanBarcodeItemViewModel>();
            using var stream = new MemoryStream(imageBytes);
            using var originalBitmap = SKBitmap.Decode(stream);
            if (originalBitmap == null)
                return results;

            var resizedBitmap = originalBitmap.Resize(new SKImageInfo(800, 600), SKFilterQuality.Medium);
            using var surface = SKSurface.Create(new SKImageInfo(resizedBitmap.Width, resizedBitmap.Height));
            var canvas = surface.Canvas;
            using var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
            0.3f, 0.3f, 0.3f, 0, 0,
            0.59f, 0.59f, 0.59f, 0, 0,
            0.11f, 0.11f, 0.11f, 0, 0,
            0, 0, 0, 1, 0
                })
            };
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(resizedBitmap, 0, 0, paint);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            var processedBitmap = new SKBitmap(image.Width, image.Height);
            pixmap?.ReadPixels(processedBitmap.Info, processedBitmap.GetPixels(), processedBitmap.RowBytes, 0, 0);

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat>
            {
                ZXing.BarcodeFormat.CODE_128,
                ZXing.BarcodeFormat.QR_CODE,
                ZXing.BarcodeFormat.AZTEC,
                ZXing.BarcodeFormat.DATA_MATRIX,
                ZXing.BarcodeFormat.PDF_417
            }
                }
            };

            var decodedResults = reader.DecodeMultiple(processedBitmap);
            if (decodedResults != null)
            {
                foreach (var r in decodedResults)
                {
                    string category = mode switch
                    {
                        BarcodeMode.Standard => BarcodeClassifier.Classify(r.Text),
                        BarcodeMode.Unique => UniqueBarcodeClassifier.Classify(r.Text),
                        _ => "Unknown"
                    };

                    results.Add(new ScanBarcodeItemViewModel
                    {
                        Value = r.Text,
                        BarcodeType = r.BarcodeFormat.ToString(),
                        Category = category,
                        ScannedTime = DateTime.Now
                    });
                }
            }
            return results;
        }

        public List<BarcodeGroupItemViewModel> BarcodeGroups { get; set; } = new();
        public Guid? SelectedBarcodeGroupId { get; set; }
        public BarcodeSource SelectedBarcodeSource { get; set; }
    }
}