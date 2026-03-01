# Module: Internal Chat

> Priority: **Phase 7** (nice-to-have). Complexity: Medium. Depends on: SignalR (existing), Media (existing).

---

## Why This Module

Internal team communication. Leverages NOIR's existing SignalR infrastructure (NotificationHub) and Media module for file sharing.

---

## Entities

```
ChatChannel (TenantAggregateRoot<Guid>)
├── Id, Name, Description
├── Type (Direct/Group/Public)
├── CreatedById (FK → User), TenantId
├── AvatarUrl (nullable)
├── IsArchived
├── LastMessageAt (for sorting)
└── ChannelMembers[]

ChatMessage (TenantAggregateRoot<Guid>)
├── Id, ChannelId (FK)
├── SenderId (FK → User)
├── Content (text, supports markdown)
├── ParentMessageId (FK → ChatMessage, nullable — thread replies)
├── IsEdited, EditedAt
├── IsDeleted (soft delete — shows "[message deleted]")
├── Attachments[] (JSON: MediaFileId[])
├── Reactions[] (JSON: [{userId, emoji}])
├── CreatedAt
└── TenantId

ChannelMember (TenantEntity)
├── Id, ChannelId (FK), UserId (FK)
├── Role (Admin/Member)
├── JoinedAt
├── LastReadMessageId (FK → ChatMessage, for unread count)
├── IsMuted (bool)
└── TenantId
```

---

## Features (Commands + Queries)

### Channels
| Command/Query | Description |
|---------------|-------------|
| `CreateChannelCommand` | Create group or public channel |
| `CreateDirectMessageCommand` | Create or find existing DM channel |
| `UpdateChannelCommand` | Update name, description |
| `ArchiveChannelCommand` | Archive channel |
| `JoinChannelCommand` | Join public channel |
| `LeaveChannelCommand` | Leave channel |
| `MuteChannelCommand` | Mute notifications |
| `GetChannelsQuery` | User's channels, sorted by lastMessageAt |
| `GetChannelByIdQuery` | Channel detail with members |

### Messaging
| Command/Query | Description |
|---------------|-------------|
| `SendMessageCommand` | Send message (via SignalR hub) |
| `EditMessageCommand` | Edit own message |
| `DeleteMessageCommand` | Soft delete own message |
| `ReplyToMessageCommand` | Reply in thread |
| `ReactToMessageCommand` | Add/remove emoji reaction |
| `MarkAsReadCommand` | Update LastReadMessageId |
| `GetMessagesQuery` | Paginated messages for channel (cursor-based) |
| `GetThreadQuery` | Messages in a thread |
| `SearchMessagesQuery` | Full-text search within channel or global |
| `GetUnreadCountsQuery` | Unread count per channel |

---

## Frontend Pages

| Route | Page | Features |
|-------|------|----------|
| `/portal/chat` | Chat interface | Left sidebar: channels list. Right: message area. |
| `/portal/chat/:channelId` | Channel view | Messages, input, thread panel |

### Key UI Components
- **ChannelSidebar**: Channel list grouped by type, unread badges, search
- **MessageList**: Virtualized message list (scroll to load older), auto-scroll on new
- **MessageInput**: Markdown editor, file upload, @mention autocomplete, emoji picker
- **ThreadPanel**: Slide-over panel for thread replies
- **MessageBubble**: Message with avatar, timestamp, reactions, edit/delete actions

---

## Real-Time Architecture

```
ChatHub (extends existing SignalR infrastructure):
├── SendMessage(channelId, content, parentMessageId?)
├── EditMessage(messageId, newContent)
├── DeleteMessage(messageId)
├── MarkAsRead(channelId, messageId)
├── ReactToMessage(messageId, emoji)
├── OnTyping(channelId)  — typing indicator
└── Groups: Users join SignalR groups per channel membership
```

- Reuse existing SignalR authentication + tenant context
- Connection management: auto-reconnect on disconnect
- Typing indicators: debounced, auto-clear after 3 seconds

---

## Integration Points

| Module | Integration |
|--------|-------------|
| **SignalR** | Extend existing NotificationHub or create separate ChatHub |
| **Media** | File sharing via drag-and-drop upload |
| **Users** | @mention users, avatar display |
| **Notifications** | Unread badge in sidebar, push notification for DMs |

---

## Phased Implementation

### Phase 1 — MVP (Channels + Messages)
```
Backend:
├── Domain: ChatChannel, ChatMessage, ChannelMember
├── SignalR: ChatHub with send/receive/mark-read
├── Application: Channel CRUD, message CRUD, cursor-based pagination
├── Endpoints: ChatEndpoints (REST for history, SignalR for real-time)
├── Module: ChatModuleDefinition
└── Permissions: chat:channels:create, chat:channels:manage

Frontend:
├── Pages: Chat layout with channel sidebar + message area
├── Components: ChannelSidebar, MessageList, MessageInput
├── Real-time: useSignalR hook for ChatHub connection
└── i18n: EN + VI
```

### Phase 2 — Threads + Reactions + Search
```
├── Threads: Reply in thread, thread panel
├── Reactions: Emoji picker, reaction display
├── Search: Message search within channel/global
├── Typing indicators
└── File sharing: Drag-and-drop, image preview
```

### Phase 3 — Advanced
```
├── Message retention: Auto-delete old messages per policy
├── Pinned messages: Pin important messages in channel
├── @channel mentions: Notify all members
├── Read receipts: Show who has read message
└── Bot integrations: Webhook-triggered messages (future)
```

---

## Architecture Notes

### Message Retention
- Consider message retention policies for large tenants
- Default: keep all messages. Configurable per tenant (30/90/365 days).
- Archived channels: read-only, messages still accessible

### Performance
- Cursor-based pagination (not offset) for message history
- Virtual scrolling for message list (reuse `useVirtualList` pattern)
- SignalR group management: add/remove from groups on channel join/leave
- Message search: consider full-text index on ChatMessage.Content
