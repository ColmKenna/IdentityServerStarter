# Theming System Documentation

## Overview
This Identity Server application now includes a comprehensive theming system with 5 built-in themes and full CSS custom property support.

## Available Themes
1. **Light Theme** (default) - Clean, bright interface
2. **Dark Theme** - Dark mode for reduced eye strain
3. **Blue Theme** - Professional blue color scheme
4. **Green Theme** - Nature-inspired green palette
5. **Purple Theme** - Modern purple aesthetic

## How to Use

### Theme Switcher
- Click the 🎨 button in the top-right corner to open the theme selector
- Select any theme to apply it immediately
- Your theme preference is saved in localStorage and persists across sessions

### Keyboard Support
- Press `Escape` to close the theme selector
- The theme toggle button is keyboard accessible

### Programmatic Usage
```javascript
// Set a theme programmatically
ThemeSwitcher.setTheme('dark');

// Get current theme
const currentTheme = ThemeSwitcher.getCurrentTheme();

// Get available themes
const themes = ThemeSwitcher.themes;
```

## CSS Custom Properties (Variables)

### Color Variables
- `--primary-color` - Main brand color
- `--primary-hover` - Primary color hover state
- `--secondary-color` - Secondary actions
- `--success-color` - Success states
- `--info-color` - Information states
- `--warning-color` - Warning states
- `--danger-color` - Error/danger states

### Background Variables
- `--body-bg` - Main body background
- `--surface-bg` - Cards, forms, elevated surfaces
- `--card-bg` - Card backgrounds
- `--navbar-bg` - Navigation bar background
- `--dropdown-bg` - Dropdown menu background
- `--form-bg` - Form container background

### Text Variables
- `--text-primary` - Main text color
- `--text-secondary` - Secondary text
- `--text-muted` - Muted/disabled text
- `--text-light` - Light text (for dark backgrounds)
- `--text-danger` - Error text color

### Border Variables
- `--border-color` - Standard borders
- `--border-light` - Light borders/dividers
- `--input-border` - Form input borders
- `--focus-border` - Focus state borders

### Shadow Variables
- `--box-shadow` - Standard shadow
- `--box-shadow-lg` - Large shadow for modals/dropdowns
- `--focus-shadow` - Focus state shadow

## Creating Custom Themes

To add a new theme, add a new CSS rule in `site.scss`:

```scss
[data-theme="custom"] {
  --primary-color: #your-color;
  --body-bg: #your-bg;
  // ... other variables
}
```

Then add it to the themes array in `theme-switcher.js`:

```javascript
const themes = [
  // existing themes...
  { name: 'custom', label: 'Custom Theme' }
];
```

## Theme-Aware Components

### Buttons
- `.button-primary` - Primary actions
- `.button-secondary` - Secondary actions
- `.button-info` - Information actions
- `.button-success` - Success actions
- `.button-warning` - Warning actions
- `.button-danger` - Danger actions

### Form Elements
- `.theme-aware-input` - Themed input fields
- `.theme-aware-select` - Themed select dropdowns
- `.theme-aware-textarea` - Themed text areas

### Tables
- `.theme-table` - Fully themed table

### Modals
- `.theme-modal` - Modal container
- `.theme-modal-header` - Modal header
- `.theme-modal-body` - Modal body
- `.theme-modal-footer` - Modal footer

### Alerts
- `.alert-success` - Success messages
- `.alert-info` - Information messages
- `.alert-warning` - Warning messages
- `.alert-danger` - Error messages

## Accessibility Features

### High Contrast Support
The theming system automatically adjusts shadows and contrast for users with high contrast preferences.

### Reduced Motion Support
Users with reduced motion preferences will see no transitions or animations.

### Print Support
When printing, the system automatically switches to a light, print-friendly theme.

### Keyboard Navigation
All theme controls are fully keyboard accessible.

## Browser Support
- Modern browsers with CSS custom property support
- Graceful fallback for older browsers
- localStorage support for theme persistence

## Best Practices

1. **Use CSS Custom Properties**: Always use the defined variables instead of hardcoded colors
2. **Test All Themes**: Ensure your components work well in all available themes
3. **Consider Accessibility**: Test with high contrast and reduced motion preferences
4. **Maintain Consistency**: Follow the established color patterns when adding new components