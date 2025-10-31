#if ANDROID
using Android.Content;

namespace Arista_ZebraTablet;

/// <summary>
/// Centralized DataWedge constants + helpers
/// </summary>
public static class DataWedgeClient
{
    // ACTION your DataWedge profile will broadcast to
    public const string ScanAction = "arista.zebra.SCAN"; // <-- use this in DW profile

    // Standard DW scan extras
    public const string ExtraDataString = "com.symbol.datawedge.data_string";
    public const string ExtraLabelType = "com.symbol.datawedge.label_type";

    // API command channel to DW
    public const string ApiAction = "com.symbol.datawedge.api.ACTION";
    public const string ApiExtraSoftScanTrigger = "com.symbol.datawedge.api.SOFT_SCAN_TRIGGER";

    // Profile management + result mechanism extras
    public const string ExtraSwitchToProfile = "com.symbol.datawedge.api.SWITCH_TO_PROFILE";
    public const string ExtraGetActiveProfile = "com.symbol.datawedge.api.GET_ACTIVE_PROFILE";
    public const string ExtraSendResult = "SEND_RESULT";
    public const string ExtraCommandIdentifier = "COMMAND_IDENTIFIER";

    public static Intent BuildSoftScanIntent(string command, bool sendResult = true, string commandId = "SOFTSCAN")
    {
        var i = new Intent(ApiAction); // "com.symbol.datawedge.api.ACTION"
        i.PutExtra(ApiExtraSoftScanTrigger, command); // START_SCANNING / STOP_SCANNING / TOGGLE_SCANNING
        if (sendResult)
        {
            i.PutExtra(ExtraSendResult, "true");
            i.PutExtra(ExtraCommandIdentifier, commandId);
        }
        return i;
    }

    /// <summary>
    /// Build a DW API intent for Soft Scan Trigger (START_SCANNING / STOP_SCANNING / TOGGLE_SCANNING)
    /// </summary>
    public static Intent BuildApiIntent(string softScanCommand)
    {
        var i = new Intent(ApiAction);
        i.PutExtra(ApiExtraSoftScanTrigger, softScanCommand);
        return i;
    }

    /// <summary>
    /// Ask DW to switch to a specific profile by name.
    /// </summary>
    public static Intent BuildSwitchProfileIntent(string profileName, string commandId = "SWITCH")
    {
        var i = new Intent(ApiAction);
        i.PutExtra(ExtraSwitchToProfile, profileName);  // e.g., "MAUIBarcodeScanner"
        i.PutExtra(ExtraSendResult, "true");
        i.PutExtra(ExtraCommandIdentifier, commandId);
        return i;
    }

    /// <summary>
    /// Query DW for the active profile; result is broadcast on ApiResultAction.
    /// </summary>
    public static Intent BuildGetActiveProfileIntent(string commandId = "GET_ACTIVE")
    {
        var i = new Intent(ApiAction);
        i.PutExtra(ExtraGetActiveProfile, "");  // empty value as per DW API
        i.PutExtra(ExtraSendResult, "true");
        i.PutExtra(ExtraCommandIdentifier, commandId);
        return i;
    }
}
#endif