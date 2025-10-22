using Arista_ZebraTablet.Shared.Data;

namespace Arista_ZebraTablet.Web.Services
{
    public class BaseService
    {
        protected readonly ApplicationDbContext context;

        public BaseService(ApplicationDbContext context)
        {
            this.context = context;
        }
    }
}