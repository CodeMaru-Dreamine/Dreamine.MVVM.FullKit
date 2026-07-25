from pathlib import Path
from urllib.parse import quote, urlencode
from urllib.request import Request, urlopen
from urllib.error import HTTPError

KEY_FILE = Path(r"D:\HiddenPieceVideo\pollinations_key.txt")
OUT = Path(r"D:\HiddenPieceVideo\webapi_test\pollinations_ltx23_vertical.mp4")

prompt = (
    "A cinematic close-up of a 69-year-old Korean engineer with swept-back silver hair working alone "
    "in a dark futuristic laboratory at night. He slowly stops typing and looks toward a glowing monitor "
    "with restrained shock. Rain moves down the window, cold blue screen light, natural facial motion, "
    "photorealistic Korean science-fiction thriller, slow camera push-in, high detail, no text, no logo."
)
params = urlencode({
    "model": "ltx-2",
    "duration": 5,
    "aspectRatio": "9:16",
    "audio": "false",
    "enhance": "true",
})
url = f"https://gen.pollinations.ai/video/{quote(prompt)}?{params}"
key = KEY_FILE.read_text(encoding="utf-8-sig").strip()
request = Request(url, headers={
    "Authorization": f"Bearer {key}",
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/140.0.0.0 Safari/537.36",
    "Accept": "video/mp4,application/json;q=0.9,*/*;q=0.8",
})
OUT.parent.mkdir(parents=True, exist_ok=True)
try:
    with urlopen(request, timeout=600) as response, OUT.open("wb") as target:
        target.write(response.read())
    print(f"FINAL={OUT} bytes={OUT.stat().st_size}")
except HTTPError as exc:
    print(f"HTTP_ERROR={exc.code} {exc.read().decode('utf-8', errors='replace')}")
    raise
