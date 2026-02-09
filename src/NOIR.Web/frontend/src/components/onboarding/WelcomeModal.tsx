import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Sparkles, ArrowRight, X, Rocket, Shield, Users } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { useOnboarding } from '@/hooks/useOnboarding'
import { useAuthContext } from '@/contexts/AuthContext'

interface FeatureCardProps {
  icon: React.ElementType
  title: string
  description: string
  delay: number
}

function FeatureCard({ icon: Icon, title, description, delay }: FeatureCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay, duration: 0.4 }}
      className="flex items-start gap-3 p-4 rounded-xl bg-muted/50 border border-border/50"
    >
      <div className="p-2 rounded-lg bg-primary/10 text-primary flex-shrink-0">
        <Icon className="h-5 w-5" />
      </div>
      <div>
        <h4 className="font-medium text-foreground">{title}</h4>
        <p className="text-sm text-muted-foreground mt-0.5">{description}</p>
      </div>
    </motion.div>
  )
}

export function WelcomeModal() {
  const navigate = useNavigate()
  const { user } = useAuthContext()
  const { shouldShowWelcome, markWelcomeShown } = useOnboarding()
  const [open, setOpen] = useState(false)

  // Show modal on mount if needed
  useEffect(() => {
    if (shouldShowWelcome && user) {
      // Small delay to let the page render first
      const timer = setTimeout(() => setOpen(true), 500)
      return () => clearTimeout(timer)
    }
  }, [shouldShowWelcome, user])

  const handleClose = () => {
    setOpen(false)
    markWelcomeShown()
  }

  const handleGetStarted = () => {
    handleClose()
    // Navigate to first onboarding step
    navigate('/portal/settings')
  }

  const handleSkip = () => {
    handleClose()
  }

  const displayName = user?.firstName || user?.fullName?.split(' ')[0] || 'there'

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent className="sm:max-w-[500px] p-0 overflow-hidden">
        {/* Header with gradient */}
        <div className="relative bg-gradient-to-br from-primary/20 via-primary/10 to-background p-6 pb-4">
          <Button
            variant="ghost"
            size="icon"
            className="absolute top-3 right-3 h-8 w-8 text-muted-foreground hover:text-foreground"
            onClick={handleSkip}
            aria-label="Close welcome dialog"
          >
            <X className="h-4 w-4" />
          </Button>

          <motion.div
            initial={{ scale: 0.8, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ duration: 0.3 }}
            className="flex justify-center mb-4"
          >
            <div className="p-4 rounded-2xl bg-primary/10 border border-primary/20 shadow-lg">
              <Sparkles className="h-10 w-10 text-primary" />
            </div>
          </motion.div>

          <DialogHeader className="text-center">
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 }}
            >
              <DialogTitle className="text-2xl font-bold">
                Welcome, {displayName}!
              </DialogTitle>
              <p className="text-muted-foreground mt-2">
                Let's get you set up and ready to go. Here's what you can do with NOIR.
              </p>
            </motion.div>
          </DialogHeader>
        </div>

        {/* Features */}
        <div className="p-6 pt-4 space-y-3">
          <FeatureCard
            icon={Rocket}
            title="Manage Products"
            description="Create and organize your product catalog with ease"
            delay={0.2}
          />
          <FeatureCard
            icon={Shield}
            title="Secure Access"
            description="Control who can access what with role-based permissions"
            delay={0.3}
          />
          <FeatureCard
            icon={Users}
            title="Team Collaboration"
            description="Invite team members and work together seamlessly"
            delay={0.4}
          />
        </div>

        {/* Actions */}
        <div className="p-6 pt-2 flex flex-col gap-2">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.5 }}
          >
            <Button
              onClick={handleGetStarted}
              className="w-full h-11 text-base font-semibold"
            >
              Get Started
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          </motion.div>
          <Button
            variant="ghost"
            onClick={handleSkip}
            className="text-muted-foreground hover:text-foreground"
          >
            Skip for now
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
