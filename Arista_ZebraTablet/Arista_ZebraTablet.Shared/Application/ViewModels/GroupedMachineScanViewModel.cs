namespace Arista_ZebraTablet.Shared.Application.ViewModels
{
    public class GroupedMachineScanViewModel
    {
        public Guid GroupId { get; set; } = Guid.NewGuid();

        // Holds barcodes by category (ASY, Serial Number, MAC Address, Deviation, PCA)
        public Dictionary<string, ScanBarcodeItemViewModel> BarcodesByCategory { get; set; } = new();
        public DetectResultViewModel DetectResult { get; set; } = null!;

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        // Optional: For display purposes
        public string DisplayName => $"Machine {GroupId.ToString().Substring(0, 8)}";
    }
}