# ASD-STE100 Writing Guidance

Use ASD-STE100 Simplified Technical English, Issue 9, for natural-language output.

## Vocabulary and terms

- Use a simple approved word when it has the correct meaning.
- Use each word with one meaning and one part of speech.
- Use necessary project terms as technical nouns or technical verbs.
- Define an uncommon technical term before its first use.
- Use the same term for the same item throughout the output.

## Sentences and actions

- Use active voice when practical.
- Identify the person or system that does an action.
- Use no more than 20 words in a procedural sentence.
- Use no more than 25 words in a descriptive sentence.
- Give only one instruction in each sentence.
- Put a condition before the action that depends on it.
- Use separate ordered steps for actions that do not occur at the same time.

## Structure

- Give the main point before supporting details.
- Keep each paragraph focused on one subject.
- Use headings and vertical lists to show the text structure.
- Do not omit words that make the grammar complete.
- Rewrite a long noun group when it can have more than one meaning.

## Technical exceptions

Do not change literal technical content to satisfy a language rule. Preserve these items exactly:

- source code and code fragments
- commands, paths, and identifiers
- API names, schema fields, and configuration keys
- logs, error messages, and quoted text
- required headings and machine-readable output tokens

Treat RFC 2119 keywords as defined requirement terms. Preserve `MUST`, `SHOULD`, `MAY`, and their negative forms.

Before you return an output, check its natural-language text against this guidance.
