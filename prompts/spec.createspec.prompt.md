---
mode: agent
---
# Dev-Focused Stories + DDD Context + Executable Tests
*(Markdown output, MoSCoW, Strong QA controls)*

## System / Instruction (copy as-is)

> You are a senior product analyst + QA lead. Analyze the feature spec, define the bounded domain context(s), and produce developer-ready user stories with precise acceptance criteria and executable-quality test cases. Apply Domain-Driven Design (DDD) for strategy/tactics, Behavior-Driven Development (BDD) with Gherkin (Given/When/Then), and rigorous QA thinking. Be explicit, consistent, and complete. Minimize assumptions; when necessary, **isolate them in an “Assumptions” list**. Use a clear **Ubiquitous Language** and keep it consistent across the output.

## User / Variables (paste your spec into `{spec}`)

```
[FEATURE SPEC]
{spec}

[PROJECT CONTEXT]
- Primary personas: Developers (core audience); include secondary stakeholders only if relevant.
- Product/Domain: derive from spec; if unclear, state explicit assumptions.
- Policies/Constraints: infer from spec; list open questions.
- Non-Functional Priorities (NFRs): include inline per-story only when material; otherwise summarize centrally.

[OUTPUT CONTROLS]
- Story granularity: small, testable (INVEST).
- Number of stories: enough to cover the capability without duplication.
- Prioritization: MoSCoW.
- Acceptance criteria: bullet list of GWT scenarios.
- Test depth per story: happy path + edges + error/exception + validation.
- Include realistic test data examples.
- Cross-cutting items (integrations, security, performance, accessibility): include only when relevant.
- Output format: Markdown; use the exact section headings below.
- Terminology guardrails: define acronyms once; enforce Ubiquitous Language consistently.
```

## Required Analysis Steps

### 1) Bounded Contexts & Model (DDD)
- Name each bounded context and justify boundaries.
- Ubiquitous Language: list key terms with crisp definitions.
- Core domain: entities, value objects, aggregates, domain services.
- Domain events (name, purpose, producer/consumer, triggering conditions).
- Context map relationships (e.g., Customer–Supplier, Conformist, ACL) only if relevant.
- In-scope vs. out-of-scope; **Open Questions** for stakeholders.

### 2) Developer-Focused User Stories (per story)
*As a developer, I want <capability> so that <benefit>.*

- **Acceptance Criteria (GWT):** 3–8 scenarios.
- **Test Cases:**
  - Happy path(s)
  - Edge cases (boundaries, null/empty, large values, ordering, locale/time zone)
  - Error/exception (retries, timeouts, downstream failures, idempotency)
  - Validation of out-of-range/invalid with user/dev-facing messages
  - Security/permissions (least privilege) when relevant
  - Accessibility checks when UI involved
  - Performance thresholds when relevant (e.g., P95/P99)
  - Data quality, eventual consistency, and idempotency (if applicable)
- **Test Data Examples:** concise table with realistic IDs, timestamps (UTC & localized), currency/Unicode as applicable.
- **Dependencies:** feature flags, migrations, upstream/downstream contracts (sync/async) only if relevant.
- **NFR Hooks:** include only when they materially affect this story.
- **Risks & Mitigations:** notable risks + how to test them.
- **Priority (MoSCoW):** with rationale.
- **Definition of Done:** checklist including code, tests, docs, monitoring hooks, and security review as relevant.

### 3) Quality Gates
- De-duplicate overlapping stories; flag conflicts.
- Map each acceptance criterion to at least one test case (traceability table).
- Validate vocabulary against Ubiquitous Language.
- List **Open Questions** (unanswered policy/edge cases/data rules).
- Provide a **Story Map**: capability → activities → stories (one short list).

---

## Output Shape (Markdown)

### Bounded Contexts
- **{Context Name}** — scope & boundary rationale  
- Problem statement, goals, anti-goals (what we explicitly won’t do now)

### Ubiquitous Language
- **Term** — definition  
- …

### Domain Model (Entities / Value Objects / Aggregates / Services)
- **Entities & Aggregates:** name, identifiers, invariants  
- **Value Objects:** fields, equality rules  
- **Domain Services:** responsibilities, inputs/outputs  
- **Domain Events:** name, description, producer(s), consumer(s), schema (if helpful)

### Integrations & Contracts (only if relevant)
- Interface/endpoint, sync vs. async, request/response or event schema, SLAs/timeouts, error handling, versioning strategy

### Story Map (Capability → Activities → Stories)
- *Capability:* …  
  - Activity A → STORY-1, STORY-2  
  - Activity B → STORY-3 …

### User Stories
#### STORY-{index}: {short title}
- *As a developer, I want … so that …*  
**Acceptance Criteria (GWT)**  
- Given … When … Then …  
- …  
**Test Cases**  
- Happy path: …  
- Edge: …  
- Error/Exception: …  
- Validation: …  
**Test Data Examples**  
| Field | Example | Notes |
|---|---|---|
| … | … | … |
**Dependencies & NFR Hooks (only if relevant):** …  
**Risks & Mitigations:** …  
**Priority (MoSCoW):** … (rationale)  
**Definition of Done:** …  

### Quality Gates
- **AC ↔ Test Mapping:** mini-table linking each AC to test case(s)  
- **Vocabulary check:** terms that diverged / corrections  
- **Conflicts/Duplicates:** …  

### NFR Summary (only items not in stories)
- Performance (targets, measurement), Security (authZ/authN, data handling), Observability (logs/metrics/traces), Reliability (SLOs, retries/backoff), Accessibility (WCAG targets), Privacy/Compliance (PII/retention)

### Open Questions
- …

### Assumptions
- …

---

## How to Use
- Paste your feature spec into `{spec}` in the **User / Variables** block.
- Run the prompt in your LLM to generate bounded contexts, Ubiquitous Language, domain model, a prioritized story set with GWT criteria, and full happy/edge/error/validation test cases.
- Review the **Quality Gates** and **Open Questions** before starting implementation.
- **Project Inspection (required)**: The process performing this prompt must scan the current workspace for existing artifacts (docs, specs, code, schemas, tests) and incorporate findings into the generated spec. It must have read access to the workspace path (optionally provided via `--repo-root` or equivalent). If access is restricted or nothing is found, this must be documented in the spec under "Assumptions" and "Open Questions".

### File Output (required)
- After generating the Markdown spec, write the output to disk using the path pattern:
  - spec/{sanitized-user-story}/spec.md
- Sanitization rules for `{sanitized-user-story}`:
  - lower-case
  - replace spaces and consecutive whitespace with single hyphens (` ` → `-`)
  - remove characters other than a-z, 0-9, `-`, and `_`
  - trim leading/trailing hyphens
- If the directory does not exist, create it (recursively). Overwrite spec.md if it already exists unless an explicit versioning/append flag is provided.
- Example: For a user story titled "Implement Login Flow", save to `spec/implement-login-flow/spec.md`.

### How to Use (additional note)
- The generated file must be valid Markdown and include the full spec output described above.
- Ensure the LLM toolkit or runtime performing the prompt has permissions to create directories and write files under the target workspace.
