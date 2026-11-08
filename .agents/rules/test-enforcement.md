---
trigger: always_on
description: "Test Enforcement rules requiring unit tests to be updated and passing for all backend and React frontend changes."
---

# Test Enforcement

## Rule
When modifying backend layers (`src/IID.Application`, `src/IID.Domain`, `src/IID.Infrastructure`, `src/IID.Services`) or the React frontend (`src/IID.FrontendApp`), you MUST update or add the corresponding unit tests.

## Mapping
| Source | Tests |
|--------|-------|
| `src/IID.Domain/` | `tests/IID.Domain.Tests/` |
| `src/IID.Application/` | `tests/IID.Application.Tests/` |
| `src/IID.Infrastructure/` | `tests/IID.Infrastructure.Tests/` |
| `src/IID.Services/IID.Api/` | `tests/IID.Api.Tests/` |
| `src/IID.Services/IID.ReportGenerator/` | `tests/IID.ReportGenerator.Tests/` |
| `src/IID.FrontendApp/src/` | Co-located `*.spec.ts` / `*.spec.tsx` (Vitest + React Testing Library) |

## Workflow
1. **Before**: Check if tests exist; if not, create them first
2. **After (Backend)**: Run `dotnet build` + `dotnet test tests/IID.<Layer>.Tests/` and fix failures
3. **After (Frontend)**: Run `npm run lint` + `npm test` (`vitest run`) inside `src/IID.FrontendApp` and fix failures
4. **New functionality**: Add tests covering the new behavior
5. **Verify**: Build succeeds + all tests pass (0 failures) before completing the change

## Standard
- **Domain**: Test entities, value objects, domain services, invariants
- **Application**: Test all handlers and validators with mocked dependencies
- **Infrastructure**: Test all services and configurations
- **Frontend (ReactJS)**: Test components, hooks, slices, interceptors, and utilities using Vitest and React Testing Library

All tests must pass (0 failures) before marking a task complete.

