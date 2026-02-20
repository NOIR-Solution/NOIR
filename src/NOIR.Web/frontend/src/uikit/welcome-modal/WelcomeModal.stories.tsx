import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { Sparkles, ArrowRight, X, Rocket, Shield, Users, Check, CircleDot } from 'lucide-react'
import { Button, Dialog, DialogContent, DialogHeader, DialogTitle } from '@uikit'

// --- Visual Replica ---
// WelcomeModal depends on AuthContext, useOnboarding hook, react-router-dom, and
// framer-motion. This self-contained demo replicates the visual appearance without
// requiring those external contexts. Animations are replaced with CSS transitions.

interface FeatureCardProps {
  icon: React.ElementType
  title: string
  description: string
}

const FeatureCard = ({ icon: Icon, title, description }: FeatureCardProps) => (
  <div className="flex items-start gap-3 p-4 rounded-xl bg-muted/50 border border-border/50 transition-all duration-300 hover:bg-muted/70">
    <div className="p-2 rounded-lg bg-primary/10 text-primary flex-shrink-0">
      <Icon className="h-5 w-5" />
    </div>
    <div>
      <h4 className="font-medium text-foreground">{title}</h4>
      <p className="text-sm text-muted-foreground mt-0.5">{description}</p>
    </div>
  </div>
)

// --- Progress indicator for multi-step onboarding ---

interface ProgressStep {
  label: string
  completed: boolean
  current: boolean
}

interface OnboardingProgressProps {
  steps: ProgressStep[]
}

const OnboardingProgress = ({ steps }: OnboardingProgressProps) => (
  <div className="space-y-2">
    {steps.map((step, index) => (
      <div key={index} className="flex items-center gap-3">
        <div
          className={`flex-shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs font-medium transition-colors ${
            step.completed
              ? 'bg-green-100 text-green-700 dark:bg-green-950/40 dark:text-green-400'
              : step.current
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground'
          }`}
        >
          {step.completed ? (
            <Check className="h-3.5 w-3.5" />
          ) : step.current ? (
            <CircleDot className="h-3.5 w-3.5" />
          ) : (
            index + 1
          )}
        </div>
        <span
          className={`text-sm ${
            step.completed
              ? 'text-muted-foreground line-through'
              : step.current
                ? 'text-foreground font-medium'
                : 'text-muted-foreground'
          }`}
        >
          {step.label}
        </span>
      </div>
    ))}
  </div>
)

// --- Demo Component ---

interface WelcomeModalDemoProps {
  displayName?: string
  /** Start with the dialog already open */
  defaultOpen?: boolean
  /** Show onboarding progress steps */
  showProgress?: boolean
  /** Progress steps configuration */
  progressSteps?: ProgressStep[]
  /** Show completed state */
  completed?: boolean
}

const WelcomeModalDemo = ({
  displayName = 'Alex',
  defaultOpen = true,
  showProgress = false,
  progressSteps = [],
  completed = false,
}: WelcomeModalDemoProps) => {
  const [open, setOpen] = useState(defaultOpen)

  return (
    <>
      <Button variant="outline" onClick={() => setOpen(true)} className="cursor-pointer">
        <Sparkles className="h-4 w-4 mr-2" />
        Show Welcome Modal
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-[500px] p-0 overflow-hidden">
          {/* Header with gradient */}
          <div className="relative bg-gradient-to-br from-primary/20 via-primary/10 to-background p-6 pb-4">
            <Button
              variant="ghost"
              size="icon"
              className="absolute top-3 right-3 h-8 w-8 text-muted-foreground hover:text-foreground cursor-pointer"
              onClick={() => setOpen(false)}
              aria-label="Close welcome dialog"
            >
              <X className="h-4 w-4" />
            </Button>

            <div className="flex justify-center mb-4">
              <div className="p-4 rounded-2xl bg-primary/10 border border-primary/20 shadow-lg">
                {completed ? (
                  <Check className="h-10 w-10 text-green-600 dark:text-green-400" />
                ) : (
                  <Sparkles className="h-10 w-10 text-primary" />
                )}
              </div>
            </div>

            <DialogHeader className="text-center">
              <DialogTitle className="text-2xl font-bold">
                {completed ? 'You\'re All Set!' : `Welcome, ${displayName}!`}
              </DialogTitle>
              <p className="text-muted-foreground mt-2">
                {completed
                  ? 'Your workspace is ready. Start exploring the platform.'
                  : 'Let\'s get you set up and ready to go. Here\'s what you can do with NOIR.'}
              </p>
            </DialogHeader>
          </div>

          {/* Content */}
          <div className="p-6 pt-4 space-y-3">
            {showProgress && progressSteps.length > 0 ? (
              <OnboardingProgress steps={progressSteps} />
            ) : completed ? (
              <div className="text-center py-4">
                <p className="text-sm text-muted-foreground">
                  You have completed all onboarding steps. You can always revisit settings from the
                  sidebar.
                </p>
              </div>
            ) : (
              <>
                <FeatureCard
                  icon={Rocket}
                  title="Manage Products"
                  description="Create and organize your product catalog with ease"
                />
                <FeatureCard
                  icon={Shield}
                  title="Secure Access"
                  description="Control who can access what with role-based permissions"
                />
                <FeatureCard
                  icon={Users}
                  title="Team Collaboration"
                  description="Invite team members and work together seamlessly"
                />
              </>
            )}
          </div>

          {/* Actions */}
          <div className="p-6 pt-2 flex flex-col gap-2">
            <Button
              onClick={() => setOpen(false)}
              className="w-full h-11 text-base font-semibold cursor-pointer"
            >
              {completed ? 'Go to Dashboard' : 'Get Started'}
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
            {!completed && (
              <Button
                variant="ghost"
                onClick={() => setOpen(false)}
                className="text-muted-foreground hover:text-foreground cursor-pointer"
              >
                Skip for now
              </Button>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/WelcomeModal',
  component: WelcomeModalDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof WelcomeModalDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Welcome/onboarding modal shown to new users on first login. ' +
          'Displays platform feature highlights with a gradient header and sparkle icon. ' +
          'This is a visual replica — the real component uses AuthContext, framer-motion, ' +
          'useOnboarding hook, and react-router-dom.',
      },
    },
  },
  args: {
    displayName: 'Alex',
  },
}

export const WithProgress: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Onboarding modal with a step-by-step progress tracker. ' +
          'Completed steps show a checkmark, the current step is highlighted, ' +
          'and future steps are dimmed.',
      },
    },
  },
  args: {
    displayName: 'Sarah',
    showProgress: true,
    progressSteps: [
      { label: 'Configure your store settings', completed: true, current: false },
      { label: 'Set up payment gateways', completed: true, current: false },
      { label: 'Add your first product', completed: false, current: true },
      { label: 'Invite team members', completed: false, current: false },
      { label: 'Launch your storefront', completed: false, current: false },
    ],
  },
}

export const Completed: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Completion state shown after all onboarding steps are done. ' +
          'The sparkle icon changes to a checkmark and the message is updated.',
      },
    },
  },
  args: {
    displayName: 'Jordan',
    completed: true,
  },
}
