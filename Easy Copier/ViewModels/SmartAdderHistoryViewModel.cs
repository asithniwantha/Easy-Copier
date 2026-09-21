using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Backs the SmartAdder Calculation History window, listing previously logged
    /// calculation sessions in reverse-chronological order.
    /// </summary>
    public sealed partial class SmartAdderHistoryViewModel(ISmartAdderHistoryService smartAdderHistoryService) : ObservableObject
    {
        private const int MaxRecords = 200;

        private readonly ISmartAdderHistoryService _smartAdderHistoryService = smartAdderHistoryService ?? throw new ArgumentNullException(nameof(smartAdderHistoryService));

        /// <summary>
        /// Gets the collection of logged calculation session entries.
        /// </summary>
        public ObservableCollection<SmartAdderHistoryEntryViewModel> Records { get; } = [];

        [ObservableProperty]
        public partial bool IsEmpty { get; set; }

        /// <summary>
        /// Event raised when the view requests to be closed.
        /// </summary>
        public event EventHandler? CloseRequested;

        [RelayCommand]
        private void CloseWindow()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Asynchronously loads recent calculation history records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            List<SmartAdderHistoryRecord> records = await _smartAdderHistoryService.GetRecentRecordsAsync(MaxRecords);

            Records.Clear();
            foreach (SmartAdderHistoryRecord record in records)
            {
                Records.Add(new SmartAdderHistoryEntryViewModel(record));
            }

            IsEmpty = Records.Count == 0;
        }
    }
}
