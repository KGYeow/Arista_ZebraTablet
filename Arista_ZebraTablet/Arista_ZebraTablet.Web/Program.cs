using Arista_ZebraTablet.Shared.Data;
using Arista_ZebraTablet.Shared.Services;
using Arista_ZebraTablet.Web.Components;
using Arista_ZebraTablet.Web.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    // Add configurations for snackbar
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ClearAfterNavigation = false;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.MaximumOpacity = 90;
});

// Add device-specific services used by the Arista_ZebraTablet.Shared project
builder.Services.AddSingleton<IFormFactorService, FormFactorService>();
builder.Services.AddSingleton<UploadBarcodeDecoderService>();
builder.Services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
builder.Services.AddScoped<IScannedBarcodeService, ScannedBarcodeService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add controller support + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMigrationsEndPoint();
}
else
{
    app.UsePathBase("/Arista_ZebraTablet");
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map attribute routed controllers (API endpoints)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Arista_ZebraTablet.Shared._Imports).Assembly);

app.Run();