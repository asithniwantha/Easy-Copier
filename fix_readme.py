with open("README.md", "r") as f:
    content = f.read()

content = content.replace("""- View a detailed history of past copy operations.
- Track success, failures, and transfer statuses.
- Generate and export reports (e.g., CSV) containing historical transfer data and logs.""", """- View a detailed history of past copy operations.
- Track success, failures, and transfer statuses.
- Generate and export reports (e.g., CSV) containing historical transfer data and logs.
- Automatic background updates via Velopack using GitHub Releases.""")

with open("README.md", "w") as f:
    f.write(content)
