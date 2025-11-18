using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Shared.Components;

/// <summary>
/// Code-behind for the CategoryEditDialog component.
/// Provides UI logic for editing the category of a detected barcode item.
/// </summary>
public partial class CategoryEditDialog : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Provides access to the parent MudDialog instance for closing and returning results.
    /// </summary>
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    #endregion

    #region Parameters

    /// <summary>
    /// The barcode item whose category is being edited.
    /// </summary>
    [Parameter] public ScanBarcodeItemViewModel BarcodeItem { get; set; } = default!;

    #endregion

    #region Constants & state

    /// <summary>
    /// Indicates whether the dialog is performing a save operation.
    /// </summary>
    private bool isBusy { get; set; }

    /// <summary>
    /// The currently selected category value.
    /// </summary>
    private string category { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the MudSelect component for focusing after render.
    /// </summary>
    private MudSelect<string>? selectRef;

    /// <summary>
    /// Default category options available for selection.
    /// </summary>
    private static readonly IReadOnlyList<string> DefaultCategories = ["ASY", "PCA", "Serial Number", "MAC Address", "Deviation", "Unknown"];

    /// <summary>
    /// Action buttons for the modal dialog (Cancel and Save).
    /// </summary>
    private readonly ModalDialog.DialogAction[] actionBtns =
    {
        ModalDialog.DialogAction.Cancel("Cancel"),
        ModalDialog.DialogAction.Ok(text: "Save", value: true, color: Color.Primary, variant: Variant.Filled)
    };

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the dialog state by copying the category from the provided barcode item.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Local copy so Cancel won't mutate the original
        category = BarcodeItem?.Category ?? string.Empty;
    }

    /// <summary>
    /// Focuses the category select input after the first render for better UX.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && selectRef is not null)
        {
            // Give focus to the select after render for better UX
            await selectRef.FocusAsync();
        }
    }

    #endregion

    #region Actions

    /// <summary>
    /// Handles dialog action clicks. Updates the barcode item's category on Save.
    /// </summary>
    /// <param name="action">The dialog action triggered by the user.</param>
    private void HandleActionClick(ModalDialog.DialogAction action)
    {
        if (action.CloseBehavior != ModalDialog.DialogCloseBehavior.Ok)
            return;

        try
        {
            isBusy = true;

            var selected = (category ?? string.Empty).Trim();
            BarcodeItem.Category = string.IsNullOrWhiteSpace(selected) ? "Unknown" : selected;
        }
        finally
        {
            isBusy = false;
        }
    }

    #endregion
}