using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

public partial class ImgItemViewModel
{
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? Bytes { get; set; }
    public string? PreviewDataUrl { get; set; }
    public FileState State { get; set; }
    public string? ErrorMessage { get; set; }

    public DetectResultViewModel DetectResult { get; set; } = null!;
}