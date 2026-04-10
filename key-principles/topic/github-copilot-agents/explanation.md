# GitHub Copilot Agents

## What GitHub Copilot agents are

GitHub Copilot agents are AI-driven coding systems that can take a high-level goal, break it into smaller steps, use tools, act on your codebase, and iterate until the task is complete or blocked. In practical terms, an agent is not only a chatbot that replies with text. It is a working system that can:

- inspect files and code structure
- search a codebase for relevant information
- edit files
- run terminal commands
- fetch external information when allowed
- validate its own work by checking output, errors, or tests
- revise its approach when something fails

This makes an agent different from a plain question-and-answer assistant. A normal assistant may explain what to do. An agent is designed to **do the work of moving a task forward**.

Within Visual Studio Code, GitHub Copilot agents are presented through a unified chat experience, but they can run in different environments and with different levels of autonomy. Some work interactively in the editor. Others run in the background on your machine. Others run remotely in the cloud and integrate directly with GitHub workflows such as pull requests.

## The core mental model

A useful way to understand GitHub Copilot agents is to think in terms of three layers:

1. **The agent loop**: how the system reasons, acts, and validates.
2. **The execution environment**: where the agent runs.
3. **The agent role**: what kind of behavior or persona the agent is using.

These three layers explain most of the system.

### The agent loop

GitHub Copilot agents follow an iterative loop that can be summarized as:

1. **Understand** the task.
2. **Act** using tools.
3. **Validate** the result.
4. **Repeat** until the work is done.

This matters because agentic work is not a single response. The system can read a file, decide it needs more context, search the codebase, edit code, run a command, inspect the result, notice a failure, and then correct itself. The value comes from this loop, not from one isolated answer.

### The execution environment

The environment determines where the agent runs and what it can access. In VS Code, the main environments are:

- **Local agents** in the editor on your machine
- **Copilot CLI sessions** in the background on your machine
- **Cloud agents** running remotely and integrated with GitHub

Different environments are good for different kinds of work.

### The agent role

The role determines how the agent behaves. A role might be general-purpose implementation, planning, asking questions, code review, or a custom persona such as a security reviewer or documentation writer.

In VS Code, this role can come from:

- built-in agents such as **Agent**, **Plan**, and **Ask**
- custom agents that define instructions, tools, and optional model choices
- orchestration patterns where one agent coordinates subagents with narrower duties

## Agents in the wider GitHub Copilot system

One reason this topic overlaps with nearby customization topics is that agents sit inside a broader GitHub Copilot system. Agents are important, but they are not the only reusable AI mechanism in the ecosystem.

A useful distinction is:

- **agents** execute work
- **instruction files** define rules and conventions
- **prompt files** define reusable task requests
- **skill files** package repeatable workflows and supporting resources

These pieces can work together, but they are not interchangeable.

### Agents versus instruction files

An instruction file tells the AI how it should behave when working in a certain context.

Examples of instruction-file behavior include:

- use a certain folder structure
- follow a certain writing framework
- prefer one code pattern over another
- keep content self-contained

An agent, by contrast, is the active worker that reads context, uses tools, and moves the task forward.

A simple way to say it is:

- an instruction file provides guidance
- an agent performs the work

### Agents versus prompt files

A prompt file is a reusable request or workflow entry point.

It helps with asking for a task in a consistent way, but it is not itself the worker. The agent is still the system that interprets the request, chooses tools, and executes the task.

So:

- a prompt file helps start the work
- an agent carries the work through

### Agents versus skill files

Skill files package reusable task knowledge, procedures, references, scripts, or assets.

A skill is closer to a capability bundle than to an autonomous worker. It can help an agent perform a specialized task, but it is not the agent itself.

So:

- a skill provides reusable operational knowledge
- an agent loads and uses that knowledge when it is relevant

### Why this distinction matters

This distinction matters because teams often confuse configuration artifacts with execution systems.

If those are mixed up, the result is confusion such as:

- expecting an instruction file to behave like a workflow engine
- expecting a skill file to replace an agent role
- expecting an agent to automatically know local rules without instructions

