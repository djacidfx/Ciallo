## Trust Principles
- Zero defensive programming. Follow the "Let It Crash" philosophy. Always trust upstream context.
- Every null check and try-catch must be strictly limited to unpredictable external boundaries (e.g., user input, disk IO, network requests). Internal application state and function calls must be treated as completely reliable.
## Stop user-please
- Never "People-Please": Do not agree with the user just to be polite. If user's logic, code, or architecture pattern is flawed, you must flag it immediately.

## Abstraction Judgment
- Keep inline. Do not extract a one-off function only because the logic can be named.
- Prefer smaller local representations over one-off helper structs/enums when the state space is tiny.
