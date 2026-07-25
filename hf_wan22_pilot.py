from pathlib import Path
import shutil
import sys

from gradio_client import Client, file

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")


SPACE = "zerogpu-aoti/wan2-2-fp8da-aoti-faster"
IMAGE = r"C:\Users\Minsu\Downloads\ChatGPT Image 2026년 7월 16일 오후 07_51_57 (15).png"
OUT = Path(r"D:\HiddenPieceVideo\webapi_test\pilot_wan22_14b.mp4")

PROMPT = (
    "The injured elderly Korean engineer suddenly regains consciousness in an ancient pine forest. "
    "He breathes heavily and slowly raises his head, staring in disbelief at three armored warriors "
    "approaching through drifting morning mist. Subtle wind moves his silver hair and the pine branches. "
    "The warriors advance naturally by two slow steps. Cinematic Korean historical fantasy thriller, "
    "realistic human motion, detailed faces, dramatic volumetric sunlight, restrained handheld camera "
    "push-in, shallow depth of field, photorealistic, no dialogue, no sudden body movement."
)

NEGATIVE = (
    "low quality, blurry, jitter, flicker, warped face, deformed hands, extra fingers, fused limbs, "
    "duplicate people, rubber body, floating objects, cartoon, anime, oversaturated, subtitles, text, logo"
)


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    client = Client(SPACE, verbose=True)
    result = client.predict(
        file(IMAGE), PROMPT, 8, NEGATIVE, 5.0, 1.0, 1.0, 19770417, False,
        api_name="/generate_video",
    )
    video_path = result[0] if isinstance(result, (tuple, list)) else result
    shutil.copy2(video_path, OUT)
    print(f"FINAL={OUT}")


if __name__ == "__main__":
    main()
