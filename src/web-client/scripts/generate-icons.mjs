// Generates the PWA icons as plain PNGs so the repository needs no image tooling.
import { deflateSync } from "node:zlib";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const outputDir = join(dirname(fileURLToPath(import.meta.url)), "..", "public", "icons");

const background = [15, 23, 42];
const foreground = [248, 250, 252];
const accent = [56, 189, 248];

function crc32(buffer) {
  let crc = 0xffffffff;

  for (const byte of buffer) {
    crc ^= byte;

    for (let bit = 0; bit < 8; bit++) {
      crc = crc & 1 ? (crc >>> 1) ^ 0xedb88320 : crc >>> 1;
    }
  }

  return (crc ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);

  const typeAndData = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(typeAndData));

  return Buffer.concat([length, typeAndData, crc]);
}

function insideRoundedRect(x, y, left, top, width, height, radius) {
  if (x < left || y < top || x >= left + width || y >= top + height) {
    return false;
  }

  const dx = Math.min(x - left, left + width - 1 - x);
  const dy = Math.min(y - top, top + height - 1 - y);

  if (dx >= radius || dy >= radius) {
    return true;
  }

  return (radius - dx) ** 2 + (radius - dy) ** 2 <= radius ** 2;
}

/** A suitcase: a rounded body with a handle above it and a lighter clasp band. */
function pixel(x, y, size) {
  const unit = size / 32;
  const bodyLeft = 6 * unit;
  const bodyTop = 11 * unit;
  const bodyWidth = 20 * unit;
  const bodyHeight = 15 * unit;

  const handleOuter = insideRoundedRect(x, y, 12 * unit, 6 * unit, 8 * unit, 6 * unit, 2 * unit);
  const handleInner = insideRoundedRect(x, y, 14 * unit, 8 * unit, 4 * unit, 5 * unit, 1 * unit);

  if (handleOuter && !handleInner) {
    return foreground;
  }

  if (insideRoundedRect(x, y, bodyLeft, bodyTop, bodyWidth, bodyHeight, 3 * unit)) {
    const bandTop = bodyTop + 6 * unit;

    return y >= bandTop && y < bandTop + 3 * unit ? accent : foreground;
  }

  return background;
}

function png(size) {
  const raw = Buffer.alloc(size * (size * 3 + 1));
  let offset = 0;

  for (let y = 0; y < size; y++) {
    raw[offset++] = 0;

    for (let x = 0; x < size; x++) {
      const [r, g, b] = pixel(x, y, size);
      raw[offset++] = r;
      raw[offset++] = g;
      raw[offset++] = b;
    }
  }

  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0);
  header.writeUInt32BE(size, 4);
  header[8] = 8;
  header[9] = 2;

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", header),
    chunk("IDAT", deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

mkdirSync(outputDir, { recursive: true });

for (const size of [192, 512]) {
  writeFileSync(join(outputDir, `icon-${size}.png`), png(size));
  console.log(`icons/icon-${size}.png`);
}
