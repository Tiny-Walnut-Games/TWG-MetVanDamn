import os

def generate_tree(path, prefix=""):
    try:
        entries = [e for e in os.listdir(path) if not e.endswith('.meta')]
    except PermissionError:
        return f"{prefix}[Permission Denied]\n"

    entries.sort()
    tree_str = ""
    for i, entry in enumerate(entries):
        full_path = os.path.join(path, entry)
        connector = "└── " if i == len(entries) - 1 else "├── "
        tree_str += f"{prefix}{connector}{entry}\n"
        if os.path.isdir(full_path):
            extension = "    " if i == len(entries) - 1 else "│   "
            tree_str += generate_tree(full_path, prefix + extension)
    return tree_str

# Start from current directory
start_path = "."
tree_output = generate_tree(start_path)

# Save to file
from pathlib import Path

out_path = Path(__file__).parent / "Assets" / "directory_tree.txt"
out_path.parent.mkdir(parents=True, exist_ok=True)

with open(out_path, "w", encoding="utf-8") as f:
    f.write(tree_output)

print(f"Directory tree saved to {out_path.resolve()}")

