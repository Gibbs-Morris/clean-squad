# Large Language Models are Zero-Shot Reasoners

## What *Large Language Models are Zero-Shot Reasoners* is

*Large Language Models are Zero-Shot Reasoners* (Kojima et al., 2022) is a paper that argues a striking idea: large language models can perform better on certain reasoning tasks in a **zero-shot** setting if they are prompted with a simple reasoning cue such as:

> Let's think step by step.

A useful way to summarize the paper is this:

- standard zero-shot prompting often asks directly for an answer
- adding a short reasoning trigger can cause the model to produce intermediate reasoning
- that reasoning process can improve performance on multi-step tasks

The paper became influential because it suggested that reasoning performance does not always require carefully crafted few-shot examples. In some cases, a simple instruction that invites step-by-step reasoning is enough to unlock a significant improvement.

## The problem the paper addresses

Before this work, one influential idea in reasoning research was that chain-of-thought prompting could improve model performance on difficult problems, especially when prompts included worked examples that demonstrated intermediate reasoning steps.

That created an important question:

> Do models need few-shot demonstrations of reasoning, or can a simpler prompt trigger reasoning behavior without examples?

This matters because few-shot prompting has costs and limitations:

- it takes more prompt space
- it requires hand-crafted examples
- it may be brittle across tasks
- it can be inconvenient in practical use

If a model could reason better with a lightweight zero-shot instruction instead of a carefully designed demonstration set, that would make reasoning prompting much easier to apply.

## What zero-shot means in this context

A **zero-shot** prompt means the model is asked to solve the task without being shown example question-answer pairs first.

In other words:

- no few-shot demonstrations
- no worked examples in the prompt
- only the task instruction and the problem itself

This is important because zero-shot prompting is simpler and more general. The model has to rely on the prompt instruction and its prior training rather than imitating the format of provided examples.

## The central idea of the paper

The paper's central idea is that a small prompt change can have a large effect on reasoning behavior.

Instead of asking only:

- solve the problem

the paper adds a reasoning trigger such as:

- "Let's think step by step."

That short phrase encourages the model to generate intermediate reasoning before giving the answer.

This is why the paper is so well known. The intervention is simple, almost minimal, yet its effect can be surprisingly strong on tasks that require multi-step reasoning.

## Why this idea was surprising

The idea was surprising because it suggested that a model's reasoning behavior could be changed significantly without:

- retraining the model
- building a complex external system
- writing elaborate few-shot examples

Instead, a small linguistic cue could cause the model to shift from direct-answer mode into a more stepwise problem-solving mode.

That does not mean the prompt magically creates reasoning from nothing. Rather, the paper suggests that the model may already contain capabilities that are easier to activate when the prompt encourages intermediate reasoning.

## Standard zero-shot prompting versus zero-shot chain-of-thought

A helpful way to understand the paper is by contrasting two prompt styles.

### Standard zero-shot prompting

In standard zero-shot prompting, the prompt asks the model for the answer directly.

The structure is roughly:

- task instruction
- problem
- answer

This can work for many tasks, but it often performs poorly on problems that require several reasoning steps.

### Zero-shot chain-of-thought prompting

In zero-shot chain-of-thought prompting, the prompt adds a reasoning trigger such as:

- "Let's think step by step."

The structure becomes more like:

- task instruction
- problem
- reasoning trigger
- intermediate reasoning
- answer

The paper's key claim is that this change can improve performance on multi-step reasoning tasks.

## Why a simple reasoning trigger can help

There are several ways to understand why this method might work.

### It changes the response mode

The phrase acts as a cue that the answer should not be compressed into one jump. Instead, the model is encouraged to unfold the reasoning.

### It encourages decomposition

A multi-step problem often becomes easier when broken into smaller operations.

### It reduces premature commitment

If the model answers too early, it may settle on a plausible but incorrect conclusion. Step-by-step prompting can delay that premature jump.

### It aligns the output with the structure of the task

If the problem itself requires multiple inferences, then an intermediate reasoning format may better fit the problem than a one-line answer request.

These explanations do not prove exactly how the model works internally, but they help explain why the method can improve performance.

## The role of the trigger phrase

The most famous phrase associated with this paper is:

> Let's think step by step.

This phrase matters because it is both simple and general. It does not specify the exact reasoning procedure. It simply nudges the model toward producing an explicit chain of thought.

The broader lesson is not that only this exact sentence works. The broader lesson is that a prompt can cue the model into a reasoning-oriented response mode.

Still, the paper became widely cited partly because the phrase is so memorable. It gave the field a concrete, easy-to-test technique.

## What kinds of tasks the paper focused on

The paper is most associated with reasoning tasks where the final answer benefits from intermediate steps.

These often include:

- arithmetic reasoning
- commonsense reasoning
- symbolic reasoning
- multi-step logic problems

The shared feature of these tasks is that the answer is not best produced in one immediate leap. The model benefits from unpacking the path.

## A simple illustration

Imagine a problem like this:

> A person buys 2 boxes of markers. Each box contains 5 markers. Then 3 more markers are added. How many markers are there in total?

A standard zero-shot prompt might yield only the answer, which may be correct or incorrect.

A zero-shot chain-of-thought prompt might yield reasoning like:

- there are 2 boxes
- each box has 5 markers
- $2 \times 5 = 10$
- adding 3 more gives $10 + 3 = 13$
- so the answer is 13

Again, the arithmetic is simple, but the point is structural. The reasoning steps make the path visible, and that often helps the model on harder versions of the same kind of task.

