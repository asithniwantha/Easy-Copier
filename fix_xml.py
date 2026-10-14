import sys

filepath = 'Easy Copier/Views/TvAndFilmsTabView.xaml'

with open(filepath, 'r') as f:
    content = f.read()

content = content.replace('Search films & TV...', 'Search films &amp; TV...')

with open(filepath, 'w') as f:
    f.write(content)
