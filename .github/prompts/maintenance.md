Maintain Lumina Flow for this day's seven-day pilot objective.

Read AGENTS.md, docs/maintenance-7-days.md, and artifacts/maintenance-context.json. The context contains today's objective, day number, and the pilot start date. Follow that objective; do not attempt the entire seven-day plan at once.

Inspect the actual project first. Reproduce a concrete issue, make one focused correction or improvement, and run relevant checks. On the visual day, inspect desktop and mobile screenshots. On the mirror day, verify reflection/splitter rules and Unity/web parity. Never overwrite authored levels with the sample generator.

Use the preinstalled Node, .NET 8 and Chromium. Network access is intentionally unavailable to your commands. artifacts/dependency-audit.json may contain a registry audit collected before your run. If dependencies cannot be updated and tested using available inputs, report the required follow-up instead of inventing a result.

The automation policy allows only the paths listed in AGENTS.md, at most 20 changed files and a 250 KiB patch. Do not modify workflows, agent instructions, automation.py, or automation tests. Do not commit, push, create PRs, or access repository credentials yourself. The workflow will validate and publish your patch from a fresh runner.

Write your final report in Vietnamese: issue found, what changed, checks actually run, and any Unity/device validation still needed. If there is no justified change, explain why; a run without a patch is acceptable.