The broader Copilot system works best when each part does its own job:

- agents execute
- instructions constrain
- prompts trigger
- skills package repeatable know-how

## Why the system has multiple kinds of agents

A single agent design would be simpler to describe, but it would not match real development work very well. Development tasks vary along a few important axes:

- how interactive the work needs to be
- whether the task needs live editor context
- whether the task should continue while you do something else
- whether the work should stay local or run remotely
- whether the goal is research, planning, implementation, review, or collaboration

Because those needs differ, GitHub Copilot exposes different agent types rather than forcing one mode to fit everything.

## The major GitHub Copilot agent types in VS Code

### Local agents

Local agents run inside Visual Studio Code on your machine. They work directly against your current workspace and are designed for interactive back-and-forth work.

Local agents are the best fit when you need:

- fast feedback
- interactive clarification
- direct access to editor context
- access to extension-provided tools or MCP servers
- access to local runtime information such as failing tests, lint errors, stack traces, or debug output

Because they run inside VS Code, local agents can use the broadest set of tools available in your editor environment.

#### Built-in local agents

VS Code provides three built-in local agents.

##### Agent

**Agent** is the main general-purpose implementation agent. It is designed for multi-file coding work and can autonomously plan, edit, run commands, and iterate.

Use Agent when the task is something like:

- implement a feature
- fix a bug
- update several related files
- run tests and refine changes
- coordinate multiple tools during implementation

##### Plan

**Plan** is the planning-focused agent. It researches the task, asks clarifying questions, and creates a structured implementation plan before code changes are made.

Use Plan when:

- the task is complex or underspecified
- the architecture matters
- you want to agree on an approach before editing code
- you need a breakdown of implementation and verification steps

The Plan agent uses a staged process centered on research, clarification, design, and refinement. Its purpose is not to write code immediately. Its purpose is to improve decisions before coding starts.

##### Ask

**Ask** is optimized for explanation, codebase questions, idea exploration, and understanding.

Use Ask when:

- you want to understand how something works
- you want answers about your codebase
- you want to explore approaches before acting
- you want guidance without direct file edits

Ask still benefits from agentic capabilities such as codebase research, but its role is question answering rather than implementation.

#### What makes local agents powerful

Local agents are powerful because they combine several things at once:

- direct workspace access
- interactive user steering
- tool access inside VS Code
- the ability to use the models available in your local VS Code setup

That combination makes them especially strong for iterative work where the task is evolving as you learn more.

#### Trade-offs of local agents

Local agents are not ideal for every task.

Their main trade-offs are:

- they are oriented toward active interaction, not unattended long-running work
- they depend on the current editor environment being open and available
- they are less suited than cloud workflows for pull-request-based team collaboration

### Copilot CLI sessions

Copilot CLI sessions are autonomous background agents that run on your local machine using the Copilot CLI harness. In VS Code, you can create, monitor, and manage these sessions from the Chat view while they continue running in the background.

This makes Copilot CLI useful when you want the agent to keep working while you do something else.

#### When Copilot CLI is a strong choice

Copilot CLI is well suited for tasks that:

- have a clear scope
- already have enough context
- do not require frequent human steering
- benefit from running in the background
- should stay on your local machine rather than in the cloud

Examples include:

- implementing a well-defined plan
- building proof-of-concept variants
- carrying out a defined refactor or fix
- running multiple independent agent sessions in parallel

#### Isolation modes

Copilot CLI supports two important isolation modes.

##### Workspace isolation

In workspace isolation, the agent works directly in your current workspace. Changes are applied in place.

This is more direct, but it means the background agent is operating on the same working copy you are using.

##### Worktree isolation

In worktree isolation, VS Code creates a separate Git worktree for the session. The agent operates in that worktree rather than in your main working directory.

This has several important effects:

- it isolates the agent's changes from your main workspace
- it reduces interference with your active work
- it makes the session safer for parallel experimentation
- it gives a cleaner review boundary for the session's output

Worktree isolation requires the workspace to be a Git repository.

#### Why Copilot CLI is distinct from local agents

Although Copilot CLI still runs on your machine, it differs from local agents in a few major ways:

