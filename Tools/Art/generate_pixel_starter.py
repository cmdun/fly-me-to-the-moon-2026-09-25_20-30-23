#!/usr/bin/env python3
"""Generate the small, deterministic pixel-art starter assets.

Run from the repository root. All drawing happens at native resolution so the
PNG files stay crisp when Unity imports them with Point filtering.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Art" / "Sprites"

PALETTE = {
    "transparent": (0, 0, 0, 0),
    "abyss": (7, 20, 38, 255),
    "space_blue": (17, 40, 73, 255),
    "shadow": (36, 48, 68, 255),
    "deep_teal": (31, 76, 91, 255),
    "teal": (46, 139, 124, 255),
    "mint": (114, 229, 194, 255),
    "coral": (241, 111, 97, 255),
    "gold": (255, 209, 102, 255),
    "lavender": (182, 156, 255, 255),
    "ice": (121, 215, 255, 255),
    "blossom": (245, 143, 198, 255),
    "paper": (234, 242, 215, 255),
}


def save(image: Image.Image, name: str) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    image.save(OUT / name, optimize=False)


def astronaut_frame(step: int) -> Image.Image:
    image = Image.new("RGBA", (24, 24), PALETTE["transparent"])
    draw = ImageDraw.Draw(image)
    # Backpack and body shadow.
    draw.rectangle((4, 9, 7, 16), fill=PALETTE["deep_teal"])
    draw.rectangle((7, 8, 16, 18), fill=PALETTE["shadow"])
    draw.rectangle((8, 8, 15, 17), fill=PALETTE["coral"])
    # Helmet and dark visor.
    draw.rectangle((7, 3, 16, 10), fill=PALETTE["paper"])
    draw.rectangle((9, 4, 16, 8), fill=PALETTE["space_blue"])
    draw.point((15, 4), fill=PALETTE["mint"])
    draw.point((16, 5), fill=PALETTE["ice"])
    # Chest light and keyboard-shaped instrument.
    draw.rectangle((10, 10, 12, 12), fill=PALETTE["mint"])
    draw.line((6, 12, 17, 17), fill=PALETTE["paper"], width=3)
    draw.line((7, 12, 17, 16), fill=PALETTE["shadow"], width=1)
    for x, y in ((9, 13), (11, 14), (13, 15), (15, 16)):
        draw.point((x, y), fill=PALETTE["gold"])
    # Arms.
    draw.rectangle((5, 10 + step, 7, 15 + step), fill=PALETTE["coral"])
    draw.rectangle((16, 10 - step, 18, 15 - step), fill=PALETTE["coral"])
    # Two walk poses share the same silhouette and collider footprint.
    if step == 0:
        draw.rectangle((8, 17, 11, 21), fill=PALETTE["coral"])
        draw.rectangle((13, 17, 16, 21), fill=PALETTE["coral"])
        draw.rectangle((7, 21, 11, 22), fill=PALETTE["paper"])
        draw.rectangle((13, 21, 17, 22), fill=PALETTE["paper"])
    else:
        draw.rectangle((7, 17, 10, 20), fill=PALETTE["coral"])
        draw.rectangle((14, 17, 17, 21), fill=PALETTE["coral"])
        draw.rectangle((5, 20, 10, 21), fill=PALETTE["paper"])
        draw.rectangle((14, 21, 18, 22), fill=PALETTE["paper"])
    return image


def make_astronaut_sheet() -> None:
    sheet = Image.new("RGBA", (96, 24), PALETTE["transparent"])
    frames = [astronaut_frame(0), astronaut_frame(1), astronaut_frame(0), astronaut_frame(1)]
    # A one-pixel idle lift in frame 3 makes breathing readable at low speed.
    frames[2] = Image.new("RGBA", (24, 24), PALETTE["transparent"])
    frames[2].alpha_composite(astronaut_frame(0), (0, -1))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index * 24, 0))
        save(frame, f"astronaut_frame_{index + 1}.png")
    save(sheet, "astronaut_starter_sheet.png")


def make_score_fragment() -> None:
    image = Image.new("RGBA", (16, 16), PALETTE["transparent"])
    draw = ImageDraw.Draw(image)
    draw.rectangle((3, 1, 12, 14), fill=PALETTE["gold"])
    draw.rectangle((4, 2, 11, 13), fill=PALETTE["paper"])
    draw.point((4, 2), fill=PALETTE["transparent"])
    draw.point((11, 13), fill=PALETTE["transparent"])
    for y in (5, 7, 9):
        draw.line((5, y, 10, y), fill=PALETTE["deep_teal"])
    draw.rectangle((7, 6, 8, 10), fill=PALETTE["space_blue"])
    draw.rectangle((6, 9, 8, 11), fill=PALETTE["space_blue"])
    # Detached glow pixels become a simple twinkle when alternating two copies.
    for point in ((1, 4), (14, 6), (2, 12), (13, 2)):
        draw.point(point, fill=PALETTE["gold"])
    save(image, "piano_score_fragment.png")


def decoration(name: str, draw_fn) -> None:
    image = Image.new("RGBA", (16, 16), PALETTE["transparent"])
    draw_fn(ImageDraw.Draw(image))
    save(image, name)


def make_decorations() -> None:
    decoration("moon_crystal.png", lambda d: (
        d.polygon(((3, 13), (6, 3), (9, 13)), fill=PALETTE["lavender"]),
        d.polygon(((7, 13), (11, 5), (14, 13)), fill=PALETTE["ice"]),
        d.line((6, 4, 6, 12), fill=PALETTE["paper"])))
    decoration("moon_volcanic_rock.png", lambda d: (
        d.polygon(((1, 13), (4, 6), (8, 4), (14, 12)), fill=PALETTE["shadow"]),
        d.line((8, 5, 7, 9, 11, 12), fill=PALETTE["coral"], width=1),
        d.point((10, 8), fill=PALETTE["gold"])))
    decoration("moon_ice_shard.png", lambda d: (
        d.polygon(((3, 14), (6, 2), (9, 14)), fill=PALETTE["ice"]),
        d.polygon(((8, 14), (11, 5), (14, 14)), fill=PALETTE["mint"]),
        d.line((6, 4, 6, 12), fill=PALETTE["paper"])))
    decoration("moon_ruin.png", lambda d: (
        d.rectangle((3, 5, 5, 13), fill=PALETTE["gold"]),
        d.rectangle((10, 5, 12, 13), fill=PALETTE["gold"]),
        d.rectangle((2, 3, 13, 5), fill=PALETTE["paper"]),
        d.rectangle((1, 13, 14, 14), fill=PALETTE["shadow"])))
    decoration("moon_flower.png", lambda d: (
        d.line((8, 7, 8, 14), fill=PALETTE["teal"], width=2),
        d.rectangle((4, 3, 11, 9), fill=PALETTE["blossom"]),
        d.rectangle((6, 1, 9, 11), fill=PALETTE["blossom"]),
        d.rectangle((7, 5, 8, 6), fill=PALETTE["gold"])))


def make_palette() -> None:
    entries = [value for key, value in PALETTE.items() if key != "transparent"]
    image = Image.new("RGB", (len(entries) * 16, 16))
    draw = ImageDraw.Draw(image)
    for index, color in enumerate(entries):
        draw.rectangle((index * 16, 0, index * 16 + 15, 15), fill=color[:3])
    save(image.convert("RGBA"), "palette_strip.png")


if __name__ == "__main__":
    make_astronaut_sheet()
    make_score_fragment()
    make_decorations()
    make_palette()
    print(f"Generated art starter assets in {OUT}")
