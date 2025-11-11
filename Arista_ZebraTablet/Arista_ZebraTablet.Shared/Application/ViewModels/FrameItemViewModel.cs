
namespace Arista_ZebraTablet.Shared.Application.ViewModels
{
    public class FrameItemViewModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CapturedTime { get; set; } = DateTime.Now;
        public string? PreviewDataUrl { get; set; } // Optional: snapshot of the frame
        public DetectResultViewModel DetectResult { get; set; } = new DetectResultViewModel();
    }
}