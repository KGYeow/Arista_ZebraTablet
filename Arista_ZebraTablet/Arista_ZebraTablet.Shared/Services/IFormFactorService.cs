namespace Arista_ZebraTablet.Shared.Services;

/// <summary>
/// Provides methods to retrieve device form factor and platform information.
/// Used for responsive UI adjustments and platform-specific logic.
/// </summary>
public interface IFormFactorService
{
    #region Form Factor

    /// <summary>
    /// Gets the current device form factor (e.g., "Web", "Phone").
    /// </summary>
    /// <returns>A string representing the device form factor.</returns>
    string GetFormFactor();

    #endregion

    #region Platform

    /// <summary>
    /// Gets the current platform.
    /// </summary>
    /// <returns>A string representing the platform type.</returns>
    string GetPlatform();

    #endregion
}