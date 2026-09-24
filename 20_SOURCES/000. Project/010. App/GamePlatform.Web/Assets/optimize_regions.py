"""Optimize generated region source PNG files into mobile WebP assets."""

from pathlib import Path
from PIL import Image


ROOT = Path(__file__).parents[1] / "wwwroot" / "images" / "maru-idle" / "regions"
MAX_WIDTH = 720
TARGET_BYTES = 500_000


for source in sorted(ROOT.glob("*.png")):
    target = source.with_suffix(".webp")
    with Image.open(source) as image:
        image = image.convert("RGB")
        if image.width > MAX_WIDTH:
            height = round(image.height * MAX_WIDTH / image.width)
            image = image.resize((MAX_WIDTH, height), Image.Resampling.LANCZOS)

        for quality in (72, 66, 60, 54, 48):
            image.save(target, "WEBP", quality=quality, method=6)
            if target.stat().st_size <= TARGET_BYTES:
                break

    print(f"{target.name}: {target.stat().st_size} bytes")
