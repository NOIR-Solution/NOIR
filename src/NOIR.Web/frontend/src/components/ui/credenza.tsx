'use client'

import * as React from 'react'
import { useIsMobile } from '@/hooks/use-mobile'
import { cn } from '@/lib/utils'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
} from '@/components/ui/drawer'

interface CredenzaContextValue {
  isMobile: boolean
}

const CredenzaContext = React.createContext<CredenzaContextValue | undefined>(undefined)

function useCredenzaContext() {
  const context = React.useContext(CredenzaContext)
  if (!context) {
    throw new Error('Credenza components must be used within a Credenza')
  }
  return context
}

interface CredenzaProps {
  children: React.ReactNode
  open?: boolean
  onOpenChange?: (open: boolean) => void
}

const Credenza = ({ children, ...props }: CredenzaProps) => {
  const isMobile = useIsMobile()

  const contextValue = React.useMemo(() => ({ isMobile }), [isMobile])

  const Comp = isMobile ? Drawer : Dialog

  return (
    <CredenzaContext.Provider value={contextValue}>
      <Comp {...props}>{children}</Comp>
    </CredenzaContext.Provider>
  )
}

const CredenzaTrigger = ({
  className,
  children,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogTrigger>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerTrigger : DialogTrigger

  return (
    <Comp className={className} {...props}>
      {children}
    </Comp>
  )
}

const CredenzaClose = ({
  className,
  children,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogClose>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerClose : DialogClose

  return (
    <Comp className={className} {...props}>
      {children}
    </Comp>
  )
}

const CredenzaContent = ({
  className,
  children,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogContent>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerContent : DialogContent

  return (
    <Comp className={className} {...props}>
      {children}
    </Comp>
  )
}

const CredenzaHeader = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerHeader : DialogHeader

  return <Comp className={className} {...props} />
}

const CredenzaTitle = ({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogTitle>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerTitle : DialogTitle

  return <Comp className={className} {...props} />
}

const CredenzaDescription = ({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogDescription>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerDescription : DialogDescription

  return <Comp className={className} {...props} />
}

const CredenzaBody = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => {
  return <div className={cn('px-4 md:px-0', className)} {...props} />
}

const CredenzaFooter = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => {
  const { isMobile } = useCredenzaContext()
  const Comp = isMobile ? DrawerFooter : DialogFooter

  return <Comp className={className} {...props} />
}

export {
  Credenza,
  CredenzaTrigger,
  CredenzaClose,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  CredenzaBody,
  CredenzaFooter,
}
