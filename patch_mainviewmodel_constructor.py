import re

with open('Easy Copier/ViewModels/MainViewModel.cs', 'r') as f:
    content = f.read()

# Fix fields
content = re.sub(
r'<<<<<<< HEAD\n=======\n\s*private readonly IFolderPickerService _folderPickerService;\n>>>>>>> [^\n]*\n',
'        private readonly IFolderPickerService _folderPickerService;\n', content)

# Fix constructor args
content = re.sub(
r'<<<<<<< HEAD\n=======\n\s*IFolderPickerService folderPickerService,\n>>>>>>> [^\n]*\n',
'            IFolderPickerService folderPickerService,\n', content)

with open('Easy Copier/ViewModels/MainViewModel.cs', 'w') as f:
    f.write(content)
