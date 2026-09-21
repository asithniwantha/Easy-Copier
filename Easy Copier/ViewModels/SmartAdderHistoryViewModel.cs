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

        /// <summary>
        /// Gets the collection of history entry ViewModels loaded from storage.
        /// </summary>
        public ObservableCollection<SmartAdderHistoryEntryViewModel> Records { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether there are no historical records available.
        /// </summary>
        [ObservableProperty]
        public partial bool IsEmpty { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdderHistoryViewModel"/> class.
        /// </summary>
        /// <param name="smartAdderHistoryService">The service for retrieving recorded calculation sessions.</param>
        public SmartAdderHistoryViewModel(ISmartAdderHistoryService smartAdderHistoryService)
        {
            _smartAdderHistoryService = smartAdderHistoryService;
        }

        /// <summary>
        /// Event raised when the view requests the containing window to close.
        /// </summary>
        public event System.EventHandler? CloseRequested;

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void CloseWindow()
        {
            CloseRequested?.Invoke(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Asynchronously loads recent history records from persistent storage into <see cref="Records"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
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
