#!/usr/bin/env python3
"""Verify the public revision and every exported WebGL asset against the build."""
import hashlib
import json
from pathlib import Path
import sys
from urllib.error import HTTPError
from urllib.parse import quote
from urllib.request import urlopen

url, directory, commit = sys.argv[1:]
root = Path(directory)
with urlopen(url.rstrip('/') + '/health', timeout=30) as response:
    assert response.read() == b'ok\n', 'Health check failed'

for path in sorted(root.rglob('*')):
    if not path.is_file():
        continue
    relative = path.relative_to(root).as_posix()
    with urlopen(url.rstrip('/') + '/' + quote(relative), timeout=90) as response:
        content = response.read()
        assert hashlib.sha256(content).digest() == hashlib.sha256(path.read_bytes()).digest(), relative
        assert response.headers.get('Cache-Control') == 'no-cache', relative
        if relative.endswith('.unityweb'):
            assert not response.headers.get('Content-Encoding'), relative
        if relative == 'build-info.json':
            assert json.loads(content)['commit'] == commit, 'Wrong deployed commit'
        print(f'PASS {relative}: {len(content)} bytes', flush=True)

assert (root / 'build-info.json').is_file(), 'Missing source commit metadata'
try:
    urlopen(url.rstrip('/') + '/Build/missing.wasm', timeout=30)
    raise AssertionError('A missing asset returned success')
except HTTPError as error:
    assert error.code == 404
print(f'Verified deployed commit {commit}')
