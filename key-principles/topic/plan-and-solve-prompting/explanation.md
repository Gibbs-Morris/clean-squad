# Plan-and-Solve Prompting: Improving Zero-Shot Chain-of-Thought Reasoning by Large Language Models

## What *Plan-and-Solve Prompting: Improving Zero-Shot Chain-of-Thought Reasoning by Large Language Models* is

*Plan-and-Solve Prompting: Improving Zero-Shot Chain-of-Thought Reasoning by Large Language Models* (Wang et al., 2023) is a paper that argues that zero-shot reasoning can often be improved when a language model is prompted to **plan first** and **solve second** instead of trying to solve the whole problem immediately in one uninterrupted response.

A useful short summary is this:

- zero-shot chain-of-thought can help, but it often still fails on complex problems
- one reason for failure is that the model may start reasoning before it has organized the task
- plan-and-solve prompting improves performance by separating planning from execution

The paper became important because it showed that better zero-shot reasoning does not always require examples. Sometimes it requires a better internal workflow inside the prompt: first decide the steps, then carry them out.

## The problem the paper addresses

Zero-shot chain-of-thought prompting showed that a simple cue such as "Let's think step by step" can improve reasoning performance. That was a major result, but it did not solve every problem.

A model can still fail in zero-shot reasoning by:

- omitting important steps
- rushing to a conclusion too early
- mixing planning and execution in a confusing way
- making local progress without a clear overall strategy
- wandering into an incomplete or poorly structured reasoning path

In other words, even when the model is encouraged to reason step by step, it may still fail because it begins solving before it has formed a good plan.

This creates the core question of the paper:

> what if the model first created a plan for solving the problem, and only after that executed the plan?

That is the main problem the paper addresses.

## The central idea of plan-and-solve prompting

The central idea is to split reasoning into two distinct stages:

1. create a plan for solving the problem
2. solve the problem by following that plan

This is different from a prompt that simply asks the model to think step by step from the beginning. In plan-and-solve prompting, the model is first asked to organize the work before trying to complete it.

In plain terms, the method says:

- do not begin by directly solving
- first decide what the steps should be
- then execute those steps carefully

This matters because reasoning failures often come from poor task organization rather than total lack of knowledge.

## Why the name "plan-and-solve" matters

The name highlights the paper's core distinction.

- **plan** means identify the structure of the solution before carrying it out
- **solve** means execute the structured steps to obtain the answer

The two parts are related, but they are not identical.

Planning answers questions such as:

- what subgoals must be completed?
- what order should the steps follow?
- what information is needed first?
- what intermediate results will be required later?

Solving then uses that structure to produce the actual reasoning and answer.

The method matters because many bad solutions are not failures of arithmetic or knowledge alone. They are failures of organization.

## Why separating planning from solving can help

There are several reasons this separation can improve performance.

### It reduces missing-step errors

If the model is asked to list the steps before solving, it is less likely to skip a crucial part of the process.

### It creates a global structure before local execution

The model can see the whole path before walking it, which helps reduce drift and confusion later.

### It reduces premature conclusions

A plan discourages the model from jumping directly to the answer before the structure of the problem is clear.

### It makes multi-step reasoning easier to manage

Once the steps are explicitly laid out, each step becomes a clearer unit of work.

This does not guarantee correctness, but it can make reasoning more coherent and complete.

## How the method works conceptually

A simple conceptual form of the method looks like this:

### Stage 1: planning

The model is prompted to produce a plan for how it will solve the problem.

That plan may include:

- identifying relevant quantities or concepts
- deciding what must be computed first
- ordering substeps
- determining what intermediate conclusions are needed

### Stage 2: solving

The model is then prompted to solve the problem by following the plan it already produced.

The overall structure becomes:

- read the problem
- make a plan
- follow the plan
- derive the final answer

This is simple, but the important shift is that planning becomes an explicit step rather than an implicit hope.

## Why this differs from zero-shot chain-of-thought

Zero-shot chain-of-thought prompting usually encourages the model to reason step by step immediately.

Plan-and-solve prompting adds a more explicit structure.

A useful contrast is:

- zero-shot chain-of-thought: start reasoning step by step
- plan-and-solve: first decide the steps, then reason through them

So plan-and-solve is more deliberate about the organization of reasoning before detailed execution begins.

This is why the paper is best understood not as rejecting zero-shot chain-of-thought, but as improving it.

## Why this differs from least-to-most prompting

Least-to-most prompting emphasizes decomposing a problem into smaller subproblems and solving them from easier parts to harder parts.

Plan-and-solve prompting overlaps with that idea, but it is not identical.

A useful contrast is:

- least-to-most prompting emphasizes ordered decomposition from simpler subproblems to harder ones
- plan-and-solve prompting emphasizes explicit advance planning before carrying out the reasoning

Least-to-most is especially about subproblem ordering.
Plan-and-solve is especially about separating planning from execution.

The two ideas are compatible, but they highlight different aspects of reasoning organization.

## Why this differs from self-consistency

Self-consistency improves reasoning by generating multiple reasoning paths and selecting the answer that appears most consistently.

Plan-and-solve tackles a different weakness.

It does not primarily ask:

- which answer is most stable across many samples?

Instead, it asks:

- how can one reasoning attempt be better organized before execution starts?

A short way to remember the difference is:

- **self-consistency improves answer selection across samples**
- **plan-and-solve improves reasoning structure within a sample**

Both methods can be useful, but they act on different parts of the reasoning process.

## Why planning is not the same as solving

This distinction is easy to miss.

A model may be able to describe a plan without carrying it out correctly.
A model may also solve parts of a problem without having formed a coherent plan.

The paper's insight is that planning and solving should be treated as separate cognitive tasks:

- planning organizes the route
- solving walks the route

