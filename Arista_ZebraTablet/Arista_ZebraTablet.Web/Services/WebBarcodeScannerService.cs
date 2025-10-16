using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Web.Services
{
    public sealed class WebBarcodeScannerService : IBarcodeScannerService
    {
        public Task<string?> ScanAsync(CancellationToken ct = default)
            => Task.FromResult<string?>(null); // or throw new NotSupportedException(...)
    }
}
