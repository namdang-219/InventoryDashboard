
---
trigger: always_on
description: "Use AgentMemory before rediscovering existing project knowledge."
---

# AgentMemory — Memory First

## Before starting a task

For questions about existing architecture, workflows, testing conventions, or previous decisions:

1. FIRST call `memory_smart_search` or `memory_recall`.
2. Wait for and inspect the tool result before exploring the repository.
3. If relevant memory exists, use it as the initial context.
4. Do not repeat broad repository discovery when memory already provides the answer.
5. Verify specific source files only when necessary.

## After completing a task

1. Identify durable technical knowledge learned.
2. Save new workflows, decisions, testing conventions, and bug lessons with `memory_save`.
3. Prefer project-scoped memories.
4. Avoid duplicate memories, trivial details, and secrets.
5. Verify successful saves through MCP results.

## Reliability

- Current source code remains authoritative for implementation and debugging.
- If recall fails, fall back to source inspection.
- Never claim a memory was recalled or saved without tool confirmation.
