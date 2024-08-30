import argparse
from PIL import Image, ImageDraw, ImageFont, ImageColor

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('filename', type=str)
    parser.add_argument('width', type=int)
    parser.add_argument('height', type=int)
    parser.add_argument('font', type=str)
    parser.add_argument('text', type=str)
    parser.add_argument('size', type=int)
    parser.add_argument('x', type=str, default='0', nargs='?')
    parser.add_argument('y', type=str, default='0', nargs='?')
    parser.add_argument('--fill', type=str, default='#ffffff')
    parser.add_argument('--stroke_fill', type=str, default='#000000')
    parser.add_argument('--stroke_width', type=int, default=0)
    args = parser.parse_args()

    with open(args.filename, 'rb') as f:
        data = f.read()

    with Image.frombuffer('RGB', (args.width, args.height), data) as im:
        font = ImageFont.truetype(args.font, args.size)
        draw = ImageDraw.Draw(im)
        fill = ImageColor.getcolor(args.fill, 'RGB')
        stroke_fill = ImageColor.getcolor(args.stroke_fill, 'RGB')

        #  tilde is uncommon enough
        join_char = '~'
        xs = [int(s) for s in args.x.split(join_char)]
        ys = [int(s) for s in args.y.split(join_char)]
        texts = args.text.split(join_char)

        for x, y, text in zip(xs, ys, texts):
            draw.text((x, y), text, font=font, fill=fill, anchor='mm', stroke_width=args.stroke_width, stroke_fill=stroke_fill)

        with open(args.filename, 'wb') as f:
            f.write(im.tobytes())
