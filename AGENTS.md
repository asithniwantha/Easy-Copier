# Repository Agent Guidelines

This file defines the operational standards for all AI agents and contributors working on the **Easy-Copier** project. Please adhere to these rules for all code generation, refactoring, or suggestions.

## 1. Documentation Standard
- **Inline Comments**: Every new function or complex logic block must include clear comments explaining the *why* (intent) rather than just the *what*.
- **README Updates**: If a new feature or structural change is introduced, update the `README.md` immediately to reflect usage, dependencies, or configuration changes. Also update `FEATURES.md` to document any new features.
- **Docstrings**: All public methods and classes must have clear, concise docstrings (following the project's language convention).
- README.md and FEATURES.md should be read and corrected if there is any grammar issue. then format the document in a clear and concise manner. Use proper headings, bullet points, and code blocks where necessary to enhance readability.if wanted use emojis to make the document more engaging and visually appealing. Ensure that the content is accurate, up-to-date, and easy to understand for both technical and non-technical readers.
- **Automatic Markdown Updates**: Always automatically update `.md` files whenever code is updated or new features are added. Leverage your active memories of recent code changes to keep documentation files fully up to date on every run.

## 2. Architecture & Design
- **Architecture**: MVVM tool kit
- **Modularity**: Prioritize clean code principles (DRY, KISS). Avoid deep nesting where possible.
- **Safety**: Ensure error handling is robust. Always assume external inputs (like file paths or clipboard data) might be malformed.

## 3. Implementation Process
- **Drafting**: When proposing changes, provide a brief summary of *why* this architectural approach was chosen.
