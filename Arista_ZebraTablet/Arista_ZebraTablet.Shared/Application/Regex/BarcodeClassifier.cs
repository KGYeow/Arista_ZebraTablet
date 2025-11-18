using System.Text.RegularExpressions;
using SystemRegex = System.Text.RegularExpressions.Regex;

namespace Arista_ZebraTablet.Shared.Application.Regex;

/// <summary>
/// Provides classification logic for standard barcodes based on predefined patterns.
/// </summary>
public static class BarcodeClassifier
{
    /// <summary>
    /// Classifies a barcode string into a category (e.g., Serial Number, MAC Address, ASY, PCA).
    /// </summary>
    /// <param name="barcode">The raw barcode value to classify.</param>
    /// <returns>
    /// A category name if the barcode matches a known pattern; otherwise, "Unknown".
    /// </returns>
    public static string Classify(string barcode)
    {
        if (SystemRegex.IsMatch(barcode, @"^[a-zA-Z]{3}[0-9]{4}[0-9a-zA-Z]{4}$"))
            return "Serial Number";

        if (SystemRegex.IsMatch(barcode, @"^[dD][eE][vV]-?[0-9]{5}$"))
            return "Deviation";

        if (SystemRegex.IsMatch(barcode, @"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))
            return "MAC Address";

        if (SystemRegex.IsMatch(barcode, @"[aA][sS][yY][- ]*([0-9]{5})[- ]*([0-9]{2}[0-9]?)[- ]*([0-9a-zA-Z][0-9])"))
            return "ASY";

        if (SystemRegex.IsMatch(barcode, @"[pP][cC][aA][- ]*([0-9]{5})[- ]*([0-9]{2})[- ]*([0-9a-zA-Z][0-9])"))
            return "PCA";

        return "Unknown";
    }
}