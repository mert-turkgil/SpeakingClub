# Security policy

Almanca Konuş runs a live website with student accounts and exam results, so security reports are taken
seriously and handled before anything else.

## Reporting a vulnerability

**Please don't open a public issue.** Report it privately instead:

1. Go to **[Security → Report a vulnerability](https://github.com/mert-turkgil/SpeakingClub/security/advisories/new)**.
2. Describe the problem, the steps to reproduce it and what an attacker could do with it.
3. If you have a proof of concept, attach it. Please don't access, change or delete other people's data, and
   don't run automated scanners or load tests against the live site (www.almanca-konus.com).

You can expect a first answer within **7 days**. Once the problem is fixed and deployed, the advisory is
published and you're credited, unless you'd rather stay anonymous.

## Supported versions

Only the current `main` branch is supported. It's what runs on www.almanca-konus.com.

## Scope

In scope: the code in this repository and the live site it deploys (sign-in, accounts, the exam room, the
admin panel, the dictionary, file uploads and downloads).

Out of scope: third-party services the site uses (Cloudflare, Google, DeepL, PONS, CKEditor's CDN, the
hosting provider), denial-of-service attacks, social engineering, and reports from automated tools without a
demonstrated impact.

## For people who deploy their own copy

- Keep secrets (connection strings, API keys, SMTP passwords) out of the repository. Use
  `appsettings.Development.json` (git-ignored) or `dotnet user-secrets` locally and GitHub **environment
  secrets** for deployment.
- Use your own keys for DeepL, PONS, Turnstile, Google sign-in and CKEditor.
- Change the passwords of the users created from `Data:Users` after the first start.