## Why this differs from few-shot chain-of-thought prompting

This paper is closely related to the earlier chain-of-thought prompting idea, but there is an important difference.

### Few-shot chain-of-thought prompting

Few-shot chain-of-thought prompting shows the model example problems with worked reasoning traces.

That means the prompt teaches by demonstration.

### Zero-shot chain-of-thought as instruction

Zero-shot chain-of-thought prompting does not provide worked examples. It only adds a reasoning cue.

That means the prompt teaches by instruction rather than demonstration.

A short way to remember the distinction is:

- few-shot chain-of-thought says: "here are examples of how to reason"
- zero-shot chain-of-thought says: "reason step by step"

This difference is central to the paper's contribution.

## Why the paper mattered

The paper mattered for several reasons.

### 1. It simplified reasoning prompting

It showed that meaningful reasoning improvements could sometimes be obtained without carefully engineered demonstrations.

### 2. It widened access to reasoning methods

Because the technique was so simple, many researchers and practitioners could try it immediately.

### 3. It helped shift the field's thinking

It reinforced the broader idea that prompting is not only about describing the task. It is also about shaping the model's cognitive style of response.

### 4. It connected prompt design to emergent capability

The paper suggested that model capability can be strongly affected by how the task is framed, especially in sufficiently capable language models.

## Strengths of the paper's contribution

The paper's contribution had several strengths.

### Simplicity

The method is extremely easy to apply.

### Practical usefulness

It offers a lightweight way to improve reasoning performance on suitable tasks.

### Conceptual clarity

It isolates a clear idea: a model may reason better when explicitly invited to do so.

### Influence on later work

The paper helped inspire later research in reasoning prompts, self-consistency, verification, reflection, and agentic workflows.

## Limitations and cautions

A good explanation should also note what the paper does not prove.

### It does not guarantee correct reasoning

A model can still produce a wrong step-by-step explanation.

### It can create persuasive but false chains

A detailed reasoning trace may look convincing even if one of the steps is mistaken.

### It is not equally useful for every task

If the task is trivial or does not genuinely require multiple steps, the added reasoning may be unnecessary.

### It may increase cost and latency

Longer outputs usually cost more and take more time.

### It depends on model capability

The benefit of the method depends in part on the underlying model's ability to make use of the reasoning cue.

## Common misunderstandings

### Mistake 1: the phrase itself is magic

The phrase is useful as a trigger, but the deeper point is about prompting the model into a reasoning mode, not worshipping one sentence like a spell.

### Mistake 2: step-by-step output means the reasoning is correct

Visible reasoning can still be flawed.

### Mistake 3: zero-shot chain-of-thought replaces verification

It can improve reasoning, but it does not remove the need to check important answers.

### Mistake 4: this proves the model reasons like a human

The paper shows a prompting effect and performance gains. It does not settle the deeper philosophical question of human-like reasoning.

### Mistake 5: more words always mean better reasoning

The value comes from useful intermediate structure, not from mere verbosity.

## Zero-shot chain-of-thought versus chain of verification

These ideas are related, but they solve different problems.

### Zero-shot chain-of-thought

This method helps the model produce an answer by encouraging step-by-step reasoning during generation.

Its core question is:

- how can we prompt the model to reason more effectively while generating the answer?

### Chain of verification

This method checks claims, logic, or conclusions after or around answer generation.

Its core question is:

- how do we test whether the answer is reliable?

A short way to remember the difference is:

- **zero-shot chain-of-thought improves the generation path**
- **chain of verification improves the checking process**

These methods often work well together.

## Why the paper still matters

The paper still matters because it showed that a very small change in prompt design can have a large effect on reasoning performance.

Its broader lessons remain influential:

- prompt structure can matter enormously
- intermediate reasoning often helps on multi-step tasks
- simple prompting changes can unlock different behavior
- strong performance is not only about model size, but also about how capability is elicited

Even as newer methods have appeared, this paper remains one of the clearest demonstrations that reasoning can be elicited through prompt design alone.

## A practical reading of the paper

A practical way to read the paper is this:

> if a task requires reasoning, do not only ask for the answer. Ask for the reasoning process, even in zero-shot settings.

That does not mean the result should be trusted blindly. It means the prompt can create a better starting point for reasoning than a direct-answer request.

This insight influenced many later developments in:

- prompt engineering
- reasoning traces
- self-consistency
- reflection
- agent planning
- answer verification workflows

## A repeatable checklist

Use this checklist when thinking about *Large Language Models are Zero-Shot Reasoners*:

1. Is the task genuinely multi-step?
2. Would a reasoning cue improve the model's response structure?
3. Are you using zero-shot prompting, rather than example-based few-shot prompting?
4. Is the model capable enough to benefit from the reasoning cue?
5. Are you mistaking visible reasoning for guaranteed correctness?
6. Would verification still be needed after generation?
7. Is the extra output length justified by the gain in quality?
8. Are you focusing on the underlying idea rather than treating one phrase as magical?

## Final summary

*Large Language Models are Zero-Shot Reasoners* (Kojima et al., 2022) is an influential paper showing that large language models can often perform better on reasoning tasks in a zero-shot setting when prompted with a simple step-by-step reasoning cue such as "Let's think step by step." Its key contribution is the claim that explicit reasoning can sometimes be elicited without demonstrations, making reasoning-oriented prompting simpler and more practical.

The paper matters because it showed how much prompt framing can shape model behavior. Its lasting lesson is that reasoning performance is not only a matter of what a model knows, but also of whether the prompt invites the model to use that knowledge through an explicit reasoning path.
