"""Create compact alpha WebP runtime sprites from project source cutouts."""

from pathlib import Path
from PIL import Image


ASSETS = Path(__file__).parent
SOURCE = ASSETS / "Source"
DESTINATION = ASSETS.parent / "wwwroot" / "images" / "maru-idle"
FILES = (
    "swordswoman-idle.png", "swordswoman-attack.png", "guardian-idle.png", "guardian-hit.png",
    "enemy-bell-fox.png", "enemy-cinder-cat.png", "enemy-frost-yak.png",
    "enemy-rain-heron.png", "enemy-amber-scarab.png", "enemy-blue-salamander.png",
    "enemy-cloud-antelope.png", "enemy-rift-crawler.png", "enemy-eclipse-lion.png"
)


for name in FILES:
    source = SOURCE / name
    target = DESTINATION / Path(name).with_suffix(".webp")
    with Image.open(source) as image:
        image = image.convert("RGBA")
        if image.width > 1024:
            height = round(image.height * 1024 / image.width)
            image = image.resize((1024, height), Image.Resampling.LANCZOS)
        image.save(target, "WEBP", quality=82, method=6, exact=True)
    print(f"{target.name}: {target.stat().st_size} bytes")
