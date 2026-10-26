---
version: alpha
name: Almanca-Konus
description: The personal website of German teacher Suna Koç, for Turkish speakers learning German (A1–C1, exams, clinical German for doctors and nurses). Not a company and not an edtech product, so the interface should feel like a teacher's well-kept notebook rather than a school chain. A cream "paper" canvas, a coral accent taken from the original logo, a soft high-contrast serif (Fraunces) for headlines and a friendly geometric sans (Poppins) for everything else. Warm near-black ink instead of pure black, rounded pill controls, slowly drifting blurred blobs and dark "islands" lit by coral glows. Turkish and German are equal everywhere, and many visitors are adults who find computers stressful, so clarity always beats cleverness.

colors:
  canvas: "#FFF5E4"
  surface: "#FFFFFF"
  surface-soft: "#FFFAF3"
  ink: "#2B2220"
  body: "#4A3F3B"
  muted: "#85766F"
  hairline: "#F0E0D2"
  blush: "#FFE3E1"
  pink: "#FFD1D1"
  coral: "#FF9494"
  coral-deep: "#E36464"
  link: "#C2413F"
  highlight: "rgba(255, 148, 148, 0.28)"
  on-ink: "#FFF5E4"
  on-coral: "#FFFFFF"
  header-glass: "rgba(255, 251, 245, 0.84)"
  footer-top: "#FFE7E2"
  footer-bottom: "#FFD6D3"
  band: "#2B2220"
  band-text: "#F6EDE6"
  band-muted: "rgba(246, 237, 230, 0.72)"
  band-card: "rgba(36, 27, 24, 0.5)"
  band-button: "#FFF7F1"
  success: "#2E8B57"
  warning: "#E0A325"
  error: "#D64545"
  dark-canvas: "#15110E"
  dark-surface: "#1F1A16"
  dark-surface-soft: "#251F1A"
  dark-ink: "#F6EFE9"
  dark-body: "#DCD2C9"
  dark-muted: "#AB9F95"
  dark-hairline: "rgba(255, 255, 255, 0.09)"
  dark-blush: "#33241F"
  dark-pink: "#4A302C"
  dark-link: "#FFADAD"
  dark-band: "#0F0B09"
  dark-footer-top: "#201A16"
  dark-footer-bottom: "#17120F"

typography:
  display-xl:
    fontFamily: "Fraunces, Georgia, 'Times New Roman', serif"
    fontSize: "clamp(43px, 6.4vw, 76px)"
    fontWeight: 600
    lineHeight: 1.02
    letterSpacing: -0.032em
  display-lg:
    fontFamily: "Fraunces, Georgia, 'Times New Roman', serif"
    fontSize: "clamp(38px, 6vw, 64px)"
    fontWeight: 600
    lineHeight: 1.05
    letterSpacing: -0.025em
  display-md:
    fontFamily: "Fraunces, Georgia, 'Times New Roman', serif"
    fontSize: "clamp(30px, 4vw, 46px)"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: -0.02em
  display-sm:
    fontFamily: "Fraunces, Georgia, 'Times New Roman', serif"
    fontSize: "clamp(26px, 3vw, 34px)"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: -0.01em
  title-md:
    fontFamily: "Fraunces, Georgia, 'Times New Roman', serif"
    fontSize: 20px
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: -0.005em
  lead:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 17px
    fontWeight: 400
    lineHeight: 1.7
    letterSpacing: 0
  body-article:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 18px
    fontWeight: 400
    lineHeight: 1.8
    letterSpacing: 0
  body-md:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 16px
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: 0
  body-sm:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 14px
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: 0
  button:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 15px
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: 0
  eyebrow:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 12.5px
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: 0.08em
  caption-uppercase:
    fontFamily: "Poppins, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
    fontSize: 11.5px
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: 0.1em
  code:
    fontFamily: "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace"
    fontSize: 0.82em
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: 0
  wordmark:
    fontFamily: "'Varela Round', sans-serif"
    fontSize: 17px
    fontWeight: 700
    lineHeight: 1
    letterSpacing: 0.02em

