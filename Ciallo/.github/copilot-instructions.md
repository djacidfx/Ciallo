# Instruction For Copilot
## Tooling & Shell Usage
- Prefer the bundled bash helpers (bash -lc) when invoking shell commands; always set the workdir parameter.
- Use rg/rg --files for searches; fall back only if unavailable.
- use sed for in-place file edits.
- Use git & gh for version control operations.
- Use jq for JSON processing.
- Avoid PowerShell-specific commands. CRITICAL DO NOT USE PYTHON, PERL or OTHER SCRIPTS TO MANIPULATE FILES.