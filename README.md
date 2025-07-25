# Task Magic: Automagically create tasks for vibe coding projects

This system helps you manage your software projects by defining plans, breaking them down into actionable tasks, and keeping a memory of what's been done, all while working seamlessly with AI agents like Cursor and Windsurf.

One of the key goals is to avoid the "AI loop of death." ☠️ 

By breaking down work into smaller, focused tasks (each with a clear start, end, and test strategy), AI agents can tackle them effectively without losing context and making dumb mistakes.

(Note: they will still sometimes make dumb mistakes but it is 10x less likely using a task-based approach.)

The system provides permanent context through the `.ai/memory/` folder. This allows AI coding agents to see what has been built before and understand the how and why, helping them to ship new tasks more effectively.

## Project: Virgin Number Management .NET 8 Migration

### Current Status: Tasks Created and Ready for Implementation

This repository contains a comprehensive .NET 8 migration project with **43 actionable tasks** broken down from **10 detailed feature plans**. The project follows an optimized implementation strategy with proper dependency management and parallel execution opportunities.

#### Task Breakdown Summary:
- **Phase 1 (Week 1)**: Foundation and Architecture - 8 tasks (Project Structure + Code Quality)
- **Phase 2 (Weeks 2-4)**: Core Migration - 12 tasks (Entity Framework + WCF + Authentication)  
- **Phase 3 (Weeks 3-5)**: Operational Excellence - 8 tasks (Observability + Resilience)
- **Phase 4 (Weeks 5-6)**: Validation and Deployment - 15 tasks (Testing + API Documentation + Deployment)

#### Key Features:
- **Complex Task Expansion**: Tasks 9 and 13 were expanded into sub-tasks for better manageability
- **Dependency Optimization**: Critical path clearly defined with parallel execution opportunities
- **Implementation Timeline**: 58 detailed implementation days across all enhanced plans
- **Production Ready**: Comprehensive testing and validation throughout migration process

#### Implementation Phases:
1. **Critical Path**: 1 → 2 → 3 → 9.1 → 9.2 → 9.3 → 9.4 → 11 → 13.1 → 13.2 → 13.3 → 13.4 → 15 → 17 → 32 → 36 → 39
2. **Parallel Opportunities**: Code Quality (6-8), Observability (21-24), Resilience (25-28)

#### Quick Start:
- View current tasks: `@.ai/TASKS.md`
- Start with foundation: Task 1 (Setup Clean Architecture Project Structure)
- Review feature plans: `@.ai/plans/features/`

## Video walkthrough

To get an idea of how to use Task Magic in your own projects, check out the video walkthrough below (and also subscribe to the channel, obvs).

