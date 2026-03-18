/**
 * Comprehensive timezone list for regional settings.
 *
 * Uses IANA timezone identifiers (JavaScript Intl standard).
 * UTC offsets are computed dynamically so they stay correct during DST transitions.
 * Curated to ~75 major business timezones (Windows-style coverage).
 */

import type { ComboboxOption } from '@uikit'

interface TimezoneEntry {
  value: string
  city: string
  region: string
}

/**
 * Curated timezone entries grouped by region, sorted by typical UTC offset.
 * Covers all Windows timezone equivalents used in business contexts.
 */
const TIMEZONE_ENTRIES: TimezoneEntry[] = [
  // UTC
  { value: 'UTC', city: 'UTC', region: 'UTC' },

  // Americas (West → East)
  { value: 'Pacific/Honolulu', city: 'Honolulu', region: 'Pacific' },
  { value: 'America/Anchorage', city: 'Anchorage', region: 'Americas' },
  { value: 'America/Los_Angeles', city: 'Los Angeles, Vancouver', region: 'Americas' },
  { value: 'America/Denver', city: 'Denver, Phoenix', region: 'Americas' },
  { value: 'America/Chicago', city: 'Chicago, Mexico City', region: 'Americas' },
  { value: 'America/New_York', city: 'New York, Toronto', region: 'Americas' },
  { value: 'America/Halifax', city: 'Halifax, Atlantic', region: 'Americas' },
  { value: 'America/St_Johns', city: "St. John's, Newfoundland", region: 'Americas' },
  { value: 'America/Sao_Paulo', city: 'São Paulo, Brasília', region: 'Americas' },
  { value: 'America/Argentina/Buenos_Aires', city: 'Buenos Aires', region: 'Americas' },
  { value: 'America/Santiago', city: 'Santiago', region: 'Americas' },
  { value: 'America/Bogota', city: 'Bogotá, Lima', region: 'Americas' },
  { value: 'America/Caracas', city: 'Caracas', region: 'Americas' },

  // Europe & Africa (West → East)
  { value: 'Atlantic/Reykjavik', city: 'Reykjavik', region: 'Europe' },
  { value: 'Europe/London', city: 'London, Dublin', region: 'Europe' },
  { value: 'Europe/Lisbon', city: 'Lisbon', region: 'Europe' },
  { value: 'Africa/Casablanca', city: 'Casablanca', region: 'Africa' },
  { value: 'Europe/Paris', city: 'Paris, Brussels', region: 'Europe' },
  { value: 'Europe/Berlin', city: 'Berlin, Amsterdam', region: 'Europe' },
  { value: 'Europe/Madrid', city: 'Madrid', region: 'Europe' },
  { value: 'Europe/Rome', city: 'Rome, Vienna', region: 'Europe' },
  { value: 'Europe/Zurich', city: 'Zurich, Geneva', region: 'Europe' },
  { value: 'Europe/Warsaw', city: 'Warsaw, Prague', region: 'Europe' },
  { value: 'Europe/Stockholm', city: 'Stockholm, Oslo', region: 'Europe' },
  { value: 'Africa/Lagos', city: 'Lagos, West Africa', region: 'Africa' },
  { value: 'Europe/Athens', city: 'Athens, Helsinki', region: 'Europe' },
  { value: 'Europe/Bucharest', city: 'Bucharest, Sofia', region: 'Europe' },
  { value: 'Europe/Istanbul', city: 'Istanbul', region: 'Europe' },
  { value: 'Africa/Cairo', city: 'Cairo', region: 'Africa' },
  { value: 'Africa/Johannesburg', city: 'Johannesburg, Pretoria', region: 'Africa' },
  { value: 'Africa/Nairobi', city: 'Nairobi, East Africa', region: 'Africa' },
  { value: 'Europe/Moscow', city: 'Moscow, St. Petersburg', region: 'Europe' },

  // Middle East
  { value: 'Asia/Riyadh', city: 'Riyadh, Kuwait', region: 'Middle East' },
  { value: 'Asia/Tehran', city: 'Tehran', region: 'Middle East' },
  { value: 'Asia/Dubai', city: 'Dubai, Abu Dhabi', region: 'Middle East' },
  { value: 'Asia/Muscat', city: 'Muscat', region: 'Middle East' },
  { value: 'Asia/Kabul', city: 'Kabul', region: 'Middle East' },

  // Central & South Asia
  { value: 'Asia/Karachi', city: 'Karachi, Islamabad', region: 'South Asia' },
  { value: 'Asia/Kolkata', city: 'Mumbai, New Delhi', region: 'South Asia' },
  { value: 'Asia/Kathmandu', city: 'Kathmandu', region: 'South Asia' },
  { value: 'Asia/Dhaka', city: 'Dhaka', region: 'South Asia' },
  { value: 'Asia/Colombo', city: 'Colombo, Sri Lanka', region: 'South Asia' },
  { value: 'Asia/Almaty', city: 'Almaty', region: 'Central Asia' },
  { value: 'Asia/Tashkent', city: 'Tashkent', region: 'Central Asia' },

  // Southeast Asia
  { value: 'Asia/Yangon', city: 'Yangon', region: 'Southeast Asia' },
  { value: 'Asia/Ho_Chi_Minh', city: 'Ho Chi Minh, Hanoi', region: 'Southeast Asia' },
  { value: 'Asia/Bangkok', city: 'Bangkok, Jakarta', region: 'Southeast Asia' },
  { value: 'Asia/Singapore', city: 'Singapore', region: 'Southeast Asia' },
  { value: 'Asia/Kuala_Lumpur', city: 'Kuala Lumpur', region: 'Southeast Asia' },
  { value: 'Asia/Manila', city: 'Manila', region: 'Southeast Asia' },

  // East Asia
  { value: 'Asia/Shanghai', city: 'Shanghai, Beijing', region: 'East Asia' },
  { value: 'Asia/Hong_Kong', city: 'Hong Kong', region: 'East Asia' },
  { value: 'Asia/Taipei', city: 'Taipei', region: 'East Asia' },
  { value: 'Asia/Seoul', city: 'Seoul', region: 'East Asia' },
  { value: 'Asia/Tokyo', city: 'Tokyo, Osaka', region: 'East Asia' },

  // Pacific & Oceania
  { value: 'Australia/Perth', city: 'Perth', region: 'Oceania' },
  { value: 'Australia/Darwin', city: 'Darwin', region: 'Oceania' },
  { value: 'Australia/Adelaide', city: 'Adelaide', region: 'Oceania' },
  { value: 'Australia/Sydney', city: 'Sydney, Melbourne', region: 'Oceania' },
  { value: 'Australia/Brisbane', city: 'Brisbane', region: 'Oceania' },
  { value: 'Pacific/Guam', city: 'Guam', region: 'Pacific' },
  { value: 'Pacific/Noumea', city: 'Noumea', region: 'Pacific' },
  { value: 'Pacific/Auckland', city: 'Auckland, Wellington', region: 'Pacific' },
  { value: 'Pacific/Fiji', city: 'Fiji', region: 'Pacific' },
  { value: 'Pacific/Tongatapu', city: 'Tonga', region: 'Pacific' },
]

