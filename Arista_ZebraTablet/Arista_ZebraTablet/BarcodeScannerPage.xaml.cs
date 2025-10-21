using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;


namespace Arista_ZebraTablet;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly ScanResultsService _results;
    private readonly TaskCompletionSource<string?>? _singleShotTcs; // null => list mode

    // Hybrid list mode
    public BarcodeScannerPage(ScanResultsService results)
    {
        InitializeComponent();
        _results = results;
    }  

    // Single-shot mode (your original pattern)
    public BarcodeScannerPage(TaskCompletionSource<string?> singleShotTcs, ScanResultsService results)
    {
        InitializeComponent();
        _singleShotTcs = singleShotTcs;
        _results = results;
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

            // Add to ScanResultsService
            _results.Add(r.Value, r.Format.ToString(), category);
        }
    }

    private void OnToggleDetecting(object sender, EventArgs e)
        => CameraView.IsDetecting = !CameraView.IsDetecting;

    private void OnClear(object sender, EventArgs e)
        => _results.Clear();
}