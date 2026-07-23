# McProtoFacts.Cli Agent Instructions

This project is the normal stdout command-line surface for agents and scripts.

## Rules

- Keep stdout reserved for the requested payload.
- Write errors and usage to stderr.
- Preserve documented exit codes.
- Keep the CLI thin over `ProtocolDataLoader`, `ProtocolQueryService`, and
  `ProtocolGraphBuilder`.
- Prefer `tools\mcproto-facts.cmd` for manual checks because it builds first and then runs
  `dotnet run --no-build`.

## Smoke Checks

```powershell
tools\mcproto-facts.cmd stats --format json
tools\mcproto-facts.cmd packet play.toClient.keep_alive --format toon
tools\mcproto-facts.cmd packet definitely_missing_packet --format json
```