/**
 * Compute the current UTC offset string for a timezone.
 * Returns format like "UTC+07:00", "UTC-05:00", "UTC".
 */
export const getUtcOffset = (tz: string): string => {
  if (tz === 'UTC') return 'UTC+00:00'

  try {
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone: tz,
      timeZoneName: 'shortOffset',
    })
    const parts = formatter.formatToParts(new Date())
    const tzPart = parts.find(p => p.type === 'timeZoneName')
    const raw = tzPart?.value ?? 'GMT'

    // Convert "GMT+7" → "UTC+07:00", "GMT-5:30" → "UTC-05:30", "GMT" → "UTC+00:00"
    if (raw === 'GMT') return 'UTC+00:00'

    const match = raw.match(/^GMT([+-])(\d{1,2})(?::(\d{2}))?$/)
    if (!match) return raw.replace('GMT', 'UTC')

    const sign = match[1]
    const hours = match[2].padStart(2, '0')
    const minutes = match[3] ?? '00'
    return `UTC${sign}${hours}:${minutes}`
  } catch {
    return 'UTC'
  }
}

/**
 * Build timezone options for the Combobox dropdown.
 * Computes UTC offsets dynamically (correct during DST transitions).
 * Sorted by UTC offset, then alphabetically within same offset.
 */
export const getTimezoneOptions = (): ComboboxOption[] => {
  return TIMEZONE_ENTRIES.map(entry => {
    const offset = getUtcOffset(entry.value)
    return {
      value: entry.value,
      label: `(${offset}) ${entry.city}`,
      description: entry.region,
    }
  }).sort((a, b) => {
    // Sort by UTC offset (parse the offset string)
    const offsetA = parseOffset(a.label)
    const offsetB = parseOffset(b.label)
    if (offsetA !== offsetB) return offsetA - offsetB
    return a.label.localeCompare(b.label)
  })
}

/**
 * Parse "(UTC+07:00) ..." → numeric offset in minutes for sorting.
 */
const parseOffset = (label: string): number => {
  const match = label.match(/\(UTC([+-])(\d{2}):(\d{2})\)/)
  if (!match) return 0
  const sign = match[1] === '+' ? 1 : -1
  return sign * (parseInt(match[2]) * 60 + parseInt(match[3]))
}
