# UI/UX Tool Routing - Quick Reference

**✅ We use `/ui-ux-pro-max` skill for ALL UI/UX work (research AND implementation).**

---

## Decision Tree

```
User asks for UI/UX work
│
├─ Is it RESEARCH/PLANNING?
│  ├─ "What color palette should I use?"
│  ├─ "Show me design inspiration for..."
│  ├─ "What are UX best practices for..."
│  └─ → USE: /ui-ux-pro-max skill
│
├─ Is it BUILDING/IMPLEMENTING code?
│  ├─ Creating components (buttons, modals, forms, pages)
│  ├─ Writing .tsx/.vue/.svelte/.html files
│  ├─ Implementing designs
│  └─ → USE: /ui-ux-pro-max skill
│
├─ Is it REFINING/IMPROVING?
│  ├─ "Improve this component"
│  ├─ "Add accessibility to..."
│  ├─ "Refactor this UI code"
│  └─ → USE: /ui-ux-pro-max skill
│
└─ Is it CODE REVIEW?
   ├─ "Review this component"
   ├─ "Check accessibility"
   ├─ "Audit this UI code"
   └─ → USE: /ui-ux-pro-max skill
```

---

## Tool Purpose

| Tool | Use For | Examples |
|------|---------|----------|
| **`/ui-ux-pro-max` skill** | **ALL UI/UX work** | Research, implementation, refinement, review |

**The `/ui-ux-pro-max` skill handles:**
- Design research (color palettes, typography, styles)
- UX best practices and guidelines
- Component generation (React/TypeScript with shadcn/ui)
- Component refinement and improvements
- Accessibility audits
- UI code review

---

## Common User Requests → Correct Tool

| User Says | Correct Tool | Type |
|-----------|-------------|------|
| "Build a product card component" | `/ui-ux-pro-max` | Implementation |
| "Create a checkout page" | `/ui-ux-pro-max` | Implementation |
| "Add a modal dialog for user settings" | `/ui-ux-pro-max` | Implementation |
| "What color palette for e-commerce?" | `/ui-ux-pro-max` | Research |
| "Show me glassmorphism examples" | `/ui-ux-pro-max` | Research |
| "UX best practices for forms" | `/ui-ux-pro-max` | Research |
| "Review my navbar component" | `/ui-ux-pro-max` | Review |
| "Implement the design from Figma" | `/ui-ux-pro-max` | Implementation |
| "Improve this component" | `/ui-ux-pro-max` | Refinement |
| "Add accessibility to form" | `/ui-ux-pro-max` | Refinement |

---

## How /ui-ux-pro-max Works

1. **User request**: Any UI/UX task (research, implementation, refinement, review)
2. **Claude calls**: `Skill tool with skill="ui-ux-pro-max"`
3. **ui-ux-pro-max skill**:
   - Analyzes the request
   - Provides design guidance (if research)
   - Generates/refines code (if implementation)
   - Reviews code quality (if review)
4. **Result**: Complete UI/UX solution

---

## Integration with SuperClaude Routing

**Priority Override**: UI/UX work has higher priority than general implementation.

When user says:
- "implement feature X" → `/sc:implement` (generic backend/logic)
- "build UI for feature X" → `/ui-ux-pro-max` (UI-specific, higher priority)
- "create component for feature X" → `/ui-ux-pro-max` (UI-specific, higher priority)

---

## Usage Pattern

### ✅ CORRECT: Always use /ui-ux-pro-max for UI/UX
```typescript
// Any UI/UX request → /ui-ux-pro-max skill

User: "Build a product card component"
Claude: Calls Skill tool with skill="ui-ux-pro-max" // CORRECT!

User: "What color scheme for dashboard?"
Claude: Calls Skill tool with skill="ui-ux-pro-max" // CORRECT!

User: "Review my navbar component"
Claude: Calls Skill tool with skill="ui-ux-pro-max" // CORRECT!

User: "Improve this modal accessibility"
Claude: Calls Skill tool with skill="ui-ux-pro-max" // CORRECT!
```

---

## FAQ

**Q: When should I use /ui-ux-pro-max?**
A: For ALL UI/UX work - research, implementation, refinement, and review.

**Q: What if user asks for both backend and frontend?**
A: Use appropriate skills for each part:
- Backend logic → `/sc:implement` or direct implementation
- Frontend UI → `/ui-ux-pro-max`

**Q: What frameworks does /ui-ux-pro-max support?**
A: React, Next.js, Vue, Svelte, SwiftUI, React Native, Flutter - with focus on React/TypeScript + shadcn/ui for this project.

**Q: Does /ui-ux-pro-max handle accessibility?**
A: Yes! It includes accessibility best practices, ARIA labels, keyboard navigation, and screen reader support.

---

## Validation Checklist

Before responding to a UI/UX request, ask yourself:

- [ ] Is this about UI components, pages, or design?
      → YES: Use `/ui-ux-pro-max`
      → NO: Use appropriate skill for the domain

- [ ] Does it involve React/TypeScript/CSS/HTML?
      → YES: Use `/ui-ux-pro-max`
      → NO: Check if it's backend logic

---

**Last Updated**: 2026-01-26
**Related Docs**: CLAUDE.md, superclaude-routing.md, frontend-architecture.md