- it is background-oriented rather than primarily interactive
- it can continue running even if you close the VS Code window
- it does not have full access to all VS Code built-in and extension-provided tools
- it is more constrained in tool and model availability than local agents

That means Copilot CLI is not merely "local agent, but different." It is a separate operating mode designed for autonomous background execution.

#### Permissions in Copilot CLI

Copilot CLI uses the same general permission ideas as local agents, but the available approval behavior depends on isolation mode.

- In **worktree isolation**, the permission level is effectively bypass approvals because the work happens in an isolated worktree.
- In **workspace isolation**, the usual permission levels are available.

This reflects a core design principle: stronger isolation can justify higher autonomy.

#### Limitations of Copilot CLI

Copilot CLI currently has some limitations compared with local agents:

- it cannot access every built-in VS Code tool
- it does not have access to extension-provided tools
- it is limited to the models available through the CLI tool
- it can only access local MCP servers that do not require authentication

These limits matter because they define when Copilot CLI is a good handoff target and when a local agent remains the better tool.

#### Copilot CLI and handoff workflows

A common pattern is:

1. use a local agent, often Plan, to refine the task
2. hand off the approved work to Copilot CLI
3. let the background agent implement autonomously
4. resume or review the session later

This pattern is useful because it separates **high-touch clarification** from **low-touch execution**.

### Cloud agents

Cloud agents run on remote infrastructure rather than on your local machine. In the GitHub ecosystem, the primary cloud agent is the **GitHub Copilot coding agent**.

Cloud agents are designed for autonomous work with strong GitHub integration, especially around repository workflows and pull requests.

#### What cloud agents are best for

Cloud agents are strongest when:

- the task is well-defined
- collaboration with teammates matters
- the output should be reviewable through a pull request
- the agent should operate remotely rather than against your local environment
- the task can proceed without direct access to local editor state

Typical cloud-agent strengths include:

- implementing features from high-level requirements
- performing larger refactors across a repository
- addressing code review feedback
- generating pull requests automatically
- participating in GitHub-centered team workflows

#### What cloud agents cannot access directly

Because they run remotely, cloud agents do not directly have access to the same kinds of context that a local agent has in VS Code.

They cannot directly rely on things like:

- your current text selection
- editor-local state
- local runtime output such as immediate failed test runs in your machine's workspace
- the full range of VS Code built-in tools

They are limited to the models and MCP capabilities configured for the cloud service.

This is a major trade-off. Cloud agents gain autonomy and collaboration benefits, but lose some direct local context.

#### GitHub Copilot coding agent

The GitHub Copilot coding agent is the main cloud agent in this ecosystem. It can:

- take a repository task
- work on it remotely
- push changes to a pull request
- add the requester as reviewer
- continue reporting progress through its session log
- accept steering input while it is running

This makes it more than a background worker. It is a cloud-native coding workflow integrated with GitHub review mechanics.

#### Managing cloud work

Cloud agent work can be started in several ways:

- directly from VS Code
- by handing off from a local or CLI session
- directly from GitHub interfaces such as repository agent views

Once running, a cloud session can be monitored, steered, opened in local tools, reviewed through a pull request, and eventually archived.

#### Why cloud agents exist separately from Copilot CLI

Both Copilot CLI and cloud agents support autonomous work, but they serve different priorities.

Copilot CLI emphasizes:

- your machine
- local isolation options
- background work in your development environment

Cloud agents emphasize:

- remote execution
- GitHub repository integration
- pull requests and collaboration
- GitHub-based monitoring and management

If the work should become a collaborative PR-centered artifact, cloud agents usually fit that goal better.

## Subagents

Subagents are not a separate top-level environment like local, CLI, or cloud agents. Instead, subagents are a way for one agent to delegate focused work to another agent instance.

A subagent performs a narrower task and returns a result to the main agent.

### Why subagents matter

The main reason subagents matter is **context isolation**.

Without subagents, all intermediate research and exploration remain in the main conversation context. Over time, that can crowd out the most important information.

Subagents solve this by:

- running in a separate context window
- doing focused work independently
- returning only a summary or final result to the main agent

