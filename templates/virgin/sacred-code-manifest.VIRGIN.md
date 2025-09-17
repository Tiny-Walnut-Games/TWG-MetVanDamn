# Sacred Code Manifest (Virgin Template)

Purpose

- Provide a clean, minimal manifest to describe code categories and handling rules.

Symbols & Classifications

- CORE: Critical runtime systems, must remain stable.
- EDITOR: Editor tooling and developer experience scripts.
- SCRIPTS: Game logic and features.
- DATA: Static data files, configs, and schemas.
- TESTS: Unit/integration tests; can be run in CI.

Rules of Handling

- CORE changes require review and a smoke test.
- EDITOR changes should not break batch mode; guard with #if UNITY_EDITOR.
- SCRIPTS follow project coding standards and null-safety.
- DATA files should be validated and schema-checked when applicable.
- TESTS accompany behavioral changes where practical.

Safety & Backups

- Commit small; use feature branches for risky work.
- Snapshot configs before migrations.
- Record notable incidents in the living dev log (TLDL).

Attribution

- This is a neutral, project-agnostic manifest template.
