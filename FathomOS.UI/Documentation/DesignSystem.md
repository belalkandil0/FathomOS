# FathomOS Design System

**Owned by: UI-AGENT**

This document describes the FathomOS design system - the single source of truth for all visual design decisions across the application.

## Overview

The FathomOS design system ensures a consistent, premium, modern, and professional user interface across all 15+ modules.

## Design Principles

1. **Consistency**: All modules look and feel like they belong together
2. **Premium**: High-end, professional aesthetic
3. **Accessibility**: WCAG 2.1 AA compliant minimum
4. **Performance**: 60fps animations, virtualized lists
5. **Responsiveness**: Adapts to different window sizes

## Color Palette

### Primary Colors
| Token | Value | Usage |
|-------|-------|-------|
| PrimaryColor | #0066CC | Main brand color, primary buttons |
| PrimaryDarkColor | #004C99 | Hover states |
| PrimaryLightColor | #3399FF | Focus rings, highlights |

### Semantic Colors
| Token | Value | Usage |
|-------|-------|-------|
| SuccessColor | #28A745 | Success states, completion |
| WarningColor | #FFC107 | Warnings, attention needed |
| ErrorColor | #DC3545 | Errors, destructive actions |
| InfoColor | #17A2B8 | Information, help |

### Dark Theme
| Token | Value | Usage |
|-------|-------|-------|
| BackgroundPrimaryDark | #1A1A2E | Main background |
| BackgroundSecondaryDark | #16213E | Secondary surfaces |
| SurfaceDark | #252542 | Cards, dialogs |
| TextPrimaryDark | #FFFFFF | Primary text |

## Typography

### Font Family
- **Primary**: Segoe UI
- **Headings**: Segoe UI Semibold
- **Monospace**: Cascadia Code, Consolas

### Type Scale
| Style | Size | Weight | Usage |
|-------|------|--------|-------|
| H1 | 32px | SemiBold | Page titles |
| H2 | 24px | SemiBold | Section titles |
| H3 | 20px | Medium | Card titles |
| H4 | 18px | Medium | Subsections |
| Body | 14px | Regular | Default text |
| Small | 12px | Regular | Secondary text |
| Caption | 10px | Regular | Labels, hints |

## Spacing

Based on 4px base unit.

| Token | Value | Usage |
|-------|-------|-------|
| SpacingXS | 4px | Tight spacing |
| SpacingS | 8px | Small gaps |
| SpacingM | 16px | Standard spacing |
| SpacingL | 24px | Section spacing |
| SpacingXL | 32px | Large sections |

## Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| RadiusS | 4px | Buttons, inputs |
| RadiusM | 8px | Cards, dialogs |
| RadiusL | 12px | Large cards |
| RadiusRound | 9999px | Pills, avatars |

## Components

### FathomButton
Premium button with variants and sizes.

**Variants:**
- Primary (default)
- Secondary
- Outline
- Ghost
- Danger
- Success

**Sizes:**
- Small (28px)
- Medium (36px)
- Large (44px)

### FathomCard
Container with elevation and sections.

**Variants:**
- Elevated (shadow)
- Outlined (border)
- Filled (background)

**Elevation levels:** 0-5

## Usage Guidelines

### DO
- Use design tokens for all colors, spacing, typography
- Use FathomOS.UI controls for all user-facing UI
- Follow the established patterns
- Coordinate with UI-AGENT for custom needs

### DON'T
- Hardcode color values
- Create custom styles
- Use raw WPF controls
- Override control templates without approval

## How to Use

### In XAML
```xaml
<Window xmlns:fathom="clr-namespace:FathomOS.UI.Controls;assembly=FathomOS.UI">

    <!-- Use design tokens -->
    <TextBlock Style="{StaticResource HeadingH2}"
               Text="Dashboard"/>

    <!-- Use premium controls -->
    <fathom:FathomButton Content="Save"
                         Variant="Primary"
                         Size="Medium"/>

    <fathom:FathomCard Elevation="2"
                       CornerRadius="{StaticResource RadiusM}">
        <TextBlock Text="Card Content"/>
    </fathom:FathomCard>
</Window>
```

### Adding Resources
```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/FathomOS.UI;component/Themes/Generic.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-01 | Initial design system |
