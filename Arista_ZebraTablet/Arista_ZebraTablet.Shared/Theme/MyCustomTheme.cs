using MudBlazor;

namespace Arista_ZebraTablet.Shared.Theme;

public class MyCustomTheme : MudTheme
{
    // Customize existing MudBlazor theme properties here
    // Jabil Brand Portal: https://jabil.sharepoint.com/sites/JabilBrandPortal/ (Navigate to Brand Guidelines)
    public MyCustomTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#003865",
            Secondary = "#005288",
            Tertiary = "#0164A1",
            Background = "#F1F2F2",
            BackgroundGray = "#F1F2F2",
            AppbarBackground = "#003865",
            DrawerBackground = "#003865",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#FFFFFF",
            TextPrimary = "#414042",
            TextSecondary = "#60605B",
            Info = "#0990CF",
            InfoLighten = "#15BEF0",
        };

        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "8px",
        };
    }
}