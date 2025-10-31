using System.Text.RegularExpressions;

public static class UniqueBarcodeClassifier
{
    public static string Classify(string barcode)
    {
        if (Regex.IsMatch(barcode, @"^(M48L(B)?|C96L(B)?|C48)-[BC]\d{1,2}-[A-Za-z0-9]{5}-[A-Za-z0-9]$"))
            return "Serial Number";

        //If Third Segment Can Vary in Length
        //if (Regex.IsMatch(barcode, @"^(M48L(B)?|C96L(B)?|C48)-[BC]\d{1,2}-[A-Za-z0-9]+-[A-Za-z0-9]$"))
        //    return "Serial Number";

        if (Regex.IsMatch(barcode, @"^[dD][eE][vV]-?[0-9]{5}$"))
            return "Deviation";

        if (Regex.IsMatch(barcode, @"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))
            return "MAC Address";

        if (Regex.IsMatch(barcode, @"[aA][sS][yY][- ]*([0-9]{5})[- ]*([0-9]{2}[0-9]?)[- ]*([0-9a-zA-Z][0-9])"))
            return "ASY";

        if (Regex.IsMatch(barcode, @"^JPN[0-9]{4}[A-Z][0-9]{3}$"))
            return "ASY-OTL";

        if (Regex.IsMatch(barcode, @"^[a-zA-Z]{3}[0-9]{4}[0-9a-zA-Z]{4}$"))
            return "PCA";

        return "Unknown";
    }
}