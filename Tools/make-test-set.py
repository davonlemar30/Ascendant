#!/usr/bin/env python3
"""Build E: writes the shipped test set, one PNG per art slot and one WAV per sound slot, with the standard library only.

The images are flat colored boxes with a border, a diagonal, transparent corners, and the slot's name; the sounds are short
synthesized cues. They exist to prove the wiring (a file in a slot lands in the right place, a cue plays on its action) and to
let the owner see every slot on the style page; they are not art. Run from the repository root:

    python3 Tools/make-test-set.py

Slot names, sizes, and the sound list mirror Assets/CelestialDial/Slots.cs; keep both in step."""
import colorsys, math, os, random, struct, wave, zlib

ART = "Assets/CelestialDial/Resources/Art/test"
AUDIO = "Assets/CelestialDial/Resources/Audio/test"
# name -> (width, height); sides are multiples of 4 so the texture compresses; backgrounds ship at half size and stretch.
IMAGES = [
    ("atrium", 180, 400), ("wing", 180, 400), ("chamber", 180, 400),
    ("caspar", 44, 76), ("keeper-idle", 20, 44), ("keeper-walk", 20, 44),
    ("dial-face", 332, 332), ("seat", 52, 52), ("bracket", 60, 60), ("floor-markings", 320, 320),
    ("shelf", 52, 36), ("chair", 44, 36), ("table", 60, 32), ("shelf-book", 12, 28),
    ("book-cover", 140, 140), ("book-page", 140, 140),
    ("shelves", 60, 180), ("furniture-covered", 120, 72), ("desk", 72, 32), ("lamp", 8, 24), ("candle", 8, 20),
    ("door-open", 64, 128), ("door-sealed", 64, 128),
    ("mechanism", 112, 112), ("crystal-book", 36, 72), ("crystal-page", 28, 60), ("lock", 8, 8), ("keeper-key", 84, 40),
    ("journal-page", 180, 400), ("journal-cover", 60, 60),  # Build F
    ("atrium-light", 180, 400), ("wing-light", 180, 400), ("chamber-light", 180, 400),  # Build H: translucent overlays
]
ROUND = {"dial-face", "floor-markings", "mechanism"}
FONT = {  # 3 x 5 capitals, digits, and the hyphen; one string per row
    "A": ("010", "101", "111", "101", "101"), "B": ("110", "101", "110", "101", "110"), "C": ("011", "100", "100", "100", "011"),
    "D": ("110", "101", "101", "101", "110"), "E": ("111", "100", "110", "100", "111"), "F": ("111", "100", "110", "100", "100"),
    "G": ("011", "100", "101", "101", "011"), "H": ("101", "101", "111", "101", "101"), "I": ("111", "010", "010", "010", "111"),
    "J": ("001", "001", "001", "101", "010"), "K": ("101", "101", "110", "101", "101"), "L": ("100", "100", "100", "100", "111"),
    "M": ("101", "111", "111", "101", "101"), "N": ("110", "101", "101", "101", "101"), "O": ("010", "101", "101", "101", "010"),
    "P": ("110", "101", "110", "100", "100"), "Q": ("010", "101", "101", "110", "011"), "R": ("110", "101", "110", "101", "101"),
    "S": ("011", "100", "010", "001", "110"), "T": ("111", "010", "010", "010", "010"), "U": ("101", "101", "101", "101", "011"),
    "V": ("101", "101", "101", "101", "010"), "W": ("101", "101", "111", "111", "101"), "X": ("101", "101", "010", "101", "101"),
    "Y": ("101", "101", "010", "010", "010"), "Z": ("111", "001", "010", "100", "111"), "-": ("000", "000", "111", "000", "000"),
    "0": ("010", "101", "101", "101", "010"), "1": ("010", "110", "010", "010", "111"), "2": ("110", "001", "010", "100", "111"),
    "3": ("110", "001", "010", "001", "110"), "4": ("101", "101", "111", "001", "001"), "5": ("111", "100", "110", "001", "110"),
    "6": ("011", "100", "110", "101", "010"), "7": ("111", "001", "010", "010", "010"), "8": ("010", "101", "010", "101", "010"),
    "9": ("010", "101", "011", "001", "110"),
}

def png(path, width, height, pixel):
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            raw.extend(pixel(x, y))
    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xffffffff)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))

