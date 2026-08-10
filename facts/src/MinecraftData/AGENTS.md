# MinecraftData Agent Instructions

This project resolves paths into the vendored `minecraft-data` dataset.

## Rules

- Treat `minecraft-data` as upstream input data.
- Do not update or normalize vendored protocol data unless the task explicitly asks for an
  upstream-data update.
- Raw `minecraft-data`: the rule and its only exceptions live in the facts root `AGENTS.md`.
- Keep generated absolute path helpers out of source-control decisions unless the project
  explicitly changes generation.

