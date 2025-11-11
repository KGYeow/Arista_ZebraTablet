using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Arista_ZebraTablet.Services
{
    /// <summary>
    /// Handles barcode scanning navigation, result management, and image-based decoding.
    /// </summary>
    public sealed class BarcodeDetectorService : IBarcodeDetectorService
    {
        private readonly IServiceProvider _services;
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
        public ObservableCollection<ScanBarcodeItemViewModel> Results { get; } = []; // Scan using Camera Result Management
        public event EventHandler<ScanBarcodeItemViewModel>? ScanReceived;  // <--- ADD
        public ObservableCollection<FrameItemViewModel> Frames { get; } = new();
        public BarcodeDetectorService(IServiceProvider services)
        {
            _services = services;
        }



        /// <summary>
        /// Navigation to live camera Scanner Page
        /// </summary>
        public async Task NavigateToScannerAsync(BarcodeMode mode)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = ActivatorUtilities.CreateInstance<LiveBarcodeScannerPage>(_services, mode);
                    await App.Current.Windows[0].Page.Navigation.PushModalAsync(page, true);
                }
                catch (Exception ex)
                {
                    await App.Current.Windows[0].Page.DisplayAlert("Navigation error", ex.Message, "OK");
                }
            });
        }

        /// <summary>
        /// Adds a scanned barcode to the result list if not already added.
        /// </summary>
        public void Add(string value, string barcodeType, string category)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            Console.WriteLine($"Adding barcode to Results: {value}");
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

        /// <summary>
        /// Reorders uploaded images in the list.
        /// </summary>
        public List<ImgItemViewModel> UploadedImages { get; set; } = new();
        public Guid? SelectedImageId { get; set; }

        public void RaiseScanReceived(ScanBarcodeItemViewModel barcode)
        {
            ScanReceived?.Invoke(this, barcode);
        }


    }
}