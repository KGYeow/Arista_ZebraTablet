using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using ZXing.Net.Maui.Controls;

namespace Arista_ZebraTablet
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the Arista_ZebraTablet.Shared project
            builder.Services.AddSingleton<ScanResultPage>();
            builder.Services.AddSingleton<UploadBarcodeDecoderService>();
            builder.Services.AddSingleton<IFormFactorService, FormFactorService>();
            //builder.Services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
            builder.Services.AddSingleton<IBarcodeScannerService>(sp => sp.GetRequiredService<BarcodeScannerService>());
            builder.Services.AddScoped<IScannedBarcodeService, ScannedBarcodeService>();
            builder.Services.AddSingleton<UploadBarcodeDecoderService>();
            //builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
            //builder.Services.AddSingleton<IScannedBarcodeService, ScannedBarcodeService>();

            builder.Services.AddHttpClient<IScannedBarcodeService, ScannedBarcodeService>(client =>
            {
                client.BaseAddress = new Uri("https://awase1penweb81.corp.jabil.org/Arista_ZebraTablet/");
            });

            builder.Services.AddTransient<BarcodeScannerPage>();
            builder.Services.AddSingleton<BarcodeScannerService>();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices(config =>
            {
                // Add configurations for snackbar
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ClearAfterNavigation = false;
                config.SnackbarConfiguration.VisibleStateDuration = 4000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.MaximumOpacity = 90;
                config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
