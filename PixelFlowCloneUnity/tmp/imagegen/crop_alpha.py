from pathlib import Path
import sys

from PIL import Image


source = Path(sys.argv[1])
padding = int(sys.argv[2]) if len(sys.argv) > 2 else 12

with Image.open(source) as image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise RuntimeError(f"No visible pixels found in {source}")

    left = max(0, bounds[0] - padding)
    top = max(0, bounds[1] - padding)
    right = min(rgba.width, bounds[2] + padding)
    bottom = min(rgba.height, bounds[3] + padding)
    cropped = rgba.crop((left, top, right, bottom))
    cropped.save(source)
    print(f"Cropped {rgba.size} -> {cropped.size}: {source}")
