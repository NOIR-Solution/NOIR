import { themeClasses } from '@/config/theme'

interface AnimatedBlobProps {
  color: string
  position: string
  delay?: string
  size?: string
}

/**
 * Animated gradient blob for decorative backgrounds
 * Used on Landing and Login pages for visual interest
 */
export const AnimatedBlob = ({
  color,
  position,
  delay = "",
  size = "w-72 h-72"
}: AnimatedBlobProps) => (
  <div
    className={`absolute ${position} ${size} ${color} rounded-full mix-blend-screen filter blur-xl opacity-70 animate-blob ${delay}`}
  />
)

interface GradientWaveProps {
  /** Unique ID for SVG gradient to avoid conflicts when multiple waves exist */
  id?: string
  /** Opacity class for the wave container */
  opacity?: string
}

/**
 * SVG gradient wave for decorative backgrounds
 * Used on Landing and Login pages for visual depth
 */
export const GradientWave = ({
  id = "gradientWave",
  opacity = "opacity-30"
}: GradientWaveProps) => (
  <div className={`absolute inset-0 ${opacity}`}>
    <svg
      className="absolute inset-0 w-full h-full"
      preserveAspectRatio="none"
      viewBox="0 0 1440 560"
    >
      <defs>
        <linearGradient id={id} x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor={themeClasses.svgGradientStart} stopOpacity="0.4" />
          <stop offset="100%" stopColor={themeClasses.svgGradientEnd} stopOpacity="0.2" />
        </linearGradient>
      </defs>
      <path
        fill={`url(#${id})`}
        d="M0,224L48,213.3C96,203,192,181,288,181.3C384,181,480,203,576,218.7C672,235,768,245,864,234.7C960,224,1056,192,1152,186.7C1248,181,1344,203,1392,213.3L1440,224L1440,560L1392,560C1344,560,1248,560,1152,560C1056,560,960,560,864,560C768,560,672,560,576,560C480,560,384,560,288,560C192,560,96,560,48,560L0,560Z"
      />
    </svg>
  </div>
)
