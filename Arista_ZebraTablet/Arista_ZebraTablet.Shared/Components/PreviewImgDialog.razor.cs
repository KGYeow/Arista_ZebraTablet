using Arista_ZebraTablet.Shared.Application.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Arista_ZebraTablet.Shared.Components;

/// <summary>
/// Code-behind for the PreviewImgDialog component.
/// Displays a carousel of uploaded images with toolbar actions for delete and close.
/// </summary>
public partial class PreviewImgDialog : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Provides access to the parent MudDialog instance for closing the dialog.
    /// </summary>
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    
    #endregion

    #region Parameters

    /// <summary>
    /// The index of the currently selected image in the carousel.
    /// </summary>
    [Parameter] public int SelectedFileIndex { get; set; }

    /// <summary>
    /// The collection of image files to preview.
    /// </summary>
    [Parameter] public List<BarcodeGroupItemViewModel> ImageFiles { get; set; } = new();

    #endregion

    #region State

    /// <summary>
    /// Reference to the MudCarousel component for controlling navigation.
    /// </summary>
    private MudCarousel<BarcodeGroupItemViewModel> carousel = null!;

    /// <summary>
    /// Indicates whether the dialog is performing an operation (e.g., delete).
    /// </summary>
    private bool isBusy;

    /// <summary>
    /// Gets the currently selected image file based on <see cref="SelectedFileIndex"/>.
    /// </summary>
    private BarcodeGroupItemViewModel? SelectedImageFile => (SelectedFileIndex >= 0 && SelectedFileIndex < ImageFiles.Count) ? ImageFiles[SelectedFileIndex] : null;

    #endregion

    #region Actions

    /// <summary>
    /// Deletes the image at the specified index and updates the carousel selection.
    /// </summary>
    /// <param name="index">The index of the image to delete.</param>
    private async Task DeleteAsync(int index)
    {
        if (ImageFiles is null || ImageFiles.Count == 0)
            return;

        if (index >= 0 && index < ImageFiles.Count)
        {
            ImageFiles.RemoveAt(index);
            await Task.Delay(1); // allow UI refresh

            if (ImageFiles.Count == 0)
            {
                MudDialog.Cancel();
                return;
            }

            // Clamp the selected index to the next valid item
            SelectedFileIndex = Math.Clamp(index, 0, ImageFiles.Count - 1);
            StateHasChanged();
        }
    }

    #endregion
}