# Contributing to Almanca Konuş

Thank you for wanting to help! 💛 Almanca Konuş started as a website for one German teacher and her students.
Every improvement you make also reaches the people who [use the project](README.md#-using-this-project) for
their own lessons.

This guide covers what you can work on, how to set up the project, the conventions the code follows, and what a
pull request needs before it's merged.

**Contents**

- [Ways to help](#ways-to-help)
- [Before you start](#before-you-start)
- [Set up the project](#set-up-the-project)
- [How the code is organised](#how-the-code-is-organised)
- [Conventions](#conventions)
- [Database changes](#database-changes)
- [Checking your change](#checking-your-change)
- [Pull requests](#pull-requests)
- [Reporting security problems](#reporting-security-problems)
- [Code of conduct](#code-of-conduct)
- [License of contributions](#license-of-contributions)

---

## Ways to help

| You are… | You could… |
|---|---|
| **A teacher or student** | Report anything confusing, [suggest a feature](https://github.com/mert-turkgil/SpeakingClub/issues/new?template=feature-request.yml), tell us which admin step was unclear |
| **A Turkish or German speaker** | Improve translations: site texts live in `Resources/*.resx`, page texts in `T("…", "…")` calls in the views |
| **A designer** | Polish layouts, contrast, dark mode, phone views; propose better guide texts for the admin panel |
| **A developer** | Fix [bugs](https://github.com/mert-turkgil/SpeakingClub/issues?q=is%3Aissue+is%3Aopen+label%3Abug), add features, write tests, improve performance and accessibility |
| **Using the project yourself** | [Register your use](https://github.com/mert-turkgil/SpeakingClub/issues/new?template=usage-registration.yml) and share what you changed: good ideas can come back here |

## Before you start

1. **Search the issues** to see whether someone already reported or is working on it.
2. For anything bigger than a small fix, **open an issue first** and describe what you plan to do. That way we can
   agree on the approach before you put time into it.
3. Keep one pull request to one topic. Small pull requests are reviewed faster.

## Set up the project

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and SQL Server (LocalDB, Express or
Developer). The [README](README.md#-getting-started) has the full setup. The short version:

```bash
git clone https://github.com/<you>/SpeakingClub.git
cd SpeakingClub
# create appsettings.Development.json (see the README), then:
dotnet run --launch-profile https
```

In Development the app applies the migrations and creates the users from `Data:Users` on its own.

- **Never commit secrets.** `appsettings.Development.json` and `appsettings.Production.json` are git-ignored. You
  can also keep keys in `dotnet user-secrets`.
- DeepL and PONS keys are optional. Without them, the dictionary only answers from the local word list.
- There is no front-end build step: CSS and JavaScript in `wwwroot/` are served as written.

## How the code is organised

```text
Controllers/   HomeController (public site), AccountController (sign-in, exams, profile),
               AdminController (admin panel), QuizMonitorController (live results), SitemapController
Services/      Business logic that is shared or testable: ExamRules / ExamService (timing, grading),
               DictionaryLookupService (our words → DeepL → PONS), LanguageService (site texts)
Data/          EF Core contexts, repositories (Abstract/ + Concrete/) and UnitOfWork
Models/        View models. Models/AdminNav.cs lists every admin section and its step-by-step guide
Views/         Razor views; Views/Shared/_Layout.cshtml (site) and _AdminLayout.cshtml (admin)
wwwroot/css/   One stylesheet per area: blog.css holds the shared design tokens
wwwroot/js/    One script per area, no framework
DESIGN.md      The design system: tokens, components and voice (start here for anything visual)
docs/          DESIGN.md explains the techniques behind it, with code
```

Request flow: controller → service (if there is real logic) → `IUnitOfWork` repositories → EF Core. Repository
methods such as `AddAsync`, `Update` and `Remove` don't save; call `_unitOfWork.SaveAsync()` once at the end.

## Conventions

### Languages: always Turkish and German

The site has exactly two languages. Every visible text needs both.

- **Inside views**, use the local helper that most views define:
  ```cshtml
  var isGerman = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "de";
  string T(string tr, string de) => isGerman ? de : tr;
  …
  <h2>@T("Sözlüğümüzden kelimeler", "Wörter aus unserem Wörterbuch")</h2>
  ```
- **Texts the teacher should be able to change** (titles, slogans, slide texts) belong in
  `Resources/SharedResource.tr-TR.resx` and `.de-DE.resx`, read with `LanguageService.GetKey("Key")`. They then
  show up in Admin → Site texts.
- In **JavaScript**, pass strings from Razor (a JSON `<script type="application/json">` block or `data-*`
  attributes) or use the `t(tr, de)` helper the admin scripts define.
- URLs come in pairs (`/sozluk` ⇄ `/woerterbuch`). Add both routes and the `hreflang` pair for new pages.

### Writing for non-technical admins

The admin panel is used by someone with no computer experience. When you touch it:

- Use plain words: "Save", "Delete this word?", not "Persist entity".
- Update the page's guide in `Models/AdminNav.cs` so the numbered steps still match the screen.
- Destructive actions use `data-confirm` (see `wwwroot/js/admin.js`) and explain what will happen.
- Messages say what happened and what to do next, in both languages.

### Styling

- **Use the design tokens.** Public pages use `--bp-*` (defined in `blog.css`), the admin panel uses `--a-*`
  (in `admin.css`). Don't hard-code colours that already exist as tokens.
- **Icons:** the site ships only the Font Awesome icons it uses. After adding an icon you haven't used before,
  rebuild the subset: `pip install fonttools brotli`, then `python .github/tools/frontend/fa_subset.py`, and commit
  the changed files in `wwwroot/lib/fontawesome`. Otherwise the new icon shows as an empty box.
- **Fonts** come from `wwwroot/fonts` (see `Views/Shared/_Fonts.cshtml`). Don't add Google Fonts links to pages.
- **Scope page styles** under the page's root class (`.site-page.dict`, `.quiz-page`, `.home-page` …) in a file
  for that area, so nothing leaks into other pages.
- **Dark mode is required.** Most components get it for free from the tokens. When it doesn't, add a rule under
  `html[data-bs-theme="dark"]`.
- **Motion must respect** `prefers-reduced-motion`. The shared rules in `blog.css` handle CSS. In JS, check the
  media query before you start loops.
- Layouts must work from **320 px** up with no horizontal scrolling, **also with the phone set to large text**
  (in desktop Chrome: Settings → Appearance → Font size → Large, then a 320–360 px wide window or device mode).
- **Grid columns:** write `minmax(0, 1fr)` instead of `1fr`, and `minmax(min(100%, 280px), 1fr)` in `auto-fill` /
  `auto-fit`; give stacked grids (`display: grid; gap: …`) `grid-template-columns: minmax(0, 1fr)`. A plain `1fr`
  column grows to its widest content (an `<input>`, the 300 px Turnstile box, an e-mail address) and pushes the
  card past a phone screen; a parent with `overflow: hidden` then silently cuts the right side off.
- Flex items that hold text or inputs need `min-width: 0`; long words and addresses wrap (`overflow-wrap`).

- **Write like the site.** Almanca Konuş is Suna Koç's personal site, not a company: don't add "our team",
  "firmamız", "unser Unternehmen", invented numbers, reviews or prices. About texts are in her first person.

[DESIGN.md](DESIGN.md) is the design system (tokens, components, voice); [docs/DESIGN.md](docs/DESIGN.md)
explains how it is built.

### JavaScript

- Plain JavaScript, no framework, no build. Wrap each file in an IIFE (or use an ES module for Three.js code) and
  return early when the page doesn't have the elements the file is for.
- **Progressive enhancement:** the page must be usable before the script runs. Scripts add behaviour.
- Hook behaviour to `data-*` attributes, not to styling classes.
- Wrap `localStorage` access in `try/catch`, because private mode can throw.
- **No jQuery in new code.** The public layout loads jQuery only for pages that use jQuery Validation: such a view
  sets `ViewData["jQuery"] = true;` (About, Login, Register, ChangePassword do). Bootstrap's script is deferred, so
  inline scripts must not call `bootstrap.*` while the page is still loading (inside event handlers is fine).
- Don't read layout (`scrollY`, `offsetWidth`, `getBoundingClientRect`) while the page loads; use an
  `IntersectionObserver` or wait for the element to come into view. PageSpeed reports it as a forced reflow.
- Read-only search forms get WebMCP attributes (`toolname`, `tooldescription`, `toolautosubmit`,
  `toolparamdescription` on the field) so AI agents in the browser can use them. Never add them to forms that sign
  in, send messages or change data.

### C#

- Follow the style of the file you're in: four-space indentation, `var` where the type is obvious, `async` all the
  way down, nullable annotations.
- Comments explain **why**, not what.
- Validate input on the server even when the form validates it in the browser. Every POST action needs
  `[ValidateAntiForgeryToken]` (there is no global filter).
- **Output encoding:** Razor encodes by default. Use `Html.Raw` only for HTML written by admins in the editor,
  never for user input or API responses.

### Commits

Write commit messages in the imperative, describing the result: *"Show the dictionary source on the result card"*,
not *"changes"*. A short body explaining *why* is very welcome.

## Database changes

There are two EF Core contexts:

| Context | Contains | Migrations folder |
|---|---|---|
| `SpeakingClubContext` | Content: blogs, quizzes, words, slides… | `Data/Migrations/SpeakingClubMigrations` |
| `ApplicationDbContext` | Identity: users, roles | `Data/Migrations/IdentityMigrations` |

```bash
dotnet tool install --global dotnet-ef     # once
dotnet ef migrations add AddWordLevel \
  --context SpeakingClubContext \
  --output-dir Data/Migrations/SpeakingClubMigrations \
  --configuration Release
```

- Name migrations after what they do (`AddWordLevel`, not `Update5`).
- Migrations must be safe on a database that already has data: give new required columns a default.
- The deploy workflow backs up production before applying migrations, but that's a safety net, not a plan.

## Checking your change

There is no automated test suite yet (tests are a great first contribution!). Before opening a pull request,
please check by hand:

- [ ] `dotnet build -c Release` → **0 warnings, 0 errors**
- [ ] The page works in **Turkish and German**
- [ ] It looks right in the **light and dark** themes
- [ ] It works at **390 px** wide (DevTools device mode) without sideways scrolling
- [ ] It can be used with the **keyboard** (Tab, Shift+Tab, Enter, Esc) and focus is visible
- [ ] Nothing new in the browser console
- [ ] For admin pages: a **Teacher** account sees only what it should (users, roles, site texts and download logs
  are admin-only)

## Pull requests

1. Fork the repository and create a branch from `main`: `fix/dictionary-umlaut-search`,
   `feature/word-levels`, `docs/contributing`.
2. Make your change, following the conventions above.
3. Fill in the [pull request template](.github/PULL_REQUEST_TEMPLATE.md), including **screenshots** for anything
   visible (light and dark, phone if relevant).
4. CI builds every pull request ([`.github/workflows/dotnet.yml`](.github/workflows/dotnet.yml)), so it must pass.
5. A maintainer reviews it. Expect questions; they're about the code, not about you. Once approved, the change is
   squash-merged and deployed with the next push to `main`.

## Reporting security problems

Please **don't open a public issue** for security problems. Use GitHub's
[private vulnerability reporting](https://github.com/mert-turkgil/SpeakingClub/security/advisories/new) and
include steps to reproduce. You'll get an answer as soon as possible, and credit for the report if you'd like it.

## Code of conduct

Be kind and patient. Many people here are teachers and learners, not developers, and every question is a fair
question. Harassment, insults and discrimination of any kind are not tolerated. Maintainers may remove comments
or block people who don't respect this.

## License of contributions

By submitting a contribution you confirm that you wrote it (or have the right to submit it), and you agree that it
is licensed under this project's [LICENSE](LICENSE). You also allow the maintainer to publish it under later
versions of that license.

Thank you for helping students speak German with confidence! 🇩🇪🇹🇷
