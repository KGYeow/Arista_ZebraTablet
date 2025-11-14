using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Shared.Components;

public partial class BarcodeGroupList : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Service that decodes barcodes from image bytes and can navigate to the native scanner.
    /// </summary>
    [Inject] public IBarcodeDetectorService Detector { get; set; } = default!;

    /// <summary>
    /// Service responsible for uploading scanned barcode data to the backend.
    /// </summary>
    [Inject] public IScannedBarcodeService ScannedBarcodeService { get; set; } = default!;

    /// <summary>
    /// Service responsible for copy result to clipboard.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    #endregion

    [Parameter] public List<BarcodeGroupItemViewModel> BarcodeGroups { get; set; } = new();
    [Parameter] public BarcodeSource Source { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool IsUploadingImg { get; set; }
    [Parameter] public bool IsUploadingResults { get; set; }

    #region Constants & state

    private List<BarcodeGroupItemViewModel> FilteredBarcodeGroups => BarcodeGroups.Where(x => x.Source == Source).ToList();

    #endregion

    #region UI actions & helpers

    /// <summary>
    /// Maps a file processing state to an appropriate MudBlazor color.
    /// </summary>
    /// <returns>MudBlazor <see cref="Color"/> indicating status.</returns>
    private static Color ImgFileStateColor(FileState state) => state switch
    {
        FileState.Ready => Color.Info,
        FileState.Detecting => Color.Warning,
        FileState.Done => Color.Success,
        FileState.Error => Color.Error,
        _ => Color.Default
    };

    /// <summary>
    /// Friendly label for a given file processing state.
    /// </summary>
    /// <returns>Human-readable label for the state.</returns>
    private static string ImgFileStateLabel(FileState state) => state switch
    {
        FileState.Ready => "Ready",
        FileState.Detecting => "Detecting...",
        FileState.Done => "Done",
        FileState.Error => "Error",
        _ => "—"
    };

    /// <summary>
    /// Copies either a single barcode value, all detected barcodes from an image,
    /// or all reordered barcode results (from drag-and-drop UI).
    /// </summary>
    private async Task CopyToClipboard(object content)
    {
        string textToCopy;

        switch (content)
        {
            // Single barcode value (e.g., from individual result row)
            case string singleText:
                textToCopy = singleText;
                break;

            // All barcodes from a single barcode group (e.g., from HomePage card)
            case BarcodeGroupItemViewModel barcodeGrp when barcodeGrp?.Barcodes?.Count > 0:
                var lines = barcodeGrp.Barcodes
                    .Select(b => $"{b.Value}") // You can also include category: $"{b.Category}: {b.Value}"
                    .ToList();
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
    /// Enables reorder mode and navigates to the reorder page.
    /// If <paramref name="imgId"/> is provided, reorders only that image; otherwise reorders all.
    /// </summary>
    /// <param name="imgId">
    /// The image ID to reorder. If <see langword="null"/>, the method enables “reorder all”.
    /// </param>
    private void EnableReorderMode(Guid? barcodeGroupId = null)
    {
        // Guid.Empty indicates “reorder all” to the downstream service
        Detector.SelectedBarcodeSource = Source;
        Detector.SelectedBarcodeGroupId = barcodeGroupId ?? Guid.Empty;
        Detector.BarcodeGroups = BarcodeGroups;
        NavigationManager.NavigateTo("/reorder");
    }

    #endregion

    #region Confirmation / dialogs

    /// <summary>
    /// Opens a full-screen dialog to preview images, starting at a specific image ID if found.
    /// </summary>
    private async Task OpenPreviewImgDialogAsync(Guid barcodeGroupId)
    {
        if (FilteredBarcodeGroups == null || FilteredBarcodeGroups.Count == 0)
        {
            Snackbar.Add("No images to preview.", Severity.Warning);
            return;
        }

        // Resolve starting index; default to 0 if the id is not found
        var index = FilteredBarcodeGroups.FindIndex(x => x.Id == barcodeGroupId);
        if (index < 0) index = 0;

        var parameters = new DialogParameters<PreviewImgDialog>
        {
            { x => x.ImageFiles, FilteredBarcodeGroups },        // pass the SAME list instance
            { x => x.SelectedFileIndex, index },            // start at resolved index
        };
        var options = new DialogOptions() { NoHeader = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.Large, FullWidth = true };

        try
        {
            var dialog = await DialogService.ShowAsync<PreviewImgDialog>("Preview Image", parameters, options);
            var result = await dialog.Result;
            return;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to open image preview: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Displays a confirmation dialog asking the user to confirm deleting the
    /// current uploaded image.
    /// </summary>
    /// <remarks>Action is disabled while uploading/decoding to prevent race conditions.</remarks>
    private async Task DeleteBarcodeGroupConfirmationAsync(BarcodeGroupItemViewModel group)
    {
        if (IsUploadingImg || IsUploadingResults || IsBusy)
            return;

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this uploaded image? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Delete" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            BarcodeGroups.Remove(group);
        }
    }

    /// <summary>
    /// Opens a dialog to edit the category of a detected barcode item.
    /// </summary>
    private async Task OpenCategoryEditDialogAsync(ScanBarcodeItemViewModel barcodeItem)
    {
        var parameters = new DialogParameters<CategoryEditDialog>
        {
            { x => x.BarcodeItem, barcodeItem },
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<CategoryEditDialog>("Edit Category", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            StateHasChanged();
            Snackbar.Add("Category updated.", Severity.Success);
        }
    }

    /// <summary>
    /// Displays a confirmation dialog asking the user to confirm deleting the
    /// current detected barcode result.
    /// </summary>
    private async Task DeleteDetectedBarcodeConfirmationAsync(BarcodeGroupItemViewModel barcodeGroupItem, ScanBarcodeItemViewModel barcodeItem)
    {
        if (IsUploadingImg || IsUploadingResults || IsBusy)
            return;

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this detected barcode? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Delete" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            if (barcodeGroupItem.Barcodes.Contains(barcodeItem))
                barcodeGroupItem.Barcodes.Remove(barcodeItem);

            StateHasChanged();
        }
    }

    #endregion
}