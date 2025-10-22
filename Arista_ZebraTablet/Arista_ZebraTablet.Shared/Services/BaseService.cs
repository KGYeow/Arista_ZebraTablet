using Arista_ZebraTablet.Shared.Data;

namespace Arista_ZebraTablet.Shared.Services
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