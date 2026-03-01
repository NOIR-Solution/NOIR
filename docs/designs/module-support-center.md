# Module: Support Center (Knowledge Base + Helpdesk)

> Priority: **Phase 5** (after Calendar). Complexity: High. Depends on: HR (employees for assignment), Customers (external tickets).

---

## Why This Module

Every SME needs a unified support system. The Support Center combines **self-service knowledge base** (articles, FAQs, documentation) with **helpdesk ticketing** (issue tracking, SLA, agent assignment). Customers find answers in the knowledge base first; unresolved issues become tickets.

---

## Entities

### Ticketing

```
Ticket (TenantAggregateRoot<Guid>)
├── Id, TicketNumber (auto: TK-00001, sequential per tenant)
├── Subject, Description (rich text)
├── Status (Open/InProgress/WaitingCustomer/WaitingInternal/Resolved/Closed/Reopened)
├── Priority (Low/Medium/High/Critical)
├── CategoryId (FK → TicketCategory)
├── AssigneeId (FK → Employee or User)
├── ReporterId (FK → Employee or User, for internal tickets)
├── CustomerId (FK → Customer, nullable — for external tickets)
├── ChannelSource (Portal/Email/Phone/Zalo/Chat)
├── DueDate, FirstResponseAt, ResolvedAt, ClosedAt
├── SlaId (FK → SlaPolicy, nullable)
├── IsEscalated, EscalatedAt
├── Tags[] (JSON), TenantId
├── Satisfaction (1-5 rating, nullable — post-resolution survey)
└── TicketComments[]

TicketComment (TenantEntity)
├── Id, TicketId (FK), AuthorId (FK → Employee or User)
├── Content (rich text)
├── IsInternal (bool — internal notes invisible to customer)
├── Attachments[] (MediaFileId)
├── CreatedAt, UpdatedAt
└── TenantId

TicketCategory (TenantEntity)
├── Id, Name, Description, Icon
├── ParentCategoryId (hierarchy, optional)
├── SortOrder, IsActive
└── TenantId

SlaPolicy (TenantAggregateRoot<Guid>)
├── Id, Name, Description, IsDefault
├── TenantId
└── SlaRules[]

SlaRule (TenantEntity)
├── Id, SlaPolicyId (FK)
├── Priority (Low/Medium/High/Critical)
├── FirstResponseTime (TimeSpan)
├── ResolutionTime (TimeSpan)
├── EscalationTime (TimeSpan, optional)
└── BusinessHoursOnly (bool)

BusinessHours (TenantEntity)
├── Id, TenantId
├── DayOfWeek (0-6)
├── StartTime, EndTime
└── IsWorkingDay (bool)
```

### Knowledge Base

```
KnowledgeCategory (TenantAggregateRoot<Guid>)
├── Id, Name, Slug, Description, Icon
├── ParentCategoryId (FK → self, hierarchy)
├── SortOrder, IsActive
├── TenantId
└── KnowledgeArticles[]

KnowledgeArticle (TenantAggregateRoot<Guid>)
├── Id, Title, Slug, Content (rich text)
├── CategoryId (FK → KnowledgeCategory)
├── Tags[] (JSON)
├── Status (Draft/Published/Archived)
├── AuthorId (FK → Employee)
├── ViewCount, HelpfulCount, NotHelpfulCount
├── TenantId
└── RelatedArticles[] (self-many-to-many)
```

---

## Features (Commands + Queries)

### Ticket Management
| Command/Query | Description |
|---------------|-------------|
| `CreateTicketCommand` | Create ticket (internal or from customer portal) |
| `UpdateTicketCommand` | Update subject, priority, category, due date |
| `AssignTicketCommand` | Assign to employee |
| `ChangeTicketStatusCommand` | Transition status with validation |
| `EscalateTicketCommand` | Mark as escalated, notify manager |
| `CloseTicketCommand` | Close with optional satisfaction survey |
| `ReopenTicketCommand` | Reopen closed ticket |
| `AddTicketCommentCommand` | Add public reply or internal note |
| `GetTicketsQuery` | Paginated, filter by status/priority/assignee/category |
| `GetTicketByIdQuery` | Full detail with comments, SLA status |
| `GetMyTicketsQuery` | Current user's assigned tickets |

### SLA Management
| Command/Query | Description |
|---------------|-------------|
| `CreateSlaPolicyCommand` | Create SLA with rules per priority |
| `UpdateSlaPolicyCommand` | Update rules |
| `GetSlaPoliciesQuery` | List all SLA policies |
| `CheckSlaBreachesQuery` | Find tickets breaching SLA (for alerts) |

### Knowledge Base
| Command/Query | Description |
|---------------|-------------|
| `CreateCategoryCommand` | Create knowledge base category |
| `UpdateCategoryCommand` | Update category |
| `CreateArticleCommand` | Create knowledge article |
| `UpdateArticleCommand` | Update content |
| `PublishArticleCommand` | Change status to Published |
| `ArchiveArticleCommand` | Archive article |
| `RateArticleCommand` | Helpful/Not helpful vote |
| `GetCategoriesQuery` | Category tree |
| `GetArticlesQuery` | Paginated, filter by category/status, search |
| `GetArticleByIdQuery` | Full article with view count tracking |
| `SearchArticlesQuery` | Full-text search for customer self-service |

