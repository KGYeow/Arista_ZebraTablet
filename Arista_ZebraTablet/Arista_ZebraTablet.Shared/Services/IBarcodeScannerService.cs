namespace Arista_ZebraTablet.Shared.Services;

public interface IBarcodeScannerService
{
    Task NavigateToScannerAsync();

    Task<string?> ScanAsync(CancellationToken ct = default);
}
