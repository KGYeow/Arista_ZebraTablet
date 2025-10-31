using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;
using ZXing.Net.Maui;

namespace Arista_ZebraTablet.Services
{
    /// <summary>
    /// Handles barcode scanning navigation, result management, and image-based decoding.
    /// </summary>
    public sealed class BarcodeDetectorService : IBarcodeDetectorService
    {
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
        public ObservableCollection<ScanBarcodeItemViewModel> Results { get; } = []; // Scan using Camera Result Management
        public event EventHandler<ScanBarcodeItemViewModel>? ScanReceived;  // <--- ADD

        public BarcodeDetectorService()
        {
        }

        /// <summary>
        /// Navigation to live camera Scanner Page
        /// </summary>
        public async Task NavigateToScannerAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Create scanner page via DI
                    var page = new LiveBarcodeScannerPage(this);
                    await Application.Current!.MainPage!.Navigation.PushModalAsync(page, animated: true);
                }
                catch (Exception ex)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Navigation error", ex.Message, "OK");
                }
            });
        }

        /// <summary>
        /// Adds a scanned barcode to the result list if not already added.
        /// </summary>
        public void Add(string value, string barcodeType, string category)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_seen.Add(value))
                {
                    var vm = new ScanBarcodeItemViewModel
                    {
                        Value = value,
                        BarcodeType = barcodeType,
                        Category = category,
                        ScannedTime = DateTime.Now
                    };
                    Results.Insert(0, vm);
                    ScanReceived?.Invoke(this, vm); // <--- TELL UI
                }

            });
        }

        /// <summary>
        /// Clears all scanned results.
        /// </summary>
        public void Clear()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Results.Clear();
                _seen.Clear();
            });
        }

        /// <summary>
        /// Decodes multiple barcodes from an uploaded image byte array using ZXing and SkiaSharp.
        /// </summary>
        public List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes)
        {
            var results = new List<ScanBarcodeItemViewModel>();

            using var stream = new MemoryStream(imageBytes);
            using var originalBitmap = SKBitmap.Decode(stream);

            if (originalBitmap == null)
                return results;

            // Resize image to improve performance
            var resizedBitmap = originalBitmap.Resize(new SKImageInfo(800, 600), SKFilterQuality.Medium);

            // Convert to grayscale for better barcode detection
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

            // Convert processed surface to bitmap
            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            var processedBitmap = new SKBitmap(image.Width, image.Height);
            pixmap?.ReadPixels(processedBitmap.Info, processedBitmap.GetPixels(), processedBitmap.RowBytes, 0, 0);

            // Setup ZXing barcode reader
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

            // Decode multiple barcodes
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