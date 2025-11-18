using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Arista_ZebraTablet.Shared.Pages;

/// <summary>
/// Code-behind for the <c>Reorder</c> page (route <c>/reorder</c>).
/// Handles initializing the reorderable barcode list, drag-and-drop reordering,
/// clipboard copying, and navigation.
/// </summary>
/// <remarks>
/// The page supports reordering barcodes from both sources:
/// Image Upload Detection and Scanner Results.
/// The markup declares a single <see cref="MudDropZone{T}"/> with identifier <c>"1"</c>.
/// Items are maintained in <see cref="reorderableBarcodeItems"/> and updated in-place
/// upon each drop event so the current order can be copied directly.
/// </remarks>
public partial class Reorder : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Provides access to barcode groups from both sources (Image Upload and Scanner)
    /// and communicates the reorder scope using <see cref="IBarcodeDetectorService.SelectedBarcodeGroupId"/>.
    /// </summary>
    [Inject] public IBarcodeDetectorService Detector { get; set; } = default!;

    /// <summary>
    /// Provides navigation helpers for moving between pages.
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Provides clipboard interop and other JS functionality.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region Constants & state & models

    /// <summary>
    /// Reference to the primary drop zone so the code-behind can
    /// optionally read its <c>Items</c> collection if needed.
    /// </summary>
    private MudDropZone<DropBarcodeItem>? dropZone;

    /// <summary>
    /// The in-memory list bound to the drag-and-drop UI.
    /// This list is updated in-place on every drop.
    /// </summary>
    private List<DropBarcodeItem> reorderableBarcodeItems = new();

    #endregion

    #region Lifecycle

    /// <summary>
    /// Builds the reorderable list based on <see cref="IBarcodeDetectorService.SelectedBarcodeGroupId"/>.
    /// If <see cref="Guid.Empty"/> is selected, aggregates barcodes from all groups of the selected source.
    /// Otherwise, loads barcodes from the chosen group only.
    /// </summary>
    protected override void OnInitialized()
    {
        if (Detector.BarcodeGroups == null || Detector.BarcodeGroups.Count == 0)
            return; // Nothing to reorder

        if (Detector.SelectedBarcodeGroupId == Guid.Empty)
        {
            // Reorder all barcodes from all barcode groups of specified barcode source.
            reorderableBarcodeItems = Detector.BarcodeGroups
                .Where(g => g.Source == Detector.SelectedBarcodeSource && g.Barcodes.Count > 0)
                .SelectMany(g => g.Barcodes)
                .Select(b => new DropBarcodeItem
                {
                    Name = $"{b.Value}",
                    Selector = "1",
                    ScanBarcodeItem = b
                })
                .ToList();
        }
        else
        {
            // Reorder barcodes only from the selected barcode group.
            var selectedBarcodeGroupItem = Detector.BarcodeGroups.FirstOrDefault(g => g.Id == Detector.SelectedBarcodeGroupId);

            if (selectedBarcodeGroupItem?.Barcodes.Count > 0)
            {
                reorderableBarcodeItems = selectedBarcodeGroupItem.Barcodes
                    .Select(b => new DropBarcodeItem
                    {
                        Name = $"{b.Value}",
                        Selector = "1",
                        ScanBarcodeItem = b
                    })
                    .ToList();
            }
        }
    }

    #endregion

    #region UI actions & helpers

    /// <summary>
    /// Copies the provided content to the clipboard.
    /// </summary>
    /// <param name="content">
    /// Currently supports a list of <see cref="DropBarcodeItem"/> representing the
    /// current order from the UI.
    /// </param>
    /// <remarks>
    /// Call with <c>reorderableBarcodeItems</c> to copy in the latest order after drag-and-drop.
    /// </remarks>
    private async Task CopyToClipboard(object content)
    {
        string textToCopy;

        switch (content)
        {
            case List<DropBarcodeItem> items when items.Count > 0:
                var lines = items.Select(i => i.Name).ToList();
                textToCopy = string.Join("\n", lines);
                break;

            default:
                Snackbar.Add("Nothing to copy.", Severity.Warning);
                return;
        }

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", textToCopy);
            Snackbar.Add("Copied to clipboard.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to copy: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Navigates back to the home page, abandoning any in-memory ordering changes.
    /// </summary>
    private void CancelReorder()
    {
        NavigationManager.NavigateTo("/");
    }

    #endregion

    #region Drag-and-drop handlers

    /// <summary>
    /// Reorders <see cref="reorderableBarcodeItems"/> when an item is dropped within the zone.
    /// </summary>
    /// <param name="dropItem">Information about the dropped item and target location.</param>
    /// <remarks>
    /// <para>
    /// MudBlazor does not automatically re-order the backing list. We remove the item from its
    /// current index and insert it at the target index.
    /// </para>
    /// </remarks>
    private void ItemUpdated(MudItemDropInfo<DropBarcodeItem> dropItem)
    {
        // Keep zone selection current
        dropItem.Item.Selector = dropItem.DropzoneIdentifier;

        var item = dropItem.Item;
        if (item is null) return;

        // Current position in the backing list.
        var currentIndex = reorderableBarcodeItems.IndexOf(item);
        if (currentIndex < 0) return;

        // Target position within this zone.
        var newIndex = dropItem.IndexInZone;

        if (currentIndex != newIndex)
        {
            // Remove from old position
            reorderableBarcodeItems.RemoveAt(currentIndex);

            // Insert at new position
            reorderableBarcodeItems.Insert(newIndex, item);
        }
        StateHasChanged();
    }

    #endregion

    #region Nested types

    /// <summary>
    /// Represents a draggable item in the reorder list.
    /// </summary>
    public class DropBarcodeItem
    {
        /// <summary>
        /// Display text for the list item (e.g., "Category: Value" or just the value).
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Identifies which drop zone the item belongs to.
        /// Defaults to <c>"1"</c> for the single-zone layout.
        /// </summary>
        public string Selector { get; set; } = "1";

        /// <summary>
        /// The underlying scanned barcode data bound to the UI.
        /// </summary>
        public ScanBarcodeItemViewModel? ScanBarcodeItem { get; init; }
    }

    #endregion
}