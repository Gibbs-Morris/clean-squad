# GitHub Instruction Files

## What GitHub instruction files are

In the context of GitHub Copilot and Visual Studio Code customization, GitHub instruction files are Markdown files that provide task-specific or file-specific guidance to the AI. In a repository, these files are typically stored under:

- `.github/instructions/*.instructions.md`

Their job is to tell the AI how to behave when working on certain kinds of files or tasks.

A useful way to think about them is this:

- normal project files contain the product or system you are building
- instruction files contain guidance about **how the AI should work with those files**

They are not application logic. They are not end-user documentation. They are operational guidance for AI-assisted work.

## Why they matter

Without instruction files, an AI assistant has to infer local conventions from whatever context it can see at the moment. Sometimes that works well. Sometimes it does not.

Instruction files reduce that ambiguity by making project expectations more explicit. They help the AI understand things such as:

- where certain content should live
- what structure to follow
- what writing style to use
- what rules apply to a specific folder or file type
- what patterns should be preferred or avoided

This matters because an AI can only follow conventions reliably if those conventions are visible. Instruction files make those conventions visible in a reusable way.

## The core idea

The central idea behind GitHub instruction files is that guidance should be:

- **close to the repository**
- **specific to the work**
- **narrow enough to be useful**
- **structured so the AI can discover it at the right time**

That last point is especially important. A good instruction file is not merely stored somewhere. It is shaped so the AI can load it when relevant.

## Where instruction files live

Workspace-level instruction files are commonly stored in:

- `.github/instructions/`

Each instruction file usually uses the `.instructions.md` extension. For example:

- `.github/instructions/python.instructions.md`
- `.github/instructions/docs.instructions.md`
- `.github/instructions/key-principles.instructions.md`

This naming pattern makes the purpose clear and helps keep guidance organized.

There can also be user-level instruction files outside the repository, but when people talk about GitHub instruction files in a project, they usually mean the workspace files stored in `.github/instructions/`.

## What problem they solve

Instruction files solve a practical problem: repository conventions are often real but implicit.

For example, a project may assume that:

- all API docs belong in a certain folder
- tests must follow a naming pattern
- certain code should use specific utilities
- documentation should use a particular framework such as Diátaxis
- specific directories should contain only one kind of content

Humans on the team may know these things already. An AI assistant may not.

Without instruction files, the assistant may produce work that is technically valid but locally wrong. It may put files in the wrong place, choose the wrong structure, or mix concerns the team prefers to keep separate.

Instruction files help the AI move from generic correctness to local correctness.

## The typical structure of an instruction file

A file-specific instruction file usually has two main parts:

1. **YAML frontmatter**
2. **instruction body**

### Frontmatter

The frontmatter appears at the top of the file between `---` markers.

A common structure looks like this:

- `description`
- `name`
- `applyTo`

### Body

The body contains the actual guidance in plain Markdown.

This is where you explain the rules the AI should follow.

## What the frontmatter fields mean

### `description`

The `description` explains when the instruction is relevant.

This field is important because it acts as a discovery signal. A good description makes it easier for the AI to recognize when the instruction should be used.

A strong description is:

- specific
- keyword-rich
- action-oriented
- clear about when it applies

A weak description is vague and generic. If the description does not clearly describe the intended use, the instruction may not be discovered when needed.

A common pattern is to write descriptions that begin with language like:

- "Use when creating or editing..."
- "Use when working on..."
- "Use when writing..."

That kind of phrasing makes the trigger condition clearer.

### `name`

The `name` gives the instruction a readable label.

This is optional in many cases, but it improves clarity, especially when a repository has several instruction files.

The name should be short and descriptive. It is not the place for a full explanation.

### `applyTo`

The `applyTo` field defines which files or folders the instruction should automatically apply to when those files are being created or edited.

This is powerful because it lets you scope an instruction to a particular part of the repository.

Examples include:

- all Python files
- all files in a documentation folder
- only files inside a specific topic directory
- a subset of API files

This helps avoid one of the biggest instruction-file mistakes: making guidance broader than it needs to be.

## Two main ways instruction files become relevant

Instruction files are typically used in two important ways.

### 1. On-demand discovery

In this mode, the AI notices that the current task matches the instruction's description.

This is why the `description` field matters so much. It helps the system decide:

- this instruction is relevant to the current request
- this guidance should be brought into context

### 2. Explicit file matching

In this mode, the instruction applies because the current work involves files that match the `applyTo` pattern.

For example, if an instruction says it applies to a certain folder, then creating or editing files in that folder should cause the instruction to be considered automatically.

These two mechanisms work together:

- `description` helps with task-based discovery
- `applyTo` helps with file-based discovery

## Why scoping matters

Scoping is one of the most important design ideas behind instruction files.

A good instruction file is usually narrow enough to be confidently correct in its own domain.

For example:

- a documentation rule for one folder
- a testing rule for one language
- a migration rule for one part of the backend

When an instruction is scoped well, it becomes easier for the AI to apply it correctly.

When an instruction is scoped badly, several problems appear:

- irrelevant guidance enters the context
- important guidance competes with unrelated rules
- the AI may overapply a local rule globally
- the instruction becomes harder to maintain

This is why very broad matching patterns should be used carefully.

