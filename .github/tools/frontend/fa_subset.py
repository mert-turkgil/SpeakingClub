"""Builds a trimmed Font Awesome (only the icons the site uses) next to the full copy.

  wwwroot/lib/fontawesome/css/fa-site.min.css      (icon rules for used icons + all base rules)
  wwwroot/lib/fontawesome/webfonts/*-site.woff2    (fonts subset to the used code points)

Icons are found by scanning the source for "fa-<name>" tokens and CSS `content: "\fxxx"` code points.
Run it again after adding an icon:

    pip install fonttools brotli
    python .github/tools/frontend/fa_subset.py
"""
import os
import re
import sys

from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
FA = os.path.join(ROOT, 'wwwroot', 'lib', 'fontawesome')
SKIP_DIRS = {'bin', 'obj', 'node_modules', '.git', '.github', 'lib', 'docs', 'logs'}
EXTS = {'.cshtml', '.cs', '.js', '.resx', '.html', '.json'}

# 1. Every fa-* token in the source, and every icon code point written directly in CSS
#    (content: "\f111" with font-family "Font Awesome 6 Free", e.g. the password rules on the sign-up page)
tokens = set()
css_points = set()
for dirpath, dirnames, filenames in os.walk(ROOT):
    dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
    for name in filenames:
        ext = os.path.splitext(name)[1].lower()
        if ext not in EXTS and ext != '.css':
            continue
        try:
            text = open(os.path.join(dirpath, name), encoding='utf-8', errors='ignore').read()
        except OSError:
            continue
        tokens.update(re.findall(r'fa-[a-z0-9]+(?:-[a-z0-9]+)*', text))
        for hexcode in re.findall(r'content:\s*["\']\\([0-9a-fA-F]{4,5})', text):
            point = int(hexcode, 16)
            if 0xE000 <= point <= 0xF8FF:   # Font Awesome lives in the Private Use Area
                css_points.add(point)

css = open(os.path.join(FA, 'css', 'all.min.css'), encoding='utf-8').read()
header_end = css.index('*/') + 2
header, body = css[:header_end], css[header_end:]

# 2. Split the stylesheet into top-level rules (handles @font-face / @keyframes / @media blocks)
rules, depth, start = [], 0, 0
for i, ch in enumerate(body):
    if ch == '{':
        depth += 1
    elif ch == '}':
        depth -= 1
        if depth == 0:
            rules.append(body[start:i + 1])
            start = i + 1
icon_rule = re.compile(r'^((?:\.fa-[a-z0-9-]+,?)+)\{--fa:"((?:\\[0-9a-f]+ ?|[^"\\])+)"\}$')


def codepoints(value):
    out, i = [], 0
    while i < len(value):
        if value[i] == '\\':
            m = re.match(r'\\([0-9a-f]+) ?', value[i:])
            out.append(int(m.group(1), 16))
            i += len(m.group(0))
        else:
            out.append(ord(value[i]))
            i += 1
    return out


kept, used_points, used_icons, all_icons = [], set(), set(), set()
for rule in rules:
    m = icon_rule.match(rule.strip())
    if not m:
        kept.append(rule)  # base rules, utilities, animations, @font-face
        continue
    names = {s[1:] for s in m.group(1).split(',') if s}
    all_icons |= names
    if names & tokens:
        kept.append(rule)
        used_icons |= names & tokens
        used_points.update(codepoints(m.group(2)))

used_points |= css_points
print('code points from CSS content:', ' '.join(hex(p) for p in sorted(css_points)))

# 3. Subset the fonts to the used code points
for font in ('fa-solid-900', 'fa-regular-400', 'fa-brands-400'):
    src = os.path.join(FA, 'webfonts', font + '.woff2')
    dst = os.path.join(FA, 'webfonts', font + '-site.woff2')
    f = TTFont(src)
    options = subset.Options()
    options.flavor = 'woff2'
    options.layout_features = ['*']
    options.notdef_outline = True
    s = subset.Subsetter(options)
    s.populate(unicodes=sorted(used_points))
    s.subset(f)
    f.flavor = 'woff2'
    f.save(dst)
    print(f'{font}: {os.path.getsize(src) // 1024} KB -> {os.path.getsize(dst) // 1024} KB')

# 4. The stylesheet. Font URLs carry a content hash (?v=…): Program.cs lets browsers keep ?v= files for a
#    year, and a rebuilt font gets a new URL, so nobody keeps an old copy with missing icons.
import hashlib
out_css = ''.join(kept)
for font in ('fa-solid-900', 'fa-regular-400', 'fa-brands-400'):
    digest = hashlib.sha256(open(os.path.join(FA, 'webfonts', font + '-site.woff2'), 'rb').read()).hexdigest()[:12]
    out_css = re.sub(r'url\(\.\./webfonts/' + font + r'\.woff2\) format\("woff2"\),url\(\.\./webfonts/' + font + r'\.ttf\) format\("truetype"\)',
                     f'url(../webfonts/{font}-site.woff2?v={digest}) format("woff2")', out_css)
note = ('\n/* Trimmed by fa_subset (see docs/DESIGN.md): only the icons used in this repository, '
        f'{len(used_icons)} of {len(all_icons)}. Add an icon → run the script again. */\n')
open(os.path.join(FA, 'css', 'fa-site.min.css'), 'w', encoding='utf-8', newline='\n').write(header + note + out_css)

missing = sorted(t for t in tokens if t not in all_icons)
print(f'icons kept: {len(used_icons)} of {len(all_icons)}; css {len(css) // 1024} KB -> {len(header + out_css) // 1024} KB')
print('fa- tokens that are not icons (utilities, styles or typos):', ' '.join(missing))