This keeps the main agent more focused and can improve both quality and efficiency.

### Typical subagent use cases

Subagents are especially useful for:

- isolated research before implementation
- parallel analysis from different perspectives
- multi-step orchestration patterns
- review workflows where separate perspectives should not bias one another

Examples include:

- one subagent checks security while another checks architecture
- one subagent researches a library while the main agent continues planning
- a coordinator agent delegates tasks to planner, implementer, and reviewer workers

### Important properties of subagents

Subagents typically have these characteristics:

- they run with isolated context
- they return focused outputs rather than full internal history
- they can be run synchronously, where the parent waits for them
- they can often run in parallel for independent analyses

Nested subagents are possible in some workflows, but they are not always enabled by default because uncontrolled recursion would be risky.

## Custom agents

Custom agents let you define your own persistent agent roles.

A custom agent can specify:

- instructions
- tool access
- model preferences
- which subagents may be used
- handoff behavior
- visibility rules such as whether users can invoke it directly

This means the system is not limited to the built-in Agent, Plan, and Ask modes. A team can create its own roles such as:

- security reviewer
- architecture checker
- documentation writer
- release coordinator
- test-driven development orchestrator

### Why custom agents matter

Custom agents matter because different tasks should not always have the same permissions, same instructions, or same priorities.

For example:

- a planning agent should often be read-only
- an implementation agent needs edit and terminal capabilities
- a review agent might focus on correctness, style, or security
- a coordinator agent might be allowed to spawn only specific worker agents

This turns agent behavior into something configurable rather than fixed.

### Handoffs

Custom agents can define handoffs, which are guided transitions from one agent role to another.

A common example is:

- planning agent produces a plan
- handoff moves to implementation agent
- implementation handoff moves to review agent

Handoffs matter because they create structure without forcing everything into one conversation pattern.

## Tools and permissions

Agents are only as useful as the actions they are allowed to take. In VS Code, tools extend agents with capabilities such as reading files, searching code, running commands, fetching web content, and calling other systems.

Tools can come from:

- built-in VS Code capabilities
- MCP servers
- extensions

### Tool access is part of agent design

An important idea is that agent quality is not only about model intelligence. It is also about whether the agent has the right tools for the task.

A planning agent with only read-oriented tools behaves very differently from an implementation agent that can edit files and run commands.

This is why tool selection is a first-class design decision for agents.

### Permission levels

VS Code exposes permission levels to control how autonomous an agent may be.

The main permission levels are:

- **Default Approvals**: approvals follow configured safety rules
- **Bypass Approvals**: tool calls are auto-approved
- **Autopilot**: tool calls are auto-approved, clarifying questions are auto-answered, and the agent continues until it decides the task is complete

These permission levels represent a trade-off between speed and oversight.

### The key trade-off

More autonomy can improve speed and continuity. But more autonomy also reduces the moments where a human can intercept or review actions before they happen.

That means autonomy should be increased deliberately, not casually.

### Security implications

Because agents can edit files, run terminal commands, and fetch external information, tool approval and sandboxing matter.

Important ideas in the Copilot agent model include:

- tool approvals can be configured and reviewed
- URL requests may require separate approval steps
- terminal auto-approval is configurable but risky
- sandboxing can restrict file system and network behavior
- least privilege is a good design principle for custom agents

A useful rule is simple: do not give an agent more capability than the task requires.

## Planning as a distinct activity

Planning is important enough in the GitHub Copilot model to be treated as its own agent role.

This reflects a practical truth: many coding failures happen before implementation starts. The problem is not always bad coding. Often the problem is weak requirements, unclear architecture, or skipped edge cases.

The Plan agent addresses this by separating:

- understanding the task
- clarifying ambiguity
- designing an approach
- only then moving to implementation

That separation is one of the clearest examples of agent specialization in the Copilot system.

## Memory

GitHub Copilot agents can use memory systems to preserve useful context across work.

In VS Code, memory can be organized into scopes such as:

- user-level memory
- repository-level memory
- session memory

This matters because agentic work becomes more useful when the system can remember preferences, conventions, and repository-specific knowledge rather than starting from nothing every time.

