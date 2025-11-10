using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services
{
    public static class ReorderCache
    {
        public static Dictionary<Guid, List<ScanBarcodeItemViewModel>> UpdatedResults { get; } = new();
    }
}