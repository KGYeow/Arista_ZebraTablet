using Arista_ZebraTablet.Shared.Application.Common;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Data;

namespace Arista_ZebraTablet.Shared.Services;

/// <summary>
/// Defines operations for querying and mutating scanned barcode records.
/// </summary>
/// <remarks>
/// This contract is host-agnostic and suitable for use from UI components in the Shared library.
/// The Web project provides an EF Core implementation; other hosts could provide API-based implementations.
/// </remarks>
public interface IScannedBarcodeService
{
    public Uri BuildUri(string relative);

    /// <summary>
    /// Gets the list of historical scanned barcodes from the data store (read-only).
    /// </summary>
    /// <returns>
    /// A <see cref="ServiceResponse{T}"/> containing the list of <see cref="ScannedBarcode"/> records,
    /// or an error message if the operation fails.
    /// </returns>
    Task<ServiceResponse<List<ScannedBarcode>>> GetScannedBarcodeListAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a list of scanned barcode items to the data store.
    /// </summary>
    /// <returns>
    /// A <see cref="ServiceResponse{T}"/> containing the number of rows affected,
    /// or an error message if the operation fails.
    /// </returns>
    Task<ServiceResponse<int>> AddScannedBarcodesAsync(List<ScanBarcodeItemViewModel> items, CancellationToken ct = default);

    /// <summary>
    /// Deletes a single scanned barcode by its identifier.
    /// </summary>
    /// <returns>
    /// A <see cref="ServiceResponse{T}"/> containing the number of rows affected (0 or 1),
    /// or an error message if the operation fails.
    /// </returns>
    /// <remarks>
    /// Implementations should be idempotent: deleting a non-existent id should return <c>Ok(0)</c>.
    /// </remarks>
    Task<ServiceResponse<int>> DeleteScannedBarcodeAsync(int id, CancellationToken ct = default);
}