using Microsoft.AspNetCore.Components;

namespace Arista_ZebraTablet.Shared.Shared;

/// <summary>
/// A reusable loading overlay component that displays a spinner and optional text.
/// </summary>
/// <remarks>
/// Use this component to indicate background operations or page loading.
/// Example usage:
/// <code>
/// <LoadingOverlay Visible="@isLoading" Text="Please wait..." />
/// </code>
/// </remarks>
public partial class LoadingOverlay : ComponentBase
{
    /// <summary>
    /// Determines whether the loading overlay is visible.
    /// </summary>
    /// <value>
    /// <c>true</c> to show the overlay; <c>false</c> to hide it.
    /// </value>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// The text displayed below the loading spinner.
    /// </summary>
    /// <value>
    /// Defaults to <c>"Loading..."</c>.
    /// </value>
    [Parameter] public string Text { get; set; } = "Loading...";
}