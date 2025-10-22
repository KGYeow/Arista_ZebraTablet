using Arista_ZebraTablet.Shared.Services;

namespace Arista_ZebraTablet.Web.Services
{
    public class FormFactorService : IFormFactorService
    {
        public string GetFormFactor()
        {
            return "Web";
        }

        public string GetPlatform()
        {
            return Environment.OSVersion.ToString();
        }
    }
}
