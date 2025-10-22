using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Web.Services
{
    public sealed class BarcodeScannerService : IBarcodeScannerService
    {
        private readonly IServiceProvider _sp;
        public BarcodeScannerService(IServiceProvider sp) => _sp = sp;

        /// <summary>
        /// No-op on web: does not navigate to any scanner UI.
        /// </summary>
        public Task NavigateToScannerAsync() => Task.CompletedTask;

        /// <summary>
        /// No-op on web: returns <c>null</c> immediately.
        /// </summary>
        /// <param name="ct">Cancellation token (ignored).</param>
        /// <returns><c>null</c> to indicate no barcode value is produced.</returns>
        public Task<string?> ScanAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    }
}
