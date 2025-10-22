using Arista_ZebraTablet.Shared.Services; // or Helpers, depending on where you placed it

namespace Arista_ZebraTablet
{

    public partial class ScanResultPage : ContentPage
    {
        private List<string> _barcodes;
        public ScanResultPage(List<string> barcodes)
        {

            InitializeComponent();
            _barcodes = barcodes;
            BarcodeListView.ItemsSource = barcodes;
        }

        private async void OnContinueScanClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Go back to MainPage to scan more
        }

        //private async void OnSaveClicked(object sender, EventArgs e)
        //{
        //    ScanHistoryStore.AddScanResult(_barcodes);
        //    await Navigation.PushAsync(new HistoryPage());
        //}

    }
}

//using SDKTuto.Services;
//using System.Collections.ObjectModel;

//namespace SDKTuto.Pages
//{
//    [QueryProperty(nameof(BarcodesString), "Barcodes")]
//    public partial class ScanResultPage : ContentPage
//    {
//        private List<string> _barcodes;


//        public string BarcodesString
//        {
//            set
//            {
//                _barcodes = value?.Split(',').ToList() ?? new List<string>();
//                BarcodeListView.ItemsSource = _barcodes;
//            }
//        }


//        public ScanResultPage(List<string> barcodes)
//        {
//            InitializeComponent();
//        }
//    }
//}