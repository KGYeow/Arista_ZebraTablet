using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Arista_ZebraTablet.Shared.Shared;

/// <summary>
/// A flexible, reusable modal dialog built on MudBlazor's <see cref="MudDialog"/>.
/// </summary>
/// <remarks>
/// <para>
/// The dialog renders a title (with optional divider), a content area (<see cref="ContentArea"/>),
/// and an optional actions/footer bar. Actions can be:
/// </para>
/// <list type="bullet">
///   <item><description>Automatically generated (default <c>Cancel</c> / <c>OK</c>), or</description></item>
///   <item><description>Supplied via <see cref="Actions"/>, or</description></item>
///   <item><description>Fully overridden using <see cref="ActionsTemplate"/>.</description></item>
/// </list>
/// <para>
/// Use <see cref="ReturnActionAsResult"/> to control what data is returned through
/// <c>DialogResult.Ok(Data)</c> when an action closes the dialog.
/// </para>
/// <para>
/// Typical usage (with <c>IDialogService</c>):
/// </para>
/// <code>
/// var parameters = new DialogParameters
/// {
///     ["Title"] = "Edit profile",
///     ["ContentArea"] = (RenderFragment)(builder => {
///         builder.AddContent(0, "Form goes here...");
///     }),
///     ["Actions"] = new[]
///     {
///         ModalDialog.DialogAction.Cancel(),
///         ModalDialog.DialogAction.Ok("Save", value: true, color: Color.Primary, variant: Variant.Filled)
///     }
/// };
///
/// var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
/// var dialogRef = DialogService.Show<ModalDialog>("", parameters, options);
/// var result = await dialogRef.Result;
/// if (!result.Cancelled)
/// {
///     // handle save
/// }
/// </code>
/// </remarks>
public partial class ModalDialog : ComponentBase
{
    /// <summary>
    /// Provides access to the MudBlazor dialog instance for controlling dialog behavior (close, cancel).
    /// </summary>
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>
    /// Overrides the entire header area. If set, this takes precedence over TitleContent and Title.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The title text displayed in the dialog header.
    /// </summary>
    [Parameter] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether a divider is shown below the title/header.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>.
    /// </remarks>
    [Parameter] public bool ShowDivider { get; set; } = true;

    /// <summary>
    /// The main content area of the dialog, provided as a RenderFragment.
    /// </summary>
    [Parameter] public RenderFragment ContentArea { get; set; } = null!;

    /// <summary>
    /// Determines whether the dialog footer with action buttons (e.g., Cancel, Save) is displayed.
    /// Set to <c>false</c> to hide the actions, typically during loading or when an error occurs.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>.
    /// </remarks>
    [Parameter] public bool ShowActions { get; set; } = true;

    /// <summary>
    /// A collection of actions (buttons) to display in the dialog footer.
    /// If null, default Cancel/OK actions are shown.
    /// </summary>
    [Parameter] public IEnumerable<DialogAction>? Actions { get; set; }

    /// <summary>
    /// A custom template for rendering dialog actions.
    /// Overrides the default or provided Actions collection.
    /// </summary>
    [Parameter] public RenderFragment? ActionsTemplate { get; set; }

    /// <summary>
    /// Event callback triggered when an action button is clicked.
    /// </summary>
    [Parameter] public EventCallback<DialogAction> OnActionClick { get; set; }

    /// <summary>
    /// If true, the clicked action (or its Value) is returned as DialogResult.Data when closing.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>.
    /// </remarks>
    [Parameter] public bool ReturnActionAsResult { get; set; } = true;

    /// <summary>
    /// Computes the list of actions to display.
    /// Defaults to Cancel/OK if no custom actions are provided.
    /// </summary>
    private IReadOnlyList<DialogAction> ComputedActions => Actions == null || !Actions.Any()
        ? new[]
        {
            DialogAction.Cancel(),                 // Cancel => DialogResult.Canceled = true
            DialogAction.Ok(value: true)           // OK     => DialogResult.Ok(true)
        }
        : Actions.ToList();

    /// <summary>
    /// Handles the click event for a dialog action button.
    /// Invokes the OnActionClick callback and closes the dialog based on the action's CloseBehavior.
    /// </summary>
    private async Task OnActionClickedAsync(DialogAction action)
    {
        if (OnActionClick.HasDelegate)
            await OnActionClick.InvokeAsync(action);

        // Decide how to close based on action.CloseBehavior
        switch (action.CloseBehavior)
        {
            case DialogCloseBehavior.None:
                // Do nothing; dialog remains open
                break;

            case DialogCloseBehavior.Cancel:
                MudDialog.Cancel(); // result.Canceled = true
                break;

            case DialogCloseBehavior.Ok:
                var data = ReturnActionAsResult ? action.Value ?? action : null;
                MudDialog.Close(DialogResult.Ok(data));
                break;
        }
    }

    /// <summary>
    /// Defines how the dialog should behave when an action button is clicked.
    /// </summary>
    public enum DialogCloseBehavior
    {
        /// <summary>No automatic closing; caller handles it manually.</summary>
        None = 0,
        /// <summary>Close with DialogResult.Ok(data).</summary>
        Ok = 1,
        /// <summary>Close with DialogResult.Canceled = true.</summary>
        Cancel = 2
    }

    /// <summary>
    /// Represents a dialog action (button) with text, style, and behavior.
    /// </summary>
    public class DialogAction
    {
        /// <summary>
        /// The text displayed on the button.
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// The color of the button (MudBlazor Color enum).
        /// </summary>
        public Color Color { get; set; } = Color.Default;

        /// <summary>
        /// The visual variant of the button (Filled, Outlined, etc.).
        /// </summary>
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Indicates whether the button is disabled.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Determines how the dialog should close when this action is clicked.
        /// <list>Ok     => Close(DialogResult.Ok(data))</list>
        /// <list>Cancel => Cancel() (result.Canceled = true)</list>
        /// <list>None   => Do not close automatically</list>
        /// </summary>
        public DialogCloseBehavior CloseBehavior { get; set; } = DialogCloseBehavior.Ok;

        /// <summary>
        /// If true, clicking the button closes the dialog.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>.
        /// </remarks>
        public bool CloseOnClick { get; set; } = true;

        /// <summary>
        /// Marks this action as the default (e.g., primary action).
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Optional payload returned via DialogResult.Data (bool, enum, object, etc.).
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Creates a Cancel action button that closes the dialog as canceled.
        /// </summary>
        public static DialogAction Cancel(string text = "Cancel") => new()
        {
            Text = text,
            Color = Color.Default,
            Variant = Variant.Outlined,
            CloseBehavior = DialogCloseBehavior.Cancel,
            Value = false
        };

        /// <summary>
        /// Creates an OK action button that closes the dialog with DialogResult.Ok.
        /// </summary>
        public static DialogAction Ok(string text = "OK", object? value = null, bool isDefault = true, Color color = Color.Primary, Variant variant = Variant.Filled) => new()
        {
            Text = text,
            Color = color,
            Variant = variant,
            CloseBehavior = DialogCloseBehavior.Ok,
            IsDefault = isDefault,
            Value = value
        };

        /// <summary>
        /// Creates an action button that does not close the dialog automatically.
        /// Useful for actions like "Preview" or "Validate".
        /// </summary>
        public static DialogAction NoClose(string text, Color color = Color.Default, Variant variant = Variant.Filled) => new()
        {
            Text = text,
            Color = color,
            Variant = variant,
            CloseBehavior = DialogCloseBehavior.None
        };
    }
}