import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { CheckCircle2, Circle, Sparkles, ChevronRight, X } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Progress,
} from '@uikit'
import { cn } from '@/lib/utils'

// Visual replica — avoids React Router (ViewTransitionLink) and localStorage (useOnboarding)
// The real component lives at src/components/onboarding/OnboardingChecklist.tsx

interface ChecklistItemData {
  id: string
  label: string
  description: string
  href: string
}

const ITEMS: ChecklistItemData[] = [
  {
    id: 'profile',
    label: 'Complete your profile',
    description: 'Add your name and profile picture',
    href: '#',
  },
  {
    id: 'explore',
    label: 'Explore the dashboard',
    description: 'Get familiar with the interface',
    href: '#',
  },
  {
    id: 'product',
    label: 'Create your first product',
    description: 'Add a product to your catalog',
    href: '#',
  },
  {
    id: 'blog',
    label: 'Write your first blog post',
    description: 'Share content with your audience',
    href: '#',
  },
  {
    id: 'team',
    label: 'Invite team members',
    description: 'Collaborate with your team',
    href: '#',
  },
]

interface ChecklistItemProps {
  label: string
  description: string
  href: string
  completed: boolean
  onToggle: () => void
}

const ChecklistItemRow = ({ label, description, href, completed, onToggle }: ChecklistItemProps) => (
  <div
    className={cn(
      'group flex items-center gap-3 p-3 rounded-lg transition-colors',
      completed ? 'bg-muted/30' : 'bg-muted/50 hover:bg-muted cursor-pointer'
    )}
  >
    <button
      onClick={onToggle}
      className={cn('flex-shrink-0 transition-transform', !completed && 'hover:scale-110')}
      aria-label={completed ? `${label} completed` : `Mark ${label} as complete`}
    >
      {completed ? (
        <CheckCircle2 className="h-5 w-5 text-green-600" />
      ) : (
        <Circle className="h-5 w-5 text-muted-foreground group-hover:text-primary" />
      )}
    </button>

    <div className="flex-1 min-w-0">
      <a
        href={href}
        onClick={(e) => e.preventDefault()}
        className={cn(
          'text-sm font-medium transition-colors',
          completed ? 'text-muted-foreground line-through' : 'text-foreground hover:text-primary'
        )}
      >
        {label}
      </a>
      <p className="text-xs text-muted-foreground truncate">{description}</p>
    </div>

    {!completed && (
      <span className="flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
        <ChevronRight className="h-4 w-4 text-muted-foreground" />
      </span>
    )}
  </div>
)

interface OnboardingChecklistDemoProps {
  initialCompleted: string[]
  dismissible?: boolean
}

const OnboardingChecklistDemo = ({
  initialCompleted,
  dismissible = true,
}: OnboardingChecklistDemoProps) => {
  const [completedItems, setCompletedItems] = useState<string[]>(initialCompleted)
  const [dismissed, setDismissed] = useState(false)

  const toggle = (id: string) => {
    setCompletedItems((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    )
  }

  const completed = completedItems.length
  const total = ITEMS.length
  const percentage = Math.round((completed / total) * 100)

  if (dismissed) {
    return (
      <div className="p-4 text-sm text-muted-foreground border rounded-lg">
        Checklist dismissed. Refresh to restore.
      </div>
    )
  }

  return (
    <Card className="border-primary/20 bg-gradient-to-br from-primary/5 to-background shadow-sm hover:shadow-md transition-shadow duration-300">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-2">
            <div className="p-1.5 rounded-lg bg-primary/10">
              <Sparkles className="h-4 w-4 text-primary" />
            </div>
            <div>
              <CardTitle className="text-base">Complete Your Setup</CardTitle>
              <CardDescription className="text-xs">
                {completed} of {total} tasks completed
              </CardDescription>
            </div>
          </div>
          {dismissible && (
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-muted-foreground hover:text-foreground"
              onClick={() => setDismissed(true)}
              aria-label="Dismiss onboarding checklist"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>

        <div className="mt-3">
          <Progress value={percentage} className="h-2" />
        </div>
      </CardHeader>

      <CardContent className="pt-0">
        <div className="space-y-2">
          {ITEMS.map((item) => (
            <ChecklistItemRow
              key={item.id}
              label={item.label}
              description={item.description}
              href={item.href}
              completed={completedItems.includes(item.id)}
              onToggle={() => toggle(item.id)}
            />
          ))}
        </div>

        {percentage === 100 && (
          <div className="mt-4 p-3 rounded-lg bg-green-500/10 border border-green-500/20 text-center">
            <p className="text-sm font-medium text-green-600">
              All done! You&apos;re all set up.
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

// ─── Meta ────────────────────────────────────────────────────────────────────

const meta = {
  title: 'Portal/OnboardingChecklist',
  component: OnboardingChecklistDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 420 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof OnboardingChecklistDemo>

export default meta
type Story = StoryObj<typeof meta>

// ─── Stories ─────────────────────────────────────────────────────────────────

/** Default in-progress state — two tasks done, three remaining. */
export const Default: Story = {
  args: {
    initialCompleted: ['profile', 'explore'],
    dismissible: true,
  },
}

/** All five checklist items completed — shows the "All done!" banner. */
export const AllCompleted: Story = {
  args: {
    initialCompleted: ['profile', 'explore', 'product', 'blog', 'team'],
    dismissible: true,
  },
}

/** Fresh start — no items checked yet (0 % progress). */
export const JustStarted: Story = {
  args: {
    initialCompleted: [],
    dismissible: true,
  },
}

/** Non-dismissible variant (no X button), e.g. when embedded in a settings page. */
export const NonDismissible: Story = {
  args: {
    initialCompleted: ['profile'],
    dismissible: false,
  },
}
