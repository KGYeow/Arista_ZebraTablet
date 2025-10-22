using Arista_ZebraTablet.Shared.Services;

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
                    var page = _sp.GetRequiredService<BarcodeScannerPage>();
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
                    var results = _sp.GetRequiredService<ScanResultsService>();
                    var page = new BarcodeScannerPage(tcs, results); // single-shot mode ctor
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
    }
}
