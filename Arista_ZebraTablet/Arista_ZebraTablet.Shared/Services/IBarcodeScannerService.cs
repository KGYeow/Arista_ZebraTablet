namespace Arista_ZebraTablet.Shared.Services
{
    public interface IBarcodeScannerService
    {
        Task<string?> ScanAsync(CancellationToken ct = default);
    }
}
