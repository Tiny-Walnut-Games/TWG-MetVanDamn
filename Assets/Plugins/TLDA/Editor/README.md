# The Scribe (TLDL Scribe Window)

The Scribe is a Unity EditorWindow for creating and managing TLDL (The Living Dev Log) entries with a GitBook-style navigator, form-driven authoring, a raw markdown editor, and a rendered preview.

- File: `Assets/Plugins/TLDA/Editor/TLDLScribeWindow.cs`
- Menu: Tools → Living Dev Agent → The Scribe
- Target: Unity Editor (C# 9, .NET Framework 4.7.1)

## Features
- GitBook-like navigator with folder selection and file operations (Open, Reveal, Duplicate)
- Single-click folder selection to set the active save directory
- Markdown file support with templates and issue creation workflow
- Form-based authoring for Discoveries, Actions, Technical details, Dependencies, Lessons, Next steps, References, DevTimeTravel, and Metadata
- Raw markdown editor with line wrapping and monospace font
- Rendered preview (lightweight markdown renderer)
- Image workflow: copy to `images/` subfolder and auto-insert markdown links
- Image visibility in navigator with small thumbnails (png/jpg/gif/bmp/tga)
- Autofill form when opening TLDL markdown created by the Scribe

## Getting Started
1. Open via menu: Tools → Living Dev Agent → The Scribe
2. Choose a documentation root folder (persisted between sessions)
3. Use the navigator to select the active folder and manage files
4. Author content using the Form or the Editor tab; Preview to verify
5. Create a TLDL file or an Issue from a template

## Navigator
- Left panel lists folders/files under the chosen root
- Click a folder label to make it the Active Directory (used when saving new files)
- Click a file to open (markdown/text); click an image to insert a markdown image link
- File actions:
  - Open (non-image files)
  - Reveal (show in OS file browser)
  - Duplicate (safe copy with incremented suffix)
- Stable alphabetical sorting for folders and files

## Images
- Add Image copies a selected image into an `images/` subfolder co-located with the current/open file
- Insert Image appends a `![image](images/...)` link at the end of the Editor buffer
- Preview renders inline images; navigator shows small thumbnails for discovered image files

## Templates and Issues
- The Issue Creator panel reads `templates/comments/registry.yaml` at the project root
- Load a template into the Editor, or create a new Issue file under `TLDL/issues/`
- A `Readme.md` is auto-created the first time in that directory for guidance

## Known Limitations
- Markdown renderer is minimal (headings, lists, code blocks, links, images, checkboxes)
- Editor TextArea has no cursor position; image links are appended to the end

## Recommended Screenshots
Drop your screenshots into:
`Assets/Plugins/TLDA/Editor/Images/`

Suggested captures and filenames (update the links if you use different names):
- Overview: ![The Scribe - Overview](Images/scribe-overview.png)
- Navigator and Selection: ![The Scribe - Navigator](Images/scribe-navigator.png)
- Form Authoring: ![The Scribe - Form](Images/scribe-form.png)
- Raw Editor: ![The Scribe - Editor](Images/scribe-editor.png)
- Rendered Preview: ![The Scribe - Preview](Images/scribe-preview.png)
- Image Thumbnails in Navigator: ![The Scribe - Image Thumbnails](Images/scribe-images.png)

## Tips
- Use the Generate From Form → Editor action to sync the form into raw markdown
- Use Save Raw or Save Raw As to persist the current Editor buffer
- The active folder is shown at the bottom of the navigator panel

## Troubleshooting
- If templates are not detected, ensure `templates/comments/registry.yaml` exists at the project root
- If images do not render, verify the relative link and that the file exists under the resolved `images/` directory
- If the navigator looks empty, re-select the root or ensure the chosen folder exists on disk
