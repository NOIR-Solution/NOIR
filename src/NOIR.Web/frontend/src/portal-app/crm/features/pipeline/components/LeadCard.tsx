import { Calendar, User } from 'lucide-react'
import { Card, CardContent } from '@uikit'
import type { LeadCardDto } from '@/types/crm'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'

interface LeadCardProps {
  lead: LeadCardDto
  onClick: (lead: LeadCardDto) => void
}

export const LeadCard = ({ lead, onClick }: LeadCardProps) => {
  const { formatDate } = useRegionalSettings()

  const formattedValue = new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: lead.currency || 'USD',
    maximumFractionDigits: 0,
  }).format(lead.value)

  return (
    <Card
      className="shadow-sm hover:shadow-md transition-all duration-200 cursor-pointer border-border/50"
      onClick={() => onClick(lead)}
    >
      <CardContent className="p-3 space-y-2">
        <p className="text-sm font-medium line-clamp-2">{lead.title}</p>
        <p className="text-lg font-bold text-primary">{formattedValue}</p>
        <div className="flex items-center gap-1 text-xs text-muted-foreground">
          <User className="h-3 w-3" />
          <span className="truncate">{lead.contactName}</span>
        </div>
        {lead.expectedCloseDate && (
          <div className="flex items-center gap-1 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" />
            <span>{formatDate(lead.expectedCloseDate)}</span>
          </div>
        )}
        {lead.ownerName && (
          <div className="text-xs text-muted-foreground truncate">
            {lead.ownerName}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
