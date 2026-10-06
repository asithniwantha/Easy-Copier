import re
import os

files_to_patch = [
    'Easy Copier/Views/GamesTabView.xaml.cs',
    'Easy Copier/Views/OsImagesTabView.xaml.cs',
    'Easy Copier/Views/TvAndFilmsTabView.xaml.cs'
]

for file_path in files_to_patch:
    with open(file_path, 'r') as f:
        content = f.read()

    # We want to remove the conflict markers and keep what's in HEAD
    content = re.sub(
        r'<<<<<<< HEAD\n(.*?)\n=======\n.*?\n>>>>>>> [^\n]*\n',
        r'\1\n',
        content,
        flags=re.DOTALL
    )

    with open(file_path, 'w') as f:
        f.write(content)