Keeping those tasks distinct can reduce confusion that occurs when the model tries to invent the structure and execute the details at the same time.

## A simple illustration

Suppose a problem asks for the result of several linked operations.

A direct zero-shot chain-of-thought answer might begin computing immediately and possibly miss an intermediate dependency.

A plan-and-solve approach might instead do this:

1. identify what quantities are given
2. determine which intermediate quantity must be found first
3. determine how that intermediate result affects the next step
4. compute the final quantity after the earlier steps are completed

Only after that plan is laid out does the model carry out the calculations.

The advantage is not that the plan is magic. The advantage is that the model is less likely to forget, reorder, or skip essential parts of the reasoning process.

## What kinds of tasks benefit most

The method is most useful on tasks where:

- the problem requires several dependent reasoning steps
- missing one intermediate step causes failure
- the model benefits from an explicit global strategy
- zero-shot chain-of-thought alone is helpful but incomplete

These often include:

- arithmetic word problems
- commonsense reasoning tasks
- symbolic or logical reasoning tasks
- other multi-step tasks where incomplete planning leads to errors

The shared pattern is that the problem is not solved best by immediate local reasoning alone. It benefits from explicit organization first.

## Why the paper was influential

The paper was influential for several reasons.

### 1. It improved zero-shot reasoning without examples

The method stays in a zero-shot setting while still improving reasoning quality.

### 2. It highlighted planning as a promptable skill

It suggested that planning itself can be elicited through prompt design rather than only through external orchestration.

### 3. It refined the idea of step-by-step reasoning

The paper showed that not all step-by-step reasoning is equally good. Reasoning often improves when a plan exists before the steps are executed.

### 4. It connected prompting with broader agentic ideas

Later systems involving planning, subgoals, and tool use benefit from the same general lesson: organize before acting.

## Strengths of the paper's contribution

The paper's contribution has several strengths.

### Simplicity

The method is easy to describe and easy to try.

### Practical usefulness

It offers a lightweight strategy for improving zero-shot reasoning on multi-step tasks.

### Better organization of reasoning

It targets a common failure mode: incomplete or poorly structured reasoning.

### Influence on later prompting strategies

The method helped reinforce the importance of planning in reasoning systems and agent workflows.

## Limitations and cautions

A good explanation should also be clear about the limits.

### A good plan can still be executed badly

Planning helps, but correct execution still matters.

### A bad plan can guide the whole solution in the wrong direction

If the planned structure is flawed, later reasoning may inherit that flaw.

### It may add cost and latency

Two-stage prompting typically uses more tokens and more time than a direct answer.

### Not every task needs explicit planning

For simple tasks, the added planning stage may be unnecessary overhead.

### It is not the same as verification

A planned solution may still be wrong and may still need checking.

## Common misunderstandings

### Mistake 1: plan-and-solve is just another phrase for chain-of-thought

It overlaps with chain-of-thought, but its distinctive feature is the explicit separation between planning and execution.

### Mistake 2: the planning step guarantees completeness

A model can still omit something important while planning.

### Mistake 3: planning alone solves reasoning reliability

Planning can improve structure, but it does not guarantee truth.

### Mistake 4: the method replaces decomposition strategies

It complements decomposition strategies, but it does not eliminate them.

### Mistake 5: planning removes the need for verification

Even a well-organized solution can still contain wrong assumptions or calculations.

## Plan-and-solve prompting versus chain of verification

These ideas solve different problems.

### Plan-and-solve prompting

This method improves generation by separating planning from execution.

Its guiding question is:

- how should the reasoning process be organized before solving begins?

### Chain of verification

This method improves reliability by checking claims or conclusions after or around generation.

Its guiding question is:

- does the answer hold up under checking?

A short way to remember the difference is:

- **plan-and-solve improves how the reasoning is organized**
- **chain of verification improves how the result is checked**

They can work together, but they are not interchangeable.

## Why the paper still matters

The paper still matters because it captured a durable insight:

> reasoning improves when the model is encouraged to organize the work before doing the work.

That lesson still matters across modern AI systems because it connects to:

- planning
- staged execution
- subgoal design
- agent workflows
- test-time reasoning improvements

The paper helped make explicit planning a more central concept in prompting and reasoning design.

## A practical reading of the paper

A practical way to read the paper is this:

> if zero-shot reasoning is failing because the model starts too quickly, do not only ask it to think step by step. Ask it to plan first and then solve.

That is a powerful shift because it changes the role of the prompt from a simple reasoning cue into a minimal workflow.

This supports a broader engineering mindset:

- organize the work first
- identify the steps
- then execute carefully
- then check the result if needed

## A repeatable checklist

Use this checklist when thinking about plan-and-solve prompting:

1. Is the task genuinely multi-step?
2. Is zero-shot chain-of-thought helpful but still incomplete?
3. Would an explicit plan reduce missing-step errors?
4. Can planning and solving be usefully separated for this task?
5. Could a bad plan mislead the later reasoning?
6. Is the extra cost of a planning stage justified by the improvement?
7. Would verification still be valuable after solving?
8. Are you treating planning as preparation rather than as proof of correctness?

## Final summary

*Plan-and-Solve Prompting: Improving Zero-Shot Chain-of-Thought Reasoning by Large Language Models* (Wang et al., 2023) is an influential paper showing that zero-shot reasoning can often be improved when a language model is first prompted to form a plan and then prompted to solve the problem by following that plan. Its core contribution is the idea that explicit planning can reduce missing-step and poor-structure errors that often limit zero-shot chain-of-thought reasoning.

The paper matters because it helped establish a simple but durable lesson: better reasoning often comes from better organization before execution. Its lasting insight is that if a model struggles when asked to reason immediately, prompting it to plan first can make the later reasoning more complete, orderly, and reliable.