[![I built a task manager that makes Cursor "JUST WORK"](https://img.youtube.com/vi/XBu8KeWuQbM/0.jpg)](https://www.youtube.com/watch?v=XBu8KeWuQbM)

## How it works

Task Magic is a file-based system. This means all your project plans, tasks, and history are stored in plain text files (mostly Markdown) right in your project's `.ai/` directory. This makes it easy to version control, track changes, and for AI agents to read and understand your project.

There are three main parts to Task Magic:

1.  **Plans (`.ai/plans/`)**:
    *   **Purpose**: This is where you define the "what" and "why" of your project or specific features. Think of these as your Product Requirements Documents (PRDs).
    *   **Key files**:
        *   `.ai/plans/PLAN.md`: A global overview of your entire project. It should be a concise summary and index, linking to more detailed feature plans.
        *   `.ai/plans/features/{your-feature}-plan.md`: Detailed PRDs for each specific feature you're building. This is where the AI will look for specifics when generating tasks.
    *   **AI interaction**: AI agents use these plans to understand the scope and requirements, helping to generate tasks.

2.  **Tasks (`.ai/tasks/` & `.ai/TASKS.md`)**:
    *   **Purpose**: This is where the actual work items live. AI agents (or you) can break down plans into individual, manageable tasks.
    *   **Key files & structure**:
        *   `.ai/tasks/task{id}_description.md`: Each task gets its own Markdown file. It includes details like status (pending, inprogress, completed), priority, dependencies, a description, and how to test it.
        *   `.ai/TASKS.md`: This is your master checklist. It's a human-friendly overview of all tasks in the `.ai/tasks/` directory, showing their status at a glance. **This file and the individual task files are kept in sync by the AI.**
    *   **AI interaction**: AI agents can create tasks from plans, update their status as they work on them, and help you manage dependencies.

3.  **Memory (`.ai/memory/`)**:
    *   **Purpose**: Completed and failed tasks, as well as old plans, are archived here. This provides a valuable history for the AI to learn from and for you to reference.
    *   **Key files**:
        *   `.ai/memory/tasks/`: Archived task files.
        *   `.ai/memory/TASKS_LOG.md`: A log of when tasks were archived.
        *   `.ai/memory/plans/`: Archived plan files.
        *   `.ai/memory/PLANS_LOG.md`: A log for archived plans.
    *   **AI interaction**: The AI can consult the memory to understand how similar things were done in the past, or why a certain approach was taken.

## Working with AI agents (Cursor & Windsurf)

Task Magic is designed to work closely with AI agents. Here's how rules and context are handled:

*   **Automatic context (`_index.md` files)**:
    *   Files named `_index.md` (like the one in `.windsurf/rules/.task-magic/_index.md` or `.cursor/rules/.task-magic/_index.mdc`) provide a high-level overview of a system or a set of rules.
    *   These `_index.md` files are **automatically included in the AI's context** when you're working within a project that uses them. This gives the AI a foundational understanding without you needing to do anything extra.

*   **On-demand rules (other `.md` or `.mdc` rule files)**:
    *   Other rule files (e.g., `tasks.mdc`, `plans.mdc` located in `.cursor/rules/.task-magic/` or `.windsurf/rules/.task-magic/`) define specific behaviors or knowledge for the AI.
    *   Each of these rule files has a `description` in its header. The AI agent (Cursor/Windsurf) can read these descriptions and **decide dynamically whether a specific rule is relevant** to your current request or the task it's performing.
    *   If the AI deems a rule relevant, it will "fetch" and use that rule.

*   **Your role: Guiding the AI with @-tags**:
    *   While the agent is usually pretty good at figuring out which rules to use, you can manually tag the rules you want to use.
    *   **For best results, @-tag specific rule files or directories in your prompts.** For example:
        *   `@.cursor/rules/.task-magic/tasks.mdc create tasks for this feature`
        *   `@.windsurf/rules/.task-magic/plans.md generate a plan for X`
        *   `@TASKS.md what is the status of my project?` (to refer to the main task checklist)
        *   `@.ai/plans/features/my-cool-feature-plan.md can you review this plan?`
    *   This helps ensure the AI looks at the exact information you want it to.

## Getting started

1.  **Initialize `.ai/` structure**: If these directories don't exist, the AI will typically create them as needed when you ask it to create a plan or task. You can also create them manually.
    *   `.ai/plans/PLAN.md` (start with a simple project title and overview)
    *   `.ai/TASKS.md` (can start with just `# Project Tasks`)
    *   `.ai/memory/TASKS_LOG.md` (can start with `# Task Archive Log`)
2.  **Create a plan**: Ask your AI assistant to create a new feature plan using the planning rule (e.g., `@.cursor/rules/.task-magic/plans.mdc create a plan for user authentication`).
3.  **Generate tasks**: Once a plan is ready, ask the AI to generate tasks from it (e.g., `@.cursor/rules/.task-magic/tasks.mdc generate tasks for the user-authentication-plan.md`).
4.  **Work on tasks**: Tell the AI to start working on tasks. It will update `.ai/TASKS.md` and the individual task files as it progresses.
5.  **Archive**: Periodically, ask the AI to archive completed or failed tasks to keep your active task list clean.

By using Task Magic, you get a structured, AI-friendly way to manage your projects, ensuring both you and your AI assistants are always on the same page.

## Customizing Task Magic

I built this for myself to fit into my workflow. It might not work as well for you.

But since this is file based rules and we're all vibe coding in Cursor/Windsurf, you can easily customize it to fit your workflow.

If something doesn't work for you, tag the rule(s) you want to change, explain what doesn't work (and what you think might) and **have the agent update them for you.** 

For example:

```
@plans.mdc I want my plans to be more concise and simple. Update the rule and make the PRD template shorter and to the point.
```

## Contributing & feedback

This system is designed to evolve. If you have ideas for improvements, find a bug, or want to make Task Magic even better:

*   **Open an issue**: If you encounter problems or have suggestions, please open an issue.
*   **Submit Pull Requests**: If you've made enhancements to the rules or the system structure that you think would benefit others, feel free to submit a pull request.