# Notification Dropdown UI/UX Design

## Overview

The notification dropdown provides users with quick access to recent notifications from the sidebar. It features modern animations, time-grouped notifications, and an engaging empty state.

## Components

### 1. NotificationDropdown (`NotificationDropdown.tsx`)

Standalone dropdown component used for header/navbar placement.

**Features:**
- Framer-motion animated entrance/exit (spring animation)
- 380px width with max viewport constraint
- Time-grouped notifications (Today, Yesterday, Earlier this week, Older)
- Header with title and unread count subtitle
- "Mark all read" button when unread notifications exist
- Scrollable notification list (max 420px height)
- Footer with "View all notifications" link

### 2. NotificationEmpty (`NotificationEmpty.tsx`)

Animated empty state displayed when there are no notifications.

**Features:**
- 3 staggered, rotated icons (Mail, BellOff, MessageSquare)
- Spring animations with sequential delays (0.1s, 0.2s, 0.3s)
- "All caught up!" heading with fade-in
- Friendly subtitle: "You don't have any notifications right now..."
- Shadow and border styling on icon cards

### 3. Sidebar Notification Item (`Sidebar.tsx`)

Notification dropdown integrated into the portal sidebar.

**Features:**
- Same animated empty state as standalone component
- Time grouping with uppercase labels
- Relative time display ("2m ago", "1h ago", "3d ago")
- 320px dropdown width
- Unread indicator (blue dot)
- Connection state indicators (amber for connecting, red for disconnected)

## Design Decisions

### Time Grouping

Notifications are grouped by time period for better scannability:
- **Today**: Same calendar day
- **Yesterday**: Previous calendar day
- **Earlier this week**: 2-7 days ago
- **Older**: More than 7 days ago

### Relative Time Format

| Time Difference | Display |
|-----------------|---------|
| < 60 seconds | "Just now" |
| < 60 minutes | "Xm ago" |
| < 24 hours | "Xh ago" |
| < 7 days | "Xd ago" |
| >= 7 days | Localized date |

### Empty State Animation

The 3-icon illustration uses staggered spring animations:
```
Icon 1 (Mail): delay 0.1s, rotate -6deg, offset left
Icon 2 (BellOff): delay 0.2s, center, z-index 10
Icon 3 (MessageSquare): delay 0.3s, rotate 6deg, offset right
```

### Visual Hierarchy

1. **Unread notifications**: Blue left border + light primary background
2. **Read notifications**: Standard background
3. **Time group labels**: Uppercase, muted, smaller font (10-11px)
4. **Relative timestamps**: Right-aligned, muted color

## Accessibility

- Bell button has aria-label with unread count
- Keyboard navigation supported via Radix DropdownMenu
- Focus indicators on all interactive elements
- Screen reader announces notification count

## Dependencies

- `framer-motion`: Animations
- `@radix-ui/react-dropdown-menu`: Dropdown behavior
- `lucide-react`: Icons (Bell, BellOff, Mail, MessageSquare, Check)

## Usage

```tsx
// Standalone dropdown (for headers)
import { NotificationDropdown } from '@/components/notifications'
<NotificationDropdown className="ml-auto" />

// Sidebar integration is built into Sidebar.tsx
// Uses NotificationSidebarItem internally
```

## Future Improvements

- [ ] Sound/vibration toggle in header
- [ ] Notification categories/filters (tabs)
- [ ] Swipe-to-dismiss on mobile
- [ ] Settings shortcut in dropdown footer
- [ ] Real-time notification count animation
