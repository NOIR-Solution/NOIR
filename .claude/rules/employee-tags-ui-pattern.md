# Employee Tags — Card Layout (Not Table)

## Why Employee Tags Uses a Different UI Than Users or Blog Tags

**Employee Tags** (`/portal/hr/tags`) uses a **card-based layout** grouped by category. **Users** and **Blog Tags** use a **DataTable** (table layout). This is intentional.

### Design Rationale

| Aspect | Users / Blog Tags | Employee Tags |
|--------|-------------------|---------------|
| **Data structure** | Flat list — all items are peers | **Categorical** — tags belong to 7 categories (Team, Skill, Project, Location, Seniority, Employment, Custom) |
| **Primary task** | Scan rows, compare values, sort/filter | **Browse by category** — "What skills do we have?" "What teams exist?" |
| **Visual emphasis** | Columns (name, slug, count) | **Color** — each tag has a color; cards give it prominence |
| **Content** | Name + slug + count | **Name + description** — cards provide space for description |
| **Mental model** | List/table for quick lookup | **Taxonomy** — grouped configuration view |

### When to Use Table vs. Card Layout

- **Use DataTable** when: flat list, many columns, sortable, filterable, paginated (Users, Blog Tags, Products, Orders, etc.)
- **Use card layout** when: items are grouped by category/type, color or visual identity matters, descriptions need space, browse-by-category UX (Employee Tags, tag taxonomies)

### Employee Tags Implementation

- `TagsPage.tsx` — groups tags by `EmployeeTagCategory`, renders cards in a grid per category
- No search (small dataset), no pagination
- Each card: color dot, name, description, employee count badge, edit/delete buttons
- Not in DataTable migration scope — different UX pattern by design

### Consistency Note

If you add a new "tags" or "taxonomy" page with categories and colors, prefer the Employee Tags card pattern over a table. If you add a flat entity list, use DataTable.
