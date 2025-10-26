using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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

            //builder.Services.AddHttpClient<IScannedBarcodeService, ScannedBarcodeService>(client =>
            //{
            //    client.BaseAddress = new Uri("https://awase1penweb81.corp.jabil.org/Arista_ZebraTablet/");
            //})
            //.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            //{
            //    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            //});


            builder.Services.AddTransient<HttpLoggingHandler>();

            builder.Services.AddHttpClient<IScannedBarcodeService, ScannedBarcodeService>(client =>
            {
                client.BaseAddress = new Uri("https://awase1penweb81.corp.jabil.org/Arista_ZebraTablet/");
            })
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                // On Android, MAUI will map this to Android’s native handler under the hood
                var handler = new HttpClientHandler();

                // Load one or more certs from the bundled .crt (PEM or DER; single or multiple)
                //var trustedCerts = LoadCertificatesFromApp("Jabil Enterprise KF ICA 1.crt");
                var trustedCerts = new X509Certificate2Collection();
                trustedCerts.AddRange(LoadCertificatesFromApp("Jabil Enterprise KF ICA 1.crt"));
                trustedCerts.AddRange(LoadCertificatesFromApp("Jabil Enterprise KF RCA 1.crt")); // optional

                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage message, X509Certificate2? serverCert, X509Chain? _, SslPolicyErrors errors) =>
                    {
                        if (serverCert is null)
                            return false;

                        // Keep hostname validation strict: if the only problem is chain, we’ll fix chain.
                        var nameOk = (errors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0;

                        // Build a new chain that trusts ONLY our embedded roots
                        using var chain2 = new X509Chain();
                        chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain2.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                        // Add roots (and optionally intermediates) to the custom trust store / extra store
                        foreach (var cert in trustedCerts)
                        {
                            // If it’s a root CA, add to CustomTrustStore; intermediates can go to ExtraStore
                            if (cert.SubjectName.Name == cert.IssuerName.Name)
                                chain2.ChainPolicy.CustomTrustStore.Add(cert);
                            else
                                chain2.ChainPolicy.ExtraStore.Add(cert);
                        }

                        var chainOk = chain2.Build(serverCert);

                        // Succeed only if both hostname and chain are valid.
                        return nameOk && chainOk;
                    };

                return handler;
            });

            static X509Certificate2Collection LoadCertificatesFromApp(string assetName)
            {
                using var s = FileSystem.OpenAppPackageFileAsync(assetName).GetAwaiter().GetResult();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                return LoadCertificatesFromBytes(ms.ToArray());
            }

            static X509Certificate2Collection LoadCertificatesFromBytes(ReadOnlySpan<byte> data)
            {
                // Detect PEM vs DER
                if (IsPem(data))
                    return LoadFromPem(System.Text.Encoding.ASCII.GetString(data));

                // DER (single cert)
                var col = new X509Certificate2Collection
                {
                    new X509Certificate2(data.ToArray())
                };
                return col;
            }

            static bool IsPem(ReadOnlySpan<byte> data)
                => System.Text.Encoding.ASCII.GetString(data).Contains("-----BEGIN CERTIFICATE-----");

            static X509Certificate2Collection LoadFromPem(string pem)
            {
                var col = new X509Certificate2Collection();
                const string begin = "-----BEGIN CERTIFICATE-----";
                const string end = "-----END CERTIFICATE-----";

                int start = 0;
                while (true)
                {
                    var i = pem.IndexOf(begin, start, StringComparison.Ordinal);
                    if (i < 0) break;
                    var j = pem.IndexOf(end, i, StringComparison.Ordinal);
                    if (j < 0) break;

                    var base64 = pem.Substring(i + begin.Length, j - i - begin.Length)
                                    .Replace("\r", "").Replace("\n", "").Trim();
                    var raw = Convert.FromBase64String(base64);
                    col.Add(new X509Certificate2(raw));

                    start = j + end.Length;
                }
                return col;
            }

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