rounded:
  xs: 6px
  sm: 10px
  input: 14px
  md: 16px
  lg: 24px
  band: "clamp(24px, 4vw, 40px)"
  sheet: 36px
  pill: 999px
  arch: "999px 999px 32px 32px"

spacing:
  xxs: 4px
  xs: 8px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  xxl: 48px
  container: "min(1180px, 100% - 2rem)"
  measure: 70ch
  section: "clamp(64px, 8vw, 104px)"
  header-offset: 96px

elevation:
  rest: "0 1px 2px rgba(43, 34, 32, 0.05), 0 1px 1px rgba(43, 34, 32, 0.03)"
  float: "0 12px 32px -14px rgba(43, 34, 32, 0.22)"
  lifted: "0 26px 50px -18px rgba(227, 100, 100, 0.38)"
  focus-ring: "3px solid #FF9494, offset 3px"

motion:
  ease: "cubic-bezier(0.22, 0.61, 0.36, 1)"
  ease-spring: "cubic-bezier(0.34, 1.56, 0.64, 1)"
  fast: 180ms
  med: 320ms
  slow: 700ms

components:
  top-nav:
    backgroundColor: "{colors.header-glass}"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    backdropFilter: "saturate(1.6) blur(14px)"
    borderBottom: "1px solid {colors.hairline}"
  wordmark:
    textColor: "{colors.ink}"
    typography: "{typography.wordmark}"
  eyebrow:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.coral-deep}"
    typography: "{typography.eyebrow}"
    rounded: "{rounded.pill}"
    padding: 6px 15px
    border: "1px solid {colors.hairline}"
    shadow: "{elevation.rest}"
  button-primary:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.on-ink}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: 12px 22px
  button-primary-active:
    backgroundColor: "{colors.coral-deep}"
    textColor: "{colors.on-coral}"
    rounded: "{rounded.pill}"
    shadow: "{elevation.lifted}"
  button-ghost:
    backgroundColor: transparent
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: 12px 22px
    border: "1px solid {colors.hairline}"
  button-on-band:
    backgroundColor: "{colors.band-button}"
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: 12px 22px
  chip:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.pill}"
    padding: 7px 16px
    border: "1px solid {colors.hairline}"
  chip-active:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.on-ink}"
    rounded: "{rounded.pill}"
  search-pill:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    rounded: "{rounded.pill}"
    padding: 6px 6px 6px 19px
    maxWidth: 560px
    shadow: "{elevation.float}"
  hero:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    typography: "{typography.display-lg}"
    padding: "clamp(48px, 7vw, 88px) 0 clamp(32px, 4vw, 48px)"
    align: center
  hero-home:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    typography: "{typography.display-xl}"
  hero-portrait:
    rounded: "{rounded.arch}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    rounded: "{rounded.lg}"
    padding: "clamp(20px, 3vw, 32px)"
    border: "1px solid {colors.hairline}"
    shadow: "{elevation.rest}"
  card-active:
    shadow: "{elevation.lifted}"
    transform: "translateY(-6px)"
  post-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    typography: "{typography.title-md}"
    rounded: "{rounded.lg}"
    border: "1px solid {colors.hairline}"
    shadow: "{elevation.rest}"
  feature-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    typography: "{typography.title-md}"
    rounded: "{rounded.lg}"
    padding: 32px 29px
    border: "1px solid {colors.hairline}"
  dark-band:
    backgroundColor: "{colors.band}"
    textColor: "{colors.band-text}"
    typography: "{typography.display-md}"
    rounded: "{rounded.band}"
    padding: "clamp(56px, 7vw, 88px) 0"
  band-card:
    backgroundColor: "{colors.band-card}"
    textColor: "{colors.band-text}"
    rounded: "{rounded.lg}"
    backdropFilter: "blur(18px) saturate(1.25)"
    border: "1px solid rgba(255, 255, 255, 0.1)"
  summary-box:
    backgroundColor: "{colors.blush}"
    textColor: "{colors.body}"
    rounded: "{rounded.lg}"
    padding: 21px 24px
    border: "1px solid {colors.pink}"
  toc:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.md}"
    position: sticky
  toc-item-active:
    backgroundColor: "{colors.blush}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    accentBar: "{colors.coral}"
  info-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.body}"
    rounded: "{rounded.sm}"
    borderLeft: "4px solid {colors.coral}"
  table:
    backgroundColor: "{colors.surface}"
    headerBackground: "{colors.surface-soft}"
    textColor: "{colors.body}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.sm}"
    border: "1px solid {colors.hairline}"
  text-input:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    rounded: "{rounded.input}"
    border: "1.5px solid {colors.hairline}"
  text-input-focused:
    backgroundColor: "{colors.surface}"
    border: "1.5px solid {colors.coral}"
    shadow: "0 0 0 4px {colors.highlight}"
  score-ring:
    trackColor: "{colors.hairline}"
    barColor: "{colors.coral-deep}"
    size: 64px
  toast:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.on-ink}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.pill}"
    padding: 13px 20px
    shadow: "{elevation.float}"
  back-to-top:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.on-ink}"
    rounded: "{rounded.pill}"
    size: 48px
  footer-sheet:
    backgroundColor: "linear-gradient(180deg, {colors.footer-top}, {colors.footer-bottom})"
    textColor: "{colors.body}"
    rounded: "{rounded.sheet} {rounded.sheet} 0 0"
    padding: "clamp(44px, 6vw, 72px) 0 24px"
