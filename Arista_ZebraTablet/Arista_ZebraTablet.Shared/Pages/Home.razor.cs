using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Components;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Shared.Pages;

/// <summary>
/// Code-behind for the Home component (route "/").
/// Handles image upload, barcode detection, and result upload flows.
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
    /// Service responsible for uploading scanned barcode data to the backend.
    /// </summary>
    [Inject] public IScannedBarcodeService ScannedBarcodeService { get; set; } = default!;

    /// <summary>
    /// Service responsible for copy result to clipboard.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

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
    private static readonly string[] allowedContentTypes = { "image/jpeg", "image/png", "image/heic", "image/heif" };

    /// <summary>
    /// In-memory list of uploaded image files bound to the UI.
    /// </summary>
    private List<ImgItemViewModel> uploadedImgFiles { get; set; } = new();

    /// <summary>
    /// Tracks which image IDs currently have their detected-barcode results expanded in the UI.
    /// </summary>
    private HashSet<Guid> displayedDetectedResults { get; set; } = new();

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
    private void ResetSwipeArea()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Current device form factor ("Web", "Android", "iOS", etc.) as provided by <see cref="IFormFactorService"/>.
    /// </summary>
    private string factor => FormFactor.GetFormFactor();

    /// <summary>
    /// Current platform string ("Browser", "MAUI", etc.) as provided by <see cref="IFormFactorService"/>.
    /// </summary>
    private string platform => FormFactor.GetPlatform();

    /// <summary>
    /// Reorderable list of detected barcode items for drag-and-drop UI.
    /// </summary>
    private List<DropItem> _reorderableItems = new();

    /// <summary>
    /// The selected image ID for reordering barcodes.
    /// </summary>
    public Guid? SelectedImageId { get; set; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Restores the uploaded image list from <see cref="Detector.UploadedImages"/> if available.
    /// </summary>
    /// <remarks>
    /// Ensures continuity when navigating back from other pages (e.g., reorder screen),
    /// so the UI reflects the previously uploaded images without requiring re-upload.
    /// </remarks>
    protected override void OnInitialized()
    {
        if (Detector.UploadedImages.Any())
        {
            uploadedImgFiles = Detector.UploadedImages; // Restore updated list
        }
    }

    #endregion

    #region UI actions & helpers

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
    private async Task OpenBarcodeScanner() => await Detector.NavigateToScannerAsync(barcodeMode);

    /// <summary>
    /// Navigates to the native/hybrid zebra tablet barcode scanner screen for the current <see cref="barcodeMode"/>.
    /// </summary>
    //private async Task OpenZebraScanner() => await Detector.NavigateToZebraScannerAsync();
    //private async void OpenZebraScanner() => await Detector.NavigateToZebraScannerAsync();

    /// <summary>
    /// Enables reorder mode and navigates to the reorder page.
    /// If <paramref name="imgId"/> is provided, reorders only that image; otherwise reorders all.
    /// </summary>
    /// <param name="imgId">
    /// The image ID to reorder. If <see langword="null"/>, the method enables “reorder all”.
    /// </param>
    private void EnableReorderMode(Guid? imgId = null)
    {
        // Guid.Empty indicates “reorder all” to the downstream service
        Detector.SelectedImageId = imgId ?? Guid.Empty;
        Detector.UploadedImages = uploadedImgFiles;
        NavigationManager.NavigateTo("/reorder");
    }

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
    /// Determines whether the detected barcodes table should be shown for a given image.
    /// </summary>
    /// <returns><c>true</c> if expanded; otherwise <c>false</c>.</returns>
    private bool IsDetectedBarcodesDisplayed(Guid imgId) => displayedDetectedResults.Contains(imgId);

    /// <summary>
    /// Collapses all detected barcode panels in the UI.
    /// </summary>
    private void CloseAllDetectedBarcodes() => displayedDetectedResults.Clear();

    /// <summary>
    /// Toggles the expanded/collapsed state of the detected barcode panel for a specific image.
    /// </summary>
    private void ToggleDisplayDetectedBarcodes(Guid imgId)
    {
        // If the ID already exists, remove it (collapse). Otherwise, add (expand).
        if (!displayedDetectedResults.Add(imgId)) displayedDetectedResults.Remove(imgId);
    }

    /// <summary>
    /// Expands detected barcode panels for all images that have at least one detected barcode.
    /// </summary>
    /// <remarks>Useful for a "Expand all" toolbar action.</remarks>
    private void OpenAllDetectedBarcodes()
    {
        displayedDetectedResults = uploadedImgFiles
            .Where(f => f.DetectResult?.Barcodes?.Any() == true)
            .Select(f => f.Id)
            .ToHashSet();
    }

    /// <summary>
    /// Copies either a single barcode value, all detected barcodes from an image,
    /// or all reordered barcode results (from drag-and-drop UI).
    /// </summary>
    private async Task CopyToClipboard(object content)
    {
        string textToCopy;

        switch (content)
        {
            // ✅ Single barcode value (e.g., from individual result row)
            case string singleText:
                textToCopy = singleText;
                break;

            // ✅ All barcodes from a single image (e.g., from HomePage card)
            case ImgItemViewModel img when img?.DetectResult?.Barcodes?.Count > 0:
                var lines = img.DetectResult.Barcodes
                    .Select(b => $"{b.Value}") // You can also include category: $"{b.Category}: {b.Value}"
                    .ToList();
                textToCopy = string.Join("\n", lines);
                break;

            // ✅ All reordered barcode results (e.g., from drag-and-drop UI)
            case List<DropItem> dropItems when dropItems.Count > 0:
                var reorderedLines = dropItems.Select(item => item.Name).ToList();
                textToCopy = string.Join("\n", reorderedLines);
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
    /// Copies all results from all uploaded images to the clipboard.
    /// </summary>
    private async Task CopyAllResults()
    {
        var allBarcodes = uploadedImgFiles
            .Where(img => img.DetectResult?.Barcodes?.Count > 0)
            .SelectMany(img => img.DetectResult.Barcodes)
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
    /// Converts the content into a base64 data URL for thumbnail preview.
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
            var preview = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

            uploadedImgFiles.Add(new ImgItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = string.IsNullOrWhiteSpace(file.Name) ? $"{Guid.NewGuid():N}.jpg" : file.Name,
                ContentType = contentType,
                Bytes = bytes,
                PreviewDataUrl = preview,
                State = FileState.Ready
            });
        }
        catch (Exception ex)
        {
            uploadedImgFiles.Add(new ImgItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = file.Name ?? "image",
                ContentType = contentType,
                State = FileState.Error,
                ErrorMessage = ex.Message
            });
        }
        StateHasChanged();
    }

    #endregion

    #region Confirmation / dialogs

    /// <summary>
    /// Displays a confirmation dialog asking the user to confirm deleting the
    /// current uploaded image.
    /// </summary>
    /// <remarks>Action is disabled while uploading/decoding to prevent race conditions.</remarks>
    private async Task DeleteUploadedImgConfirmationAsync(ImgItemViewModel imgItem)
    {
        if (isUploadingImg || isUploadingResults || isBusy)
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
            uploadedImgFiles.Remove(imgItem);
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
    private async Task DeleteDetectedBarcodeConfirmationAsync(ImgItemViewModel imgItem, ScanBarcodeItemViewModel barcodeItem)
    {
        if (isUploadingImg || isUploadingResults || isBusy)
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
            if (imgItem.DetectResult.Barcodes.Contains(barcodeItem))
                imgItem.DetectResult.Barcodes.Remove(barcodeItem);


            // Remove from reorderable list
            var dropItem = _reorderableItems.FirstOrDefault(x => x.Original == barcodeItem);
            if (dropItem != null)
                _reorderableItems.Remove(dropItem);
            StateHasChanged();
        }
    }

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
            uploadedImgFiles.Clear();
            isBusy = false;
            decodeProgress = 0;
        }
    }

    /// <summary>
    /// Opens a full-screen dialog to preview images, starting at a specific image ID if found.
    /// </summary>
    private async Task OpenPreviewImgDialogAsync(Guid imgFileId)
    {
        if (uploadedImgFiles == null || uploadedImgFiles.Count == 0)
        {
            Snackbar.Add("No images to preview.", Severity.Warning);
            return;
        }

        // Resolve starting index; default to 0 if the id is not found
        var index = uploadedImgFiles.FindIndex(x => x.Id == imgFileId);
        if (index < 0) index = 0;

        var parameters = new DialogParameters<PreviewImgDialog>
        {
            { x => x.ImageFiles, uploadedImgFiles },        // pass the SAME list instance
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
    /// Displays a confirmation dialog asking the user to confirm uploading all the
    /// detected barcodes.
    /// </summary>
    private async Task SaveDetectedBarcodesConfirmationAsync()
    {
        var totalBarcodes = uploadedImgFiles
            .Where(f => f.DetectResult?.Barcodes?.Any() ?? false)
            .Sum(f => f.DetectResult.Barcodes.Count);

        var message = totalBarcodes == 1
            ? "Are you sure you want to upload this detected barcode?"
            : $"Are you sure you want to upload these {totalBarcodes} detected barcodes?";

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, message },
            { x => x.SubmitBtnText, "Upload" },
            { x => x.DialogIconColor, Color.Info }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Upload Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await SaveDetectedBarcodesAsync();
            }
            catch
            {
                Snackbar.Add("An unexpected error occurred while uploading the detected barcodes. Please try again.", Severity.Error);
            }
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
        if (!uploadedImgFiles.Any() || isBusy)
            return;

        isBusy = true;
        decodeProgress = 5;

        try
        {
            int index = 0;
            foreach (var item in uploadedImgFiles)
            {
                if (item.Bytes is null)
                {
                    item.State = FileState.Error;
                    continue;
                }

                item.State = FileState.Detecting;
                StateHasChanged();

                // Optional short delay for visible UI progress.
                await Task.Delay(100);

                // Use the decoder service to extract barcode values.
                var results = Detector.DecodeFromImage(item.Bytes, barcodeMode);


                // Sorts the detected barcode results based on a predefined category order.Categories are matched against the PreferredCategoryOrder list.
                // If a category is not found in the list, it is placed at the end.Results with the same category are further sorted by scan time.
                item.DetectResult = new DetectResultViewModel
                {
                    Barcodes = results
                        .OrderBy(b => PreferredCategoryOrder.IndexOf(b.Category) >= 0
                        ? PreferredCategoryOrder.IndexOf(b.Category)
                        : int.MaxValue)
                        .ThenBy(b => b.ScannedTime) // optional: secondary sort
                        .ToList()
                };
                _reorderableItems = results
                    .OrderBy(b => PreferredCategoryOrder.IndexOf(b.Category) >= 0
                        ? PreferredCategoryOrder.IndexOf(b.Category)
                        : int.MaxValue)
                    .ThenBy(b => b.ScannedTime)
                    .Select(b => new DropItem
                    {
                        Name = $"{b.Category}: {b.Value}",
                        Selector = "1",
                        Original = b
                    })
                    .ToList();
                item.State = FileState.Done;

                // Auto-expand results the first time we have something to show.
                if (!IsDetectedBarcodesDisplayed(item.Id) && item.DetectResult.Barcodes.Count > 0)
                    ToggleDisplayDetectedBarcodes(item.Id);

                index++;
                decodeProgress = Math.Clamp(5 + (int)((double)index / uploadedImgFiles.Count * 95), 5, 100);
            }

            decodeProgress = 100;
        }
        catch (Exception ex)
        {
            foreach (var item in uploadedImgFiles)
            {
                item.State = FileState.Error;
                item.ErrorMessage ??= ex.Message;
            }
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Uploads the current detected barcode results to the backend via <see cref="IScannedBarcodeService"/>.
    /// </summary>
    /// <remarks>
    /// Deduplication is handled by the service layer. Uses a short timeout to keep UX snappy on mobile.
    /// Clears <see cref="uploadedImgFiles"/> on success.
    /// </remarks>
    private async Task SaveDetectedBarcodesAsync()
    {
        if (isUploadingImg || isUploadingResults || isBusy)
            return;

        var barcodeItems = uploadedImgFiles
            .Where(f => f.DetectResult?.Barcodes?.Any() ?? false)
            .SelectMany(f => f.DetectResult.Barcodes)
            .ToList();

        if (barcodeItems.Count == 0)
        {
            Snackbar.Add("No detected barcodes to upload.", Severity.Normal);
            return;
        }

        isUploadingResults = true;
        try
        {
            // Short timeout to keep UX snappy on mobile
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var response = await ScannedBarcodeService.AddScannedBarcodesAsync(barcodeItems, cts.Token);
            if (response.Success)
            {
                Snackbar.Add(response.Message ?? $"{response.Data} barcode(s) uploaded.", Severity.Success);
                uploadedImgFiles.Clear();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Upload failed.", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            Snackbar.Add("Upload cancelled or timed out.", Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Upload error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isUploadingResults = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Defines the desired order for barcode categories.
    /// Used for sorting both display and copy operations.
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

    /// <summary>
    /// Represents an item in the drag-and-drop reorderable list.
    /// </summary> 

    public class DropItem
    {
        public string Name { get; set; } = string.Empty; // Display text: "Category: Value"
        public string Selector { get; set; } = "1";      // Optional: grouping zone
        public ScanBarcodeItemViewModel? Original { get; set; } // Reference to original data
    }
    private void OnItemDropped(MudItemDropInfo<DropItem> dropItem)
    {
        dropItem.Item.Selector = dropItem.DropzoneIdentifier;

        // ✅ Rebuild _reorderableItems based on current order in MudDropContainer
        _reorderableItems = _reorderableItems
            .OrderBy(x => x.Selector) // If you have multiple zones
            .ToList();

        StateHasChanged();
    }

    #endregion
}