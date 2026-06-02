## Trust Principles
- Zero defensive programming. Follow the "Let It Crash" philosophy. Always trust upstream context.
- Every null check and try-catch must be strictly limited to unpredictable external boundaries (e.g., user input, disk IO, network requests). Internal application state and function calls must be treated as completely reliable.