using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;
using System.Collections.ObjectModel;

namespace Arista_ZebraTablet.Shared.Services
{
    public class ScanResultsService : BaseService
    {
        public ObservableCollection<ScanBarcodeItemViewModel> Results { get; } = new();

        // Optional session-level de-dup
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        public ScanResultsService(ApplicationDbContext context) : base(context)
        {
        }

        public void Add(string value, string format)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            // Always mutate ObservableCollection on the UI thread in MAUI
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_seen.Add(value))
                    Results.Insert(0, new ScanBarcodeItemViewModel()
                    {
                        Value = value,
                        Format = format,
                        Time = DateTimeOffset.Now
                    });
            });
        }

        public void Clear()
        {
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                Results.Clear();
                _seen.Clear();
            });
        }
    }
}