def text_pixels(name, width, height):
    """The slot's name in the tiny font, as large as fits, top-left; empty for slots too small to carry it."""
    label = name.upper(); cells = len(label) * 4 - 1
    scale = min((width - 4) // cells, (height - 4) // 5, 4)
    if scale < 1:
        return set(), 0
    on = set()
    for i, ch in enumerate(label):
        rows = FONT.get(ch)
        if rows is None:
            continue
        for r, row in enumerate(rows):
            for c, bit in enumerate(row):
                if bit == "1":
                    for dy in range(scale):
                        for dx in range(scale):
                            on.add((2 + (i * 4 + c) * scale + dx, 2 + r * scale + dy))
    return on, scale

def image(index, name, width, height):
    hue = (index * 137.508) % 360 / 360
    fill = tuple(int(v * 255) for v in colorsys.hsv_to_rgb(hue, .55, .75))
    dark = tuple(int(v * 255) for v in colorsys.hsv_to_rgb(hue, .7, .35))
    label, scale = text_pixels(name, width, height)
    corner = 3 if min(width, height) >= 16 else 0
    walk_band = name == "keeper-walk"
    light = name.endswith("-light")  # Build H: a translucent amber wash, brighter at the top, with diagonal shafts; the name stays readable
    def pixel(x, y):
        if light:
            if (x, y) in label:
                return (255, 255, 255, 255)
            shaft = (x + y) % 40 < 8
            return (255, 196, 110, min(200, int(30 + 90 * (1 - y / height)) + (60 if shaft else 0)))
        if corner and (x + y < corner or (width - 1 - x) + y < corner or x + (height - 1 - y) < corner or (width - 1 - x) + (height - 1 - y) < corner):
            return (0, 0, 0, 0)  # transparent corners: alpha reaches the screen
        if (x, y) in label:
            return (255, 255, 255, 255)
        if x < 2 or y < 2 or x >= width - 2 or y >= height - 2:
            return dark + (255,)
        if name in ROUND:
            r = math.hypot(x - (width - 1) / 2, y - (height - 1) / 2)
            if abs(r - (min(width, height) / 2 - 6)) < 2:
                return dark + (255,)
        if abs(x * (height - 1) - y * (width - 1)) < max(width, height):  # the diagonal, top-left to bottom-right: a flip shows
            return dark + (255,)
        if walk_band and int(height * .62) <= y < int(height * .62) + 3:
            return (255, 255, 255, 255)
        return fill + (255,)
    png(os.path.join(ART, name + ".png"), width, height, pixel)

RATE = 22050
def tone(seconds, freq, volume=.5, decay=6.0, shape="sine"):
    n = int(RATE * seconds); out = []
    for i in range(n):
        t = i / RATE; env = math.exp(-decay * t)
        v = math.sin(2 * math.pi * freq * t)
        if shape == "square":
            v = 1 if v >= 0 else -1
        out.append(v * env * volume)
    return out

def noise(seconds, volume=.4, decay=30.0, seed=1):
    rnd = random.Random(seed); n = int(RATE * seconds)
    return [(rnd.random() * 2 - 1) * math.exp(-decay * i / RATE) * volume for i in range(n)]

def wav(path, samples):
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples))

def ambient(seconds=2.0):
    n = int(RATE * seconds); rnd = random.Random(7); out = []; low = 0.0
    for i in range(n):
        t = i / RATE
        low = low * .97 + (rnd.random() * 2 - 1) * .03  # slow noise
        v = .12 * math.sin(2 * math.pi * 55 * t) + .06 * math.sin(2 * math.pi * 110.5 * t) + low * .5
        edge = min(1.0, i / (RATE * .05), (n - 1 - i) / (RATE * .05))  # short fades so the loop point is quiet
        out.append(v * edge)
    return out

SOUNDS = {
    "step": tone(.05, 1800, .4, 60),
    "seal": tone(.09, 660, .45, 10) + tone(.16, 990, .45, 12),
    "miss": tone(.25, 180, .35, 8, "square"),
    "key": tone(.12, 523, .4, 8) + tone(.12, 659, .4, 8) + tone(.3, 784, .45, 5),
    "page": noise(.09, .4, 30),
    "door": tone(.2, 90, .6, 12) + noise(.06, .2, 40, 3),
    "ambient": ambient(),
}

if __name__ == "__main__":
    os.makedirs(ART, exist_ok=True); os.makedirs(AUDIO, exist_ok=True)
    for index, (name, width, height) in enumerate(IMAGES):
        image(index, name, width, height)
    for name, samples in SOUNDS.items():
        wav(os.path.join(AUDIO, name + ".wav"), samples)
    total = sum(os.path.getsize(os.path.join(ART, f)) for f in os.listdir(ART)) + sum(os.path.getsize(os.path.join(AUDIO, f)) for f in os.listdir(AUDIO))
    print("test set: %d images, %d sounds, %.0f KB on disk" % (len(IMAGES), len(SOUNDS), total / 1024))
