import re

with open('Easy Copier/ViewModels/MainViewModel.cs', 'r') as f:
    content = f.read()

# Let's remove [NotifyPropertyChangedFor(nameof(SelectionSummary))] from SelectedGamesCount too since we removed SelectionSummary
content = re.sub(
r'\s*\[ObservableProperty\]\n\s*\[NotifyPropertyChangedFor\(nameof\(SelectionSummary\)\)\]\n\s*public partial int SelectedGamesCount \{ get; set; \}',
'''
        [ObservableProperty]
        public partial int SelectedGamesCount { get; set; }''', content)

# Also fix the weird formatting around SelectedGamesTotalBytes and SelectedGamesTotalPrice
content = re.sub(
r'public partial long SelectedGamesTotalBytes \{ get; set; \}        /// <summary>',
'''public partial long SelectedGamesTotalBytes { get; set; }

        /// <summary>''', content)

# Check if there are any other left over `SelectionSummary` stuff
content = re.sub(
r'\s*/// <summary>\n\s*/// Gets a formatted summary string describing item count, total byte size, and price for selected items\.\n\s*/// </summary>\n\s*public string SelectionSummary => SelectedGamesCount == 0\n\s*\? string\.Empty\n\s*: \$\"\{SelectedGamesCount\} item\(s\) selected \\u2022 \{FormattingHelpers\.FormatBytes\(SelectedGamesTotalBytes\)\} \\u2022 Rs\. \{SelectedGamesTotalPrice\}\";',
'', content)

with open('Easy Copier/ViewModels/MainViewModel.cs', 'w') as f:
    f.write(content)
