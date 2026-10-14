import sys

filepath = 'Easy Copier/ViewModels/AppsTabViewModel.cs'

with open(filepath, 'r') as f:
    content = f.read()

if "using System.Collections.Generic;" not in content:
    content = "using System.Collections.Generic;\n" + content

with open(filepath, 'w') as f:
    f.write(content)
