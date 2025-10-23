using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using System.Collections.ObjectModel;

namespace Arista_ZebraTablet.Services
{
    public sealed class BarcodeScannerService : IBarcodeScannerService
    {
        private readonly IServiceProvider _sp;
        public BarcodeScannerService(IServiceProvider sp) => _sp = sp;

        public async Task NavigateToScannerAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Create page via DI so it gets ScanResultsService injected
                    var page = new BarcodeScannerPage(this);
                    await Application.Current!.MainPage!.Navigation.PushModalAsync(page, animated: true);
                }
                catch (Exception ex)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Navigation error", ex.Message, "OK");
                }
            });
        }


        public async Task<string?> ScanAsync(CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<string?>();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = new BarcodeScannerPage(tcs, this); // Create manually
                    await Application.Current!.MainPage!.Navigation.PushModalAsync(page, animated: true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            using (ct.Register(() => tcs.TrySetCanceled()))
                return await tcs.Task.ConfigureAwait(false);
        }

        //Scan result service

        public ObservableCollection<ScanBarcodeItemViewModel> Results { get; } = new();
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string value, string BarcodeType, string category)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_seen.Add(value))
                    Results.Insert(0, new ScanBarcodeItemViewModel()
                    {
                        Value = value,
                        BarcodeType = BarcodeType,
                        Category = category,
                        ScannedTime = DateTime.Now
                    });
            });
        }

        public void Clear()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Results.Clear();
                _seen.Clear();
            });
        }
    }

}
