using Microsoft.AspNetCore.Components;

namespace Arista_ZebraTablet.Shared.Shared;

/// <summary>
/// A reusable page header component that displays a title, optional right-aligned content, and an optional subtitle.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// <PageHeader Title="Dashboard">
///     <RightHeaderContent>
///         <MudButton Color="Color.Primary">Add Item</MudButton>
///     </RightHeaderContent>
///     <SubtitleContent>
///         <MudText Typo="Typo.body2">Manage your items and settings here.</MudText>
///     </SubtitleContent>
/// </PageHeader>
/// </code>
/// </remarks>
public partial class PageHeader : ComponentBase
{
    /// <summary>
    /// The main title displayed in the page header.
    /// </summary>
    [Parameter] public string Title { get; set; } = "Page Title";

    /// <summary>
    /// Optional content rendered on the right side of the header (e.g., buttons, icons).
    /// </summary>
    [Parameter] public RenderFragment? RightHeaderContent { get; set; }

    /// <summary>
    /// Optional content rendered below the header title (e.g., subtitle, description).
    /// </summary>
    [Parameter] public RenderFragment? SubtitleContent { get; set; }
}