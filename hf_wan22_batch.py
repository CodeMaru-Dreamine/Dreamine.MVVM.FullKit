from pathlib import Path
import os
import shutil
import sys
import time

from PIL import Image
from gradio_client import Client, handle_file

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

SPACE = "zerogpu-aoti/wan2-2-fp8da-aoti-faster"
SOURCE_DIR = Path(r"C:\Users\Minsu\Downloads")
ROOT = Path(r"D:\HiddenPieceVideo\webapi_v3")
CROPS = ROOT / "portrait_sources"
CLIPS = ROOT / "clips"

NEGATIVE = (
    "low quality, blurry, jitter, flicker, warped face, face change, deformed hands, extra fingers, "
    "fused limbs, duplicate people, rubber body, unnatural motion, floating objects, cartoon, anime, "
    "oversaturated, subtitles, text, logo, watermark, camera shake"
)

SCENES = [
    (1, 0.55, "Night office", "The elderly Korean engineer works alone at his desk late at night. His weathered hands type steadily while cold monitor light reflects on his focused face. Rain slides down the window and distant city lights shimmer. Very slow cinematic camera push-in, subtle breathing and natural blinking, restrained realistic motion, photorealistic Korean science-fiction thriller, no dialogue."),
    (2, 0.60, "Hands typing", "Extreme close-up of the elderly engineer's weathered hands typing a short command on an old mechanical keyboard. Only his fingers move with precise natural timing while his serious face remains softly out of focus behind them. Cool monitor light, shallow depth of field, macro cinema lens, subtle camera slide, realistic fingers and anatomy, tense quiet atmosphere."),
    (3, 0.55, "Hidden page", "The elderly engineer discovers an obscure archival page on the monitor. The cold white screen changes to a dark classified interface and its light washes across his shocked face. He stops typing and slowly leans closer. Controlled over-the-shoulder camera push-in, realistic screen glow, subtle eye movement, cinematic mystery thriller, no readable generated text."),
    (5, 0.67, "Loading", "A minimal loading indicator advances on the monitor. The elderly Korean engineer freezes, then slowly lifts one hand away from the keyboard as dread appears in his eyes. City lights and rain remain behind him. Slow dolly toward his face, realistic breathing and blinking, cold blue lighting, high-end cinematic suspense, restrained motion."),
    (6, 0.57, "Realization", "Close-up of the elderly engineer as he realizes the hidden record is connected to his forgotten past. His pupils shift, his lips part slightly, and his expression changes from concentration to disbelief. Background monitors pulse softly. Very slow camera orbit, natural facial micro-expression, realistic skin detail, dark Korean techno-thriller cinematography."),
    (7, 0.50, "48 folders", "Rows of hidden archive folders appear across the monitor in front of the elderly engineer. He reaches toward the screen but hesitates just before touching it. Reflections ripple across the glass and rain falls outside. Slow centered camera push-in from behind, subtle shoulder movement, realistic body motion, ominous cinematic science fiction."),
    (8, 0.62, "Blueprints", "The elderly engineer urgently sketches a modular architecture diagram by hand while technical blueprints glow on surrounding monitors. His pen moves naturally, one page shifts under his wrist, and his eyes compare the paper with the screens. Slow lateral camera move, detailed hands, moody blue light, sophisticated cinematic engineering thriller."),
    (10, 0.67, "Secret team", "A female scientist and a stern man in black stand beside the elderly engineer in a dark laboratory. The scientist exchanges a worried glance with the man while the engineer studies a white neural interface glove on the table. Slow camera push around the group, subtle natural gestures, tense silence, photorealistic corporate science-fiction cinema."),
    (11, 0.50, "Neural lab", "Through observation glass, the elderly engineer sits inside a sealed neural laboratory while a scientist attaches fine sensors to his head. The man in black watches without moving. Indicator lights pulse and delicate cables sway slightly. Slow ominous zoom through the glass, restrained realistic human motion, cold clinical cinematic lighting."),
    (13, 0.72, "Emergency", "Red emergency lights suddenly flash inside the neural laboratory. The female scientist lunges toward the shutdown controls while the man in black turns toward the sealed chamber. The elderly engineer remains unconscious as monitors erupt with warning patterns. Urgent but coherent motion, controlled handheld camera, cinematic red strobe, realistic thriller action."),
    (16, 0.62, "Transfer", "The elderly engineer floats weightlessly through a vast dark void as fragments of blue technical schematics, memories and autumn leaves spiral around him. His body rotates slowly and his silver hair drifts in zero gravity. The camera circles gently, luminous particles stream past, epic mysterious transition between worlds, photorealistic cinematic fantasy."),
    (15, 0.28, "Awakening", "The injured elderly Korean engineer awakens in an ancient pine forest and slowly turns toward three armored warriors approaching through the morning mist. Wind moves his silver hair and pine branches as the warriors advance naturally. Low camera slowly pushes toward him, dramatic volumetric sunlight, realistic Korean historical fantasy thriller, no dialogue."),
]


def source_for(number: int) -> Path:
    matches = list(SOURCE_DIR.glob(f"ChatGPT Image 2026년 7월 16일 오후 07_51_* ({number}).png"))
    if len(matches) != 1:
        raise RuntimeError(f"Source {number}: found {len(matches)} matches")
    return matches[0]


def crop_portrait(src: Path, dst: Path, focus: float) -> None:
    with Image.open(src) as im:
        im = im.convert("RGB")
        crop_w = round(im.height * 9 / 16)
        center_x = round(im.width * focus)
        left = max(0, min(im.width - crop_w, center_x - crop_w // 2))
        im.crop((left, 0, left + crop_w, im.height)).save(dst, quality=96)


def main() -> None:
    CROPS.mkdir(parents=True, exist_ok=True)
    CLIPS.mkdir(parents=True, exist_ok=True)
    prompt_lines = []
    for index, (number, focus, label, prompt) in enumerate(SCENES, 1):
        crop = CROPS / f"scene_{index:02d}.jpg"
        crop_portrait(source_for(number), crop, focus)
        prompt_lines.extend([
            f"SCENE {index:02d} — {label}",
            f"IMAGE: {crop}",
            f"PROMPT: {prompt}",
            f"NEGATIVE: {NEGATIVE}",
            "",
        ])
    (ROOT / "hailuo_prompts.txt").write_text("\n".join(prompt_lines), encoding="utf-8-sig")
    print(f"PREPARED {len(SCENES)} portrait images and prompts", flush=True)
    if os.environ.get("PREPARE_ONLY") == "1":
        return

    client = Client(SPACE, verbose=True)
    for index, (number, focus, label, prompt) in enumerate(SCENES, 1):
        crop = CROPS / f"scene_{index:02d}.jpg"
        out = CLIPS / f"scene_{index:02d}.mp4"
        if out.exists() and out.stat().st_size > 100_000:
            print(f"SKIP {index:02d}/12 {label}", flush=True)
            continue
        print(f"START {index:02d}/12 {label}", flush=True)
        try:
            result = client.predict(
                handle_file(str(crop)), prompt, 8, NEGATIVE, 5.0, 1.0, 1.0,
                19770417 + index * 101, False, api_name="/generate_video"
            )
            video_path = result[0] if isinstance(result, (tuple, list)) else result
            shutil.copy2(video_path, out)
            print(f"DONE {index:02d}/12 {label} bytes={out.stat().st_size}", flush=True)
        except Exception as exc:
            print(f"FAILED {index:02d}/12 {label}: {exc!r}", flush=True)
            break
        time.sleep(2)
    print("BATCH_FINISHED", flush=True)


if __name__ == "__main__":
    main()
