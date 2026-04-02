# Diátaxis

## What Diátaxis is

Diátaxis is a framework for organizing documentation by the **kind of help the reader needs**. Instead of treating all documentation as one thing, it divides documentation into four distinct content types:

1. **Tutorials**
2. **How-to guides**
3. **Reference**
4. **Explanations**

The core idea is simple: different reader needs require different kinds of writing. A person who is trying to learn by doing does not need the same document as a person who wants a precise fact, and neither of those readers needs the same thing as a person who is trying to understand why something works the way it does.

Diátaxis matters because many documentation problems come from mixing these needs together. A document becomes confusing when it tries to teach, instruct, define, and justify everything at once. The reader then has to do extra work to figure out what the document is trying to be.

A Diátaxis-based approach removes that confusion by matching document type to reader intent.

## The problem Diátaxis solves

Documentation often fails for reasons that are structural rather than grammatical. A document may be well written sentence by sentence and still be hard to use because it serves several purposes at once.

For example, one document may:

- teach a beginner the basics
- give step-by-step instructions for a task
- list technical options and parameters
- explain architectural reasoning

Each of those is a valid need, but they are not the same need. When combined carelessly, they create friction.

A beginner may get lost in technical details.
A practitioner may get slowed down by teaching material they do not need.
A person looking for a precise fact may be forced to read paragraphs of explanation.
A person trying to understand the reasoning may be given only commands with no context.

Diátaxis solves this by recognizing that documentation should be shaped by the reader's immediate goal.

## The central idea: map content to user need

At the heart of Diátaxis is a distinction between four different reader situations.

### 1. The reader wants to learn

This reader is building skill and confidence. They benefit from guided practice. They need a path, not a pile of facts.

This is the role of a **tutorial**.

### 2. The reader wants to accomplish something

This reader already has some context and wants to complete a specific task. They need practical steps, not a lesson in theory.

This is the role of a **how-to guide**.

### 3. The reader wants exact information

This reader needs a reliable source of truth. They may want syntax, fields, options, behaviors, or definitions. They need clarity and precision, not persuasion or storytelling.

This is the role of **reference**.

### 4. The reader wants understanding

This reader is trying to make sense of concepts, design choices, trade-offs, or relationships. They need reasoning, not merely procedure.

This is the role of **explanation**.

These four needs are different enough that a document becomes stronger when it chooses one clearly.

## The four content types in depth

### Tutorials

A tutorial is for learning through guided experience. Its purpose is to help a reader get started and build confidence by completing a sequence of steps in a safe, structured order.

A tutorial should:

- assume the reader is still learning
- lead the reader step by step
- focus on a concrete outcome
- reduce decision-making for the reader
- build confidence through successful completion

A tutorial is not mainly about completeness. Its job is to help the learner cross the gap from unfamiliarity to first success.

### What a tutorial feels like

A tutorial says, in effect:

> Follow me. We will do this together, and by the end you will have learned something important.

### What tutorials should avoid

A tutorial should avoid:

- too many side paths
- large amounts of reference detail
- deep theory that interrupts progress
- expecting the learner to make expert judgments too early

### Example of a tutorial question

- "How do I get started with this system from nothing?"

### How-to guides

A how-to guide is for solving a specific problem. Its purpose is to help a reader achieve a goal as efficiently as possible.

A how-to guide should:

- focus on one clear task
- assume the reader has a practical goal
- provide the steps needed to complete that goal
- include only the context needed to succeed
- optimize for usefulness and completion

A how-to guide is not a lesson for beginners and not an encyclopedia. It is a route to an outcome.

### What a how-to guide feels like

A how-to guide says, in effect:

> If you want to achieve this result, do these steps in this order.

### What how-to guides should avoid

A how-to guide should avoid:

- broad conceptual lectures
- trying to cover every related feature
- turning into a full reference manual
- mixing several unrelated tasks into one document

### Example of a how-to question

- "How do I configure this feature for my project?"

### Reference

Reference is for looking up facts. Its purpose is to provide precise, structured, dependable information.

Reference should:

- be accurate and complete within its scope
- be easy to scan
- use consistent structure and terminology
- describe what something is and how it behaves
- avoid unnecessary commentary

Reference is where readers go when they need exact details, not coaching.

### What reference feels like

Reference says, in effect:

> Here is the factual information you need, clearly stated and easy to find.

### What reference should avoid

Reference should avoid:

- persuasive argument
- informal storytelling
- hidden steps or vague wording
- long teaching sections that bury the facts

### Example of a reference question

- "What options does this command support?"

### Explanations

An explanation is for understanding. Its purpose is to help a reader see the reasoning behind a concept, model, decision, or relationship.

Explanations should:

- clarify why something is the way it is
- connect concepts together
- describe trade-offs and implications
- provide background and mental models
- support deeper understanding

An explanation does not primarily tell the reader what to do next. It helps the reader understand the subject more deeply.

### What an explanation feels like

An explanation says, in effect:

> Here is how to think about this topic and why it makes sense this way.

### What explanations should avoid

Explanations should avoid:

- pretending to be a task checklist
- burying concepts under syntax tables
- mixing too many instructional steps into conceptual writing

### Example of an explanation question

