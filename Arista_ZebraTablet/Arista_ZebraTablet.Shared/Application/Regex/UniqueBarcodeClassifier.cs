using System.Text.RegularExpressions;
using SystemRegex = System.Text.RegularExpressions.Regex;

namespace Arista_ZebraTablet.Shared.Application.Regex;

/// <summary>
/// Provides classification logic for unique barcodes based on specialized patterns.
/// </summary>
public static class UniqueBarcodeClassifier
{
    /// <summary>
    /// Classifies a barcode string into a category (e.g., Serial Number, ASY, ASY-OTL, PCA).
    /// </summary>
    /// <param name="barcode">The raw barcode value to classify.</param>
    /// <returns>
    /// A category name if the barcode matches a known unique pattern; otherwise, "Unknown".
    /// </returns>
    public static string Classify(string barcode)
    {
        if (SystemRegex.IsMatch(barcode, @"^(M48L(B)?|C96L(B)?|C48)-[BC]\d{1,2}-[A-Za-z0-9]{5}-[A-Za-z0-9]$"))
            return "Serial Number";

        //If Third Segment Can Vary in Length
        //if (Regex.IsMatch(barcode, @"^(M48L(B)?|C96L(B)?|C48)-[BC]\d{1,2}-[A-Za-z0-9]+-[A-Za-z0-9]$"))
        //    return "Serial Number";

        if (SystemRegex.IsMatch(barcode, @"^[dD][eE][vV]-?[0-9]{5}$"))
            return "Deviation";

        if (SystemRegex.IsMatch(barcode, @"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))
            return "MAC Address";

        if (SystemRegex.IsMatch(barcode, @"[aA][sS][yY][- ]*([0-9]{5})[- ]*([0-9]{2}[0-9]?)[- ]*([0-9a-zA-Z][0-9])"))
            return "ASY";

        if (SystemRegex.IsMatch(barcode, @"^JPN[0-9]{4}[A-Z][0-9]{3}$"))
            return "ASY-OTL";

        if (SystemRegex.IsMatch(barcode, @"^[a-zA-Z]{3}[0-9]{4}[0-9a-zA-Z]{4}$"))
            return "PCA";

        return "Unknown";
    }
}