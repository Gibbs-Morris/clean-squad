# ASD-STE100 Simplified Technical English

## What ASD-STE100 is

ASD-STE100 is an international standard for technical documentation.

It defines Simplified Technical English, also called STE. STE is a controlled form of English.

The standard has two main parts:

- Writing rules control grammar, style, sentence structure, and text organization.
- A controlled dictionary defines approved words, meanings, and parts of speech.

The current source for this principle is ASD-STE100 Issue 9, dated January 15, 2025.

## Purpose

Technical text must give one clear meaning to each reader. Complex language can cause different interpretations.

STE reduces this risk. It limits unnecessary vocabulary and uses consistent sentence structures.

This control is useful when readers have different roles, technical knowledge, or first languages.

## Why STE helps an AI workflow

An AI workflow moves information between specialized agents. Each agent must understand the output from earlier workflow stages.

Unclear words and long sentences can cause errors during these handoffs. Inconsistent terms can also describe one item as different items.

STE gives the workflow a common writing method. This method can make requirements, plans, findings, and instructions easier to compare.

STE does not improve the model's reasoning by itself. It improves the language that carries the reasoning between workflow stages.

## Core writing principles

### Use controlled words

Use an approved dictionary word when it has the correct meaning. Use the word only with its approved meaning and part of speech.

Software work also needs technical terms. Use necessary technical nouns and technical verbs when the controlled dictionary has no precise alternative.

Define an uncommon technical term before its first use. Use the same term for the same item throughout the text.

Do not replace words without checking the full sentence. A direct word replacement can change the meaning or create incorrect grammar.

### Write short sentences

Use no more than 20 words in a procedural sentence. A procedural sentence tells the reader to do an action.

Use no more than 25 words in a descriptive sentence. A descriptive sentence gives information or explains a condition.

These limits do not include code, commands, paths, identifiers, or required machine-readable text.

### Prefer active voice

Identify the person or system that does an action. Active voice usually makes this relationship clear.

Use passive voice only when the actor is unknown, unimportant, or less important than the result.

### Give one instruction at a time

Write one action in each procedural sentence. Put conditions before the action when the reader must know them first.

Use separate steps when actions do not occur at the same time. Keep the step order equal to the required action order.

### Keep grammar complete

Do not omit articles, subjects, verbs, or other necessary words. Complete grammar helps readers identify the intended relationship between terms.

Avoid long groups of nouns. Rewrite the text when a noun group can have more than one meaning.

### Organize information clearly

Give information gradually. Put the main point before supporting details.

Use headings, lists, and repeated key terms to show the structure. Keep each paragraph focused on one subject.

## Application in CleanSquad

The default workflow applies STE to visible natural-language output, including:

- requirements and acceptance signals
- plans and architecture descriptions
- implementation and validation notes
- review findings and correction instructions
- risks, decisions, and status summaries

The workflow preserves literal technical content. It does not rewrite:

- source code and code fragments
- commands, paths, and identifiers
- API names, schema fields, and configuration keys
- logs, error messages, and quoted text
- required headings and machine-readable output tokens

RFC 2119 keywords are defined requirement terms. The workflow preserves `MUST`, `SHOULD`, `MAY`, and related negative forms.

## Practical review checklist

Before an agent returns an output, it checks these questions:

1. Does each sentence have one clear meaning?
2. Does the text use one term for each item?
3. Does each instruction contain one action?
4. Does the text use active voice when practical?
5. Are procedural sentences within 20 words?
6. Are descriptive sentences within 25 words?
7. Are code, commands, identifiers, and required tokens unchanged?
8. Do headings and lists make the information easy to scan?

## Expected result

STE makes workflow communication more consistent and easier to review. It also reduces ambiguity during handoffs between agents.

The standard is a communication control, not proof of technical correctness. Tests, evidence, and specialist review remain necessary.

## Official source

The ASD Simplified Technical English Maintenance Group publishes the standard:

- [ASD-STE100 Simplified Technical English, Issue 9](https://www.asd-ste100.org/assets/files/ASD-STE100_ISSUE9.pdf)
