"""Make original sign and foliage atlases. Run with Python + Pillow; no photo extraction."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import random, math

OUT = Path(__file__).resolve().parents[2] / 'Unity/Assets/Resources/Jaksa'
OUT.mkdir(parents=True, exist_ok=True)
FONT = '/System/Library/Fonts/Supplemental/Arial Bold.ttf'
signs = [
    ('KOPI JAKSA', 'KOPI · TEH · SARAPAN', '#283e36', '#eee7d0'),
    ('WARUNG NASI', 'MASAKAN RUMAHAN', '#d2c7a7', '#4b3927'),
    ('WISMA SIRIH', 'KAMAR · ROOMS', '#e7e1d4', '#3d5e62'),
    ('LAUNDRY', 'CUCI · SETRIKA', '#647f87', '#ffffff'),
    ('TOKO HARIAN', 'KEBUTUHAN SEHARI-HARI', '#9d493d', '#f5e7ce'),
    ('RUMAH MAKAN', 'SOTO AYAM · NASI GORENG', '#d6bb77', '#423b29'),
    ('GUEST HOUSE', 'SELAMAT DATANG', '#e4e1d9', '#334b44'),
    ('KEDAI SENJA', 'MAKAN · MINUM · SANTAI', '#424b47', '#e9d8b7'),
    ('PANGKAS RAMBUT', 'BUKA SETIAP HARI', '#c4d2d1', '#263c41'),
    ('TOKO BUNGA', 'TANAMAN & POT', '#526a48', '#efe9cd'),
    ('BENGKEL MOTOR', 'SERVIS · OLI · BAN', '#abac9f', '#29312f'),
    ('SIRIH INN', 'ROOMS AVAILABLE', '#3b565c', '#e7e4d5'),
    ('SOTO & BAKSO', 'ES TEH · JERUK', '#a04738', '#f5ead5'),
    ('JL. JAKSA', 'KEBON SIRIH · JAKARTA PUSAT', '#245d4d', '#f1f0dd'),
    ('SELAMAT DATANG', 'JALAN JAKSA', '#384d45', '#ede4d0'),
    ('PELAN-PELAN', 'UTAMAKAN PEJALAN KAKI', '#dfd8c1', '#3e4440'),
]
atlas = Image.new('RGB', (2048, 1024)); d = ImageDraw.Draw(atlas)
for i, (label, sub, bg, fg) in enumerate(signs):
    x, y = (i % 4)*512, (i//4)*256
    d.rectangle((x, y, x+511, y+255), fill=bg)
    d.rectangle((x+12, y+12, x+500, y+244), outline=fg, width=2)
    size = 52
    while ImageFont.truetype(FONT,size).getlength(label)>462:size-=1
    f = ImageFont.truetype(FONT,size)
    d.text((x+256,y+94),label,font=f,anchor='mm',fill=fg)
    d.text((x+256,y+167),sub,font=ImageFont.truetype(FONT,21),anchor='mm',fill=fg)
atlas.save(OUT/'signs.png')

rng=random.Random(41)
im=Image.new('RGBA',(512,512));d=ImageDraw.Draw(im)
for branch in range(9):
    a=branch*math.tau/9
    start=(256,270); end=(256+math.cos(a)*195,256+math.sin(a)*195)
    d.line((start,end),fill=(79,72,41,255),width=3)
    for i in range(16):
        t=(i+1)/17;cx=start[0]*(1-t)+end[0]*t;cy=start[1]*(1-t)+end[1]*t
        angle=a+(1 if i%2==0 else -1)*.85
        length=rng.uniform(27,53);width=length*.32
        tip=(cx+math.cos(angle)*length,cy+math.sin(angle)*length)
        mid=((cx+tip[0])*.5,(cy+tip[1])*.5)
        points=[(cx,cy),(mid[0]-math.sin(angle)*width,mid[1]+math.cos(angle)*width),tip,
                (mid[0]+math.sin(angle)*width,mid[1]-math.cos(angle)*width)]
        shade=rng.randrange(0,42)
        d.polygon(points,fill=(62+shade,91+shade,35+shade//2,255))
        d.line(((cx,cy),tip),fill=(105+shade,125+shade,55,255),width=1)
im.save(OUT/'leaves.png')
print('Wrote original signs and foliage to',OUT)
