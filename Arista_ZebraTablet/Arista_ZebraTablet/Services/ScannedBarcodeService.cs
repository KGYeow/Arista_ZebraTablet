using Arista_ZebraTablet.Shared.Application.Common;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;
using Arista_ZebraTablet.Shared.Services;
using System.Net.Http.Json;

namespace Arista_ZebraTablet.Services
{
    public class ScannedBarcodeService : IScannedBarcodeService
    {
        private readonly HttpClient _http;

        public ScannedBarcodeService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets the list of historical scanned barcodes.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ServiceResponse{T}"/> containing an empty list.
        /// </returns>
        public Task<ServiceResponse<List<ScannedBarcode>>> GetScannedBarcodeListAsync(CancellationToken ct = default)
        {
            // No DB access on mobile: return an empty list.
            var empty = new List<ScannedBarcode>();
            return Task.FromResult(ServiceResponse<List<ScannedBarcode>>.Ok(
                empty,
                "Mobile no-op: returning empty list."
            ));
        }

        /// <summary>
        /// Adds scanned barcodes by calling <c>POST /api/ScannedBarcodes</c>.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{T}"/> with the number of rows affected (dedup handled server-side).
        /// </returns>
        public async Task<ServiceResponse<int>> AddScannedBarcodesAsync(List<ScanBarcodeItemViewModel> items, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("/api/ScannedBarcode", items, ct);
            if (!response.IsSuccessStatusCode)
                return ServiceResponse<int>.Fail($"HTTP {(int)response.StatusCode}");

            return await response.Content.ReadFromJsonAsync<ServiceResponse<int>>(cancellationToken: ct)
                   ?? ServiceResponse<int>.Fail("Invalid response.");
        }

        /// <summary>
        /// Deletes a single scanned barcode.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{T}"/> with <c>0</c> rows affected.
        /// </returns>
        /// <remarks>
        /// <b>Mobile no-op:</b> This method intentionally does nothing on mobile and returns <c>Ok(0)</c>.
        /// </remarks>
        public Task<ServiceResponse<int>> DeleteScannedBarcodeAsync(int id, CancellationToken ct = default)
        {
            // No DB access on mobile: do nothing, report 0 affected.
            return Task.FromResult(ServiceResponse<int>.Ok(0, "Mobile no-op: delete skipped."));
        }
    }
}