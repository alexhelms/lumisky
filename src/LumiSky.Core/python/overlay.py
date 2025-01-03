import sys
import json
import traceback
from dataclasses import dataclass, field
from PIL import Image, ImageDraw, ImageFont
from typing import Optional


@dataclass
class TextOverlay:
    x: int
    y: int
    text: str
    font_size: int = 30
    text_fill: str = '#ffffff'
    text_anchor: str = 'mm'
    stroke_fill: str = '#000000'
    stroke_width: int = 0


@dataclass
class CrosshairOverlay:
    x: int
    y: int
    size: int
    width: int
    text: str
    font_size: int = 30
    stroke_fill: str = '#000000'
    stroke_width: int = 0
    color: str = '#ffffff'


@dataclass
class Config:
    data_filename: str
    image_width: int
    image_height: int
    font_filename: str
    text_overlays: list[TextOverlay]
    crosshair_overlays: Optional[list[CrosshairOverlay]] = None


if __name__ == '__main__':
    # load json from stdin
    try:
        json_obj = json.load(sys.stdin)
        config = Config(**json_obj)
        config.text_overlays = [TextOverlay(**overlay) for overlay in config.text_overlays]
        if config.crosshair_overlays:
            config.crosshair_overlays = [CrosshairOverlay(**overlay) for overlay in config.crosshair_overlays]
    except:
        traceback.print_exc()
        sys.exit(1)

    def draw_text(draw: ImageDraw, overlay: CrosshairOverlay) -> None:
        font = ImageFont.truetype(config.font_filename, overlay.font_size)
        draw.text((overlay.x, overlay.y),
                  overlay.text,
                  font=font,
                  fill=overlay.text_fill,
                  anchor=overlay.text_anchor,
                  stroke_width=overlay.stroke_width,
                  stroke_fill=overlay.stroke_fill)

    def draw_crosshair(draw: ImageDraw, overlay: CrosshairOverlay) -> None:
        horizontal = [
            (overlay.x - overlay.size, overlay.y),
            (overlay.x + overlay.size, overlay.y),
        ]
        vertical = [
            (overlay.x, overlay.y - overlay.size),
            (overlay.x, overlay.y + overlay.size)
        ]

        draw.line(horizontal, fill=overlay.color, width=overlay.width)
        draw.line(vertical, fill=overlay.color, width=overlay.width)

        if overlay.text:
            font = ImageFont.truetype(config.font_filename, overlay.font_size)
            draw.text((overlay.x, overlay.y + 1.4 * overlay.size),
                      overlay.text,
                      font=font,
                      fill=overlay.color,
                      anchor='mt',
                      stroke_width=overlay.stroke_width,
                      stroke_fill=overlay.stroke_fill)

    # load image data in memory
    with open(config.data_filename, 'rb') as f:
        data = f.read()

    # create a PIL image
    with Image.frombuffer('RGB', (config.image_width, config.image_height), data) as im:

        draw = ImageDraw.Draw(im)

        # draw each text overlay
        for overlay in config.text_overlays:
            try:
                draw_text(draw, overlay)
            except:
                print(f'error drawing text overlay: {overlay.text}', file=sys.stderr)
                traceback.print_exc()

        # draw each crosshair overlay
        if config.crosshair_overlays:
            for crosshair in config.crosshair_overlays:
                try:
                    draw_crosshair(draw, crosshair)
                except:
                    print(f'error drawing crosshair overlay: {crosshair.text}', file=sys.stderr)
                    traceback.print_exc()

        with open(config.data_filename, 'wb') as f:
            f.write(im.tobytes())
