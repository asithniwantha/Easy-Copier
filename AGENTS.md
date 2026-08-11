# Repository Agent Guidelines

This file defines the operational standards for all AI agents and contributors working on the **Easy-Copier** project. Please adhere to these rules for all code generation, refactoring, or suggestions.

## 1. Documentation Standard
- **Inline Comments**: Every new function or complex logic block must include clear comments explaining the *why* (intent) rather than just the *what*.
- **README Updates**: If a new feature or structural change is introduced, update the `README.md` immediately to reflect usage, dependencies, or configuration changes. Also update `FEATURES.md` to document any new features.
- **Docstrings**: All public methods and classes must have clear, concise docstrings (following the project's language convention).

## 2. Architecture & Design
- **Architecture**: MVVM tool kit
- **Modularity**: Prioritize clean code principles (DRY, KISS). Avoid deep nesting where possible.
- **Safety**: Ensure error handling is robust. Always assume external inputs (like file paths or clipboard data) might be malformed.

## 3. Implementation Process
- **Drafting**: When proposing changes, provide a brief summary of *why* this architectural approach was chosen.
