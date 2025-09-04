# Ciallo Coding Styles

- No defensive programming. Follow the "Let It Crash" philosophy.
  - About ["Let It Crash"](https://stackoverflow.com/questions/4393197/erlangs-let-it-crash-philosophy-applicable-elsewhere).
  - Copilot, please stop adding redundant boundary checks. C# lists/arrays will throw on their own, unlike in C++.
  - I'm still learning how to make Ciallo robust while following this philosophy.
- Be mindful of Rider's suggestions. Follow most of them.
- No plural forms for class, folder, or namespace names. Use plurals for variables, fields, and properties.
  - Ignore English countable/uncountable rules if necessary.
    - e.g., `Datas` is an acceptable name for a `List<Data>` object.
    - Counting rules in English are spaghetti code and not worth the effort.