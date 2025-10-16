using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Services
{
    public sealed class MauiBarcodeScannerService : IBarcodeScannerService
    {
        public async Task<string?> ScanAsync(CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<string?>();
            var page = new ScanPage(tcs);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var navPage = Application.Current?.MainPage;
                if (navPage is null) { tcs.TrySetResult(null); return; }
                await navPage.Navigation.PushModalAsync(page, animated: true);
            });

            using (ct.Register(() => tcs.TrySetCanceled()))
                return await tcs.Task.ConfigureAwait(false);
        }
    }
}
