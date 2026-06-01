"""Tiny RGBA PNG encoder used for debug output."""

from __future__ import annotations

import struct
import zlib
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class RgbaImage:
    width: int
    height: int
    pixels: bytes

    def __post_init__(self) -> None:
        if self.width <= 0 or self.height <= 0:
            raise ValueError("image dimensions must be positive")
        expected = self.width * self.height * 4
        if len(self.pixels) != expected:
            raise ValueError(f"expected {expected} RGBA bytes, got {len(self.pixels)}")


def _chunk(kind: bytes, payload: bytes) -> bytes:
    return (
        struct.pack(">I", len(payload))
        + kind
        + payload
        + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)
    )


def encode_png_rgba(image: RgbaImage) -> bytes:
    """Encode an RGBA image as a PNG with filter type 0 rows."""

    header = struct.pack(">IIBBBBB", image.width, image.height, 8, 6, 0, 0, 0)
    stride = image.width * 4
    raw_rows = bytearray()
    for y in range(image.height):
        raw_rows.append(0)
        start = y * stride
        raw_rows.extend(image.pixels[start : start + stride])

    return (
        b"\x89PNG\r\n\x1a\n"
        + _chunk(b"IHDR", header)
        + _chunk(b"IDAT", zlib.compress(bytes(raw_rows), 9))
        + _chunk(b"IEND", b"")
    )


def write_png(path: Path, image: RgbaImage) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(encode_png_rgba(image))


def scale_half_nearest(image: RgbaImage) -> RgbaImage:
    """Scale to approximately 50% using nearest-neighbour sampling."""

    new_width = max(1, (image.width + 1) // 2)
    new_height = max(1, (image.height + 1) // 2)
    out = bytearray(new_width * new_height * 4)

    for y in range(new_height):
        sy = min(image.height - 1, int((y + 0.5) * image.height / new_height))
        for x in range(new_width):
            sx = min(image.width - 1, int((x + 0.5) * image.width / new_width))
            src = (sy * image.width + sx) * 4
            dst = (y * new_width + x) * 4
            out[dst : dst + 4] = image.pixels[src : src + 4]

    return RgbaImage(new_width, new_height, bytes(out))


def round_half_up(value: float) -> int:
    return int(value + 0.5)
