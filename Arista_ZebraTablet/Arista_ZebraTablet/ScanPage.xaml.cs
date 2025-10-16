using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Arista_ZebraTablet
{
    public partial class ScanPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public ScanPage(TaskCompletionSource<string?> tcs)
        {
            InitializeComponent();

            CameraView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };

            _tcs = tcs;
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

        private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            var value = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return;

            // Complete once and close the page
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!_tcs.Task.IsCompleted)
                    _tcs.TrySetResult(value);

                await Navigation.PopModalAsync();
            });
        }
    }
}