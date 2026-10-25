"""Pinned original oracle dependencies for original-300-root P01 evidence.

This reader never changes frozen root expectations or selects refreshed artifacts.
Current-revision R03 verification continues to use the normal working-tree oracle.
"""
import hashlib
import importlib.util
import io
from pathlib import Path, PurePosixPath
import tempfile
import zipfile

_ROOT = Path(__file__).resolve().parent
_ARCHIVES = {
    'combat-evidence-legacy-v1.zip': '3d7614b580ad5c82727170e4ff193e03f4931afbc7e7ed7b14d5eb49c45a6a98',
    'combat-evidence-legacy-oracle-support-v1.zip': '848d084745f0582f5f4ca39013f202774213df947b711069aa2476dbe0c3cf8c',
}
_directory = None


def legacy_root():
    global _directory
    if _directory is not None:
        return Path(_directory.name)
    directory = tempfile.TemporaryDirectory(prefix='sandtable-original-evidence-')
    seen = set()
    try:
        for name, expected in _ARCHIVES.items():
            path = _ROOT / 'fixtures' / name
            data = path.read_bytes()
            if hashlib.sha256(data).hexdigest() != expected:
                raise ValueError('Original evidence archive hash mismatch: ' + name)
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                for entry in archive.infolist():
                    relative = PurePosixPath(entry.filename)
                    if relative.is_absolute() or '..' in relative.parts or '\\' in entry.filename or entry.filename in seen:
                        raise ValueError('Invalid original evidence archive path')
                    seen.add(entry.filename)
                    target = Path(directory.name).joinpath(*relative.parts)
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(archive.read(entry))
        _directory = directory
        return Path(directory.name)
    except BaseException:
        directory.cleanup()
        raise


def load_r03(name):
    path = legacy_root() / 'docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py'
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
