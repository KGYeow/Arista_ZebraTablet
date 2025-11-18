using Arista_ZebraTablet.Shared.Application.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Arista_ZebraTablet.Shared.Components;

/// <summary>
/// Code-behind for the BarcodeSettingsDrawer component.
/// Provides UI logic for toggling the settings drawer and handling swipe gestures.
/// </summary>
public partial class BarcodeSettingsDrawer : ComponentBase
{
    #region Parameters

    /// <summary>
    /// The currently selected barcode classification mode.
    /// </summary>
    [Parameter] public BarcodeMode BarcodeMode { get; set; }

    #endregion

    #region State

    /// <summary>
    /// Tracks whether the settings drawer is open.
    /// </summary>
    private bool moreSettingOpen { get; set; }

    /// <summary>
    /// Reference to the MudSwipeArea component for swipe gesture handling.
    /// </summary>
    private MudSwipeArea swipeArea = null!;

    #endregion

    #region Actions

    /// <summary>
    /// Toggles the visibility of the settings drawer.
    /// </summary>
    private void ToggleSettingDrawer() => moreSettingOpen = !moreSettingOpen;

    /// <summary>
    /// Handles swipe movement events to close the drawer on top-to-bottom swipe.
    /// </summary>
    /// <param name="e">Event arguments containing swipe direction details.</param>
    public void HandleSwipeMove(MultiDimensionSwipeEventArgs e)
    {
        for (int i = 0; i < e.SwipeDirections.Count; i++)
        {
            if (e.SwipeDirections[i] == MudBlazor.SwipeDirection.TopToBottom && moreSettingOpen)
            {
                moreSettingOpen = false;
            }
        }
    }

    /// <summary>
    /// Called when the swipe gesture leaves the swipe area.
    /// Cancels ongoing swipe and resets UI.
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
}