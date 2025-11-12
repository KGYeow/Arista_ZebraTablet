using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using ZXing.Common;
using ZXing.SkiaSharp;
using static Arista_ZebraTablet.Shared.Pages.Home;

namespace Arista_ZebraTablet.Services;

/// <summary>
/// Provides navigation to the live scanner page, manages in-memory scan results,
/// and decodes barcodes from image bytes using ZXing + SkiaSharp.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Threading:</strong> Methods that update UI-bound state (e.g., <see cref="Add"/>, <see cref="Clear"/>) 
/// marshal to the main thread via <see cref="MainThread"/>. Consumers can subscribe to 
/// <see cref="ScanReceived"/> to react to new scan items as they arrive.
/// </para>
/// <para>
/// <strong>State sharing:</strong> <see cref="UploadedImages"/> and <see cref="SelectedImageId"/> are used by
/// the web/hybrid UI to persist selections across page navigation (e.g., Reorder page).
/// </para>
/// </remarks>
public sealed class BarcodeDetectorService : IBarcodeDetectorService
{
    #region Dependencies

    private readonly IServiceProvider _services;

    #endregion

    #region State & events

    /// <summary>
    /// Used to filter out duplicate barcodes in a single session (case-insensitive).
    /// </summary>
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Live collection of scan results shown in the UI when scanning via camera.
    /// New items are inserted at index 0.
    /// </summary>
    public ObservableCollection<ScanBarcodeItemViewModel> Results { get; } = [];

    /// <summary>
    /// Raised after a new scan result has been added to <see cref="Results"/>.
    /// </summary>
    public event EventHandler<ScanBarcodeItemViewModel>? ScanReceived;

    public ObservableCollection<FrameItemViewModel> Frames { get; } = new();

    #endregion

    #region Constructer

    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeDetectorService"/> class.
    /// </summary>
    /// <param name="services">Application service provider used for page activation.</param>
    public BarcodeDetectorService(IServiceProvider services)
    {
        _services = services;
    }

    #endregion

    #region Properties (IBarcodeDetectorService)

    /// <inheritdoc/>
    public List<ImgItemViewModel> UploadedImages { get; set; } = new();

    /// <inheritdoc/>
    /// <remarks>
    /// Use <see cref="Guid.Empty"/> to indicate that the reorder scope is “all images”.
    /// </remarks>
    public Guid? SelectedImageId { get; set; }

    public void RaiseScanReceived(ScanBarcodeItemViewModel barcode)
    {
        ScanReceived?.Invoke(this, barcode);
    }

    #endregion

    #region Navigation

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The navigation is executed on the UI thread. Any exceptions are shown via a modal alert.
    /// </para>
    /// <para>
    /// The page is pushed <em>modally</em> to isolate scanning UX; adjust to non-modal if preferred.
    /// </para>
    /// </remarks>
    public async Task NavigateToScannerAsync(BarcodeMode mode)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Create the page with DI so its constructor dependencies are resolved.
                var page = ActivatorUtilities.CreateInstance<LiveBarcodeScannerPage>(_services, mode);
                await App.Current.Windows[0].Page.Navigation.PushModalAsync(page, true);
            }
            catch (Exception ex)
            {
                await App.Current.Windows[0].Page.DisplayAlert("Navigation error", ex.Message, "OK");
            }
        });
    }

    #endregion

    #region Result management (camera scanning)

    /// <summary>
    /// Adds a scanned barcode to <see cref="Results"/> (if not already present) and
    /// raises <see cref="ScanReceived"/> for subscribers.
    /// </summary>
    /// <param name="value">The decoded barcode value.</param>
    /// <param name="barcodeType">A human-readable barcode format (e.g., "QR_CODE", "CODE_128").</param>
    /// <param name="category">App-specific category assigned to the value.</param>
    /// <remarks>
    /// Duplicate values are ignored during a session; comparison is case-insensitive.
    /// </remarks>
    public void Add(string value, string barcodeType, string category)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        //Console.WriteLine($"Adding barcode to Results: {value}");

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
    /// Clears <see cref="Results"/> and the in-memory duplicate filter.
    /// </summary>
    public void Clear()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Results.Clear();
            _seen.Clear();
        });
    }

    #endregion

    #region Decoding (image -> barcodes)

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Uses SkiaSharp to resize and convert to grayscale, then decodes with ZXing’s
    /// <see cref="BarcodeReader"/> configured with <see cref="DecodingOptions.TryHarder"/> and a
    /// common set of formats (Code 128, QR, Aztec, Data Matrix, PDF417).
    /// </para>
    /// <para>
    /// The method is CPU-bound and runs on the calling thread. Consider running it on a background
    /// thread if you invoke it from the UI during large batch processing.
    /// </para>
    /// </remarks>
    public List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode)
    {
        var results = new List<ScanBarcodeItemViewModel>();
        if (imageBytes is null || imageBytes.Length == 0)
            return results;

        using var stream = new MemoryStream(imageBytes);
        using var originalBitmap = SKBitmap.Decode(stream);
        if (originalBitmap == null)
            return results;

        // Resize to a reasonable working size to speed up decoding (tweak as needed).
        var resizedBitmap = originalBitmap.Resize(new SKImageInfo(800, 600), SKFilterQuality.Medium);
        if (resizedBitmap == null)
            return results;

        // Grayscale conversion onto a new surface.
        using var surface = SKSurface.Create(new SKImageInfo(resizedBitmap.Width, resizedBitmap.Height));
        var canvas = surface.Canvas;
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.3f,   0.3f,   0.3f,   0,  0,
                0.59f,  0.59f,  0.59f,  0,  0,
                0.11f,  0.11f,  0.11f,  0,  0,
                0,      0,      0,      1,  0
            })
        };
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(resizedBitmap, 0, 0, paint);
        canvas.Flush();

        using var image = surface.Snapshot();
        using var pixmap = image.PeekPixels();

        // Convert snapshot to a bitmap that ZXing can work with.
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
                    BarcodeMode.Unique   => UniqueBarcodeClassifier.Classify(r.Text),
                    _                    => "Unknown"
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

    #endregion
}