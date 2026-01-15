import * as React from "react";
import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: LucideIcon;
  title?: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

const EmptyState = React.forwardRef<HTMLDivElement, EmptyStateProps>(
  (
    {
      icon: Icon,
      title = "No data found",
      description = "Get started by creating your first item.",
      action,
      className,
    },
    ref
  ) => {
    return (
      <div
        ref={ref}
        className={cn(
          "flex flex-col items-center justify-center text-center p-12 rounded-xl border-2 border-dashed border-border bg-background hover:bg-muted/50 transition-colors duration-300 group animate-in fade-in-0 slide-in-from-bottom-4",
          className
        )}
      >
        {Icon && (
          <div className="mb-6 p-4 rounded-xl bg-muted/50 border border-border shadow-sm group-hover:shadow-md transition-all duration-300">
            <Icon className="h-8 w-8 text-muted-foreground group-hover:text-foreground transition-colors duration-300" />
          </div>
        )}

        <div className="space-y-2 mb-6">
          <h3 className="text-lg font-semibold text-foreground tracking-tight">
            {title}
          </h3>
          <p className="text-sm text-muted-foreground max-w-md leading-relaxed">
            {description}
          </p>
        </div>

        {action && (
          <Button
            onClick={action.onClick}
            variant="outline"
            className="shadow-sm hover:shadow-md transition-all duration-200"
          >
            {action.label}
          </Button>
        )}
      </div>
    );
  }
);

EmptyState.displayName = "EmptyState";

export { EmptyState };
export type { EmptyStateProps };
