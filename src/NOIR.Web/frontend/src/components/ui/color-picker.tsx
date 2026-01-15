import React, { useRef } from 'react';
import { Check, Pipette } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Input } from '@/components/ui/input';

interface ColorSwatchProps {
  color: string;
  isSelected: boolean;
  onClick: () => void;
}

const ColorSwatch: React.FC<ColorSwatchProps> = ({ color, isSelected, onClick }) => {
  return (
    <button
      type="button"
      className={cn(
        'relative w-8 h-8 rounded-md transition-all duration-200 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
        isSelected && 'ring-2 ring-foreground ring-offset-2'
      )}
      style={{ backgroundColor: color }}
      onClick={onClick}
      aria-label={`Select color ${color}`}
    >
      {isSelected && (
        <div className="absolute inset-0 flex items-center justify-center">
          <Check className="w-4 h-4 text-white drop-shadow-lg" strokeWidth={3} />
        </div>
      )}
    </button>
  );
};

interface ColorPickerProps {
  value?: string;
  onChange?: (color: string) => void;
  colors?: string[];
  className?: string;
  showCustomInput?: boolean;
}

const defaultColors = [
  '#3B82F6', // blue
  '#10B981', // green
  '#EF4444', // red
  '#F59E0B', // yellow
  '#8B5CF6', // purple
  '#06B6D4', // cyan
  '#F97316', // orange
  '#EC4899', // pink
  '#6B7280', // gray
  '#14B8A6', // teal
  '#84CC16', // lime
  '#6366F1', // indigo
];

const ColorPicker: React.FC<ColorPickerProps> = ({
  value = '#3B82F6',
  onChange = () => {},
  colors = defaultColors,
  className,
  showCustomInput = true,
}) => {
  const colorInputRef = useRef<HTMLInputElement>(null);
  const isCustomColor = !colors.includes(value?.toUpperCase()) && !colors.includes(value?.toLowerCase());

  const handleColorInputClick = () => {
    colorInputRef.current?.click();
  };

  const handleNativeColorChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value);
  };

  return (
    <div className={cn("space-y-3", className)}>
      {/* Preset color swatches */}
      <div className="grid grid-cols-6 gap-2">
        {colors.map((color) => (
          <ColorSwatch
            key={color}
            color={color}
            isSelected={value?.toUpperCase() === color.toUpperCase()}
            onClick={() => onChange(color)}
          />
        ))}
      </div>

      {/* Custom color picker */}
      {showCustomInput && (
        <div className="flex items-center gap-2 pt-2 border-t border-border">
          <button
            type="button"
            onClick={handleColorInputClick}
            className={cn(
              'relative w-8 h-8 rounded-md transition-all duration-200 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 flex items-center justify-center border-2 border-dashed border-muted-foreground/50',
              isCustomColor && 'ring-2 ring-foreground ring-offset-2 border-solid border-transparent'
            )}
            style={isCustomColor ? { backgroundColor: value } : undefined}
            aria-label="Pick custom color"
          >
            {isCustomColor ? (
              <Check className="w-4 h-4 text-white drop-shadow-lg" strokeWidth={3} />
            ) : (
              <Pipette className="w-4 h-4 text-muted-foreground" />
            )}
          </button>
          <Input
            ref={colorInputRef}
            type="color"
            value={value || '#3B82F6'}
            onChange={handleNativeColorChange}
            className="sr-only"
            aria-label="Color picker"
          />
          <Input
            type="text"
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            placeholder="#000000"
            className="w-28 h-8 text-sm font-mono"
            maxLength={7}
          />
          <span className="text-xs text-muted-foreground">Custom</span>
        </div>
      )}
    </div>
  );
};

export { ColorPicker, ColorSwatch, defaultColors };
export type { ColorPickerProps, ColorSwatchProps };