### Reports
| Query | Description |
|-------|-------------|
| `GetTicketVolumeReportQuery` | Tickets by period, status, category |
| `GetResponseTimeReportQuery` | Average first response time by agent |
| `GetResolutionTimeReportQuery` | Average resolution time by priority |
| `GetAgentPerformanceReportQuery` | Tickets handled, avg time, satisfaction by agent |
| `GetSatisfactionReportQuery` | Average satisfaction score, trend |
| `GetKnowledgeBaseStatsQuery` | Article views, helpful rate, popular categories |

---

## Ticket Status Workflow

```
Open → InProgress → WaitingCustomer → InProgress → Resolved → Closed
                  → WaitingInternal ↗
Open → Closed (direct close, no resolution needed)
Closed → Reopened → InProgress → ...
```

Valid transitions enforced in `ChangeTicketStatusCommand` validator.

---

## Frontend Pages

| Route | Page | Features |
|-------|------|----------|
| `/portal/support/tickets` | Ticket list | Table, filters (status, priority, assignee, category), my tickets toggle |
| `/portal/support/tickets/:id` | Ticket detail | Comments thread (public + internal), SLA timer, assign, status transitions |
| `/portal/support/knowledge` | Knowledge base | Category tree, article list, search |
| `/portal/support/knowledge/:id` | Article view | Content, helpful rating, related articles |
| `/portal/support/knowledge/new` | Article editor | Rich text editor, category, tags, publish |
| `/portal/support/knowledge/:id/edit` | Article editor | Edit existing article |
| `/portal/support/reports` | Support analytics | Volume, response time, agent performance, satisfaction charts |
| `/portal/support/settings` | Settings | SLA policies, categories, business hours |

### Key UI Components
- **TicketDetailView**: Split view — left: ticket info + status, right: comment thread
- **CommentThread**: Public replies (visible to customer) + internal notes (yellow background, lock icon)
- **SlaTimer**: Countdown showing time remaining for first response / resolution
- **TicketStatusWorkflow**: Visual status transition buttons based on current status
- **KnowledgeCategoryTree**: Collapsible category tree sidebar
- **KnowledgeSearchBar**: Search-as-you-type for articles
- **ArticleRating**: Helpful / Not helpful buttons with count display

---

## Integration Points

| Module | Integration |
|--------|-------------|
| **Customers** | Ticket.CustomerId links to e-commerce customer |
| **HR/Employees** | Ticket.AssigneeId and Article.AuthorId reference Employee |
| **Notifications** | New ticket assigned, SLA breach warning, customer reply |
| **Webhooks** | ticket.created, ticket.resolved, ticket.escalated |
| **Activity Timeline** | Ticket status changes, assignment changes |
| **CRM** | Link ticket to CRM Contact/Lead (future) |
| **Media** | Attachments on comments and articles reuse MediaFile storage |

---

## Phased Implementation

### Phase 1 — Tickets + Comments (MVP)
```
Backend:
├── Domain: Ticket, TicketComment, TicketCategory
├── Application: Ticket CRUD, comments, status transitions, assign
├── Endpoints: TicketEndpoints
├── Module: SupportCenterModuleDefinition (Features: SupportCenter.Tickets)
├── Permissions: support:tickets:*, support:tickets:assign
└── Seed: Default categories (Bug, Feature, Question, Other)

Frontend:
├── Pages: Ticket list, Ticket detail with comment thread
├── Components: TicketDetailView, CommentThread, TicketStatusWorkflow
├── i18n: EN + VI
└── Hooks: useTickets, useTicketById
```

### Phase 2 — Knowledge Base
```
Backend:
├── Domain: KnowledgeCategory, KnowledgeArticle
├── Application: Category tree, article CRUD, publish, rate, search
├── Endpoints: KnowledgeEndpoints
├── Permissions: support:knowledge:*, support:knowledge:publish

Frontend:
├── Pages: Knowledge base list, article view, article editor
├── Components: KnowledgeCategoryTree, KnowledgeSearchBar, ArticleRating
├── i18n: EN + VI
└── Hooks: useKnowledgeCategories, useArticles, useArticleById
```

### Phase 3 — SLA + Business Hours
```
├── Domain: SlaPolicy, SlaRule, BusinessHours
├── Commands: SLA management, business hours config
├── Frontend: SLA timer component, settings page
└── Background: Hangfire job to check SLA breaches every 15 min
```

### Phase 4 — Reports + Auto-assign + Advanced
```
├── Reports: Volume, response time, resolution time, agent performance, satisfaction, KB stats
├── Auto-assign: Round-robin or least-loaded assignment rule (configurable)
├── Escalation: Auto-escalate on SLA breach (notify manager)
├── Customer portal: External customer can create/view tickets (future)
└── Email integration: Create ticket from email (future)
```
