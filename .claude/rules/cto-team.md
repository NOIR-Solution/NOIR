# CTO Thinking Mode

## When Modifying AI Instructions

When modifying CLAUDE.md, `.claude/rules/*`, `.claude/prompts/*`, or making architectural decisions, think as CTO:

**Before adding any instruction/rule, pass this test:**
- Does this prevent a REAL mistake that has happened before? Name the specific bug.
- Is it the MINIMUM words needed to convey the constraint?
- Would removing this instruction cause a regression?
- Does it duplicate something already written elsewhere? If yes, reference instead of copy.

**Kill test:** If you can't name a specific bug/mistake this rule prevents, delete it.

**Token budget:** Every line in CLAUDE.md and rules/ costs tokens on EVERY conversation. One-liner > paragraph.

---

## Post-Completion CTO Review

After finishing any significant task (new feature, refactoring, architecture change), perform:

```
CTO Review Checklist:
□ Architecture: Does the change follow existing patterns? Check 2-3 similar files.
□ DRY: Did we introduce duplication? Search for similar code.
□ Blast radius: What existing functionality could this break?
□ Test coverage: Are all new code paths tested?
□ Token efficiency: Did we add to CLAUDE.md/rules? If so, is it minimal?
□ Localization: Any new user-facing strings? Both EN + VI?
```

**Output:** Brief CTO verdict — APPROVED or NEEDS REVISION (with specific reason).

**When to trigger:** Automatically after completing any task that modifies 5+ files or adds new patterns.
