using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Backs the SmartAdder Calculation History window, listing previously logged
    /// calculation sessions in reverse-chronological order.
    /// </summary>
    public sealed partial class SmartAdderHistoryViewModel : ObservableObject
    {
        private const int MaxRecords = 200;

        private readonly ISmartAdderHistoryService _smartAdderHistoryService;

        public ObservableCollection<SmartAdderHistoryEntryViewModel> Records { get; } = [];

        [ObservableProperty]
        public partial bool IsEmpty { get; set; }

        public SmartAdderHistoryViewModel(ISmartAdderHistoryService smartAdderHistoryService)
        {
            _smartAdderHistoryService = smartAdderHistoryService;
        }

        public async Task InitializeAsync()
        {
            System.Collections.Generic.List<SmartAdderHistoryRecord> records = await _smartAdderHistoryService.GetRecentRecordsAsync(MaxRecords);

            Records.Clear();
            foreach (SmartAdderHistoryRecord record in records)
            {
                Records.Add(new SmartAdderHistoryEntryViewModel(record));
            }

            IsEmpty = Records.Count == 0;
        }
    }
}
