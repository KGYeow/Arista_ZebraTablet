using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.Regex;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using SkiaSharp;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Arista_ZebraTablet.Services;

/// <summary>
/// Provides barcode detection services for the application.
/// Handles navigation to the live scanner page, manages in-memory barcode groups,
/// and decodes barcodes from image bytes using ZXing and SkiaSharp.
/// </summary>
/// <remarks>
/// <para>
/// Consumers can subscribe to <see cref="ScanReceived"/> to react to new scan items as they arrive.
/// State sharing between pages is achieved through <see cref="BarcodeGroups"/> and
/// <see cref="SelectedBarcodeGroupId"/> for operations like reordering.
/// </para>
/// <para>
/// Navigation uses <see cref="MainThread"/> to ensure UI thread execution.
/// Decoding is CPU-bound and should be run on a background thread for large batches.
/// </para>
/// </remarks>
public sealed class BarcodeDetectorService : IBarcodeDetectorService
{
    #region Dependencies

    /// <summary>
    /// Provides access to the application's service provider for resolving dependencies
    /// and creating instances of pages or components via <see cref="ActivatorUtilities"/>.
    /// </summary>
    /// <remarks>
    /// This is primarily used for navigation scenarios where the scanner page requires
    /// dependency injection to construct its view model or services.
    /// </remarks>
    private readonly IServiceProvider _services;

    #endregion

    #region State & events

    /// <summary>
    /// Raised after a new scan result has been added to <see cref="CurrentGroup"/>.
    /// </summary>
    public event EventHandler<ScanBarcodeItemViewModel>? ScanReceived;

    /// <summary>
    /// Represents the currently active barcode group being populated during scanning.
    /// </summary>
    public BarcodeGroupItemViewModel CurrentGroup { get; set; } = new();

    /// <summary>
    /// Completes the current group and adds it to <see cref="BarcodeGroups"/>.
    /// Resets <see cref="CurrentGroup"/> for the next session.
    /// </summary>
    public void CompleteCurrentGroup()
    {
        BarcodeGroups.Add(CurrentGroup);
        CurrentGroup = new BarcodeGroupItemViewModel();
    }

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
    public List<BarcodeGroupItemViewModel> BarcodeGroups { get; set; } = new();

    /// <inheritdoc/>
    public Guid? SelectedBarcodeGroupId { get; set; }

    /// <inheritdoc/>
    public BarcodeSource SelectedBarcodeSource { get; set; }

    /// <summary>
    /// Raises the <see cref="ScanReceived"/> event for the specified barcode.
    /// </summary>
    /// <param name="barcode">The barcode item that was scanned.</param>
    public void RaiseScanReceived(ScanBarcodeItemViewModel barcode)
    {
        ScanReceived?.Invoke(this, barcode);
    }

    #endregion

    #region Navigation

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Navigation is executed on the UI thread using <see cref="MainThread"/>.
    /// The scanner page is pushed modally for isolation of scanning UX.
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