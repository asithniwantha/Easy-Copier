import os
import time
from config import BASE_PATH
from requirements import fetch_requirements_step
from covers import download_covers_step


def cleanup_step(base_path=BASE_PATH):
    """Step 4: Clean up old placeholder files."""
    print("\n" + "=" * 70)
    print("STEP 4: CLEANING UP OLD FILES")
    print("=" * 70 + "\n")

    if not os.path.exists(base_path):
        print("✗ Base path does not exist")
        return

    game_folders = [f for f in os.listdir(base_path)
                    if os.path.isdir(os.path.join(base_path, f))]

    print(f"Found {len(game_folders)} game folders\n")

    removed = 0

    for idx, game_folder in enumerate(game_folders, 1):
        game_path = os.path.join(base_path, game_folder)
        info_file = os.path.join(game_path, "cover_image_info.txt")

        if os.path.exists(info_file):
            try:
                os.remove(info_file)
                print(f"[{idx}/{len(game_folders)}] {game_folder}... ✓")
                removed += 1
            except Exception:
                pass

    print(f"\n✓ Removed: {removed} old info files")


def select_directory(start_path="."):
    """Text-based menu to navigate and select a directory."""
    current_path = os.path.abspath(start_path)

    while True:
        print("\n" + "=" * 70)
        print(f"CURRENT DIRECTORY: {current_path}")
        print("=" * 70)

        try:
            # Get list of directories
            items = os.listdir(current_path)
            directories = [d for d in items if os.path.isdir(
                os.path.join(current_path, d))]
            directories.sort()

            for i, d in enumerate(directories, 2):
                print(f"{i}: {d}")

            print("\n" + "=" * 70)  
            print(f"CURRENT DIRECTORY: {current_path}")
            print("=" * 70)
            print("0: [Select this directory]")
            print("1: [Go up to parent directory (..)]")
            print("\nEnter number to navigate, or 'c' to cancel and use default:")
            
            choice = input("> ").strip().lower()

            
            if choice == 'c':
                return None

            if choice == '0':
                return current_path

            if choice == '1':
                current_path = os.path.abspath(
                    os.path.join(current_path, os.pardir))
                continue
            
            
            
            # Try to navigate to chosen directory
            try:
                idx = int(choice)
                if 2 <= idx < len(directories) + 2:
                    selected_dir = directories[idx - 2]
                    current_path = os.path.join(current_path, selected_dir)
                else:
                    print("Invalid selection. Try again.")
            except ValueError:
                print("Invalid input. Please enter a number.")
                
            # Print options
            print("0: [Select this directory]")
            print("1: [Go up to parent directory (..)]")

        except PermissionError:
            print(f"Permission denied to access {current_path}.")
            current_path = os.path.abspath(
                os.path.join(current_path, os.pardir))
            time.sleep(1)


def main():
    """Main function - Menu workflow."""
    print("=" * 70)
    print("PC GAMES DATABASE BUILDER")
    print("=" * 70)
    print()

    print("Please select the base directory for game folders:")
    selected_path = select_directory()

    if selected_path:
        base_path = selected_path
    else:
        base_path = BASE_PATH

    print(f"\nUsing base path: {base_path}")

    while True:
        print("\n" + "=" * 70)
        print("MAIN MENU")
        print("=" * 70)
        print("1. Download system requirements")
        print("2. Download cover images")
        print("3. Clean up old placeholder files")
        print("4. Exit")
        print()

        choice = input("Select an option (1-4): ").strip()

        if choice == '1':
            fetch_requirements_step(base_path)

        elif choice == '2':
            download_covers_step(base_path)

        elif choice == '3':
            cleanup_step(base_path)

        elif choice == '4':
            print("\nExiting program. Goodbye!")
            break

        else:
            print("\nInvalid choice. Please select 1-4.")


if __name__ == "__main__":
    main()
