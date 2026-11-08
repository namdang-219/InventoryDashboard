---
trigger: always_on
description: "Frontend Engineering Standards for React 19, TypeScript, Redux Toolkit, React Router, SCSS, and Vitest."
---

# Frontend Engineering Standards (ReactJS)

## Tech Stack
- **Framework & Build**: React 19+ (functional components + hooks only), Vite, TypeScript (`strict: true`, `jsx: "react-jsx"`)
- **UI & Styling**: Custom SCSS design system with CSS custom properties (`--iid-*` theme tokens for light/dark modes), `clsx` for conditional class composition, Material Symbols Outlined icons
- **State Management**: Redux Toolkit (`configureStore`, `createSlice`, `createAsyncThunk`) + `react-redux` typed hooks (`useAppDispatch`, `useAppSelector`) for global/shared state · React hooks (`useState`, `useReducer`, `useMemo`, `useCallback`) for local component state
- **Routing**: React Router v7 (`createBrowserRouter`, `RouterProvider`, route guard wrappers such as `AuthGuard`)
- **HTTP & Realtime**: `axios` (`http` instance in `core/services/api.service.ts`) with interceptors (`auth.interceptor.ts`) and typed helpers (`get<T>`, `post<T>`, `put<T>`, `del<T>`) · `@microsoft/signalr` via `useRealtimeEvent` hook
- **Testing**: Vitest + React Testing Library (`@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom`) + `jsdom`

---

## Architecture

### Container / Presentational
- **Container** (`features/<feature>/pages/`): invokes domain services/hooks (`useAuth`, `useDealerships`, `useRealtimeEvent`), dispatches Redux actions/thunks, orchestrates data fetching and side effects; minimal layout markup delegating to child components.
- **Presentational** (`features/<feature>/components/` or `shared/components/`): pure UI driven by typed `props` (`interface <Name>Props`) and callback props (`onSave`, `onClose`, `onSelect`); no direct HTTP calls or unrelated store dispatches.

### Folder Layout (feature-based, one folder per component)
```
src/app/
├── core/
│   ├── guards/        # Route guard components (e.g. auth.guard.tsx)
│   ├── interceptors/  # Axios request/response interceptors
│   ├── models/        # Domain interfaces, DTOs, types (*.model.ts)
│   ├── services/      # API clients, domain services, custom domain hooks (*.service.ts)
│   ├── store/         # Redux Toolkit store, slices (*.slice.ts), typed hooks.ts
│   └── utils/         # Pure helper functions (error-extractor, navigation)
├── features/<feature>/
│   ├── pages/         # Routed smart/container page components
│   └── components/    # Feature-specific UI & modal components
├── layout/            # App shell, header, sidebar components
└── shared/
    ├── components/    # Reusable pure UI components (stat-card, status-badge, etc.)
    └── pipes/         # Pure formatting utilities (formatCurrency, daysAgo)

component-name/
├── component-name.tsx
├── component-name.scss
├── component-name.spec.tsx
└── index.ts           # optional barrel export
```

### Component & Hook Conventions
- **Functional components only**: Use named exports (e.g. `export function VehicleTableComponent(props: VehicleTableProps)`), never class components.
- **Class naming**: Root element class uses kebab-case with `iid-` prefix (e.g. `iid-stat-card`, `iid-vehicle-list`); compose dynamic classes with `clsx()`.
- **Props**: Define and export an explicit `<ComponentName>Props` interface; destructure props with sensible default values in the function signature.
- **Lists & Conditional Rendering**: Use declarative JSX (`&&`, ternary, early return) and `.map()` with a stable unique `key` (`key={item.id}`) — never use array index as `key` for dynamic lists.
- **Hooks & Effects**: Follow Rules of Hooks; use `useCallback` and `useMemo` to stabilize references passed to effects or children; always clean up subscriptions, timers, and stale async requests (via request ID ref or `AbortController`) in `useEffect` cleanup.
- **Forms**: Controlled form inputs with typed state, explicit submit handlers (`e.preventDefault()`), inline validation feedback, and disabled submit state while saving.

---

## Styling
- External co-located `*.scss` only — **no inline** `style={{ ... }}` (except dynamic runtime metrics such as chart bar widths/percentages).
- Theme exclusively with CSS custom properties (`--iid-*` tokens in `src/styles.scss`) supporting `[data-theme='light']` and `[data-theme='dark']`.
- Global resets, typography, design tokens, and shared utility classes (`.iid-card`, `.iid-btn`, `.iid-input`, `.iid-modal-backdrop`) belong in `src/styles.scss` only.
- Responsive layouts using SCSS flexbox/grid and media queries.

---

## TypeScript & Quality
- `strict: true`, `noImplicitReturns: true`, `noFallthroughCasesInSwitch: true`; no `any`, no `@ts-ignore` / `@ts-expect-error`.
- Use `readonly` on immutable array/object properties and `as const` for literal unions.
- Explicitly handle `loading`, `error`, and `empty` states in every data-driven view (`LoadingSpinnerComponent`, `EmptyStateComponent`, error banners/toasts).
- SRP — keep components focused (< 150–200 lines); extract subcomponents or custom hooks when growing.

---

## Do
Co-locate `.tsx/.scss/.spec.tsx` · separate container pages vs. presentational components · Redux Toolkit slices + `useAppDispatch`/`useAppSelector` for shared state, `useState`/`useMemo` for local state · stable `key` on `.map()` · clean up `useEffect` and guard against stale async responses · compose classes with `clsx` · run `npm run lint` (`tsc --noEmit`) and `npm test` (`vitest run`).

## Don't
Class components · static inline `style={{ ... }}` · `any` or `@ts-ignore` · array index as `key` on dynamic lists · direct HTTP calls inside presentational components · mutating state outside Redux Toolkit Immer reducers · unhandled `.then()/.catch()` or state updates on unmounted/stale requests.

