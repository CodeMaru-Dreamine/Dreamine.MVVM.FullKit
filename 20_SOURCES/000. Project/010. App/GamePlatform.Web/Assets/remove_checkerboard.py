"""Convert image-generator checkerboard previews into real alpha cutouts."""

from collections import deque
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).parent / "Source"
FILES = ("swordswoman-idle.png", "swordswoman-attack.png", "guardian-idle.png")


def is_background(pixel: tuple[int, int, int, int], edge: bool = False) -> bool:
    red, green, blue, _ = pixel
    spread = max(red, green, blue) - min(red, green, blue)
    return min(red, green, blue) > (202 if edge else 220) and spread < (18 if edge else 14)


for name in FILES:
    path = ROOT / name
    source = Image.open(path)
    if source.mode == "RGBA" and source.getchannel("A").getextrema()[0] == 0:
        print(f"{name}: already has alpha")
        continue
    image = source.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    queue: deque[tuple[int, int]] = deque()
    cleared: set[tuple[int, int]] = set()

    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))

    while queue:
        x, y = queue.popleft()
        if (x, y) in cleared or not is_background(pixels[x, y]):
            continue
        cleared.add((x, y))
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < width and 0 <= ny < height:
                queue.append((nx, ny))

    fringe: set[tuple[int, int]] = set()
    for x, y in cleared:
        pixels[x, y] = (*pixels[x, y][:3], 0)
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < width and 0 <= ny < height and (nx, ny) not in cleared:
                if is_background(pixels[nx, ny], edge=True):
                    fringe.add((nx, ny))

    for x, y in fringe:
        pixels[x, y] = (*pixels[x, y][:3], 48)

    image.save(path, optimize=True)
    print(f"{name}: cleared {len(cleared):,} pixels")