- "Why is this system designed this way?"

## Why these content types should stay distinct

The four content types are all useful, but they are useful for different reasons. A document becomes harder to use when it blurs them together.

For example:

- a tutorial becomes harder if it turns into a long conceptual essay
- a how-to guide becomes slower if it includes beginner teaching for every step
- reference becomes frustrating if it hides facts inside narrative prose
- an explanation becomes shallow if it is reduced to bullet-point commands

This distinction does not mean the four content types are enemies. They support one another. A healthy documentation set usually includes all four. The point is that each individual document should know its own job.

## A simple way to classify a document

If you are unsure what kind of document you are writing, ask:

- Is the reader trying to learn? Write a tutorial.
- Is the reader trying to get something done? Write a how-to guide.
- Is the reader trying to look something up? Write reference.
- Is the reader trying to understand? Write an explanation.

This classification method works because it begins with reader intent rather than author preference.

## Worked examples

### Example 1: a database tool

Imagine a documentation set for a database tool.

A tutorial might be:

- "Build your first local database project"

A how-to guide might be:

- "Back up a database before a schema change"

A reference page might be:

- "Configuration file fields"

An explanation might be:

- "Why connection pooling improves performance"

Each document serves the same product, but each serves a different reader need.

### Example 2: a documentation rule in a repository

Imagine a repository that stores topic-based documentation.

A tutorial might help a new contributor create their first topic file.
A how-to guide might show how to add a new topic under the correct folder structure.
A reference page might list required file paths, naming rules, and content conventions.
An explanation might describe why the project separates tutorials, how-to guides, reference, and explanations.

Again, the difference is not the subject matter. The difference is the reader's purpose.

## Common signs a document is the wrong type

A document may be the wrong Diátaxis type if:

- it starts with a task but drifts into theory for several sections
- it claims to be reference but includes advice without clear facts
- it claims to teach beginners but assumes prior expertise
- it tries to answer every possible question in one file
- the reader cannot tell whether they are meant to learn, do, look up, or understand

When that happens, the solution is often not "write more." The real solution is often "choose the right document type and separate concerns."

## Why Diátaxis improves documentation quality

Diátaxis improves documentation because it forces clarity of purpose.

That has several practical benefits:

- documents become easier to navigate
- writers make better decisions about what to include
- readers find answers faster
- teams can identify gaps more clearly
- maintenance becomes easier because each document has a defined role

Without a framework like this, documentation often grows by accumulation. New paragraphs are added wherever they fit at the moment. Over time, the structure weakens. Diátaxis provides a stable way to resist that drift.

## Diátaxis and self-contained content

A self-contained document gives the reader what they need inside the document itself. That idea fits well with Diátaxis.

A self-contained tutorial should define the terms a beginner needs and provide all essential steps.
A self-contained how-to guide should include the prerequisites, actions, and outcome needed for the task.
A self-contained reference page should define the item clearly and present the facts in one place.
A self-contained explanation should define key concepts, provide the reasoning, and avoid assuming hidden background knowledge.

Self-contained writing does not mean every file must contain everything about the entire subject. It means the file must contain everything required for its own purpose.

## Common mistakes when using Diátaxis

### Mistake 1: treating it as a rigid template system

Diátaxis is a framework for reasoning about documentation, not a decorative label system. Simply naming a file "tutorial" does not make it a tutorial.

### Mistake 2: mixing reader needs in one document

This is the most common failure. A document may begin with one purpose and slowly collect material from the other three.

### Mistake 3: assuming explanations are optional

Teams often write tasks and reference first, then neglect explanation. That creates documentation that may be usable in the short term but hard to understand deeply.

### Mistake 4: confusing tutorials and how-to guides

A tutorial is guided learning. A how-to guide is goal-oriented problem solving. They may both contain steps, but they are not the same thing.

### Mistake 5: treating reference as narrative

Reference becomes weak when factual content is hidden inside flowing prose instead of being structured for lookup.

## A repeatable checklist

Use this checklist when writing or reviewing documentation with Diátaxis in mind:

1. What is the reader trying to do right now?
2. Is that need to learn, do, look up, or understand?
3. Does the document clearly serve one of those needs?
4. Have unrelated content types been mixed into the same file?
5. Does the structure match the document type?
6. Is the content self-contained for its purpose?
7. Can the reader quickly tell how to use this document?
8. If this file belongs in a larger set, what companion files should exist in the other three categories?

## When to use Diátaxis

Diátaxis is especially useful when:

- a documentation set is growing and becoming messy
- readers are asking for the same information in different forms
- documents feel hard to scan or hard to trust
- contributors are unsure what kind of content to write
- teaching material, task instructions, facts, and concepts keep getting mixed together

## Final summary

Diátaxis is a framework that organizes documentation around four distinct reader needs: learning, doing, looking up, and understanding. It expresses those needs as tutorials, how-to guides, reference, and explanations.

Its power comes from a simple discipline: make each document serve one clear purpose. When documentation follows that discipline, it becomes easier to write, easier to maintain, and much easier to use.

A team using Diátaxis is not just sorting documents into boxes. It is making a more fundamental decision: documentation should be shaped by what the reader needs at that moment, not by whatever information the writer happens to have available.
