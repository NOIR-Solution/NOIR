import * as React from "react";
import { ViewTransitionLink } from "@/components/navigation/ViewTransitionLink";
import type { LucideIcon } from "lucide-react";
import { ExternalLink } from "lucide-react";
import { Button } from '../button/Button'
import { cn } from "@/lib/utils";

interface EmptyStateAction {
  label: string;
  onClick?: () => void;
  href?: string;
  variant?: "default" | "outline" | "secondary" | "ghost";
}

interface EmptyStateProps {
  /** Icon to display */
  icon?: LucideIcon;
  /** Custom illustration component (overrides icon) */
  illustration?: React.ReactNode;
  /** Main title */
  title?: string;
  /** Description text */
  description?: string;
  /** Primary action button */
  action?: EmptyStateAction;
  /** Secondary action button */
  secondaryAction?: EmptyStateAction;
  /** Help/documentation link */
  helpLink?: {
    label: string;
    href: string;
    external?: boolean;
  };
  /** Custom className */
  className?: string;
  /** Size variant */
  size?: "sm" | "md" | "lg";
}

const sizeClasses = {
  sm: {
    container: "p-6",
    icon: "h-6 w-6",
    iconWrapper: "p-3",
    title: "text-base",
    description: "text-xs",
    gap: "mb-4",
  },
  md: {
    container: "p-12",
    icon: "h-8 w-8",
    iconWrapper: "p-4",
    title: "text-lg",
    description: "text-sm",
    gap: "mb-6",
  },
  lg: {
    container: "p-16",
    icon: "h-10 w-10",
    iconWrapper: "p-5",
    title: "text-xl",
    description: "text-base",
    gap: "mb-8",
  },
};

const ActionButton = ({ action, variant = "outline" }: { action: EmptyStateAction; variant?: EmptyStateAction["variant"] }) => {
  const buttonVariant = action.variant || variant;

  if (action.href) {
    return (
      <Button
        variant={buttonVariant}
        asChild
        className="shadow-sm hover:shadow-md transition-all duration-200"
      >
        <ViewTransitionLink to={action.href}>{action.label}</ViewTransitionLink>
      </Button>
    );
  }

  return (
    <Button
      onClick={action.onClick}
      variant={buttonVariant}
      className="shadow-sm hover:shadow-md transition-all duration-200"
    >
      {action.label}
    </Button>
  );
}

const EmptyState = React.forwardRef<HTMLDivElement, EmptyStateProps>(
  (
    {
      icon: Icon,
      illustration,
      title = "No data found",
      description = "Get started by creating your first item.",
      action,
      secondaryAction,
      helpLink,
      className,
      size = "md",
    },
    ref
  ) => {
    const sizes = sizeClasses[size];

    return (
      <div
        ref={ref}
        className={cn(
          "flex flex-col items-center justify-center text-center rounded-xl border-2 border-dashed border-border bg-background hover:bg-muted/50 transition-colors duration-300 group animate-in fade-in-0 slide-in-from-bottom-4",
          sizes.container,
          className
        )}
      >
        {/* Illustration or Icon */}
        {illustration ? (
          <div className={sizes.gap}>{illustration}</div>
        ) : Icon ? (
          <div className={cn("rounded-xl bg-muted/50 border border-border shadow-sm group-hover:shadow-md transition-all duration-300", sizes.iconWrapper, sizes.gap)}>
            <Icon className={cn("text-muted-foreground group-hover:text-foreground transition-colors duration-300", sizes.icon)} />
          </div>
        ) : null}

        {/* Text content */}
        <div className={cn("space-y-2", sizes.gap)}>
          <h3 className={cn("font-semibold text-foreground tracking-tight", sizes.title)}>
            {title}
          </h3>
          <p className={cn("text-muted-foreground max-w-md leading-relaxed", sizes.description)}>
            {description}
          </p>
        </div>

        {/* Actions */}
        {(action || secondaryAction) && (
          <div className="flex flex-wrap items-center justify-center gap-3">
            {action && <ActionButton action={action} variant="default" />}
            {secondaryAction && <ActionButton action={secondaryAction} variant="outline" />}
          </div>
        )}

        {/* Help link */}
        {helpLink && (
          <div className="mt-4">
            {helpLink.external ? (
              <a
                href={helpLink.href}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-sm text-primary hover:underline"
              >
                {helpLink.label}
                <ExternalLink className="h-3 w-3" />
              </a>
            ) : (
              <ViewTransitionLink
                to={helpLink.href}
                className="text-sm text-primary hover:underline"
              >
                {helpLink.label}
              </ViewTransitionLink>
            )}
          </div>
        )}
      </div>
    );
  }
);

EmptyState.displayName = "EmptyState";

export { EmptyState };
export type { EmptyStateProps };
