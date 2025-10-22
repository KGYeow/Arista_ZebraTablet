using Arista_ZebraTablet.Shared.Application.Common;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Arista_ZebraTablet.Services
{
    public class ScannedBarcodeService : BaseService, IScannedBarcodeService
    {
        public ScannedBarcodeService(ApplicationDbContext context) : base(context)
        {
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
        /// Adds a list of scanned barcode items to the database.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{T}"/> containing the number of rows affected,
        /// or an error message if the operation fails.
        /// </returns>
        public async Task<ServiceResponse<int>> AddScannedBarcodesAsync(List<ScanBarcodeItemViewModel> items, CancellationToken ct = default)
        {
            if (items == null || items.Count == 0)
            {
                return ServiceResponse<int>.Fail("No barcode items provided.");
            }

            try
            {
                var inputScannedBarcodes = items.Select(item => new ScannedBarcode
                {
                    Value = item.Value.Trim(),
                    Format = item.Format,
                    Category = item.Category,
                    ScannedTime = item.ScannedTime
                }).ToList();

                // De-duplicate within the batch by Value (case-sensitive)
                var inputDistinctScannedBarcodes = inputScannedBarcodes
                    .GroupBy(x => x.Value)
                    .Select(g => g.First())
                    .ToList();

                // Find existing values already in DB (case-sensitive)
                var inputValuesToCheck = inputDistinctScannedBarcodes
                    .Select(i => i.Value.Trim())
                    .ToList();
                var existingScannedBarcodes = await context.ScannedBarcodes
                    .AsNoTracking()
                    .Where(b => inputValuesToCheck.Contains(b.Value))
                    .Select(b => b.Value)
                    .ToListAsync(ct);

                // Build list for only the new (non-existing) barcode values
                var newScannedBarcodes = inputDistinctScannedBarcodes
                    .Where(i => !existingScannedBarcodes.Contains(i.Value))
                    .ToList();

                if (newScannedBarcodes.Count == 0)
                    return ServiceResponse<int>.Ok(0, "No new barcodes to save. All provided values already exist or are duplicates.");

                await context.ScannedBarcodes.AddRangeAsync(newScannedBarcodes, ct);

                int affectedRows;
                try
                {
                    affectedRows = await context.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    // Handle race condition: if a unique index exists, we might get here. Re-check and report.
                    return ServiceResponse<int>.Fail("Some barcodes were already inserted (possibly by another process). Please retry.");
                }

                var skipped = items.Count - newScannedBarcodes.Count;
                var message = $"{affectedRows} new barcode(s) saved. {skipped} duplicate/existing value(s) skipped.";
                return ServiceResponse<int>.Ok(affectedRows, message);
            }
            catch (OperationCanceledException)
            {
                return ServiceResponse<int>.Fail("Operation cancelled.");
            }
            catch (Exception)
            {
                return ServiceResponse<int>.Fail("Unexpected error occurred. Please try again later.");
            }
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