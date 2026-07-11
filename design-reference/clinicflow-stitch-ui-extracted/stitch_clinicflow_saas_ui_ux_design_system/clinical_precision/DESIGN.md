---
name: Clinical Precision
colors:
  surface: '#faf8ff'
  surface-dim: '#d8d9e6'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f3ff'
  surface-container: '#ecedfa'
  surface-container-high: '#e6e7f4'
  surface-container-highest: '#e1e2ee'
  on-surface: '#191b24'
  on-surface-variant: '#424656'
  inverse-surface: '#2e303a'
  inverse-on-surface: '#eff0fd'
  outline: '#727687'
  outline-variant: '#c2c6d8'
  surface-tint: '#0054d6'
  primary: '#0050cb'
  on-primary: '#ffffff'
  primary-container: '#0066ff'
  on-primary-container: '#f8f7ff'
  inverse-primary: '#b3c5ff'
  secondary: '#4a607c'
  on-secondary: '#ffffff'
  secondary-container: '#c5dcfd'
  on-secondary-container: '#4b617d'
  tertiary: '#a33200'
  on-tertiary: '#ffffff'
  tertiary-container: '#cc4204'
  on-tertiary-container: '#fff6f4'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae1ff'
  primary-fixed-dim: '#b3c5ff'
  on-primary-fixed: '#001849'
  on-primary-fixed-variant: '#003fa4'
  secondary-fixed: '#d2e4ff'
  secondary-fixed-dim: '#b1c8e9'
  on-secondary-fixed: '#021c36'
  on-secondary-fixed-variant: '#324863'
  tertiary-fixed: '#ffdbd0'
  tertiary-fixed-dim: '#ffb59d'
  on-tertiary-fixed: '#390c00'
  on-tertiary-fixed-variant: '#832600'
  background: '#faf8ff'
  on-background: '#191b24'
  surface-variant: '#e1e2ee'
typography:
  display-lg:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
  display-lg-mobile:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
  headline-md:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-base:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-bold:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
  utility-mono:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 32px
  container-margin: 24px
  gutter: 16px
---

## Brand & Style
The design system is engineered for high-stakes medical environments, prioritizing clarity, trust, and rapid information processing. The brand personality is authoritative yet empathetic, blending a "Corporate Modern" aesthetic with "Minimalist" functionalism to ensure the interface never obstructs the clinician’s workflow. 

The visual narrative is built on high-fidelity healthcare standards: clean surfaces, generous white space to reduce cognitive load, and a strict adherence to RTL (Right-to-Left) hierarchy. The emotional response is one of reliability and calm—essential for software managing patient lives and complex schedules.

## Colors
This design system utilizes a palette rooted in traditional medical trust. 
- **Primary Blue** is reserved for high-intent actions and active states.
- **Deep Navy** provides structural grounding, used primarily for navigation sidebars and headers to create a professional frame.
- **Functional Colors** (Teal, Orange, Red) follow global medical standards for status indication (Success, Warning, Critical Alert).
- **Surfaces** rely on a high-contrast relationship between pure white cards and a light gray background to create distinct "work zones."

## Typography
IBM Plex Sans Arabic is the cornerstone of this design system, chosen for its exceptional legibility in technical contexts and its professional, structured Arabic glyphs. 

The hierarchy is "Top-Heavy" to ensure patient names and medical statuses are immediately identifiable. For data-dense screens, `body-sm` is the workhorse for table content, while `utility-mono` (using the sans-arabic variant) is used for timestamps, medical codes, and metadata. All text must be aligned to the right by default, with numerical data often utilizing tabular lining to maintain vertical alignment in medical reports.

## Layout & Spacing
The design system employs a strict **8px grid system** to maintain mathematical harmony across dense medical forms. 

- **Layout:** A fluid 12-column grid is used for the main workspace, while the sidebar occupies a fixed 280px width on desktop.
- **Density:** Information density is "Comfortable" for dashboards but "Compact" for patient records and data tables. 
- **RTL Flow:** The layout starts from the top-right. Navigation is anchored to the right, and content flows toward the left. 
- **Breakpoints:**
  - Desktop: 1280px+ (Full sidebar + multi-column grid)
  - Tablet: 768px - 1279px (Collapsed sidebar, 2-column grid)
  - Mobile: Under 767px (Bottom navigation or hamburger menu, single-column stack, reduced margins of 16px).

## Elevation & Depth
Depth is signaled through **Tonal Layering** and **Subtle Ambient Shadows**. 

1. **Level 0 (Base):** Surface Secondary (#F9FAFB) – used for the application background.
2. **Level 1 (Cards):** Surface Primary (#FFFFFF) – used for all content containers with a 1px border (#E5E7EB).
3. **Level 2 (Interaction):** Soft shadow (0px 4px 6px -1px rgba(0, 0, 0, 0.05)) – used for hovered states and dropdowns.
4. **Level 3 (Overlay):** High-diffusion shadow (0px 20px 25px -5px rgba(0, 0, 0, 0.1)) – reserved for modals and urgent clinical alerts.

Avoid heavy shadows or "neomorphic" effects; the interface should feel flat and efficient, with depth used only to indicate priority or "floating" temporary elements.

## Shapes
A "Rounded" shape language (8px default) is applied to strike a balance between clinical precision and modern accessibility. 

- **Cards & Large Containers:** Use `rounded-lg` (12px) to soften the large volume of data screens.
- **Buttons & Inputs:** Use the base `rounded` (8px) for a crisp, professional look.
- **Badges/Status Chips:** Use `rounded-xl` (24px/Pill) to distinguish them from interactive buttons.

## Components
### Buttons
- **Primary:** Medical Blue fill, White text. High-contrast.
- **Secondary:** Transparent fill, Deep Navy border and text.
- **Ghost:** No border, Primary Blue text. Used for less prominent actions within tables.

### Inputs & Form Controls
- Inputs must have a 1px border (#E5E7EB) that thickens and changes to Primary Blue on focus.
- Labels are always positioned above the field, right-aligned. 
- Error states use a Red (#EF4444) border and include an inline icon for accessibility.

### Cards
- White background, 12px corner radius, 1px subtle border.
- Padding should follow the `md` (16px) or `lg` (24px) spacing tokens depending on content density.

### Data Tables
- Header rows use Surface Secondary (#F9FAFB) with `label-bold` text.
- Row dividers are 1px solid (#E5E7EB).
- No vertical borders between columns; use horizontal spacing to define columns.

### Badges/Chips
- Used for patient status (e.g., "Stable", "Critical").
- Subtle background tints (e.g., 10% opacity of the functional color) with 100% opacity text of the same color for high legibility.