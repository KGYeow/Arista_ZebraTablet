using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Services;

/// <summary>
/// This service is used for responsive UI adjustments and platform-specific logic
/// in the application.
/// </summary>
/// <remarks>
/// The form factor typically returns values like "Phone", "Tablet", or "Desktop",
/// while the platform includes OS and version information (e.g., "Android - 13").
/// </remarks>
public class FormFactorService : IFormFactorService
{

    #region Form Factor

    /// <inheritdoc />
    public string GetFormFactor()
    {
        return DeviceInfo.Idiom.ToString();
    }

    #endregion

    #region Platform

    /// <inheritdoc />
    public string GetPlatform()
    {
        return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
    }
    
    #endregion
}