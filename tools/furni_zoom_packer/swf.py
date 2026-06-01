"""Small SWF reader/writer for Habbo furniture asset libraries."""

from __future__ import annotations

import struct
import zlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable

from .pngutil import RgbaImage


TAG_END = 0
TAG_SHOW_FRAME = 1
TAG_DEFINE_BITS_LOSSLESS2 = 36
TAG_SYMBOL_CLASS = 76
TAG_DO_ABC = 82
TAG_DEFINE_BINARY_DATA = 87

TAG_NAMES = {
    TAG_END: "End",
    TAG_SHOW_FRAME: "ShowFrame",
    9: "SetBackgroundColor",
    TAG_DEFINE_BITS_LOSSLESS2: "DefineBitsLossless2",
    41: "ProductInfo",
    43: "FrameLabel",
    65: "ScriptLimits",
    69: "FileAttributes",
    TAG_SYMBOL_CLASS: "SymbolClass",
    77: "Metadata",
    TAG_DO_ABC: "DoABC",
    TAG_DEFINE_BINARY_DATA: "DefineBinaryData",
}


@dataclass
class SwfTag:
    code: int
    payload: bytes

    @property
    def name(self) -> str:
        return TAG_NAMES.get(self.code, f"Tag{self.code}")


@dataclass
class BitmapTag:
    character_id: int
    width: int
    height: int
    image: RgbaImage


@dataclass
class BinaryDataTag:
    character_id: int
    reserved: int
    data: bytes
    tag_index: int


@dataclass
class DoAbcTag:
    flags: int
    name: str
    abc: bytes
    tag_index: int


@dataclass
class SwfFile:
    signature: bytes
    version: int
    frame_header: bytes
    tags: list[SwfTag]
    symbols: list[tuple[int, str]] = field(default_factory=list)

    @classmethod
    def read(cls, path: Path) -> "SwfFile":
        raw = path.read_bytes()
        if len(raw) < 8:
            raise ValueError("file is too small to be a SWF")

        signature = raw[:3]
        if signature not in (b"FWS", b"CWS"):
            raise ValueError(f"unsupported SWF signature {signature!r}; only FWS/CWS are supported")

        version = raw[3]
        if signature == b"CWS":
            body = zlib.decompress(raw[8:])
        else:
            body = raw[8:]

        frame_header_len = _frame_header_length(body)
        frame_header = body[:frame_header_len]
        pos = frame_header_len
        tags: list[SwfTag] = []

        while pos < len(body):
            if pos + 2 > len(body):
                raise ValueError("truncated SWF tag header")
            code_len = struct.unpack_from("<H", body, pos)[0]
            pos += 2
            code = code_len >> 6
            length = code_len & 0x3F
            if length == 0x3F:
                if pos + 4 > len(body):
                    raise ValueError("truncated long SWF tag length")
                length = struct.unpack_from("<I", body, pos)[0]
                pos += 4
            payload = body[pos : pos + length]
            if len(payload) != length:
                raise ValueError("truncated SWF tag payload")
            pos += length
            tags.append(SwfTag(code, payload))
            if code == TAG_END:
                break

        swf = cls(signature=signature, version=version, frame_header=frame_header, tags=tags)
        swf.symbols = swf.parse_symbols()
        return swf

    def write(self, path: Path) -> None:
        body = self.frame_header + b"".join(_encode_tag(tag) for tag in self.tags)
        file_length = len(body) + 8
        header = self.signature + bytes([self.version]) + struct.pack("<I", file_length)
        if self.signature == b"CWS":
            payload = zlib.compress(body, 9)
        else:
            payload = body
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(header + payload)

    def parse_symbols(self) -> list[tuple[int, str]]:
        symbols: list[tuple[int, str]] = []
        for tag in self.tags:
            if tag.code == TAG_SYMBOL_CLASS:
                symbols.extend(parse_symbol_class(tag.payload))
        return symbols

    def replace_symbol_class(self, symbols: list[tuple[int, str]]) -> None:
        for tag in self.tags:
            if tag.code == TAG_SYMBOL_CLASS:
                tag.payload = build_symbol_class(symbols)
                self.symbols = symbols
                return
        raise ValueError("SWF does not contain a SymbolClass tag")

    def bitmaps(self) -> dict[int, BitmapTag]:
        out: dict[int, BitmapTag] = {}
        for tag in self.tags:
            if tag.code == TAG_DEFINE_BITS_LOSSLESS2:
                bitmap = parse_define_bits_lossless2(tag.payload)
                out[bitmap.character_id] = bitmap
        return out

    def binary_data(self) -> list[BinaryDataTag]:
        out: list[BinaryDataTag] = []
        for index, tag in enumerate(self.tags):
            if tag.code != TAG_DEFINE_BINARY_DATA:
                continue
            if len(tag.payload) < 6:
                raise ValueError("truncated DefineBinaryData tag")
            character_id = struct.unpack_from("<H", tag.payload, 0)[0]
            reserved = struct.unpack_from("<I", tag.payload, 2)[0]
            out.append(BinaryDataTag(character_id, reserved, tag.payload[6:], index))
        return out

    def set_binary_data(self, binary: BinaryDataTag, data: bytes) -> None:
        self.tags[binary.tag_index].payload = (
            struct.pack("<HI", binary.character_id, binary.reserved) + data
        )

    def do_abc(self) -> DoAbcTag:
        for index, tag in enumerate(self.tags):
            if tag.code != TAG_DO_ABC:
                continue
            if len(tag.payload) < 5:
                raise ValueError("truncated DoABC tag")
            flags = struct.unpack_from("<I", tag.payload, 0)[0]
            end = tag.payload.index(0, 4)
            name = tag.payload[4:end].decode("utf-8", "replace")
            return DoAbcTag(flags, name, tag.payload[end + 1 :], index)
        raise ValueError("SWF does not contain a DoABC tag")

    def set_do_abc(self, do_abc: DoAbcTag, abc: bytes) -> None:
        self.tags[do_abc.tag_index].payload = (
            struct.pack("<I", do_abc.flags) + do_abc.name.encode("utf-8") + b"\x00" + abc
        )

    def insert_tags_before_doabc(self, new_tags: Iterable[SwfTag]) -> None:
        insert_at = self.do_abc().tag_index
        self.tags[insert_at:insert_at] = list(new_tags)
        self.symbols = self.parse_symbols()

    def max_character_id(self) -> int:
        ids = [cid for cid, _ in self.symbols]
        ids.extend(self.bitmaps().keys())
        ids.extend(binary.character_id for binary in self.binary_data())
        return max(ids, default=0)