Planning also uses memory in a practical way: the Plan agent can save its implementation plan into session memory for the current conversation.

## A practical comparison of the main agent types

The easiest way to compare the major GitHub Copilot agent types is by asking three questions:

1. Where does it run?
2. How interactive is it?
3. What kind of outcome is it best for?

### Local agents at a glance

- Runs in VS Code on your machine
- Highly interactive
- Best for iterative work, editor-aware tasks, live debugging, and tool-rich workflows

### Copilot CLI sessions at a glance

- Runs on your machine in the background
- Less interactive during execution
- Best for clearly scoped autonomous tasks that should continue while you work on something else

### Cloud agents at a glance

- Run remotely
- Autonomous and collaboration-oriented
- Best for GitHub-integrated workflows, larger repository tasks, and PR-based review and team collaboration

## How to choose the right GitHub Copilot agent

A simple decision model looks like this:

- If you need immediate interaction and editor context, use a **local agent**.
- If you already know what should be done and want the work to continue on your machine in the background, use **Copilot CLI**.
- If the task is well-defined and should end in a pull request for collaborative review, use a **cloud agent**.
- If the task is unclear or complex, start with **Plan**.
- If the task is mostly about understanding or questions, start with **Ask**.
- If the task needs focused delegation or multiple perspectives, use **subagents**.
- If the task should always follow a specific persona or tool policy, create a **custom agent**.

## A worked workflow example

Imagine a team wants to implement a significant authentication change.

A strong GitHub Copilot agent workflow could look like this:

1. Use **Plan** locally to refine requirements and produce an implementation plan.
2. Review the plan and clarify edge cases.
3. Hand off to **Copilot CLI** if the team wants autonomous work on the developer's machine, possibly in a worktree.
4. Or hand off to a **cloud agent** if the team wants the work to proceed remotely and end in a pull request.
5. Use **custom agents** or **subagents** for specialized review, such as security analysis or architectural validation.
6. Review the resulting changes and merge once satisfied.

What matters here is not memorizing the exact sequence. What matters is understanding that GitHub Copilot agents are meant to support **multi-stage workflows**, not just isolated prompts.

## Common misunderstandings

### Mistake 1: thinking all agents are basically the same

They are not. Local, CLI, and cloud agents differ in environment, context access, autonomy, and collaboration model.

### Mistake 2: assuming autonomy means no oversight is needed

Higher autonomy increases efficiency, but it also increases the importance of permissions, review, and tool safety.

### Mistake 3: using implementation agents when the task is still unclear

Many tasks should begin with planning or clarification rather than direct code generation.

### Mistake 4: treating subagents as magic rather than structure

Subagents are valuable because they isolate context and focus work. Their value is architectural, not mystical.

### Mistake 5: giving every agent every tool

That creates unnecessary risk and weakens role specialization. Better agents often have narrower, well-chosen tool access.

## Why GitHub Copilot agents matter

GitHub Copilot agents matter because they move AI-assisted development away from simple suggestion generation and toward structured, tool-using, iterative work.

The key shift is this:

- older assistance models mainly suggested code
- agentic models can investigate, act, validate, revise, and coordinate

This does not remove the need for human judgment. Instead, it changes the role of the developer from typing every step manually to supervising, steering, constraining, and reviewing a more capable system.

## Final summary

GitHub Copilot agents in Visual Studio Code form a family of agentic workflows rather than a single mode. Local agents handle interactive, tool-rich work in the editor. Copilot CLI handles autonomous background work on your machine, often with isolation options. Cloud agents handle autonomous remote work integrated with GitHub collaboration and pull requests.

Around those main environments are important supporting ideas: built-in agents such as Agent, Plan, and Ask; subagents for focused delegation; custom agents for specialized roles; tools and permissions for controlled capability; memory for continuity across work; and adjacent customization mechanisms such as instruction files, prompt files, and skill files.

The most important principle is to match the agent to the task. The best GitHub Copilot workflow is not the one with the most autonomy. It is the one where the execution environment, role, tools, and level of oversight fit the real job to be done.
