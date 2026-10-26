"""Usage: python .github/tools/frontend/fetch_fonts.py  (then update Views/Shared/_Fonts.cshtml if you add a font)

Downloads the site's Google Fonts, subset by Google itself (text=) to the characters Turkish and German
pages need, into wwwroot/fonts/. Same font files Google serves today, just one small file per style."""
import os
import re
import urllib.parse
import urllib.request

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', 'wwwroot', 'fonts'))
os.makedirs(OUT, exist_ok=True)
UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36'

chars = set(chr(c) for c in range(0x20, 0x7F))            # ASCII
chars |= set(chr(c) for c in range(0xA0, 0x100))           # Latin-1: ä ö ü ß é ç « » © ° · …
chars |= set('ğĞıİşŞ' 'œŒ' '–—‘’‚“”„…•‹›€™′″→←↗⇄✓')       # Turkish letters, typography
text = ''.join(sorted(chars))

fonts = [
    ('poppins-400', 'Poppins:wght@400'),
    ('poppins-600', 'Poppins:wght@600'),
    ('varela-round-400', 'Varela+Round'),
    ('fraunces-600', 'Fraunces:opsz,wght@9..144,600'),
    ('fraunces-600-italic', 'Fraunces:ital,opsz,wght@1,9..144,600'),
]
for name, family in fonts:
    url = 'https://fonts.googleapis.com/css2?family=' + family + '&display=swap&text=' + urllib.parse.quote(text)
    css = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': UA}), timeout=30).read().decode()
    srcs = re.findall(r"src: url\(([^)]+)\) format\('woff2'\)", css)
    assert len(srcs) == 1, (name, css[:300])
    data = urllib.request.urlopen(urllib.request.Request(srcs[0], headers={'User-Agent': UA}), timeout=30).read()
    open(os.path.join(OUT, name + '.woff2'), 'wb').write(data)
    print(f'{name}: {len(data) // 1024} KB')
