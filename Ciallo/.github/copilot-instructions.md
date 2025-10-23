# Ciallo Coding Styles

## Guidelines
- No defensive programming. Follow the "Let It Crash" philosophy.
  - Every null check and try-catch imply possible null values or errors raised by user's input.
  - About ["Let It Crash"](https://stackoverflow.com/questions/4393197/erlangs-let-it-crash-philosophy-applicable-elsewhere).
  - Copilot, please stop adding redundant boundary checks. C# lists/arrays will throw on their own, unlike in C++.
  - I'm still learning how to make Ciallo robust while following this philosophy.
- Be mindful of Rider's suggestions, and follow most of them.
- Zero memory leaks.
  - After closing Ciallo's window in Rider debug, make sure zero Godot warning messages in the Rider console.
  - Running project in Godot editor seems lacking of necessary check. Must in Rider debug.

### Solo development
Following guidelines are suitable for solo development or tiny team. May have to be changed in the future.

- Architect and avoid encapsulation
  - Encapsulation costs efforts and flexibility.
  - Architect systems and consider each system as a tool.
    - Tools always need to be fine-tuned, over encapsulation (make some key stuff `private`) makes the fine-tune hard to achieve.  

## Details
- Global variables or static classes (that need to work with) are named as "App*"
- No plural forms for class, folder, or namespace names.
  - Use plural for `Enum` represents bit fields same as C# tradtional. 
  - Optionally, use plural names for collection-type variables, fields, and properties to hint collection type.
  - Ignore English countable/uncountable rules.
    - e.g., `Datas` is a valid name for a `List<Data>` object.
    - Counting rules in English are legacy system and not worth the effort.
- Code in stack (Last-In, First-Out) order for undo/redo, create/delete, activate/deactiate
  - If being confused, check serveral `*Cmd.cs` files. 
