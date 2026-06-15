# MinecraftData Agent Instructions

This project resolves paths into the vendored `minecraft-data` dataset.

## Rules

- Treat `minecraft-data` as upstream input data.
- Do not update or normalize vendored protocol data unless the task explicitly asks for an
  upstream-data update.
- Use prepared protocol access surfaces for packet examples and packet history.
- Open raw `protocol.json` only for loader/path bugs, parser bugs, or upstream discrepancy
  checks.
- Keep generated absolute path helpers out of source-control decisions unless the project
  explicitly changes generation.

