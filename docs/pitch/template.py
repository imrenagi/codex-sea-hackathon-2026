"""Editable RoadSense 16:9 PDF primitives; coordinates use a top-left origin."""
from pathlib import Path
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.colors import HexColor
from PIL import Image
WIDTH, HEIGHT = 1280, 720
INK, PAPER, RED = '#191B19', '#F4F1E9', '#CF402D'
MUTED, GRAY = '#B9B9AF', '#61645C'
def register_fonts():
    folder=Path('/System/Library/Fonts/Supplemental')
    for name,file in [('Sans','Arial.ttf'),('Bold','Arial Bold.ttf'),('Serif','Georgia.ttf'),('Italic','Georgia Italic.ttf')]:
        pdfmetrics.registerFont(TTFont(name,str(folder/file)))
class Deck:
    def __init__(self,canvas): self.c=canvas
    def rect(self,x,y,w,h,color,alpha=1):
        c=self.c;c.saveState();c.setFillColor(HexColor(color));c.setFillAlpha(alpha)
        c.rect(x,HEIGHT-y-h,w,h,stroke=0,fill=1);c.restoreState()
    def text(self,value,x,y,size=28,color=INK,font='Sans'):
        obj=self.c.beginText(x,HEIGHT-y-size*.82);obj.setFont(font,size);obj.setFillColor(HexColor(color));obj.setCharSpace(0);obj.textOut(value);self.c.drawText(obj)
    def block(self,values,x,y,size=54,color=INK,font='Bold',leading=None):
        for i,value in enumerate(values):self.text(value,x,y+i*(leading or size*1.12),size,color,font)
    def label(self,value,x,y,color=GRAY,size=13):
        obj=self.c.beginText(x,HEIGHT-y-size*.82);obj.setFont('Bold',size);obj.setFillColor(HexColor(color));obj.setCharSpace(1.7);obj.textOut(value.upper());self.c.drawText(obj)
    def line(self,x,y,x2,y2,color,width=1):
        c=self.c;c.setStrokeColor(HexColor(color));c.setLineWidth(width);c.line(x,HEIGHT-y,x2,HEIGHT-y2)
    def dot(self,x,y,r,color):
        c=self.c;c.setFillColor(HexColor(color));c.circle(x,HEIGHT-y,r,fill=1,stroke=0)
    def image(self,path,x,y,w,h,crop=None,anchor=(.5,.5)):
        """Place original pixels through a PDF clip without modifying the asset."""
        iw,ih=Image.open(path).size
        if crop:
            left,top,right,bottom=crop;cw,ch=right-left,bottom-top;scale=max(w/cw,h/ch)
            ox=x-left*scale-(cw*scale-w)*anchor[0];oy=y-top*scale-(ch*scale-h)*anchor[1]
        else:
            scale=max(w/iw,h/ih);ox=x-(iw*scale-w)*anchor[0];oy=y-(ih*scale-h)*anchor[1]
        c=self.c;c.saveState();p=c.beginPath();p.rect(x,HEIGHT-y-h,w,h);c.clipPath(p,stroke=0,fill=0)
        c.drawImage(str(path),ox,HEIGHT-oy-ih*scale,width=iw*scale,height=ih*scale,mask='auto');c.restoreState()
    def header(self,section,color=GRAY):
        self.label('RoadSense',64,39,color);self.label(section,276,39,color)
    def footer(self,n,color=GRAY):
        self.label('We Will Handle',64,677,color,11);self.label(f'{n:02d} / 08',1148,677,color,11)
    def page(self,color=PAPER):self.rect(0,0,WIDTH,HEIGHT,color)
    def end(self):self.c.showPage()
