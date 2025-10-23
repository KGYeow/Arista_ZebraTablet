using Arista_ZebraTablet.Shared.Data;
using Microsoft.AspNetCore.Mvc;

namespace Arista_ZebraTablet.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext context;

        public BaseController(ApplicationDbContext context)
        {
            this.context = context;
        }
    }
}