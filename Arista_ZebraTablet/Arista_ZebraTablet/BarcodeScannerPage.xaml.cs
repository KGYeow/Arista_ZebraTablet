using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;


namespace Arista_ZebraTablet;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly BarcodeScannerService _scannerService;
    private readonly TaskCompletionSource<string?>? _singleShotTcs; // null => list mode


    // Hybrid list mode
    public BarcodeScannerPage(BarcodeScannerService scannerService)
    {
        InitializeComponent();
        _scannerService = scannerService;
    }

    // Single-shot mode
    public BarcodeScannerPage(TaskCompletionSource<string?> singleShotTcs, BarcodeScannerService scannerService)
    {
        InitializeComponent();
        _singleShotTcs = singleShotTcs;
        _scannerService = scannerService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        CameraView.IsDetecting = false;
        base.OnDisappearing();
    }

    private async void CompleteSingleShotAsync(string value)
    {
        if (_singleShotTcs is null) return;
        if (!_singleShotTcs.Task.IsCompleted)
            _singleShotTcs.TrySetResult(value);

        // Close the modal page automatically (single-shot flow)
        await Navigation.PopModalAsync(animated: true);
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var list = e.Results ?? Enumerable.Empty<BarcodeResult>();
        foreach (var r in list)
        {
            if (string.IsNullOrWhiteSpace(r.Value))
                continue;

            var category = BarcodeClassifier.Classify(r.Value);

            if (_singleShotTcs is not null)
            {
                MainThread.BeginInvokeOnMainThread(() => CompleteSingleShotAsync($"{r.Value} | {category}"));
                return;
            }


            // Add to BarcodeScannerService
            _scannerService.Add(r.Value, r.Format.ToString(), category);

        }
    }

    private void OnToggleDetecting(object sender, EventArgs e)
        => CameraView.IsDetecting = !CameraView.IsDetecting;

    private void OnClear(object sender, EventArgs e)
        => _scannerService.Clear();

}