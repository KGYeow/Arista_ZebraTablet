using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Services;

public class FormFactorService : IFormFactorService
{
    public string GetFormFactor()
    {
        return DeviceInfo.Idiom.ToString();
    }

    public string GetPlatform()
    {
        return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
    }
}