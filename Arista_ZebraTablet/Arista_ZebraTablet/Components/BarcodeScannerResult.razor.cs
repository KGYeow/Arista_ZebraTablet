using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Components;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.Specialized;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Components;

public partial class BarcodeScannerResult : ComponentBase, IDisposable
{
    #region Dependencies

    [Inject] private BarcodeDetectorService scanResultsService { get; set; } = null!;
    [Inject] private IBarcodeDetectorService Detector { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    #endregion

    #region Constants and State

    private NotifyCollectionChangedEventHandler? _collectionChangedSub;
    private bool isSaving { get; set; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the component and subscribes to events for UI updates.
    /// </summary>
    protected override void OnInitialized()
    {
        scanResultsService.ScanReceived += OnScanReceived;
        _collectionChangedSub = (_, __) => InvokeAsync(StateHasChanged);

    }

    /// <summary>
    /// Cleans up event subscriptions to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        scanResultsService.ScanReceived -= OnScanReceived;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles barcode scan events and updates UI.
    /// </summary>
    private void OnScanReceived(object? sender, ScanBarcodeItemViewModel scanned)
    {
        //scanResultsService.AddBarcodeToCurrentGroup(scanned);
        InvokeAsync(StateHasChanged);
    }
    #endregion

    #region Confirmation / dialogs
    /// <summary>
    /// Opens a dialog to edit the category of a scanned barcode.
    /// </summary>
    private async Task OpenCategoryEditDialogAsync(ScanBarcodeItemViewModel barcodeItem)
    {
        var parameters = new DialogParameters<CategoryEditDialog>
        {
            { x => x.BarcodeItem, barcodeItem }
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
    /// Confirms and deletes a barcode from the current group.
    /// </summary>
    private async Task DeleteScannedResultConfirmationAsync(ScanBarcodeItemViewModel barcodeItem)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this scanned result? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Delete" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };

        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {

            {
                scanResultsService.CurrentGroup.Barcodes.Remove(barcodeItem);
                InvokeAsync(StateHasChanged);
                Snackbar.Add($"Deleted {barcodeItem.Category} from current group.", Severity.Warning);
            }

        }
    }

    /// <summary>
    /// Confirms and deletes the current scanning barcode group.
    /// </summary>
    private async Task DeleteCurrentScanningBarcodeGroupConfirmationAsync()
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this scanned result? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Delete" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };

        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {      
            scanResultsService.CurrentGroup = new BarcodeGroupItemViewModel();
        }
    }

    /// <summary>
    /// Displays a confirmation dialog asking the user to confirm clearing all
    /// scanned barcode results currently shown in the list.
    /// </summary> 
    private async Task ClearAllScannedResultConfirmationAsync()
    {
        if (isSaving || (!scanResultsService.CurrentGroup.Barcodes.Any() && !scanResultsService.BarcodeGroups.Any()))
            return;

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to clear all grouped machine results? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Clear" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };

        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Clear All Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            // ✅ Clear grouped machine data
            scanResultsService.BarcodeGroups.Clear();
            scanResultsService.CurrentGroup = new BarcodeGroupItemViewModel();

            // ✅ Optional: Clear frames if needed
            // scanResultsService.Frames.Clear();

            Snackbar.Add("All results cleared.", Severity.Success);
        }
    }

    #endregion

    #region Clipboard Operations
    /// <summary>
    /// Copies all grouped results to clipboard.
    /// </summary>
    private async Task CopyAllGroupedResultsAsync()
    {
        var allGroups = scanResultsService.BarcodeGroups.Append(scanResultsService.CurrentGroup).ToList();
        var allBarcodes = allGroups.SelectMany(g => g.Barcodes)
            .ToList();

        if (!allBarcodes.Any())
        {
            Snackbar.Add("No results to copy.", Severity.Warning);
            return;
        }

        var textToCopy = string.Join("\n", allBarcodes.Select(b => b.Value));
        await Clipboard.Default.SetTextAsync(textToCopy);
        Snackbar.Add("All grouped results copied to clipboard.", Severity.Success);
    }

    /// <summary>
    /// Copies a single barcode value to clipboard.
    /// </summary>
    private async Task CopySingleBarcodeAsync(string value)
    {
        await Clipboard.Default.SetTextAsync(value);
        Snackbar.Add("Copied barcode.", Severity.Success);
    }
    #endregion

    #region Group Management

    /// <summary>
    /// Moves to the next group.
    /// </summary>
    private void CompleteCurrentScanningGroup() => scanResultsService.CompleteCurrentGroup();

    /// <summary>
    /// Completes scanning and navigates home.
    /// </summary>
    private async Task CompleteAndNavigateHomeAsync()
    {
        await App.Current.MainPage.Navigation.PopModalAsync();
    }
    #endregion
}