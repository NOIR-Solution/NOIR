import React, { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, Pipette } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Input } from '../input/Input';
import { Popover, PopoverContent, PopoverTrigger } from '../popover/Popover';

interface ColorSwatchProps {
  color: string;
  isSelected: boolean;
  onClick: () => void;
}

const ColorSwatch: React.FC<ColorSwatchProps> = ({ color, isSelected, onClick }) => {
  const { t } = useTranslation('common')
  return (
    <button
      type="button"
      className={cn(
        'relative w-8 h-8 rounded-md cursor-pointer transition-all duration-200 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
        isSelected && 'ring-2 ring-foreground ring-offset-2'
      )}
      style={{ backgroundColor: color }}
      onClick={onClick}
      aria-label={t('labels.colorValue', { color, defaultValue: 'Color: {{color}}' })}
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
  const { t } = useTranslation('common');
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
              'relative w-8 h-8 rounded-md cursor-pointer transition-all duration-200 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 flex items-center justify-center border-2 border-dashed border-muted-foreground/50',
              isCustomColor && 'ring-2 ring-foreground ring-offset-2 border-solid border-transparent'
            )}
            style={isCustomColor ? { backgroundColor: value } : undefined}
            aria-label={t('labels.pickCustomColor', 'Pick custom color')}
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
            aria-label={t('labels.colorPicker', 'Color picker')}
          />
          <Input
            type="text"
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            placeholder="#000000"
            className="w-28 h-8 text-sm font-mono"
            maxLength={7}
          />
          <span className="text-xs text-muted-foreground">{t('labels.customColor', 'Custom')}</span>
        </div>
      )}
    </div>
  );
};

interface CompactColorPickerProps {
  value?: string;
  onChange?: (color: string) => void;
  colors?: string[];
  showCustomInput?: boolean;
  className?: string;
  triggerClassName?: string;
  align?: 'start' | 'center' | 'end';
  side?: 'top' | 'bottom' | 'left' | 'right';
}

const CompactColorPicker: React.FC<CompactColorPickerProps> = ({
  value = '#3B82F6',
  onChange = () => {},
  colors = defaultColors,
  showCustomInput = true,
  className,
  triggerClassName,
  align = 'start',
  side = 'bottom',
}) => {
  const { t } = useTranslation('common')
  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            'w-8 h-8 rounded-md cursor-pointer ring-1 ring-border hover:ring-2 hover:ring-primary/50 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
            triggerClassName,
          )}
          style={{ backgroundColor: value }}
          aria-label={t('labels.clickToChangeColor', { color: value, defaultValue: 'Color: {{color}}. Click to change.' })}
        />
      </PopoverTrigger>
      <PopoverContent
        className={cn('w-auto p-3', className)}
        align={align}
        side={side}
      >
        <ColorPicker
          value={value}
          onChange={onChange}
          colors={colors}
          showCustomInput={showCustomInput}
        />
      </PopoverContent>
    </Popover>
  );
};

export { ColorPicker, CompactColorPicker, ColorSwatch, defaultColors };
export type { ColorPickerProps, CompactColorPickerProps, ColorSwatchProps };