## A simple mental model

A useful mental model is:

- **description tells the AI when the rule matters**
- **applyTo tells the AI where the rule matters**
- **body tells the AI what the rule is**

That simple model explains most of how these files work.

## What the instruction body should contain

The body of an instruction file should contain concise, actionable guidance.

Good instruction bodies usually do things like:

- define folder-specific rules
- explain required structure
- specify content boundaries
- name important conventions
- identify preferred patterns
- warn against local anti-patterns

The body should not try to become a general-purpose encyclopedia. The more focused it is, the more likely it is to be applied correctly.

## Worked example: a folder-specific documentation rule

Suppose a repository has a folder where all principle documents must:

- be organized under `topic/xxx`
- be self-contained
- follow Diátaxis

A folder-scoped instruction file can encode that directly. Then when the AI is asked to add content in that folder, it has explicit guidance instead of having to guess.

This creates several benefits:

- the AI puts files in the right place
- the structure stays consistent
- each file follows the intended content model
- the rules become reusable across many requests

This is much stronger than repeating the same explanation manually in every chat request.

## Worked example: a language-specific coding rule

Imagine a project wants all Python files to:

- use type hints
- prefer one testing pattern
- avoid a certain style of stateful utility

A Python-scoped instruction file can encode those preferences. Then the AI has local standards available whenever it edits Python files.

Again, the value is not only convenience. The value is consistency.

## Instruction files versus workspace-wide instructions

It is important to distinguish file-specific instruction files from broader workspace instructions.

### File-specific instruction files

These are usually stored in `.github/instructions/*.instructions.md` and are meant to apply to particular tasks, file types, or folders.

They are best when the rule is local and scoped.

### Workspace-wide instructions

A workspace-wide instruction file applies more broadly across the repository.

This is best when the rule truly applies almost everywhere.

The distinction matters because a local rule should not be promoted to a global rule without a good reason. If everything becomes global, relevance drops and the instruction system becomes noisy.

## Instruction files versus prompt files and agents

These things are related, but they are not the same.

### Instruction files

Instruction files provide guidance that shapes how the AI should work when certain conditions are met.

They are about **rules and conventions**.

### Prompt files

Prompt files are more like reusable task prompts.

They are about **asking for a particular operation or workflow**.

### Agents

Custom agents define a persona, tool set, and behavior profile.

They are about **who the AI is acting as and what capabilities it uses**.

A simple distinction is:

- instruction files = rules
- prompt files = reusable requests
- agents = reusable roles

## What makes an instruction file good

A good instruction file is usually:

- specific
- concise
- scoped correctly
- focused on one concern
- explicit about what to do
- explicit about what not to do when necessary

Good instruction files reduce ambiguity without becoming bloated.

## Common mistakes

### Mistake 1: vague descriptions

A description like "Helpful coding tips" does very little. It does not clearly tell the AI when the file should apply.

### Mistake 2: overly broad `applyTo`

A pattern that applies everywhere can flood the AI with irrelevant guidance if the rule is only meant for one area.

### Mistake 3: mixing too many concerns

An instruction file that tries to handle testing, architecture, style, documentation, and deployment all at once is harder to discover, harder to trust, and harder to maintain.

### Mistake 4: restating large project docs without filtering

Instruction files should distill what the AI needs to act well. They should not become a copy of the entire README.

### Mistake 5: writing rules that are not actionable

Advice such as "write better code" is too vague. A useful instruction says what better means in this context.

## Why instruction files improve AI-assisted work

Instruction files improve AI-assisted work because they reduce the gap between generic model behavior and repository-specific expectations.

That improvement shows up in several ways:

- fewer misplaced files
- fewer style mismatches
- better consistency across requests
- less need to repeat the same rules manually
- more reliable adherence to local patterns

They effectively turn repeated human reminders into reusable repository knowledge.

## The trade-off

Instruction files are powerful, but they introduce a maintenance responsibility.

If the instructions are:

- outdated
- contradictory
- too broad
- too vague

then they can mislead the AI instead of helping it.

So the trade-off is straightforward:

- good instructions improve consistency and quality
- bad instructions create confusion at scale

This means instruction files should be treated as part of the repository's operational design, not as disposable notes.

## A repeatable checklist

Use this checklist when creating or reviewing a GitHub instruction file:

1. What exact problem is this instruction solving?
2. Is the rule truly local, or should it be broader?
3. Does the `description` clearly say when the instruction applies?
4. Does the `applyTo` field clearly say where it applies?
5. Is the body focused on one concern?
6. Are the rules specific enough to be actionable?
7. Is the guidance concise enough to stay useful?
8. Would a new contributor understand the instruction without outside context?

## Final summary

GitHub instruction files are repository-level AI guidance files, typically stored in `.github/instructions/*.instructions.md`, that help GitHub Copilot or VS Code apply local rules and conventions at the right time. Their purpose is to make repository expectations explicit so the AI can work more consistently and more accurately within the project's structure.

Their effectiveness comes from a simple combination: a clear description of when the rule matters, a clear scope for where it matters, and a concise body that explains what the rule is. When instruction files are narrow, actionable, and well-scoped, they become a practical way to turn local project knowledge into reliable AI behavior.