def _frame_header_length(body: bytes) -> int:
    rect_nbits = body[0] >> 3
    rect_len = (5 + rect_nbits * 4 + 7) // 8
    return rect_len + 4


def _encode_tag(tag: SwfTag) -> bytes:
    length = len(tag.payload)
    if length < 0x3F:
        return struct.pack("<H", (tag.code << 6) | length) + tag.payload
    return struct.pack("<HI", (tag.code << 6) | 0x3F, length) + tag.payload


def _read_cstring(data: bytes, pos: int) -> tuple[str, int]:
    end = data.index(0, pos)
    return data[pos:end].decode("utf-8", "replace"), end + 1


def parse_symbol_class(payload: bytes) -> list[tuple[int, str]]:
    if len(payload) < 2:
        raise ValueError("truncated SymbolClass tag")
    count = struct.unpack_from("<H", payload, 0)[0]
    pos = 2
    symbols: list[tuple[int, str]] = []
    for _ in range(count):
        if pos + 2 > len(payload):
            raise ValueError("truncated SymbolClass entry")
        character_id = struct.unpack_from("<H", payload, pos)[0]
        pos += 2
        name, pos = _read_cstring(payload, pos)
        symbols.append((character_id, name))
    return symbols


def build_symbol_class(symbols: list[tuple[int, str]]) -> bytes:
    payload = bytearray(struct.pack("<H", len(symbols)))
    for character_id, name in symbols:
        payload.extend(struct.pack("<H", character_id))
        payload.extend(name.encode("utf-8"))
        payload.append(0)
    return bytes(payload)


def parse_define_bits_lossless2(payload: bytes) -> BitmapTag:
    if len(payload) < 7:
        raise ValueError("truncated DefineBitsLossless2 tag")
    character_id = struct.unpack_from("<H", payload, 0)[0]
    bitmap_format = payload[2]
    width, height = struct.unpack_from("<HH", payload, 3)
    if bitmap_format != 5:
        raise ValueError(
            f"unsupported DefineBitsLossless2 bitmap format {bitmap_format}; only 32-bit ARGB is supported"
        )
    raw = zlib.decompress(payload[7:])
    expected = width * height * 4
    if len(raw) != expected:
        raise ValueError(
            f"bitmap {character_id} decompressed to {len(raw)} bytes, expected {expected}"
        )

    rgba = bytearray(expected)
    for i in range(0, expected, 4):
        alpha, red, green, blue = raw[i], raw[i + 1], raw[i + 2], raw[i + 3]
        rgba[i : i + 4] = bytes((red, green, blue, alpha))

    return BitmapTag(character_id, width, height, RgbaImage(width, height, bytes(rgba)))


def build_define_bits_lossless2(character_id: int, image: RgbaImage) -> SwfTag:
    argb = bytearray(len(image.pixels))
    for i in range(0, len(image.pixels), 4):
        red, green, blue, alpha = image.pixels[i], image.pixels[i + 1], image.pixels[i + 2], image.pixels[i + 3]
        argb[i : i + 4] = bytes((alpha, red, green, blue))

    payload = (
        struct.pack("<HBHH", character_id, 5, image.width, image.height)
        + zlib.compress(bytes(argb), 9)
    )
    return SwfTag(TAG_DEFINE_BITS_LOSSLESS2, payload)
