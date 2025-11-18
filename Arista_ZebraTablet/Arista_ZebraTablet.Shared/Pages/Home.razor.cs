using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using SkiaSharp;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Shared.Pages;

/// <summary>
/// Code-behind for the Home page component (route "/").
/// Provides the main interface for barcode operations, including image upload,
/// live scanner navigation, barcode detection, and result management.
/// </summary>
public partial class Home : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Provides device form factor and platform information (Web/Android/iOS, etc.).
    /// </summary>
    [Inject] public IFormFactorService FormFactor { get; set; } = default!;

    /// <summary>
    /// Service that decodes barcodes from image bytes and can navigate to the native scanner.
    /// </summary>
    [Inject] public IBarcodeDetectorService Detector { get; set; } = default!;

    /// <summary>
    /// Service responsible for copy result to clipboard.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Provides navigation capabilities within the Blazor application.
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    #endregion

    #region Constants & state

    /// <summary>
    /// Maximum per-file size in bytes for uploads (20 MB).
    /// Used to guard the stream opened by <see cref="IBrowserFile.OpenReadStream(long, long?)"/>.
    /// </summary>
    private const long maxFileBytes = 20L * 1024 * 1024;

    /// <summary>
    /// Whitelist of image MIME types accepted by the uploader.
    /// If a file's content type is empty, it is treated as <c>image/jpeg</c>.
    /// </summary>
    private static readonly string[] allowedContentTypes = { "image/jpeg", "image/png" };

    /// <summary>
    /// Collection of barcode groups representing detection results from multiple sources.
    /// Each group may originate from an image upload or a live scanner session.
    /// </summary>
    private List<BarcodeGroupItemViewModel> barcodeGroups { get; set; } = new();

    /// <summary>
    /// Current barcode source selected by the user (Upload or Camera).
    /// Determines which results are displayed in the UI.
    /// </summary>
    private BarcodeSource barcodeSource { get; set; } = BarcodeSource.Upload;

    /// <summary>
    /// Current barcode categorizing mode selected by the user (Standard vs Unique).
    /// </summary>
    private BarcodeMode barcodeMode { get; set; } = BarcodeMode.Standard;

    /// <summary>
    /// Current MudBlazor breakpoint, updated by <see cref="HandleBreakpointChanged(Breakpoint)"/>.
    /// </summary>
    private Breakpoint currentBreakpoint { get; set; } = Breakpoint.Xs;

    /// <summary>
    /// True while the app is decoding barcodes; disables actions and shows progress.
    /// </summary>
    private bool isBusy { get; set; }

    /// <summary>
    /// True while image files are being added; drives the upload progress UI and disables conflicting actions.
    /// </summary>
    private bool isUploadingImg { get; set; }

    /// <summary>
    /// True while detected barcodes are being uploaded to the backend service.
    /// </summary>
    private bool isUploadingResults { get; set; }

    /// <summary>
    /// Overall decode progress (0–100) across all uploaded images.
    /// </summary>
    private int decodeProgress { get; set; }

    /// <summary>
    /// Upload/add progress (0–100) while ingesting selected files into memory.
    /// </summary>
    private int uploadImgProgress { get; set; }

    /// <summary>
    /// Tracks whether the "More Options" drawer is currently open.
    /// Bound to the MudDrawer component in the Home page.
    /// </summary>
    private bool moreDrawerOpen { get; set; }

    /// <summary>
    /// Reference to the MudSwipeArea component used for detecting swipe gestures
    /// on the "More Options" drawer.
    /// </summary>
    private MudSwipeArea swipeArea = null!;

    /// <summary>
    /// Current device form factor ("Web", "Android", "iOS", etc.) as provided by <see cref="IFormFactorService"/>.
    /// </summary>
    private string factor => FormFactor.GetFormFactor();

    /// <summary>
    /// Current platform string ("Browser", "MAUI", etc.) as provided by <see cref="IFormFactorService"/>.
    /// </summary>
    private string platform => FormFactor.GetPlatform();

    /// <summary>
    /// The selected image ID for reordering barcodes.
    /// </summary>
    public Guid? SelectedImageId { get; set; }

    /// <summary>
    /// Reference to MudTabs for switching between sources.
    /// </summary>
    private MudTabs barcodeSoureTabs = null!;

    /// 
    /// <summary>
    /// Specifies the preferred sort order for barcode categories.
    /// Used to organize detected barcodes consistently in UI and copy operations.
    /// </summary>
    private static readonly List<string> PreferredCategoryOrder = new()
    {
        "ASY",
        "ASY-OTL",
        "Serial Number",
        "MAC Address",
        "Deviation",
        "PCA"
    };

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the Home component and restores previously detected barcode groups from the shared service.
    /// This ensures that when the user navigates back from other pages (such as the reorder screen),
    /// the UI reflects the latest state without requiring re-upload or re-scan.
    /// </summary>
    /// <remarks>
    /// This method runs during the component's initialization phase and checks <see cref="Detector.BarcodeGroups"/>.
    /// If any groups exist, they are assigned to the local <c>barcodeGroups</c> collection for display and further actions.
    /// </remarks>
    protected override void OnInitialized()
    {
        if (Detector.BarcodeGroups.Count > 0)
        {
            barcodeGroups = Detector.BarcodeGroups; // Restore updated list
        }
    }

    #endregion

    #region UI actions & helpers

    /// <summary>
    /// Changes the active tab panel to display results for the specified barcode source.
    /// Tabs represent two sources: Image Upload Detection and Scanner Results.
    /// </summary>
    /// <param name="source">The barcode source to activate (Upload or Scanner).</param>
    void ChangeBarcodeResultTabPanel(BarcodeSource source)
    {
        barcodeSource = source;
        barcodeSoureTabs.ActivatePanel(source.ToString());
    }

    /// <summary>
    /// Called by <c>MudBreakpointProvider</c> when the viewport breakpoint changes.
    /// Updates <see cref="currentBreakpoint"/> and requests a re-render.
    /// </summary>
    private Task HandleBreakpointChanged(Breakpoint bp)
    {
        currentBreakpoint = bp;

        // Re-render when breakpoint changes to ensure the responsive layout updates.
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Navigates to the native/hybrid barcode scanner screen for the current <see cref="barcodeMode"/>.
    /// </summary>
    private async Task OpenBarcodeScanner()
    {
        Detector.BarcodeGroups = barcodeGroups;
        await Detector.NavigateToScannerAsync(barcodeMode);
    }

    /// <summary>
    /// Enables reorder mode and navigates to the reorder page.
    /// If <paramref name="barcodeGroupId"/> is provided, reorders only that barcode group; otherwise reorders all.
    /// </summary>
    private void EnableReorderMode(Guid? barcodeGroupId = null)
    {
        // Guid.Empty indicates “reorder all” to the downstream service
        Detector.SelectedBarcodeSource = barcodeSource;
        Detector.SelectedBarcodeGroupId = barcodeGroupId ?? Guid.Empty;
        Detector.BarcodeGroups = barcodeGroups;
        NavigationManager.NavigateTo("/reorder");
    }

    /// <summary>
    /// Copies either a single barcode value, or all detected barcodes from a specific group.
    /// </summary>
    /// <param name="content">
    /// The content to copy: either a string (single barcode) or a barcode group.
    /// </param>
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
            case BarcodeGroupItemViewModel barcodeGroup when barcodeGroup.Barcodes.Count > 0:
                var lines = barcodeGroup.Barcodes
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
    /// Copies all detected results from all barcode groups to the clipboard.
    /// </summary>
    /// <remarks>
    /// Aggregates all detected barcode values from every barcode group associated with the currently active source.
    /// This method filters <see cref="barcodeGroups"/> by <see cref="barcodeSource"/> to ensure only results
    /// from the selected source tab are included.
    /// </remarks>
    private async Task CopyAllResults()
    {
        var allBarcodes = barcodeGroups
            .Where(g => g.Barcodes.Count > 0 && g.Source == barcodeSource)
            .SelectMany(g => g.Barcodes)
            .Select(b => $"{b.Value}")
            .ToList();

        if (allBarcodes.Count == 0)
        {
            Snackbar.Add("No barcode results to copy.", Severity.Warning);
            return;
        }

        await CopyToClipboard(string.Join("\n", allBarcodes));
    }

    /// <summary>
    /// Toggles the visibility of the "More Options" drawer.
    /// Called by the UI when the user clicks the floating action button.
    /// </summary>
    private void ToggleMoreDrawer() => moreDrawerOpen = !moreDrawerOpen;

    /// <summary>
    /// Handles swipe movement events from the MudSwipeArea.
    /// Closes the "More Options" drawer when a top-to-bottom swipe is detected.
    /// </summary>
    /// <param name="e">Event arguments containing swipe direction details.</param>
    public void HandleSwipeMove(MultiDimensionSwipeEventArgs e)
    {
        for (int i = 0; i < e.SwipeDirections.Count; i++)
        {
            if (e.SwipeDirections[i] == MudBlazor.SwipeDirection.TopToBottom && moreDrawerOpen)
            {
                moreDrawerOpen = false;
            }
        }
    }

    /// <summary>
    /// Called when the swipe gesture leaves the MudSwipeArea.
    /// Cancels any ongoing swipe and resets the UI state.
    /// </summary>
    private void OnSwipeLeave()
    {
        swipeArea.Cancel();
        ResetSwipeArea();
    }

    /// <summary>
    /// Requests a UI refresh after swipe-related state changes.
    /// </summary>
    private void ResetSwipeArea() => StateHasChanged();

    #endregion

    #region File upload

    /// <summary>
    /// Adds multiple image files provided by the file picker into memory and updates progress UI.
    /// </summary>
    /// <remarks>
    /// Files are validated for content type and size in <see cref="AddImageFileAsync(IBrowserFile)"/>.
    /// This method yields between items to keep the UI responsive.
    /// </remarks>
    private async Task UploadImageFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        if (files == null || files.Count == 0)
            return;

        isUploadingImg = true;
        uploadImgProgress = 0;

        var totalFiles = files.Count;
        double processedFile = 0;

        try
        {
            foreach (var file in files)
            {
                await AddImageFileAsync(file);

                processedFile++;
                uploadImgProgress = Math.Clamp(
                    (int)Math.Round(processedFile / totalFiles * 100.0),
                    0, 100);

                StateHasChanged();
                await Task.Yield(); // Let the UI update between files.
            }
            // Ensure the progress reaches 100% after the loop, even if rounding occurred.
            uploadImgProgress = 100;
        }
        finally
        {
            isUploadingImg = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Validates and loads a single image file into memory with a preview data URL.
    /// </summary>
    /// <remarks>
    /// Rejects files whose <c>ContentType</c> is not recognized as an image.
    /// </remarks>
    private async Task AddImageFileAsync(IBrowserFile file)
    {
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType.ToLowerInvariant();

        // Quick guard: only accept typical image content types (or anything starting with "image/").
        if (!allowedContentTypes.Contains(contentType) && !contentType.StartsWith("image/"))
            return;

        try
        {
            await using var readFile = file.OpenReadStream(maxFileBytes);
            using var ms = new MemoryStream();
            await readFile.CopyToAsync(ms);

            var bytes = ms.ToArray();

            // Full-size preview for modal
            var previewUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

            // Generate thumbnail using SkiaSharp
            using var originalBitmap = SKBitmap.Decode(bytes);
            var thumbnailBitmap = originalBitmap.Resize(new SKImageInfo(100, 100), SKFilterQuality.Medium);
            using var image = SKImage.FromBitmap(thumbnailBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);
            var thumbnailBytes = data.ToArray();
            var thumbnailUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(thumbnailBytes)}";

            var fileName = string.IsNullOrWhiteSpace(file.Name) ? $"{Guid.NewGuid():N}.jpg" : file.Name;

            barcodeGroups.Add(new BarcodeGroupItemViewModel
            {
                Id = Guid.NewGuid(),
                Name = fileName,
                Source = BarcodeSource.Upload,
                ImgItem = new ImgItemViewModel
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Bytes = bytes,
                    ThumbnailUrl = thumbnailUrl,
                    PreviewDataUrl = previewUrl,
                    State = FileState.Ready
                }
            });
        }
        catch (Exception ex)
        {
            barcodeGroups.Add(new BarcodeGroupItemViewModel
            {
                Id = Guid.NewGuid(),
                Name = file.Name ?? "image",
                Source = BarcodeSource.Upload,
                ErrorMessage = ex.Message,
                ImgItem = new ImgItemViewModel
                {
                    FileName = file.Name ?? "image",
                    ContentType = contentType,
                    State = FileState.Error
                }
            });
        }
        StateHasChanged();
    }

    #endregion

    #region Confirmation / dialogs

    /// <summary>
    /// Displays a confirmation dialog asking the user to confirm clearing all uploaded images in the list.
    /// </summary>
    private async Task ClearAllUploadedImagesConfirmationAsync()
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to clear all the uploaded images? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Clear" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Clear All Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            barcodeGroups.RemoveAll(g => g.Source == barcodeSource);
            isBusy = false;
            decodeProgress = 0;
        }
    }
    #endregion

    #region Barcode decoding & upload

    /// <summary>
    /// Runs barcode detection across all uploaded images using <see cref="IBarcodeDetectorService"/>.
    /// </summary>
    /// <remarks>
    /// Updates <see cref="decodeProgress"/> and per-item <see cref="FileState"/> to provide responsive UI feedback.
    /// Collapses/expands result sections automatically when results are first available.
    /// </remarks>
    private async Task DecodeBarcodes()
    {
        var uploadedImgFiles = barcodeGroups.Where(g => g.Source == BarcodeSource.Upload).ToList();
        if (uploadedImgFiles.Count == 0 || isBusy)
            return;

        isBusy = true;
        decodeProgress = 5;

        try
        {
            int index = 0;
            foreach (var item in uploadedImgFiles)
            {
                if (item.ImgItem.Bytes is null)
                {
                    item.ImgItem.State = FileState.Error;
                    continue;
                }  

                item.ImgItem.State = FileState.Detecting;
                StateHasChanged();

                // Optional short delay for visible UI progress.
                await Task.Delay(100);

                // Use the decoder service to extract barcode values.
                var results = Detector.DecodeFromImage(item.ImgItem.Bytes, barcodeMode);

                // Sorts the detected barcode results based on a predefined category order.Categories are matched against the PreferredCategoryOrder list.
                // If a category is not found in the list, it is placed at the end.Results with the same category are further sorted by scan time.
                item.Barcodes = results
                    .OrderBy(b => PreferredCategoryOrder.IndexOf(b.Category) >= 0 ? PreferredCategoryOrder.IndexOf(b.Category) : int.MaxValue)
                    .ThenBy(b => b.ScannedTime) // optional: secondary sort
                    .ToList();
                item.ImgItem.State = FileState.Done;

                index++;
                decodeProgress = Math.Clamp(5 + (int)((double)index / uploadedImgFiles.Count * 95), 5, 100);
            }

            decodeProgress = 100;
        }
        catch (Exception ex)
        {
            foreach (var item in uploadedImgFiles)
            {
                item.ImgItem.State = FileState.Error;
                item.ErrorMessage ??= ex.Message;
            }
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    #endregion
}