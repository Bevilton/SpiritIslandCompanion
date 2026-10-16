@AGENTS.md

<!-- Only Claude Code specific notes below. Everything shared with other tools lives in AGENTS.md. -->

## Claude Code

- The layer rules in `.claude/rules/` carry a `paths:` frontmatter and load when you open a
  matching file. Read the one for the layer before writing code there, even for a small change;
  a one-line fix that ignores the template is what the rules exist to prevent.
- Run the app through the `apphost` preview configuration in `.claude/launch.json`, not through
  Bash: it starts Aspire with the SQL Server container and opens the site. Use `/demo` on it for a
  populated account.
- Prefer the demo sandbox for anything that writes data; the real local database is the
  developer's own.
