import sys

filepath = 'Easy Copier/ViewModels/MainViewModel.cs'

with open(filepath, 'r') as f:
    content = f.read()

content = content.replace("[NotifyPropertyChangedFor(nameof(EmptyOsImagesMessage))]\n        [ObservableProperty]\n        public partial string GamesSearchText",
                          "[NotifyPropertyChangedFor(nameof(EmptyOsImagesMessage))]\n        public partial string GamesSearchText")

with open(filepath, 'w') as f:
    f.write(content)
