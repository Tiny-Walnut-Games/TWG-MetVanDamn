# 💪🏼 ForceNewestCSharp

**Enforce the latest C# language version and nullable reference types in any Unity project — with developer approval.**

This Unity Editor utility ensures your project is always compiled with the newest available C# features (`<LangVersion>latest</LangVersion>`) and `#nullable enable` by injecting a `Directory.Build.props` file at the project root.
It also offers to create `.csc.rsp` files for non‑Unity assemblies so they compile with the same language level.

*"In the ancient scrolls of code, where semicolons are runes and functions are incantations, remember: a well-cast spell compiles without error, but true mastery lies in debugging the dragon's breath of runtime exceptions."* — The Archmage of Algorithms 🍑✨

---

## ✨ Features

- **Automatic Language Level Enforcement**
  On script reload, writes a `Directory.Build.props` file to the project root with:
  ```xml
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  ```
  This applies to all assemblies in the project.

- **Developer‑Agency `.csc.rsp` Creation**
  Scans for `.asmdef` files without a matching `.csc.rsp` and prompts you to create them.
  Each `.rsp` file contains:
  ```
  -langversion:latest
  ```

- **Safe & Non‑Intrusive**
  - Skips Unity‑built‑in assemblies.
  - Skips malformed `.asmdef` files without aborting the scan.
  - Prompts before creating any `.rsp` files — no silent changes.

- **Validation Log**
  On load, logs the enforced language level and nullable status.

![image](https://gist.github.com/user-attachments/assets/f4878b0a-3cc2-4aae-92c0-5cb0e68e5a6c)

---

## 📂 Installation

1. Copy `ForceNewestCSharp.cs` into an `Editor/` folder in your Unity project.
2. Save and let Unity recompile scripts.

---

## 🛠 How It Works

- **On Script Reload** (`[DidReloadScripts]`):
  - Ensures `Directory.Build.props` exists and matches the enforced content.
  - Logs the enforced settings.
  - Calls the `.csc.rsp` prompt.

- **On Editor Load** (`[InitializeOnLoadMethod]`):
  - Logs a confirmation that `latest` is enforced.

- **`.csc.rsp` Prompt**:
  - Lists all non‑Unity assemblies missing `.csc.rsp`.
  - Asks if you want to create them.
  - Writes each file with `-langversion:latest` if approved.

---

## 🚦 Usage

![image](https://gist.github.com/user-attachments/assets/b43452b9-bc44-4928-8012-be5ab41a3bed)

*The dialogue you will be greeted with if any *.csc.rsp files are missing.*

I have found that keeping an rsp file with each asmdef is a great way to ensure the newest language.

- **First Run**:
  The script will create `Directory.Build.props` automatically.
  If missing `.csc.rsp` files are found, you’ll be prompted to create them.

- **Subsequent Runs**:
  Only updates files if content changes.
  `.csc.rsp` prompt will only appear if new missing files are detected.

---

## 📜 Example Console Output

```
🧙 Injected Directory.Build.props into project root to enforce C# latest + nullable.
📜 LangVersion injected: latest
📜 Nullable enabled: true
📜 Props path: /path/to/project/Directory.Build.props
✅ All non‑Unity assemblies already have .csc.rsp files. No action needed.
🔍 C# Language Level: 'latest' enforced via Directory.Build.props
```

---

## ⚠ Notes

- This script **does not** silently create `.csc.rsp` files — you must approve them.
- Unity limits which versions are acceptable and `latest` will always choose the highest supported version.
- If you use version control, commit the generated `Directory.Build.props` and any `.csc.rsp` files so the settings apply to all developers.

---

## 📄 License

MIT — free to use, modify, and distribute.
You may find a copy of the license at [https://opensource.org/licenses/MIT](https://opensource.org/licenses/MIT)

---
