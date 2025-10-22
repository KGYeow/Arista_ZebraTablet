namespace Arista_ZebraTablet.Shared.Data;

public class ScannedBarcode
{
    public int Id { get; set; }
    public string Value { get; set; } = null!;
    public string Format { get; set; } = null!;
    public string Category { get; set; } = null!;
    public DateTime ScannedTime { get; set; } = DateTime.Now;
}