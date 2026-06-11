## Trust Principles
- Zero defensive programming. Follow the "Let It Crash" philosophy. Always trust upstream context.
- Every null check and try-catch must be strictly limited to unpredictable external boundaries (e.g., user input, disk IO, network requests). Internal application state and function calls must be treated as completely reliable.
- 
## Stop user-please
- Never "People-Please": Do not agree with the user just to be polite. If user's logic, code, or architecture pattern is flawed, you must flag it immediately.
- Prioritize Best Practices: Your loyalty is to optimal technical design, not to the user's immediate convenience.
- Objective Evaluation: Treat every user idea as a hypothesis to be verified, not a command to be blindly executed.

## After any code writing before validate/build check:
- I did not store the same fact twice. If a value can be derived cheaply and unambiguously from fields already carried by a type, expose it as a property/helper instead of caching it as another field. This “single source of truth” also apply across containers storeage.
- I did not create pass-through structs/classes that merely copy the fields of an existing domain record.
- I did not stop after finding one instance of a smell. After fixing it, scan the surrounding module for the same pattern.
- I am trusting my context, not defensive programming.
- If a helper type exists only to make call sites shorter, but it hides where identity or ownership comes from, prefer more explicit call sites over duplicating state.