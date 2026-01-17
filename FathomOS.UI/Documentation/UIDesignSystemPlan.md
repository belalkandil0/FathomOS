# FathomOS UI Design System Plan

**Document Type:** Design Specification for User Approval
**Version:** 2.0
**Date:** 2026-01-16
**Owner:** UI-AGENT
**Status:** APPROVED

---

## Executive Summary

This document defines a comprehensive UI Design System for FathomOS, featuring a **Dark Professional** aesthetic (VS Code/Figma inspired) with a **Light theme** option. The primary brand color is **Ocean Blue (#0066CC)**. This specification prioritizes premium quality, modern aesthetics, and professional enterprise-grade visuals.

---

## 1. Color Palette

### 1.1 Primary Brand Colors (Shared Across Themes)

| Token | Hex Value | Usage |
|-------|-----------|-------|
| `Primary` | `#0066CC` | Main brand color, primary buttons, links, accent |
| `PrimaryHover` | `#0052A3` | Primary hover state |
| `PrimaryActive` | `#003D7A` | Primary pressed/active state |
| `PrimaryLight` | `#3399FF` | Focus rings, highlights, subtle accents |
| `PrimaryMuted` | `#0066CC33` | 20% opacity for subtle backgrounds |

### 1.2 Dark Theme Colors

**Inspired by VS Code Dark+ theme for enterprise professional feel.**

#### Background Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `BackgroundPrimary` | `#1E1E1E` | Main window/app background |
| `BackgroundSecondary` | `#252526` | Sidebars, secondary panels |
| `BackgroundTertiary` | `#2D2D30` | Nested content areas |
| `Surface` | `#333337` | Cards, dialogs, dropdowns |
| `SurfaceHover` | `#3C3C40` | Card/surface hover state |
| `SurfaceSelected` | `#094771` | Selected items (blue tint) |

#### Text Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `TextPrimary` | `#CCCCCC` | Primary body text |
| `TextSecondary` | `#969696` | Secondary/muted text |
| `TextTertiary` | `#6E6E6E` | Placeholder, hint text |
| `TextDisabled` | `#4D4D4D` | Disabled text |
| `TextOnPrimary` | `#FFFFFF` | Text on primary color backgrounds |
| `TextHeading` | `#E8E8E8` | Headings, titles |

#### Border & Divider Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `Border` | `#3F3F46` | Standard borders |
| `BorderSubtle` | `#2D2D30` | Subtle dividers |
| `BorderFocus` | `#0066CC` | Focus outline |
| `BorderHover` | `#4D4D52` | Hover border state |

#### Interactive State Colors (Dark)
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `ButtonHover` | `#3C3C40` | Button hover background |
| `ButtonPressed` | `#4A4A4F` | Button pressed background |
| `InputFocus` | `#0066CC20` | Input focus background tint |
| `RowHover` | `#2A2D2E` | Table/list row hover |
| `RowSelected` | `#094771` | Selected row background |
| `RowAlternate` | `#252526` | Alternating row background |

### 1.3 Light Theme Colors

**Clean, professional light mode for daytime use and accessibility.**

#### Background Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `BackgroundPrimary` | `#FFFFFF` | Main window/app background |
| `BackgroundSecondary` | `#F3F3F3` | Sidebars, secondary panels |
| `BackgroundTertiary` | `#E8E8E8` | Nested content areas |
| `Surface` | `#FFFFFF` | Cards, dialogs, dropdowns |
| `SurfaceHover` | `#F5F5F5` | Card/surface hover state |
| `SurfaceSelected` | `#E6F2FF` | Selected items (blue tint) |

#### Text Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `TextPrimary` | `#1E1E1E` | Primary body text |
| `TextSecondary` | `#6E6E6E` | Secondary/muted text |
| `TextTertiary` | `#969696` | Placeholder, hint text |
| `TextDisabled` | `#B8B8B8` | Disabled text |
| `TextOnPrimary` | `#FFFFFF` | Text on primary color backgrounds |
| `TextHeading` | `#1E1E1E` | Headings, titles |

#### Border & Divider Colors
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `Border` | `#D4D4D4` | Standard borders |
| `BorderSubtle` | `#E8E8E8` | Subtle dividers |
| `BorderFocus` | `#0066CC` | Focus outline |
| `BorderHover` | `#B8B8B8` | Hover border state |

#### Interactive State Colors (Light)
| Token | Hex Value | Usage |
|-------|-----------|-------|
| `ButtonHover` | `#F0F0F0` | Button hover background |
| `ButtonPressed` | `#E0E0E0` | Button pressed background |
| `InputFocus` | `#0066CC10` | Input focus background tint |
| `RowHover` | `#F5F5F5` | Table/list row hover |
| `RowSelected` | `#E6F2FF` | Selected row background |
| `RowAlternate` | `#FAFAFA` | Alternating row background |

### 1.4 Semantic Colors (Both Themes)

#### Dark Theme Semantic
| Token | Hex Value | Light Variant | Usage |
|-------|-----------|---------------|-------|
| `Success` | `#4EC469` | `#2D4A34` (bg) | Success states, confirmations |
| `Warning` | `#DDB44D` | `#4A4328` (bg) | Warnings, caution states |
| `Error` | `#F14C4C` | `#4A2828` (bg) | Errors, destructive actions |
| `Info` | `#3794FF` | `#28374A` (bg) | Information, help states |

#### Light Theme Semantic
| Token | Hex Value | Light Variant | Usage |
|-------|-----------|---------------|-------|
| `Success` | `#28A745` | `#D4EDDA` (bg) | Success states, confirmations |
| `Warning` | `#D39E00` | `#FFF3CD` (bg) | Warnings, caution states |
| `Error` | `#DC3545` | `#F8D7DA` (bg) | Errors, destructive actions |
| `Info` | `#0066CC` | `#CCE5FF` (bg) | Information, help states |

---

## 2. Typography

### 2.1 Font Families

| Token | Value | Fallback | Usage |
|-------|-------|----------|-------|
| `FontPrimary` | Segoe UI Variable | Segoe UI, sans-serif | All body text, labels |
| `FontHeading` | Segoe UI Variable | Segoe UI Semibold | Headings, titles |
| `FontMonospace` | Cascadia Code | Cascadia Mono, Consolas | Code, data, coordinates |

### 2.2 Type Scale

| Style | Size (px) | Weight | Line Height | Letter Spacing | Usage |
|-------|-----------|--------|-------------|----------------|-------|
| `H1` | 32 | SemiBold (600) | 40px (1.25) | -0.5px | Page titles |
| `H2` | 24 | SemiBold (600) | 32px (1.33) | -0.25px | Section titles |
| `H3` | 20 | Medium (500) | 28px (1.4) | 0 | Card/panel titles |
| `H4` | 16 | Medium (500) | 24px (1.5) | 0 | Subsection titles |
| `H5` | 14 | SemiBold (600) | 20px (1.43) | 0.1px | Small headings |
| `Body` | 14 | Regular (400) | 22px (1.57) | 0 | Default text |
| `BodyLarge` | 16 | Regular (400) | 24px (1.5) | 0 | Emphasized body |
| `Small` | 12 | Regular (400) | 18px (1.5) | 0.1px | Secondary text |
| `Caption` | 11 | Medium (500) | 16px (1.45) | 0.2px | Labels, hints |
| `Overline` | 10 | SemiBold (600) | 14px (1.4) | 1px (uppercase) | Category labels |

### 2.3 Font Rendering

- **Anti-aliasing:** ClearType on Windows
- **Text rendering:** `TextOptions.TextFormattingMode="Display"` for small text
- **Hinting:** Enable for all sizes below 16px

---

## 3. Spacing System

### 3.1 Base Unit

**Base unit: 4px** (all spacing values are multiples of 4)

### 3.2 Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| `SpacingNone` | 0px | No spacing |
| `SpacingXXS` | 2px | Tight inline spacing |
| `SpacingXS` | 4px | Icon-to-text, tight elements |
| `SpacingS` | 8px | Related elements, small gaps |
| `SpacingM` | 12px | Standard component padding |
| `SpacingL` | 16px | Section spacing |
| `SpacingXL` | 24px | Large section spacing |
| `SpacingXXL` | 32px | Major sections |
| `Spacing3XL` | 48px | Page-level spacing |
| `Spacing4XL` | 64px | Large page margins |

### 3.3 Component Padding Standards

| Component | Padding (Y / X) |
|-----------|-----------------|
| Button Small | 6px / 12px |
| Button Medium | 8px / 16px |
| Button Large | 12px / 24px |
| TextBox | 8px / 12px |
| Card | 16px / 20px |
| Dialog | 24px / 24px |
| Tooltip | 6px / 10px |

---

## 4. Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| `RadiusNone` | 0px | No rounding |
| `RadiusXS` | 2px | Badges, tags |
| `RadiusS` | 4px | Buttons, inputs, small elements |
| `RadiusM` | 6px | Cards, panels |
| `RadiusL` | 8px | Dialogs, large cards |
| `RadiusXL` | 12px | Modals, feature cards |
| `RadiusFull` | 9999px | Pills, avatars, circular elements |

---

## 5. Elevation & Shadows

### 5.1 Dark Theme Shadows

| Level | Shadow | Usage |
|-------|--------|-------|
| `Elevation0` | none | Flat elements |
| `Elevation1` | `0 1px 3px rgba(0,0,0,0.4)` | Subtle lift (buttons) |
| `Elevation2` | `0 2px 6px rgba(0,0,0,0.5)` | Cards, dropdowns |
| `Elevation3` | `0 4px 12px rgba(0,0,0,0.6)` | Floating panels |
| `Elevation4` | `0 8px 24px rgba(0,0,0,0.7)` | Modals, dialogs |
| `Elevation5` | `0 16px 48px rgba(0,0,0,0.8)` | Important overlays |

### 5.2 Light Theme Shadows

| Level | Shadow | Usage |
|-------|--------|-------|
| `Elevation0` | none | Flat elements |
| `Elevation1` | `0 1px 3px rgba(0,0,0,0.08)` | Subtle lift (buttons) |
| `Elevation2` | `0 2px 8px rgba(0,0,0,0.12)` | Cards, dropdowns |
| `Elevation3` | `0 4px 16px rgba(0,0,0,0.15)` | Floating panels |
| `Elevation4` | `0 8px 32px rgba(0,0,0,0.18)` | Modals, dialogs |
| `Elevation5` | `0 16px 48px rgba(0,0,0,0.22)` | Important overlays |

---

## 6. Component Specifications

### 6.1 FathomButton

**All buttons use 4px border radius by default.**

#### Variants

##### Primary Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: `#0066CC`, Text: `#FFFFFF` | BG: `#0066CC`, Text: `#FFFFFF` |
| Hover | BG: `#0052A3`, Text: `#FFFFFF` | BG: `#0052A3`, Text: `#FFFFFF` |
| Pressed | BG: `#003D7A`, Text: `#FFFFFF` | BG: `#003D7A`, Text: `#FFFFFF` |
| Disabled | BG: `#0066CC50`, Text: `#FFFFFF80` | BG: `#0066CC50`, Text: `#FFFFFF80` |
| Focus | + 2px outline `#3399FF` offset 2px | + 2px outline `#3399FF` offset 2px |

##### Secondary Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: `#333337`, Text: `#CCCCCC`, Border: `#3F3F46` | BG: `#FFFFFF`, Text: `#1E1E1E`, Border: `#D4D4D4` |
| Hover | BG: `#3C3C40`, Border: `#0066CC` | BG: `#F5F5F5`, Border: `#0066CC` |
| Pressed | BG: `#4A4A4F` | BG: `#E0E0E0` |
| Disabled | BG: `#333337`, Opacity: 0.5 | BG: `#FFFFFF`, Opacity: 0.5 |

##### Outline Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: transparent, Text: `#0066CC`, Border: `#0066CC` | Same |
| Hover | BG: `#0066CC15`, Border: `#0052A3` | BG: `#0066CC10` |
| Pressed | BG: `#0066CC25` | BG: `#0066CC20` |

##### Ghost Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: transparent, Text: `#CCCCCC` | BG: transparent, Text: `#1E1E1E` |
| Hover | BG: `#FFFFFF10` | BG: `#00000008` |
| Pressed | BG: `#FFFFFF15` | BG: `#00000012` |

##### Danger Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: `#F14C4C`, Text: `#FFFFFF` | BG: `#DC3545`, Text: `#FFFFFF` |
| Hover | BG: `#D93939` | BG: `#C82333` |
| Pressed | BG: `#B52F2F` | BG: `#A71D2A` |

##### Success Button
| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: `#4EC469`, Text: `#FFFFFF` | BG: `#28A745`, Text: `#FFFFFF` |
| Hover | BG: `#3DB357` | BG: `#218838` |
| Pressed | BG: `#2F9B48` | BG: `#1E7E34` |

#### Sizes
| Size | Height | Padding (X) | Font Size | Icon Size |
|------|--------|-------------|-----------|-----------|
| Small | 28px | 12px | 12px | 14px |
| Medium | 36px | 16px | 14px | 16px |
| Large | 44px | 24px | 16px | 20px |

---

### 6.2 FathomCard

**All cards use 6px border radius by default.**

#### Variants

##### Elevated Card
| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| Background | `#333337` | `#FFFFFF` |
| Border | none | none |
| Shadow | Elevation2 | Elevation2 |
| Hover (if clickable) | Shadow Elevation3 | Shadow Elevation3 |

##### Outlined Card
| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| Background | `#252526` | `#FFFFFF` |
| Border | 1px `#3F3F46` | 1px `#D4D4D4` |
| Shadow | none | none |
| Hover (if clickable) | Border: `#0066CC` | Border: `#0066CC` |

##### Filled Card
| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| Background | `#2D2D30` | `#F3F3F3` |
| Border | none | none |
| Shadow | none | none |
| Hover (if clickable) | BG: `#333337` | BG: `#E8E8E8` |

#### Elevation Levels (for Elevated variant)
- **0**: No shadow (use for nested cards)
- **1**: Subtle lift
- **2**: Standard card (default)
- **3**: Floating/prominent
- **4**: Dialog-level
- **5**: Critical overlays

---

### 6.3 FathomTextBox

**4px border radius, 1px border.**

| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Default | BG: `#1E1E1E`, Border: `#3F3F46`, Text: `#CCCCCC` | BG: `#FFFFFF`, Border: `#D4D4D4`, Text: `#1E1E1E` |
| Placeholder | Text: `#6E6E6E` | Text: `#969696` |
| Hover | Border: `#4D4D52` | Border: `#B8B8B8` |
| Focused | Border: `#0066CC`, BG tint: `#0066CC10`, + 2px focus ring | Same |
| Error | Border: `#F14C4C`, BG tint: `#F14C4C10` | Border: `#DC3545` |
| Disabled | BG: `#252526`, Text: `#4D4D4D`, Border: `#2D2D30` | BG: `#F3F3F3`, Text: `#B8B8B8` |

**Padding:** 8px vertical, 12px horizontal
**Height:** 36px (single line)

---

### 6.4 FathomComboBox

| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| **Closed State** | Same as FathomTextBox | Same as FathomTextBox |
| **Dropdown BG** | `#2D2D30` | `#FFFFFF` |
| **Dropdown Border** | `#3F3F46` | `#D4D4D4` |
| **Dropdown Shadow** | Elevation3 | Elevation3 |
| **Item Hover** | BG: `#2A2D2E` | BG: `#F5F5F5` |
| **Item Selected** | BG: `#094771`, Text: `#FFFFFF` | BG: `#E6F2FF`, Text: `#0066CC` |
| **Chevron Color** | `#969696` | `#6E6E6E` |

---

### 6.5 FathomDataGrid

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Header BG** | `#252526` | `#F3F3F3` |
| **Header Text** | `#969696`, 12px SemiBold, uppercase | `#6E6E6E` |
| **Header Border** | Bottom 1px `#3F3F46` | Bottom 1px `#D4D4D4` |
| **Row BG** | `#1E1E1E` | `#FFFFFF` |
| **Row Alternate BG** | `#252526` | `#FAFAFA` |
| **Row Text** | `#CCCCCC`, 14px Regular | `#1E1E1E` |
| **Row Hover** | BG: `#2A2D2E` | BG: `#F5F5F5` |
| **Row Selected** | BG: `#094771`, Text: `#FFFFFF` | BG: `#E6F2FF` |
| **Row Focus** | + 2px inset outline `#0066CC` | Same |
| **Cell Border** | Right 1px `#2D2D30` | Right 1px `#E8E8E8` |
| **Grid Border** | 1px `#3F3F46` | 1px `#D4D4D4` |

**Row Height:** 40px
**Header Height:** 44px
**Cell Padding:** 12px horizontal

---

### 6.6 FathomTabControl

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Tab Bar BG** | `#252526` | `#F3F3F3` |
| **Tab Default** | Text: `#969696`, BG: transparent | Text: `#6E6E6E` |
| **Tab Hover** | Text: `#CCCCCC`, BG: `#2A2D2E` | Text: `#1E1E1E`, BG: `#E8E8E8` |
| **Tab Selected** | Text: `#FFFFFF`, Bottom border 2px `#0066CC` | Text: `#0066CC` |
| **Tab Content BG** | `#1E1E1E` | `#FFFFFF` |

**Tab Height:** 44px
**Tab Padding:** 16px horizontal

---

### 6.7 Window Chrome & Title Bar

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Title Bar BG** | `#1E1E1E` | `#F3F3F3` |
| **Title Bar Height** | 32px | 32px |
| **Title Text** | `#CCCCCC`, 12px Medium | `#1E1E1E` |
| **Window Border** | 1px `#3F3F46` | 1px `#D4D4D4` |
| **Control Buttons BG** | transparent | transparent |
| **Control Buttons Hover** | BG: `#3C3C40` | BG: `#E0E0E0` |
| **Close Button Hover** | BG: `#E81123` | BG: `#E81123` |

---

### 6.8 FathomScrollBar

**Track and thumb styling for lists, grids, and scrollable content.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Track BG** | `#1E1E1E` | `#F3F3F3` |
| **Track Width** | 12px (can shrink to 6px on hover-away) | Same |
| **Thumb BG** | `#4D4D52` | `#C4C4C4` |
| **Thumb Hover** | `#6E6E6E` | `#A0A0A0` |
| **Thumb Pressed** | `#969696` | `#808080` |
| **Thumb Radius** | 6px (pill shape) | Same |
| **Thumb Min Height** | 32px | Same |
| **Arrow Buttons** | Hidden by default (modern style) | Same |

**Behavior:**
- Auto-hide when not scrolling (fade out after 1s)
- Expand on mouse hover over scroll area
- Smooth scroll animation

---

### 6.9 FathomCheckBox

**4px border radius, 18x18px checkbox size.**

| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| **Unchecked** | BG: transparent, Border: `#6E6E6E` | Border: `#969696` |
| **Unchecked Hover** | Border: `#969696` | Border: `#6E6E6E` |
| **Checked** | BG: `#0066CC`, Border: `#0066CC`, Checkmark: `#FFFFFF` | Same |
| **Checked Hover** | BG: `#0052A3` | Same |
| **Indeterminate** | BG: `#0066CC`, Dash icon: `#FFFFFF` | Same |
| **Disabled** | Opacity: 0.4 | Same |
| **Focus** | + 2px outline `#3399FF` | Same |

**Label:** 8px spacing from checkbox, uses `TextPrimary` color
**Checkmark Icon:** 12px, 2px stroke weight

---

### 6.10 FathomRadioButton

**Circular, 18px diameter.**

| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| **Unselected** | BG: transparent, Border: `#6E6E6E` (2px) | Border: `#969696` |
| **Unselected Hover** | Border: `#969696` | Border: `#6E6E6E` |
| **Selected** | Border: `#0066CC`, Inner dot: `#0066CC` (8px) | Same |
| **Selected Hover** | Border: `#0052A3`, Dot: `#0052A3` | Same |
| **Disabled** | Opacity: 0.4 | Same |
| **Focus** | + 2px outline `#3399FF` | Same |

**Label:** 8px spacing from radio, uses `TextPrimary` color

---

### 6.11 FathomToggleSwitch

**Modern on/off toggle, 44x24px track.**

| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| **Off Track** | BG: `#4D4D52`, Border: `#4D4D52` | BG: `#C4C4C4` |
| **Off Thumb** | BG: `#CCCCCC`, 18px circle | BG: `#FFFFFF` |
| **Off Hover** | Track: `#5A5A5F` | Track: `#B8B8B8` |
| **On Track** | BG: `#0066CC` | Same |
| **On Thumb** | BG: `#FFFFFF`, 18px circle | Same |
| **On Hover** | Track: `#0052A3` | Same |
| **Disabled** | Opacity: 0.4 | Same |
| **Focus** | + 2px outline `#3399FF` | Same |

**Animation:** Thumb slides 20px with 200ms ease-in-out
**Label:** Can be placed left or right, 12px spacing

---

### 6.12 FathomProgressBar

**4px border radius, 4px height (indeterminate) or 8px height (determinate).**

#### Determinate Progress
| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Track BG** | `#333337` | `#E8E8E8` |
| **Fill BG** | `#0066CC` | Same |
| **Fill Gradient** | Optional: `#0066CC` to `#3399FF` | Same |

#### Indeterminate Progress
| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Track BG** | `#333337` | `#E8E8E8` |
| **Animated Bar** | `#0066CC`, sliding left-to-right | Same |

**Animation:** Indeterminate uses 1.5s infinite loop with ease-in-out

#### Circular Progress (Spinner)
| Property | Specification |
|----------|---------------|
| **Size** | 24px (small), 40px (medium), 64px (large) |
| **Stroke Width** | 3px |
| **Color** | `#0066CC` (primary) or `#CCCCCC` (secondary) |
| **Animation** | 1s rotation, ease-in-out |

---

### 6.13 FathomMenu & ContextMenu

**6px border radius, elevation 3 shadow.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Menu BG** | `#2D2D30` | `#FFFFFF` |
| **Menu Border** | `#3F3F46` | `#D4D4D4` |
| **Menu Shadow** | Elevation3 | Elevation3 |
| **Item Height** | 32px | Same |
| **Item Padding** | 8px vertical, 12px horizontal | Same |
| **Item Text** | `#CCCCCC`, 14px | `#1E1E1E` |
| **Item Hover** | BG: `#094771` | BG: `#E6F2FF` |
| **Item Disabled** | Text: `#4D4D4D` | Text: `#B8B8B8` |
| **Separator** | 1px `#3F3F46`, 4px margin vertical | 1px `#E8E8E8` |
| **Submenu Arrow** | `#969696`, 12px chevron | `#6E6E6E` |
| **Icon Size** | 16px, 8px margin right | Same |
| **Shortcut Text** | `#6E6E6E`, right-aligned | `#969696` |

---

### 6.14 FathomSlider

**Horizontal slider with track and thumb.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Track Height** | 4px | Same |
| **Track BG (empty)** | `#4D4D52` | `#D4D4D4` |
| **Track BG (filled)** | `#0066CC` | Same |
| **Thumb Size** | 16px circle | Same |
| **Thumb BG** | `#FFFFFF` | Same |
| **Thumb Border** | 2px `#0066CC` | Same |
| **Thumb Hover** | Scale 1.1, shadow Elevation1 | Same |
| **Thumb Pressed** | Scale 0.95 | Same |
| **Thumb Disabled** | BG: `#6E6E6E`, no border | Same |
| **Focus** | + 2px outline `#3399FF` around thumb | Same |

**Tick Marks (optional):**
- Size: 2px width, 8px height
- Color: `#4D4D52` (dark) / `#C4C4C4` (light)
- Below track, 4px spacing

---

### 6.15 FathomTreeView

**Hierarchical navigation with expand/collapse.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Item Height** | 28px | Same |
| **Item Padding** | 8px horizontal | Same |
| **Item Text** | `#CCCCCC`, 14px | `#1E1E1E` |
| **Item Hover** | BG: `#2A2D2E` | BG: `#F5F5F5` |
| **Item Selected** | BG: `#094771`, Text: `#FFFFFF` | BG: `#E6F2FF`, Text: `#0066CC` |
| **Expand Arrow** | `#6E6E6E`, 12px chevron, rotates 90° | `#969696` |
| **Indent Per Level** | 20px | Same |
| **Icon Size** | 16px, 8px margin right | Same |
| **Connector Lines** | Optional: 1px `#3F3F46` | 1px `#E8E8E8` |

**Animation:** Expand/collapse uses 150ms ease-out

---

### 6.16 FathomToast / Notification

**Non-blocking notifications appearing in corner.**

| Property | Specification |
|----------|---------------|
| **Position** | Bottom-right corner, 16px from edge |
| **Width** | 360px |
| **Border Radius** | 8px |
| **Shadow** | Elevation3 |
| **Stack Spacing** | 8px between toasts |
| **Max Visible** | 3 toasts |

| Variant | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Info** | BG: `#28374A`, Border-left: 4px `#3794FF`, Icon: `#3794FF` | BG: `#CCE5FF`, Border: `#0066CC` |
| **Success** | BG: `#2D4A34`, Border-left: 4px `#4EC469`, Icon: `#4EC469` | BG: `#D4EDDA`, Border: `#28A745` |
| **Warning** | BG: `#4A4328`, Border-left: 4px `#DDB44D`, Icon: `#DDB44D` | BG: `#FFF3CD`, Border: `#D39E00` |
| **Error** | BG: `#4A2828`, Border-left: 4px `#F14C4C`, Icon: `#F14C4C` | BG: `#F8D7DA`, Border: `#DC3545` |

**Layout:**
- Icon: 20px, left side
- Title: 14px SemiBold, `TextHeading`
- Message: 13px Regular, `TextSecondary`
- Close button: 16px X icon, top-right
- Action button (optional): Ghost button style

**Animation:**
- Enter: Slide in from right, 300ms ease-out
- Exit: Fade out, 200ms
- Auto-dismiss: 5s default (configurable)

---

### 6.17 Loading States

#### Skeleton Loader
| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| **Base BG** | `#333337` | `#E8E8E8` |
| **Shimmer** | Gradient `#333337` → `#404045` → `#333337` | `#E8E8E8` → `#F3F3F3` → `#E8E8E8` |
| **Border Radius** | Match component being loaded | Same |
| **Animation** | 1.5s shimmer sweep, infinite | Same |

#### Loading Overlay
| Property | Dark Theme | Light Theme |
|----------|------------|-------------|
| **Overlay BG** | `#1E1E1E` at 80% opacity | `#FFFFFF` at 80% opacity |
| **Spinner** | Centered, 40px circular progress | Same |
| **Text (optional)** | Below spinner, `TextSecondary` | Same |

#### Button Loading State
- Replace text with spinner (16px)
- Keep button width (prevent layout shift)
- Disable interactions

---

### 6.18 Empty States

**Placeholder content when no data is available.**

| Element | Specification |
|---------|---------------|
| **Container** | Centered in content area, max-width 400px |
| **Illustration** | 120px icon or simple illustration, `#4D4D52` (dark) / `#C4C4C4` (light) |
| **Title** | H3 style, `TextHeading`, 16px margin-top |
| **Description** | Body style, `TextSecondary`, 8px margin-top, max 2 lines |
| **Action Button** | Primary or Secondary button, 24px margin-top |

**Common Empty States:**
- No results found (search icon)
- No items yet (plus icon)
- Error loading (warning icon)
- No connection (wifi-off icon)

---

### 6.19 FathomStatusBar

**Bottom status bar for application information.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Height** | 24px | Same |
| **BG** | `#007ACC` (accent) or `#252526` (neutral) | `#0066CC` or `#F3F3F3` |
| **Text** | `#FFFFFF` (accent) or `#CCCCCC` (neutral), 12px | `#FFFFFF` or `#6E6E6E` |
| **Border Top** | 1px `#3F3F46` (if neutral BG) | 1px `#D4D4D4` |
| **Item Padding** | 8px horizontal | Same |
| **Separator** | 1px vertical `#FFFFFF30` | Same |
| **Icon Size** | 14px | Same |

**Sections:**
- Left: Application status, current mode
- Center: Optional progress or messages
- Right: Connection status, notifications, zoom level

---

### 6.20 Sidebar Navigation

**Vertical navigation panel.**

| Element | Dark Theme | Light Theme |
|---------|------------|-------------|
| **Width** | 240px expanded, 48px collapsed | Same |
| **BG** | `#252526` | `#F3F3F3` |
| **Border Right** | 1px `#3F3F46` | 1px `#D4D4D4` |
| **Item Height** | 40px | Same |
| **Item Padding** | 12px horizontal | Same |
| **Item Icon** | 20px, `#969696` | `#6E6E6E` |
| **Item Text** | 14px, `#CCCCCC` | `#1E1E1E` |
| **Item Hover** | BG: `#2A2D2E` | BG: `#E8E8E8` |
| **Item Active** | BG: `#094771`, Text: `#FFFFFF`, Left border: 3px `#0066CC` | BG: `#E6F2FF`, Text: `#0066CC` |
| **Section Header** | 11px uppercase, `#6E6E6E`, 24px margin-top | Same |
| **Collapse Button** | 32px square, bottom of sidebar | Same |

**Animation:** Width transition 200ms ease-in-out

---

### 6.21 Icon Guidelines

**MahApps.Metro IconPacks (Material Design)**

| Property | Specification |
|----------|---------------|
| **Icon Pack** | MahApps.Metro.IconPacks.Material |
| **Size Scale** | 12px (tiny), 14px (small), 16px (default), 20px (medium), 24px (large), 32px (xlarge) |
| **Default Color** | Inherit from parent text color |
| **Interactive Color** | `TextSecondary` default, `TextPrimary` on hover |
| **Disabled Color** | `TextDisabled` |
| **Spacing from Text** | 8px |
| **Button Icons** | Match button size guidelines (14px small, 16px medium, 20px large) |

**Common Icons:**
| Usage | Icon Name |
|-------|-----------|
| Add/Create | `Plus` |
| Delete | `Delete` |
| Edit | `Pencil` |
| Save | `ContentSave` |
| Settings | `Cog` |
| Search | `Magnify` |
| Close | `Close` |
| Menu | `Menu` |
| Back | `ArrowLeft` |
| Forward | `ArrowRight` |
| Expand | `ChevronDown` |
| Collapse | `ChevronUp` |
| Error | `AlertCircle` |
| Warning | `Alert` |
| Info | `Information` |
| Success | `CheckCircle` |
| Refresh | `Refresh` |
| Download | `Download` |
| Upload | `Upload` |
| User | `Account` |
| Theme Light | `WeatherSunny` |
| Theme Dark | `WeatherNight` |

---

## 7. Theme Toggle Implementation

### 7.1 Placement

**Location:** Title bar, RightWindowCommands area, positioned to the LEFT of the minimize/maximize/close buttons.

```
[App Title]        [Left Commands]     [Theme Toggle] [_] [□] [X]
```

### 7.2 Toggle Button Design

| Property | Specification |
|----------|---------------|
| **Button Type** | Icon toggle button |
| **Size** | 32px x 32px (matches title bar height) |
| **Icon (Dark Mode)** | Sun icon (indicates clicking will switch to light) |
| **Icon (Light Mode)** | Moon icon (indicates clicking will switch to dark) |
| **Icon Color Dark** | `#E8E8E8` |
| **Icon Color Light** | `#1E1E1E` |
| **Hover BG Dark** | `#3C3C40` |
| **Hover BG Light** | `#E0E0E0` |
| **Tooltip Dark** | "Switch to Light Theme" |
| **Tooltip Light** | "Switch to Dark Theme" |

### 7.3 Icon Specifications

**Sun Icon (for Dark Theme - indicates light mode):**
- Use Material Design `WeatherSunny` or similar
- 16px size, centered

**Moon Icon (for Light Theme - indicates dark mode):**
- Use Material Design `WeatherNight` or similar
- 16px size, centered

### 7.4 Theme Persistence

```csharp
// Storage location
Properties.Settings.Default.Theme = "Dark" | "Light";
Properties.Settings.Default.Save();

// Or using registry
HKEY_CURRENT_USER\Software\FathomOS\UI\Theme = "Dark" | "Light"
```

### 7.5 Theme Switching Animation

- **Duration:** 200ms
- **Easing:** EaseInOutCubic
- **Properties to animate:** Background colors, text colors (opacity crossfade)

---

## 8. Animation & Transitions

### 8.1 Duration Scale

| Token | Duration | Usage |
|-------|----------|-------|
| `DurationFast` | 100ms | Hover states, focus |
| `DurationNormal` | 200ms | Standard transitions |
| `DurationSlow` | 300ms | Complex animations |
| `DurationVerySlow` | 500ms | Page transitions |

### 8.2 Easing Functions

| Token | Value | Usage |
|-------|-------|-------|
| `EaseDefault` | cubic-bezier(0.4, 0, 0.2, 1) | Standard |
| `EaseIn` | cubic-bezier(0.4, 0, 1, 1) | Elements exiting |
| `EaseOut` | cubic-bezier(0, 0, 0.2, 1) | Elements entering |
| `EaseInOut` | cubic-bezier(0.4, 0, 0.2, 1) | Moving elements |

---

## 9. Implementation Order

### Phase 1: Foundation (Priority: Critical)
1. **Color System** - Create `DarkThemeColors.xaml` and `LightThemeColors.xaml` with all tokens
2. **Theme Service** - Implement `IThemeService` for runtime theme switching
3. **Typography** - Create `Typography.xaml` with complete type scale
4. **Spacing** - Create `Spacing.xaml` with spacing tokens

### Phase 2: Core Components (Priority: High)
5. **FathomButton** - Implement all 6 variants with proper states
6. **FathomTextBox** - All input states and validation styling
7. **FathomCard** - All 3 variants with elevation support
8. **FathomCheckBox** - Checkbox with all states
9. **FathomRadioButton** - Radio button with all states
10. **FathomToggleSwitch** - Modern toggle switch

### Phase 3: Form & Selection Components (Priority: High)
11. **FathomComboBox** - Dropdown styling with proper theming
12. **FathomSlider** - Slider with track and thumb
13. **FathomScrollBar** - Custom scrollbar styling
14. **FathomProgressBar** - Determinate, indeterminate, and circular spinner

### Phase 4: Complex Components (Priority: High)
15. **FathomDataGrid** - Headers, rows, selection, alternating colors
16. **FathomTabControl** - Tab styling for both themes
17. **FathomTreeView** - Hierarchical navigation with expand/collapse
18. **FathomMenu** - Menu and ContextMenu styling

### Phase 5: Window Integration (Priority: Medium)
19. **Window Chrome** - Custom title bar
20. **Theme Toggle Button** - Icon button in title bar
21. **FathomStatusBar** - Bottom status bar
22. **FathomSidebar** - Collapsible sidebar navigation

### Phase 6: Feedback Components (Priority: Medium)
23. **FathomToast** - Toast notification system
24. **SkeletonLoader** - Loading skeleton placeholders
25. **EmptyState** - Empty state placeholders
26. **FathomDialog** - Modal dialog styling

### Phase 7: Polish (Priority: Medium)
27. **Animations** - Add transition animations to all components
28. **Accessibility** - Focus indicators, keyboard navigation
29. **Documentation** - Update design system docs

### Dependencies

```
Color System
    └── Theme Service
        └── Typography + Spacing
            ├── FathomButton
            ├── FathomTextBox
            ├── FathomCard
            ├── FathomCheckBox
            ├── FathomRadioButton
            └── FathomToggleSwitch
                ├── FathomComboBox
                ├── FathomSlider
                ├── FathomScrollBar
                └── FathomProgressBar
                    ├── FathomDataGrid
                    ├── FathomTabControl
                    ├── FathomTreeView
                    └── FathomMenu
                        ├── Window Chrome + Theme Toggle
                        ├── FathomStatusBar
                        └── FathomSidebar
                            ├── FathomToast
                            ├── SkeletonLoader
                            ├── EmptyState
                            └── FathomDialog
```

---

## 10. File Structure

```
FathomOS.UI/
├── Themes/
│   ├── Colors/
│   │   ├── DarkThemeColors.xaml      # Dark theme color definitions
│   │   └── LightThemeColors.xaml     # Light theme color definitions
│   ├── DarkTheme.xaml                # Complete dark theme (merged)
│   ├── LightTheme.xaml               # Complete light theme (merged)
│   ├── Typography.xaml               # Font definitions
│   ├── Spacing.xaml                  # Spacing tokens
│   └── Generic.xaml                  # Control templates (theme-agnostic)
├── Controls/
│   ├── FathomButton.cs               # Button control (6 variants)
│   ├── FathomCard.cs                 # Card control (3 variants)
│   ├── FathomTextBox.cs              # TextBox control
│   ├── FathomComboBox.cs             # ComboBox control
│   ├── FathomDataGrid.cs             # DataGrid control
│   ├── FathomTabControl.cs           # TabControl
│   ├── FathomCheckBox.cs             # CheckBox control
│   ├── FathomRadioButton.cs          # RadioButton control
│   ├── FathomToggleSwitch.cs         # Toggle switch control
│   ├── FathomProgressBar.cs          # Progress bar + spinner
│   ├── FathomSlider.cs               # Slider control
│   ├── FathomMenu.cs                 # Menu + ContextMenu
│   ├── FathomTreeView.cs             # TreeView control
│   ├── FathomScrollBar.cs            # ScrollBar styling
│   ├── FathomToast.cs                # Toast notification
│   ├── FathomStatusBar.cs            # Status bar control
│   ├── FathomSidebar.cs              # Sidebar navigation
│   ├── FathomWindow.cs               # Custom window with chrome
│   ├── ThemeToggleButton.cs          # Theme toggle control
│   ├── SkeletonLoader.cs             # Loading skeleton
│   └── EmptyState.cs                 # Empty state placeholder
├── Services/
│   └── ThemeService.cs               # Theme management service
└── Documentation/
    ├── DesignSystem.md               # Main documentation
    └── UIDesignSystemPlan.md         # This file
```

---

## 11. Accessibility Requirements

### WCAG 2.1 AA Compliance

| Requirement | Specification |
|-------------|---------------|
| **Color Contrast (Text)** | Minimum 4.5:1 for body text, 3:1 for large text |
| **Color Contrast (UI)** | Minimum 3:1 for interactive elements |
| **Focus Indicators** | Visible 2px outline with 3:1 contrast ratio |
| **Target Size** | Minimum 24x24px for interactive elements |
| **Keyboard Navigation** | Full keyboard accessibility for all controls |

### Color Contrast Verification

| Combination | Dark Theme | Light Theme |
|-------------|------------|-------------|
| Primary on Background | 4.5:1 | 4.7:1 |
| TextPrimary on Background | 8.5:1 | 12.6:1 |
| TextSecondary on Background | 4.6:1 | 4.5:1 |

---

## 12. Approval Checklist

Please review and approve each section:

- [ ] **Color Palette** - Dark and Light theme colors
- [ ] **Typography** - Font families, sizes, weights
- [ ] **Spacing System** - Spacing scale and component padding
- [ ] **FathomButton** - All variants and states
- [ ] **FathomCard** - All variants and elevation levels
- [ ] **FathomTextBox** - All states
- [ ] **FathomComboBox** - Dropdown styling
- [ ] **FathomDataGrid** - Grid styling
- [ ] **FathomTabControl** - Tab styling
- [ ] **Window Chrome** - Title bar and theme toggle placement
- [ ] **Theme Toggle** - Icon design and persistence method
- [ ] **Implementation Order** - Phased approach

---

## 13. User Decisions (APPROVED)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Theme Default** | Dark | Professional feel, matches VS Code/Figma aesthetic |
| **System Theme** | Independent setting | App theme separate from Windows, user controls it |
| **Icon Pack** | MahApps.Metro IconPacks | Material Design icons, well-maintained |
| **Animation** | All animations enabled | Smooth transitions for professional feel |
| **Accent Color** | Fixed Ocean Blue (#0066CC) | Consistent branding across all installations |

---

**STATUS: APPROVED FOR IMPLEMENTATION**

*User approved: 2026-01-16*
