import re

with open('Easy Copier/ViewModels/MainViewModel.cs', 'r') as f:
    content = f.read()

# Fix HasSelectedGames and SelectedGamesTotalBytes which were mangled
content = re.sub(
r'\s*/// <summary>\n\s*/// Gets or sets the aggregate byte size of currently selected library items\.\n\s*/// </summary>\n\s*\[ObservableProperty\]\n\s*public partial long SelectedGamesTotalBytes \{ get; set; \}',
'''        /// <summary>
        /// Gets a value indicating whether any library items are currently selected.
        /// </summary>
        public bool HasSelectedGames => SelectedGamesCount > 0;

        /// <summary>
        /// Gets or sets the aggregate byte size of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial long SelectedGamesTotalBytes { get; set; }''', content)

# Fix SelectedGamesTotalPrice
content = re.sub(
r'\s*/// <summary>\n\s*/// Gets or sets the total calculated monetary price of currently selected library items\.\n\s*/// </summary>\n\s*\[ObservableProperty\]\n\s*\[NotifyPropertyChangedFor\(nameof\(SelectionSummary\)\)\]\n\s*public partial int SelectedGamesTotalPrice \{ get; set; \}',
'''        /// <summary>
        /// Gets or sets the total calculated monetary price of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial int SelectedGamesTotalPrice { get; set; }''', content)

# Fix SelectionSummary removal (it shouldn't be there, we use HasSelectedGames)
content = re.sub(
r'\s*/// <summary>\n\s*/// Gets a formatted summary string describing item count, total byte size, and price for selected items\.\n\s*/// </summary>\n\s*public string SelectionSummary => SelectedGamesCount == 0\n\s*\? string\.Empty\n\s*: \$\"\{SelectedGamesCount\} item\(s\) selected \\u2022 \{FormattingHelpers\.FormatBytes\(SelectedGamesTotalBytes\)\} \\u2022 Rs\. \{SelectedGamesTotalPrice\}\";',
'', content)

with open('Easy Copier/ViewModels/MainViewModel.cs', 'w') as f:
    f.write(content)
