using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Components;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.Specialized;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Components;

/// <summary>
/// Code-behind for the <c>BarcodeScannerResult</c> component.
/// Represents the Blazor component that displays live barcode scanning results
/// within the MAUI hybrid page <see cref="LiveBarcodeScannerPage"/>,
/// provides actions for editing, deleting, clearing, and copying scanned barcodes.
/// </summary>
/// <remarks>
/// <para>
/// This component is hosted inside a <see cref="BlazorWebView"/> alongside the native
/// camera view (<c>scanner:CameraView</c>) in the XAML layout. It provides real-time
/// feedback for scanned barcodes, grouped results, and user actions such as edit,
/// delete, clear, and copy.
/// </para>
/// <para>
/// The component subscribes to <see cref="BarcodeDetectorService.ScanReceived"/> for
/// live updates and interacts with <see cref="BarcodeDetectorService"/> to manage
/// grouped barcode data during scanning sessions.
/// </para>
/// <para>
/// Typical usage:
/// - Display current frame group results while scanning.
/// - Show grouped results after completing a scan.
/// - Provide UI actions for category editing and clipboard operations.
/// </para>
/// </remarks>
public partial class BarcodeScannerResult : ComponentBase, IDisposable
{

    #region Dependencies

    /// <summary>
    /// Provides access to the current scanning session and barcode groups.
    /// </summary>
    [Inject] private BarcodeDetectorService scanResultsService { get; set; } = null!;

    #endregion

    #region Constants and State

    /// <summary>
    /// Gets the collection of barcode groups that originate from the camera scanner source.
    /// </summary>
    /// <remarks>
    /// This property filters <see cref="scanResultsService.BarcodeGroups"/> to include only
    /// groups where <see cref="BarcodeGroupItemViewModel.Source"/> equals <see cref="BarcodeSource.Camera"/>.
    /// It is used to simplify UI logic for displaying live scanning results.
    /// </remarks>
    private IEnumerable<BarcodeGroupItemViewModel> CameraBarcodeGroups => scanResultsService.BarcodeGroups.Where(b => b.Source == BarcodeSource.Camera);

    /// <summary>
    /// Indicates whether a save operation is in progress.
    /// </summary>
    private bool isSaving { get; set; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the component and subscribes to scan events for UI updates.
    /// </summary>
    protected override void OnInitialized()
    {
        scanResultsService.ScanReceived += OnScanReceived;
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
    /// Handles barcode scan events and triggers UI refresh.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="scanned">The scanned barcode item.</param>
    private void OnScanReceived(object? sender, ScanBarcodeItemViewModel scanned)
    {
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
    /// Confirms and deletes a barcode from the current scanning barcode group.
    /// </summary>
    private async Task DeleteScannedResultConfirmationAsync(ScanBarcodeItemViewModel barcodeItem)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this scanned barcode? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Delete" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            scanResultsService.CurrentGroup.Barcodes.Remove(barcodeItem);
            await InvokeAsync(StateHasChanged);
            Snackbar.Add($"Deleted {barcodeItem.Category} from current barcode group.", Severity.Warning);
        }
    }

    /// <summary>
    /// Confirms and deletes the current scanning barcode group.
    /// </summary>
    private async Task DeleteCurrentScanningBarcodeGroupConfirmationAsync()
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete this scanned barcodes? This action is permanent and cannot be undone." },
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
        if (isSaving || !(scanResultsService.CurrentGroup.Barcodes.Count > 0 || CameraBarcodeGroups.Any()))
            return;

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Are you sure you want to clear all grouped barcode results? This action is permanent and cannot be undone." },
            { x => x.SubmitBtnText, "Clear" },
            { x => x.DialogIcon, Icons.Material.Rounded.Warning },
            { x => x.DialogIconColor, Color.Error }
        };
        var options = new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Clear All Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            // Clear grouped barcode data
            scanResultsService.BarcodeGroups.RemoveAll(b => b.Source == BarcodeSource.Camera);
            scanResultsService.CurrentGroup = new BarcodeGroupItemViewModel();
            Snackbar.Add("All results cleared.", Severity.Success);
        }
    }

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Copies all grouped results to the clipboard.
    /// </summary>
    private async Task CopyAllGroupedResultsAsync()
    {
        var allGroups = CameraBarcodeGroups.Append(scanResultsService.CurrentGroup).ToList();
        var allBarcodes = allGroups.SelectMany(g => g.Barcodes).ToList();

        if (allBarcodes.Count == 0)
        {
            Snackbar.Add("No results to copy.", Severity.Warning);
            return;
        }

        var textToCopy = string.Join("\n", allBarcodes.Select(b => b.Value));
        await Clipboard.Default.SetTextAsync(textToCopy);
        Snackbar.Add("All grouped results copied to clipboard.", Severity.Success);
    }

    /// <summary>
    /// Copies a single barcode value to the clipboard.
    /// </summary>
    private async Task CopySingleBarcodeAsync(string value)
    {
        await Clipboard.Default.SetTextAsync(value);
        Snackbar.Add("Copied barcode.", Severity.Success);
    }

    #endregion

    #region Barcode Group Management

    /// <summary>
    /// Completes the current scanning group and moves to the next.
    /// </summary>
    private void CompleteCurrentScanningGroup() => scanResultsService.CompleteCurrentGroup();

    /// <summary>
    /// Completes scanning and navigates back to the home page.
    /// </summary>
    private async Task CompleteAndNavigateHomeAsync()
    {
        await App.Current.MainPage.Navigation.PopModalAsync().ConfigureAwait(false);
    }

    #endregion
}