import re

with open('Easy Copier/ViewModels/MainViewModel.cs', 'r') as f:
    content = f.read()

# Make sure we add HasSelectedGames back. Wait, let's see where SelectedGamesTotalBytes is defined.
content = re.sub(
r'\s*/// <summary>\n\s*/// Gets or sets the aggregate byte size of currently selected library items\.\n\s*/// </summary>\n\s*\[ObservableProperty\]\n\s*\[NotifyPropertyChangedFor\(nameof\(SelectionSummary\)\)\]\n\s*public partial long SelectedGamesTotalBytes \{ get; set; \}',
'''
        /// <summary>
        /// Gets a value indicating whether any library items are currently selected.
        /// </summary>
        public bool HasSelectedGames => SelectedGamesCount > 0;

        /// <summary>
        /// Gets or sets the aggregate byte size of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial long SelectedGamesTotalBytes { get; set; }''', content)

with open('Easy Copier/ViewModels/MainViewModel.cs', 'w') as f:
    f.write(content)
