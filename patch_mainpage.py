import re

with open('Easy Copier/Views/MainPage.xaml', 'r') as f:
    content = f.read()

content = re.sub(
r'<<<<<<< HEAD\n=======\n(.*?)\n>>>>>>> [^\n]*\n',
r'\1\n',
content,
flags=re.DOTALL
)

with open('Easy Copier/Views/MainPage.xaml', 'w') as f:
    f.write(content)
