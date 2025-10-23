using Arista_ZebraTablet.Shared.Application.Common;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;
using Arista_ZebraTablet.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Arista_ZebraTablet.Web.Services
{
    public class ScannedBarcodeService : BaseService, IScannedBarcodeService
    {
        public ScannedBarcodeService(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get the list of historical scanned barcodes from the DB (read-only).
        /// </summary>
        public async Task<ServiceResponse<List<ScannedBarcode>>> GetScannedBarcodeListAsync(CancellationToken ct = default)
        {
            try
            {
                var list = await context.ScannedBarcodes.AsNoTracking().OrderByDescending(u => u.Id).ToListAsync(ct);

                return ServiceResponse<List<ScannedBarcode>>.Ok(list);
            }
            catch (OperationCanceledException)
            {
                return ServiceResponse<List<ScannedBarcode>>.Fail("Operation cancelled.");
            }
            catch (Exception)
            {
                return ServiceResponse<List<ScannedBarcode>>.Fail("Unexpected error occurred. Please try again later.");
            }
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
            catch (Exception ex)
            {
                return ServiceResponse<int>.Fail(ex.Message);
                //return ServiceResponse<int>.Fail("Unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Deletes a single scanned barcode.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{T}"/> containing the number of rows affected (0 or 1),
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation is idempotent. If the specified <paramref name="id"/> does not exist,
        /// the method returns <c>Ok(0)</c> with a descriptive message.
        /// </remarks>
        public async Task<ServiceResponse<int>> DeleteScannedBarcodeAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResponse<int>.Fail("Invalid barcode ID.");

            try
            {
                var scannedBarcode = await context.ScannedBarcodes.FindAsync([id], ct);
                if (scannedBarcode == null)
                {
                    // Idempotent behavior: nothing to delete; report 0 rows affected.
                    return ServiceResponse<int>.Ok(0, $"Barcode (Id={id}) not found. Nothing to delete.");
                }
                context.ScannedBarcodes.Remove(scannedBarcode);

                var affected = await context.SaveChangesAsync(ct);
                return ServiceResponse<int>.Ok(affected, $"Barcode value ({scannedBarcode.Value}) deleted.");
            }
            catch (OperationCanceledException)
            {
                return ServiceResponse<int>.Fail("Operation cancelled.");
            }
            catch (DbUpdateException)
            {
                return ServiceResponse<int>.Fail("Unable to delete the barcode due to a database constraint or concurrency issue.");
            }
            catch (Exception)
            {
                return ServiceResponse<int>.Fail("Unexpected error occurred. Please try again later.");
            }
        }
    }
}