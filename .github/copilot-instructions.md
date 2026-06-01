# GitHub Copilot Project Instructions

You must strictly adhere to the following coding standards for all code generation, refactoring, and explanations in this project.

## Language Context
- **Primary Language:** C# / .NET

## Variable & Naming Conventions
- **No `var`:** Never use the `var` keyword. Always explicitly declare variables using their full type (e.g., `List<int> listOfNames = new();` instead of `var listOfNames = new List<int>();`).
- **No Abbreviations:** Do not shorten names. This applies to variables, functions, classes, and interfaces.
  - **Bad:** `ColumnDef`
  - **Good:** `ColumnDefinition`
- Always use the following format for floats: 0.0f (not .0f or 0f).

## Architecture & Code Structure
- **Single Responsibility:** Prefer small functions with a single responsibility. Split larger functions into smaller ones with easily comprehensible names.
- **Small Classes/files:** Prefer small classes and files that are easier to navigate. If a file exceeds 300 lines, consider splitting it into multiple files.
- **Don't group things by comments:** If you feel the need to add a comment to explain a group of functions or variables, it's a sign that those functions or variables should be moved into a separate class or file.

## Language features
- **No Tuples** Don't use Tuples, always prefer defining a class or struct with meaningful property names

## Braces & Formatting
- **Allman Style Braces:** Never put the opening brace on the same line as a control flow statement, class definition, or initialization. The opening brace must *always* be on a new line.
- **Mandatory Braces:** Every control flow statement (even single-line `if` statements) must use braces and be split across multiple lines. Never put a statement on the same line as an `if`.

### Formatting Examples

**Bad:**
```csharp
if (x > 0) { DoSomething(); }
if (x > 0) DoSomething();
if (condition) return;
```

**Good:**
```csharp
if (x > 0)
{
    DoSomething();
}
```