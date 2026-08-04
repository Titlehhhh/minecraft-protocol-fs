# Design

> Note (2026-07-23): LLM generation and model-settings surfaces described below were
> removed from the backend — McProtoFacts is now a facts-only server. Generation
> screens and settings panes in this document are historical; the inspection surfaces
> (protocol panes, chunks, graph, usage) remain accurate.

## Source of truth
- Status: Active draft
- Last refreshed: 2026-06-19
- Primary product surfaces: MCP Server Web UI, protocol source panes, protocol generation screen, RAG/AI preparation workbench, graph, usage explorer, settings.
- Evidence reviewed: `AI_CONTEXT.md`, `AGENTS.md`, `src/McpServer/AGENTS.md`, `src/McpServer/ClientApp/src/App.tsx`, `Main.tsx`, `Sidebar.tsx`, `ChunksPanel.tsx`, `ConfigPane.tsx`, stores, and `index.css`.

## Brand
- Personality: technical, compact, inspectable, operator-focused.
- Trust signals: visible selected packet/type, deterministic controls, explicit vector status, no hidden AI execution, validation-ready output surfaces.
- Avoid: generic chat UI, marketing layout, decorative hero treatment, hidden context packing, raw `minecraft-data` as the normal inspection surface.

## Product goals
- Goals: make protocol facts easy to inspect, make RAG chunks searchable and debuggable, prepare for small AI canonicalization jobs, keep model settings repeatable.
- Non-goals: replace a general chat client, run full protocol conversion through one chat, make AI proposals source of truth without validation.
- Success signals: a user can select a packet/type, see schema/chunks/related facts, test RAG retrieval, and understand what would be sent to a future F# generator.

## Personas and jobs
- Primary personas: McProtoNet maintainer, protocol tooling developer, AI workflow operator.
- User jobs: inspect packet/type evolution, discover related types, prepare context for generator jobs, tune model/RAG settings, validate conversion candidates.
- Key contexts of use: local development, LM Studio/OpenRouter-compatible endpoints, Qdrant-backed RAG, future batch canonicalizer runs.

## Information architecture
- Primary navigation: Protocol, Workbench, Graph, Usage, Settings.
- Core routes/screens: protocol generation and schema view, AI preparation workbench, protocol graph, usage/dependency explorer, model/settings screen.
- Content hierarchy: source selection is global; each main screen decides how that selected owner is used.

## Design principles
- Principle 1: keep facts, context, generated output, and settings in separate visible surfaces.
- Principle 2: every AI-adjacent action must expose inputs and readiness before execution.
- Tradeoffs: dense operational UI is preferred over spacious editorial presentation; fewer generic cards, more stable panels and tables.

## Visual language
- Color: restrained dark operational palette with blue for active navigation, green for ready state, yellow for warning, red for failure.
- Typography: system UI for controls, monospace for protocol ids and code.
- Spacing/layout rhythm: compact 8-16px spacing, stable panels, no nested decorative cards.
- Shape/radius/elevation: 6-8px radius, borders over shadows, minimal elevation.
- Motion: only small loading spinners and hover states.
- Imagery/iconography: text-first technical UI; icons are optional and should not replace clear packet/type ids.

## Components
- Existing components to reuse: packet/type/native source panes, schema panel, graph panel, usage panel, config sections, chunk cards.
- New/changed components: app shell navigation, global selected owner chip, Workbench hero/status strip, settings page wrapper.
- Variants and states: selected owner, RAG enabled/disabled, near-token-limit warning, future job readiness states.
- Token/component ownership: `index.css` currently owns styling; do not introduce a new design system dependency until repeated component extraction is justified.

## Accessibility
- Target standard: pragmatic keyboard/focus accessibility for local developer tooling.
- Keyboard/focus behavior: buttons and inputs must remain native controls; Enter should run local lookup/search where expected.
- Contrast/readability: dark UI contrast must stay readable for dense tables/code.
- Screen-reader semantics: main navigation uses `nav`, sidebar uses `aside`, main content uses `main`.
- Reduced motion and sensory considerations: avoid unnecessary animation beyond loading feedback.

## Responsive behavior
- Supported breakpoints/devices: desktop-first, usable down to narrow laptop widths.
- Layout adaptations: right-side Workbench panels stack below main content under 1100px; header wraps instead of clipping.
- Touch/hover differences: no hover-only required actions.

## Interaction states
- Loading: inline spinner and disabled action buttons.
- Empty: dashed empty states for chunks/search results.
- Error: red bordered message with backend error text.
- Success: green confirmation message and ready chips.
- Disabled: explicit disabled controls when vectors/config are missing.
- Offline/slow network, if applicable: REST errors surface in the current panel; no silent background AI work.

## Content voice
- Tone: direct engineering labels, no hype.
- Terminology: use owner, packet, type, chunk, vector, job, proposal, validation consistently.
- Microcopy rules: do not describe features at length inside the app; use short labels and visible state.

## Implementation constraints
- Framework/styling system: React 18, Zustand, Vite, plain CSS, existing REST endpoints.
- Design-token constraints: current CSS variables are absent; keep colors centralized by class reuse until tokens are worth extracting.
- Performance constraints: lists are capped by existing logic; avoid rendering raw giant protocol files in the browser.
- Compatibility constraints: read-only protocol access must work without OpenRouter or vector services.
- Test/screenshot expectations: run client build after UI changes; use browser smoke testing for major layout changes when a dev server is running.

## Open questions
- [ ] Should future AI jobs be stored in files, SQLite, or server-side JSONL run directories?
- [ ] Should provider/model discovery be a Settings subpanel or a separate Models screen?
- [ ] What is the first dry-run job schema for F# canonicalization?
