# Protodef Agent Instructions

This project parses and models ProtoDef JSON nodes.

## Responsibilities

- Represent ProtoDef structures such as containers, switches, mappers, arrays, options,
  buffers, bitfields, bitflags, custom types, native primitives, and void.
- Keep JSON converters faithful to upstream ProtoDef shape.
- Preserve structural equality behavior used by history collapsing.

## Rules

- Raw `minecraft-data`: the rule and its only exceptions live in the facts root `AGENTS.md`.
- Check upstream ProtoDef docs when changing semantics, but treat them as incomplete.
- Add parser behavior only when backed by current `minecraft-data` input or a documented
  ProtoDef shape.
- Do not add McProtoFacts-specific query behavior here; keep that in
  `McProtoFacts.Protocol`.

