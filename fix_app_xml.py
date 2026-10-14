import sys

filepath = 'Easy Copier/Views/AppsTabView.xaml'

with open(filepath, 'r') as f:
    content = f.read()

content = content.replace('<AutoSuggestBox Grid.Row="0"', '<AutoSuggestBox x:Name="SearchBox" Grid.Row="0"')

# Wait, the error is missing SearchBox but earlier my patch for apps, tv, osimages was exactly the same string.
# Let's check the patch: <AutoSuggestBox x:Name="SearchBox" Grid.Row="0" Margin="0,0,0,8"

# Let's see what is actually in AppsTabView.xaml
with open(filepath, 'w') as f:
    f.write(content)
