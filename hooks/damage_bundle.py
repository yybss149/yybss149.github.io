"""Publish the damage page and its runtime atomically, avoiding mixed cache versions."""
from pathlib import Path
import re


def on_post_build(config, **kwargs):
    page = Path(config['site_dir']) / 'damage' / 'index.html'
    html = page.read_text(encoding='utf-8')

    def script(match):
        name = match.group(1)
        source = (page.parent / name).read_text(encoding='utf-8')
        if re.search(r'</script', source, re.I):
            raise ValueError(f'Unsafe inline script closing tag in {name}')
        return f'<script data-source="{name}">\n{source}\n</script>'

    def style(match):
        name = match.group(1)
        source = (page.parent / name).read_text(encoding='utf-8')
        return f'<style data-source="{name}">\n{source}\n</style>'

    html, scripts = re.subn(r'<script src="([\w-]+\.js)"></script>', script, html)
    html, styles = re.subn(r'<link rel="stylesheet" href="([\w-]+\.css)">', style, html)
    if scripts != 7 or styles != 3:
        raise ValueError(f'Incomplete damage bundle: {scripts} scripts, {styles} styles')
    html = html.replace('<head>', '<head>\n<meta name="damage-release" content="20260910-switch-fix">')
    page.write_text(html, encoding='utf-8')
