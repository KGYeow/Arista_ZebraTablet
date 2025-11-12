using Microsoft.AspNetCore.Components.WebView.Maui;

public partial class ReorderPage : ContentPage
{
    public ReorderPage()
    {
        //InitializeComponent();
        // Pass initial route to Blazor via query string
        ((BlazorWebView)Content).HostPage = "wwwroot/index.html#/reorder";
    }
}