# Module: Calendar & Meeting

> Priority: **Phase 5+**. Complexity: Small. Depends on: None (standalone, but integrates with HR/PM/CRM).

---

## Why This Module

Shared calendar ties together leave (HR), task due dates (PM), deal meetings (CRM), and team events. Small scope but high integration value.

---

## Entities

```
CalendarEvent (TenantAggregateRoot<Guid>)
├── Id, Title, Description (rich text)
├── StartTime, EndTime, IsAllDay
├── Location (text or URL for virtual meetings)
├── Recurrence (None/Daily/Weekly/Monthly/Yearly/Custom)
├── RecurrenceRule (iCal RRULE format string, nullable)
├── RecurrenceEndDate (nullable)
├── CreatedById (FK → User), TenantId
├── Color (hex), RemindBefore (minutes: 5/10/15/30/60)
├── Visibility (Private/Team/Public)
├── EventAttendees[]
└── Source (Manual/Leave/TaskDue/CrmMeeting — identifies origin)

EventAttendee (TenantEntity)
├── Id, EventId (FK)
├── UserId (FK → User)
├── Status (Pending/Accepted/Declined/Tentative)
├── IsOrganizer (bool)
├── RespondedAt
└── TenantId
```

---

## Features (Commands + Queries)

### Event Management
| Command/Query | Description |
|---------------|-------------|
| `CreateEventCommand` | Create one-time or recurring event |
| `UpdateEventCommand` | Update event (this occurrence / all / future) |
| `DeleteEventCommand` | Delete event (this / all / future occurrences) |
| `RsvpEventCommand` | Accept/Decline/Tentative |
| `GetEventsQuery` | Events for date range, filter by user/calendar |
| `GetEventByIdQuery` | Full event detail with attendees |

### Views
| Query | Description |
|-------|-------------|
| `GetMonthViewQuery` | All events for a month (optimized) |
| `GetWeekViewQuery` | All events for a week (time slots) |
| `GetDayViewQuery` | Detailed day view with hour slots |
| `GetAgendaQuery` | Upcoming events list (next 7/30 days) |
| `CheckConflictsQuery` | Find time conflicts for attendees |

---

## Frontend Pages

| Route | Page | Features |
|-------|------|----------|
| `/portal/calendar` | Calendar | Month/Week/Day toggle, event creation, click event to view |
| `/portal/calendar/event/:id` | Event detail | Attendees, RSVP, edit, delete |

### Key UI Components
- **CalendarGrid**: Month/Week/Day views (use `react-big-calendar` or custom CSS Grid)
- **EventPopover**: Quick view on event click
- **EventForm**: Create/edit dialog with recurrence picker
- **MiniCalendar**: Small month calendar for sidebar navigation
- **AgendaList**: Upcoming events as timeline

---

## Integration Points (read-only overlays)

| Source Module | What shows on Calendar | Color |
|---------------|----------------------|-------|
| **Manual events** | User-created events | User-chosen color |
| **HR Leave** | Approved leave requests | Orange |
| **HR Holidays** | Company holidays | Red |
| **PM Tasks** | Task due dates | Blue |
| **CRM Activities** | Scheduled meetings/calls | Green |

Integration is read-only overlays — source modules create CalendarEvent records with `Source` field indicating origin.

---

## Phased Implementation

### Phase 1 — MVP (Calendar + Events)
```
Backend:
├── Domain: CalendarEvent, EventAttendee
├── Application: Event CRUD, RSVP, date range queries
├── Endpoints: CalendarEndpoints
├── Module: CalendarModuleDefinition
├── Permissions: calendar:events:read, calendar:events:create, calendar:events:manage
└── No recurrence in MVP (one-time events only)

Frontend:
├── Pages: Calendar with month/week/day views
├── Library: react-big-calendar or @schedule-x/react
├── Components: CalendarGrid, EventPopover, EventForm
└── i18n: EN + VI (day names, month names handled by library locale)
```

### Phase 2 — Recurrence + Integration
```
├── Recurrence: iCal RRULE parsing, recurring event expansion
├── Integration: Display HR leaves, PM due dates, CRM meetings
├── Conflict detection: Warn when scheduling overlapping events
└── Reminders: Use existing Notification module for event reminders
```

### Phase 3 — Advanced
```
├── Multiple calendars: Personal + team + shared calendars
├── External sync: iCal export/import (.ics files)
├── Google Calendar sync (OAuth, future)
└── Meeting rooms: Resource booking (future)
```
