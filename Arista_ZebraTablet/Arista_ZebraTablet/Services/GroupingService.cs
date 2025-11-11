using Arista_ZebraTablet.Shared.Application.ViewModels;
using Arista_ZebraTablet.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace Arista_ZebraTablet.Services
{
    public class GroupingService : INotifyPropertyChanged
    {
        public ObservableCollection<GroupedMachineScanViewModel> CompletedGroups { get; set; } = new();
        private GroupedMachineScanViewModel _currentGroup = new();
        public GroupedMachineScanViewModel CurrentGroup
        {
            get => _currentGroup;
            set
            {
                _currentGroup = value;
                NotifyPropertyChanged(nameof(CurrentGroup));
            }
        }

        //public void AddBarcode(ScanBarcodeItemViewModel barcode)
        //{
        //    CurrentGroup.BarcodesByCategory[barcode.Category] = barcode;
        //    OnPropertyChanged(nameof(CurrentGroup));
        //}

        public void AddBarcode(ScanBarcodeItemViewModel barcode)
        {
            // Add or replace barcode by category (even if category is unexpected)
            CurrentGroup.BarcodesByCategory[barcode.Category] = barcode;
        }
        public void RemoveCompletedGroupAt(int index)
        {
            if (index >= 0 && index < CompletedGroups.Count)
            {
                CompletedGroups.RemoveAt(index);
                NotifyPropertyChanged(nameof(CompletedGroups));
            }
        }

        public void NextGroup()
        {
            CompletedGroups.Add(CurrentGroup);
            CurrentGroup = new GroupedMachineScanViewModel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}