---

# Almanca Konuş — Design System

> This file follows the [DESIGN.md format](https://github.com/VoltAgent/awesome-design-md) (Google Stitch): tokens
> in the front matter above, the reasoning below. Give it to a person or an AI tool that designs a new page, and
> the result should look like it belongs here. **How** things are built (CSS, JavaScript, Three.js, the exam
> room, the admin panel) is explained with code in [docs/DESIGN.md](docs/DESIGN.md).

## Overview

Almanca Konuş ("Speak German") is the personal website of **Suna Koç**, a German teacher who teaches Turkish
speakers online. It is **not a company, a school or a platform**, and the design
must never pretend otherwise: no stock photos of smiling teams, no "trusted by 10,000 learners" counters, no
pricing tables, no corporate blue. Suna runs it herself; the technical side is looked after for her by
family.

The visual idea is **a teacher's notebook on warm paper**: a cream canvas (`{colors.canvas}`), headlines in a soft
serif with a coral italic for the one word that matters (`Almancayı <em>konuşarak</em> öğrenin.`), real photos of
Suna, and rounded, tactile controls. Depth comes from blurred coral blobs that drift slowly behind the hero, from
cards that lift on hover with a warm coral shadow, and from two dark "islands" on the home page (clinical German
and YouTube Shorts) lit by radial coral glows.

Who the design is for shapes every decision:

- **Adults who are nervous about computers.** Big targets, plain words, one main action per screen, nothing that
  moves unexpectedly.
- **Two languages, equally.** Every text, URL, alt text and error message exists in Turkish and German. The two
  versions of a label often differ a lot in length, so check every layout with both.
- **Phones first.** Most students arrive from WhatsApp or Instagram on a phone.

## Voice & Identity

The words are part of the design. Because this is one person's site, the voice is personal and honest:

| Context | Voice | Example |
|---|---|---|
| About page, lesson descriptions | Suna, first person singular | "Merhaba, ben Suna." / "Hallo, ich bin Suna." |
| Interface (buttons, forms, errors) | Neutral, polite, short, formal *siz* / *Sie* | "Bir kelime ara…" / "Ein Wort suchen…" |
| Legal pages | "We" is defined once: Suna Koç and the person who technically manages the site for her | "Almanca Konuş bir şirket değil; Almanca öğretmeni Suna Koç'un kişisel sitesidir." |
| Navigation | "Hakkımda" / "Vita", never "Hakkımızda" / "Über uns" | |

Never write *our team*, *our company*, *ekibimiz*, *firmamız*, *şirketimiz*, *unser Team*, *unser Unternehmen*,
*Kundenservice*, or invent staff, offices, numbers of students or awards. Paid private lessons are arranged
personally by e-mail or WhatsApp; the site never shows prices, checkouts or "Buy now" buttons.

## Colors

### Brand & Accent

- **Coral** `{colors.coral}` `#FF9494`: the brand colour from the original logo. Decorative accents, list markers,
  focus rings, the eyebrow icon, active bars. Too light for text on cream.
- **Coral deep** `{colors.coral-deep}` `#E36464`: hover state of every primary button, eyebrow text, italic
  headline emphasis, icons on blush backgrounds.
- **Link** `{colors.link}` `#C2413F`: links inside running text. Darkened until it reached 4.7:1 on the canvas.
- **Blush** `{colors.blush}` `#FFE3E1` and **pink** `{colors.pink}` `#FFD1D1`: soft fills (summary boxes, active
  table-of-contents item, icon tiles) and their borders.
- **Highlight** `{colors.highlight}`: the marker-pen stripe under hero words and card titles.

### Surface

- **Canvas** `{colors.canvas}` `#FFF5E4`: cream paper, the page background.
- **Surface** `{colors.surface}` `#FFFFFF`: cards, inputs, tables. White on cream reads as "paper on paper".
- **Surface soft** `{colors.surface-soft}` `#FFFAF3`: quiet panels, table headers, quotes, code.
- **Band** `{colors.band}` `#2B2220`: the dark islands. Same hue as the ink, so they feel related, not foreign.
- **Header glass** `{colors.header-glass}`: translucent cream over a 14 px blur.
- **Footer sheet**: a vertical gradient `{colors.footer-top}` → `{colors.footer-bottom}`, a pink sheet that the
  page "rests on".

### Text

- **Ink** `{colors.ink}` `#2B2220`: headings and primary buttons. A warm near-black, never `#000`.
- **Body** `{colors.body}` `#4A3F3B`: running text.
- **Muted** `{colors.muted}` `#85766F`: meta lines, labels, captions. Not for anything people must read to act.
- **Hairline** `{colors.hairline}` `#F0E0D2`: all borders and dividers.

### Semantic

- **Success** `{colors.success}` `#2E8B57`, **warning** `{colors.warning}` `#E0A325`, **error** `{colors.error}`
  `#D64545`: exam results, answer feedback, form messages. Always paired with an icon and a word, never colour
  alone.

### Dark mode

Dark mode swaps the token values; components stay the same. The dark canvas is a **warm** near-black
(`{colors.dark-canvas}` `#15110E`), never blue-grey. Coral and coral-deep keep their values; links lighten to
`{colors.dark-link}`; the band deepens to `{colors.dark-band}` and gets a hairline border so it still reads as an
island; decorative blobs drop to 25 % opacity so they don't glow.

## Typography

### Font Family

- **Fraunces 600** (roman and italic, variable optical size): all headlines and card titles. Soft, slightly
  old-fashioned, bookish. The italic is reserved for emphasis in coral.
- **Poppins 400 / 600**: everything else. Geometric and friendly, with clear Turkish and German diacritics
  (ç ğ ı İ ş ö ü ä ß).
- **Varela Round**: the "Almanca Konuş" wordmark only (`{typography.wordmark}`).
- A system monospace for cookie names and code in blog posts.

All fonts are served from the site itself (subset to Turkish + German characters), never from Google's servers.

### Hierarchy

| Token | Use | Size |
|---|---|---|
| `{typography.display-xl}` | Home hero headline | 43 → 76 px fluid |
| `{typography.display-lg}` | Page hero (blog, dictionary, legal, quizzes) | 38 → 64 px |
| `{typography.display-md}` | Home section headings, band headings | 30 → 46 px |
| `{typography.display-sm}` | Section titles inside pages | 26 → 34 px |
| `{typography.title-md}` | Card titles | 20 px |
| `{typography.lead}` | Hero lead paragraph, max 620 px wide, centred | 17 px / 1.7 |
| `{typography.body-article}` | Blog post body, max `{spacing.measure}` | 17 → 18 px / 1.8 |
| `{typography.body-md}` | Interface text | 16 px / 1.5 |
| `{typography.eyebrow}` | Uppercase label in a pill above a headline | 12.5 px, +0.08em |
| `{typography.caption-uppercase}` | Tiny section labels ("İÇİNDEKİLER", "DİĞER METİN") | 11.5 px, +0.1em |

### Principles

- Headlines tighten as they grow (−0.01em at section size, −0.032em at hero size) and use `text-wrap: balance`.
- One serif italic emphasis per headline at most.
- Sizes are fluid with `clamp()`; there are almost no typographic breakpoints.
- Body text never goes below 16 px; small text never below 11.5 px and never for essential information.
- Watch Turkish: uppercase `i` must render as `İ`, so uppercase labels get `lang="tr"` from the page.

## Layout

### Spacing System

A 4 px base (`{spacing.xxs}` … `{spacing.xxl}`), but most large spacing is fluid: sections are
`{spacing.section}` apart (64 → 104 px), heroes use `clamp()` padding, cards `clamp(20px, 3vw, 32px)`.

### Grid & Container

- One container: `{spacing.container}`, i.e. at most 1180 px with a 16 px gutter on each side.
- Card grids need no breakpoints: `repeat(auto-fit, minmax(min(100%, 290px), 1fr))`.
- Reading pages (blog post, legal) use a two-column grid on wide screens: a sticky table of contents (260 px on
  legal pages) beside the text column (blog text limited to `{spacing.measure}`). On narrower screens the contents
  box moves above the text and collapses.
- Nothing may cause sideways scrolling. Wide tables scroll inside their own box.

### Whitespace Philosophy

Generous and calm. A section does one thing: an eyebrow, a headline, one lead sentence, then the content.
Heroes are centred; content sections are left-aligned.

## Elevation & Depth

| Level | Token | Use |
|---|---|---|
| Rest | `{elevation.rest}` | Cards, eyebrows, the next-document card |
| Float | `{elevation.float}` | Search pill, dropdowns, toasts, the condensed header |
| Lifted | `{elevation.lifted}` | Hovered cards and buttons. Coral-tinted, so lifting feels warm, not grey |

### Decorative Depth

- **Drifting blobs:** two large blurred circles (coral and pink, `blur(60px)`) behind every hero, drifting over
  16 s. Decoration only; they stop for *reduce motion*.
- **Glass:** the sticky header (`saturate(1.6) blur(14px)`) and the frosted cards on the dark band
  (`blur(18px)`).
- **Three.js:** the home page slider uses a shader "liquid" transition, and the clinical band has a slow 3D scene
  behind its cards. Both load last, only when visible, and the page is complete without them.

## Shapes

### Border Radius Scale

| Token | Value | Use |
|---|---|---|
| `{rounded.xs}` | 6 px | Inline code, small tags |
| `{rounded.sm}` | 10 px | Tables, info cards, TOC items |
| `{rounded.input}` | 14 px | Text inputs |
| `{rounded.md}` | 16 px | Table of contents, medium panels |
| `{rounded.lg}` | 24 px | Cards, summary boxes, band cards |
| `{rounded.band}` | 24 → 40 px | Dark bands |
| `{rounded.sheet}` | 36 px | Top corners of the footer sheet |
| `{rounded.pill}` | 999 px | Buttons, chips, eyebrows, search, toast, back-to-top |
| `{rounded.arch}` | arch | Suna's portrait in the home hero |

Nothing is square. Interactive things are pills; containers are rounded rectangles.

### Photography & Illustrations

- **Real photos of Suna**, warm and natural, never stock photography of strangers or "teams".
- The home portrait is an **arch** framed by a slowly turning dashed coral ring and a dot grid.
- Icons are Font Awesome (solid + regular), always decorative (`aria-hidden`) next to a word, never alone without
  a label.
- Speech bubbles with German / Turkish words float in the hero and on the sign-in page (pointer parallax).

## Components

### Top Navigation

`{components.top-nav}`: sticky, translucent cream glass, hairline bottom border. The wordmark on the left
(`{components.wordmark}` with the logo icon), links with small coloured icons in the middle (About, Blog, Clinical
German ▾, Contact), account button and a TR | DE segmented switch on the right. After 8 px of scrolling it
condenses and gains `{elevation.float}`. On phones the links move into an off-canvas sheet.

### Buttons

- `{components.button-primary}`: ink pill with cream text; turns coral-deep, rises 2 px and gains
  `{elevation.lifted}` (`{components.button-primary-active}`). An arrow icon inside nudges 4 px in its direction
  with the spring easing.
- `{components.button-ghost}`: transparent with a hairline border; gains a white surface and `{elevation.float}`.
- `{components.button-on-band}`: light cream pill with ink text, used on dark bands only; turns coral-deep like
  the primary button.
- One primary button per view. Labels are verbs: "Sınava başla", "Wort suchen".

### Eyebrow

`{components.eyebrow}`: a small white pill with a coral icon and an uppercase coral-deep label, sitting above
every hero headline ("GİZLİLİK", "BLOG", "KLİNİK ALMANCA"). On dark bands it becomes translucent white.

### Cards & Containers

- `{components.card}`: the general white card on cream.
- `{components.post-card}`: blog card with a 16:10 image that zooms slightly on hover, a serif title with a
  highlighter stripe that grows under it, an excerpt clamped to 3 lines, and a "read more" arrow. A soft coral
  glow follows the pointer.
- `{components.feature-card}`: home "how it works" cards; a coral bar grows along the top edge on hover and the
  icon tilts with the spring easing.
- `{components.dark-band}` + `{components.band-card}`: the dark islands with frosted cards.
- `{components.summary-box}`: blush box with "Kısaca / Kurz gesagt" bullets at the top of long texts; the first
  bullet is bold.
- `{components.info-card}`: white card with a 4 px coral left border, for contact details and notes.

### Reading aids (blog posts and legal pages)

- **Reading progress:** a 3 px coral bar under the header, scaled by scroll position.
- `{components.toc}`: numbered table of contents, sticky beside the text; the section being read gets
  `{components.toc-item-active}` (blush fill, bold, coral bar on the left).
- Numbered section headings with a coral number in a blush circle, and a "copy link to this section" button that
  appears on hover.
- `{components.table}`: hairline-bordered table that scrolls sideways inside its box on phones.
- A meta row under the hero: author, date, reading time ("≈ 6 dk okuma"), and on legal pages a *Print / PDF*
  button. The print stylesheet removes all chrome and prints black on white.

### Inputs & Forms

- `{components.text-input}`: a soft cream field with a muted icon on the left, a visible label and a realistic
  placeholder ("ornek@eposta.com" / "beispiel@email.de"). On focus (`{components.text-input-focused}`) it turns
  white, the border and icon turn coral and a soft coral ring appears.
- `{components.search-pill}`: the dictionary and blog search, a white pill with `{elevation.float}` and a round
  ink button inside. The focus ring is on the pill (`:focus-within`), not on the input.
- Errors appear under the field in plain words (what went wrong and what to do), and the field border turns red.
- Password rules are a checklist that ticks itself off while typing.

### Tags / Filters

`{components.chip}` and `{components.chip-active}` (ink fill) for categories, tags and filters. They wrap onto
new lines (centred under the hero on the blog), 8 px apart.

### Feedback

- `{components.toast}`: ink pill at the bottom centre, a check icon and one short sentence, gone after ~2 s.
- `{components.score-ring}`: SVG ring for exam scores, coral-deep on a hairline track, animated when it scrolls
  into view; confetti only at ≥ 80 % and never with *reduce motion*.
- `{components.back-to-top}`: 48 px ink circle bottom right, appears after 700 px of scrolling.

### Footer

`{components.footer-sheet}`: a pink gradient sheet with rounded top corners: the wordmark, link columns (pages,
clinical German, legal: *Gizlilik* / *Datenschutz*, *Kullanım Koşulları* / *Nutzungsbedingungen*), contact details,
a contact button and a WhatsApp button, and the light/dark theme toggle.

## Do's and Don'ts

### Do

- Use tokens (`var(--bp-ink)`), never raw hex values, so dark mode keeps working.
- Write every string twice: Turkish and German, and let the Turkish one decide the layout.
- Keep one primary action per screen and say what happens next.
- Use real photos of Suna and real examples from her lessons.
- Prefer a bigger serif headline over bolder text when something needs emphasis.
- Respect *reduce motion*: everything that moves must have a still version.
- Keep focus visible: `3px solid {colors.coral}`, offset 3 px.

### Don't

- Don't present the site as a company: no "our team", "firmamız", "Kundenservice", invented reviews, student
  counters, awards, prices or checkout flows.
- Don't introduce new hues (no blue or green sections, no gradients outside coral/pink/cream).
- Don't use pure black or pure grey; ink and dark surfaces are warm.
- Don't put coral `#FF9494` text on cream or white (contrast too low); use coral-deep or link.
- Don't animate layout properties or flash things for attention. Motion is slow and soft.
- Don't load fonts, icons or scripts from new third-party domains; every new service must be added to the
  privacy page.
- Don't use colour alone to say right/wrong; add an icon and a word.

## Responsive Behavior

### Breakpoints

| Width | What changes |
|---|---|
| < 576 px | Single column almost everywhere; hero type at its `clamp()` minimum |
| < 768 px | The 3D clinical scene moves into its own strip above the band heading |
| < 900 px | Legal pages: the table of contents moves above the text and collapses |
| < 992 px | Navigation moves into the off-canvas menu |
| ≥ 1180 px | Container stops growing |

### Touch Targets

Buttons are at least 44 px tall (12 px vertical padding); icon-only buttons are 40–48 px circles with an
`aria-label`. Chips are smaller (≈ 32 px) and therefore always spaced 8 px apart.

### Collapsing Strategy

Grids collapse by themselves (`auto-fit` + `minmax(min(100%, …), 1fr)`). Side-by-side becomes stacked; sticky
side panels become collapsible boxes above the content; wide tables scroll inside their own box.

### Image Behavior

`<picture>` with WebP + JPEG at 480/800/1200 px and `sizes`; `loading="lazy"` and `decoding="async"` below the
fold; width/height or `aspect-ratio` always set, so nothing jumps.

## Iteration Guide

1. Start a new page with `class="site-page your-page"`, load `blog.css` and then your own stylesheet (recipe in
   [docs/DESIGN.md → Recipes](docs/DESIGN.md#18-recipes)).
2. Open with the hero pattern: `{components.eyebrow}` → `{typography.display-lg}` headline with at most one coral
   italic word → `{typography.lead}`.
3. Build with existing components (`{components.card}`, `{components.chip}`, `{components.button-primary}`) before
   inventing new ones. A new component gets its own entry in `components:` above.
4. Use `{token.refs}`, never inline hex. Variants (`-active`, `-focused`) are separate entries.
5. Check both themes, both languages, a 360 px phone, keyboard-only use and *reduce motion*.
6. Read the copy aloud: does it sound like Suna, or like a company? If it sounds like a company, rewrite it.
7. Cream + coral + warm ink is the palette. Don't add a fourth tone.

## Known Gaps

- The **admin panel** has its own, calmer token set (`--a-*`, stronger status colours, a sidebar layout) tuned
  for work rather than reading. It is described in [docs/DESIGN.md §15](docs/DESIGN.md#15-the-admin-panel-design-system),
  not here. It still loads its fonts from Google Fonts.
- The **exam room** (`_ExamLayout`) is a deliberately distraction-free full-screen surface with its own timer,
  watermark and integrity warnings; only its colours and type come from this system.
- Some older pages still carry global layers (`site.css`, `custom.css`, `universe.css`) from before the redesign.
  New work should not depend on them.
- Motion details (entrance stagger, scroll reveal, blob drift, shader parameters) are documented with code in
  [docs/DESIGN.md §5–9](docs/DESIGN.md#5-motion-system), not as tokens.
- The logo (two speech bubbles in German colours) is an image asset, not a token. The wordmark is drawn at weight
  700 from the single Varela Round 400 file, so the browser thickens it.
