using Arista_ZebraTablet.Shared.Application.Common;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arista_ZebraTablet.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScannedBarcodeController : BaseController
    {
        private readonly IScannedBarcodeService scannedBarcodeService;
        private readonly ILogger<ScannedBarcodeController> _logger;

        public ScannedBarcodeController(IScannedBarcodeService service, ILogger<ScannedBarcodeController> logger, ApplicationDbContext context) : base(context)
        {
            scannedBarcodeService = service;
            _logger = logger;
        }

        /// <summary>
        /// Adds scanned barcode items.
        /// </summary>
        /// <returns>
        /// 200 OK with <see cref="ServiceResponse{T}"/> containing number of rows affected.
        /// </returns>
        /// <remarks>
        /// The service enforces deduplication and returns a user-friendly message.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<int>), 200)]
        public async Task<ActionResult<ServiceResponse<int>>> AddScannedBarcodesAsync([FromBody] List<ScanBarcodeItemViewModel> items, CancellationToken ct)
        {
            var response = await scannedBarcodeService.AddScannedBarcodesAsync(items, ct);
            return Ok(response);
        }
    }
}