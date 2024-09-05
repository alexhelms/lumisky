import sys
import json
import traceback
from dataclasses import dataclass
from PIL import Image, ImageDraw, ImageFont, ImageColor


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
class Config:
    data_filename: str
    image_width: int
    image_height: int
    font_filename: str
    text_overlays: list[TextOverlay]


if __name__ == '__main__':
    # load json from stdin
    try:
        json_obj = json.load(sys.stdin)
        config = Config(**json_obj)
        config.text_overlays = [TextOverlay(**overlay) for overlay in config.text_overlays]
        print(config)
    except:
        traceback.print_exc()
        sys.exit(1)

    # load image data in memory
    with open(config.data_filename, 'rb') as f:
        data = f.read()

    # create a PIL image
    with Image.frombuffer('RGB', (config.image_width, config.image_height), data) as im:
        # draw each overlay
        for overlay in config.text_overlays:
            try:
                draw = ImageDraw.Draw(im)
                font = ImageFont.truetype(config.font_filename, overlay.font_size)
                text_fill = ImageColor.getcolor(overlay.text_fill, 'RGB')
                stroke_fill = ImageColor.getcolor(overlay.stroke_fill, 'RGB')
                draw.text((overlay.x, overlay.y), overlay.text, font=font, fill=text_fill, anchor=overlay.text_anchor,
                          stroke_width=overlay.stroke_width, stroke_fill=overlay.stroke_fill)
            except:
                # skip overlays that cause an exception
                print(f'error drawing overlay: {overlay.text}', file=sys.stderr)
                traceback.print_exc()

        with open(config.data_filename, 'wb') as f:
            f.write(im.tobytes())
