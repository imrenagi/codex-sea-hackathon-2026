"""Build the English RoadSense pitch and render each page for review.
Usage: python docs/pitch/build_deck.py [--desktop]
Dependencies: reportlab, pymupdf, pillow. Fonts: macOS Arial and Georgia.
Text and diagrams remain editable here; generated artwork lives in assets/.
"""
from pathlib import Path
import json,shutil,tempfile,sys
from reportlab.pdfgen import canvas
import pymupdf as fitz
from PIL import Image,ImageDraw
from template import Deck,register_fonts,WIDTH,HEIGHT,INK,PAPER,RED,MUTED,GRAY
HERE=Path(__file__).resolve().parent
ASSET=HERE/'assets';GENERATED=ASSET/'generated'
OUT=Path(tempfile.gettempdir())/'roadsense-pitch-review';OUT.mkdir(parents=True,exist_ok=True)
PDF=HERE/'RoadSense-Pitch-Draft.pdf'
register_fonts();c=canvas.Canvas(str(PDF),pagesize=(WIDTH,HEIGHT),pageCompression=1)
c.setTitle('RoadSense — Practice road decisions');c.setAuthor('We Will Handle')
c.setSubject('English hackathon pitch. Six minutes including a 90-second live demo.')
d=Deck(c)
# 01: cinematic opener with quiet negative space.
d.page(INK);d.image(GENERATED/'road-crossing-hero.png',0,0,1280,720);d.rect(0,0,690,720,INK,.23)
d.label('Road safety practice / Indonesia',64,50,PAPER)
d.text('RoadSense',60,181,100,PAPER,'Bold')
d.block(['Practice the decision.','Before the road.'],65,316,38,PAPER,'Sans',49)
d.text('A playable learning prototype',66,448,23,MUTED)
d.label('We Will Handle',66,591,PAPER,18);d.text('Hackathon pitch',66,629,16,MUTED)
d.rect(858,666,358,32,INK,.72);d.text('AI-generated concept artwork',875,675,13,PAPER);d.end()
# 02: a tangible decision, no invented safety statistics.
d.page();d.image(GENERATED/'pedestrian-decision.png',588,0,692,720,anchor=(.74,.5))
d.header('The learning gap');d.block(['A crossing.','A decision.'],64,157,66,INK,leading=76)
d.block(['Knowing the rule is a start.','Choosing when to act takes practice.'],68,377,23,GRAY,'Sans',34)
d.block(['When is it safe','to move?'],65,505,35,RED,'Italic',43)
d.label('We Will Handle',64,677,GRAY,11);d.label('02 / 08',480,677,GRAY,11)
d.rect(913,666,303,32,INK,.72);d.text('AI-generated concept artwork',928,675,13,PAPER);d.end()
# 03: native, editable learning-loop diagram.
d.page(INK);d.header('The learning loop',MUTED)
d.text('A mistake becomes another attempt',64,130,56,PAPER,'Bold')
d.text('The prototype connects each decision to a way to try again.',67,214,25,MUTED)
xs=[110,410,710,1010]
labels=[('01','Drive',['Make a decision','in a road scenario.']),('02','Feedback',['See what happened','and the relevant rule.']),('03','Example',['Watch the correct','driving action.']),('04','Retry',['Apply it in','another attempt.'])]
d.line(110,345,1110,345,'#5E625A',2)
for x,(num,title,body) in zip(xs,labels):
    d.text(num,x-2,279,41,RED,'Bold');d.dot(x,345,8,RED);d.text(title,x-2,376,32,PAPER,'Bold');d.block(body,x-2,427,21,MUTED,'Sans',29)
d.line(1110,345,1216,345,'#5E625A',2);d.line(1216,345,1216,548,'#5E625A',2);d.line(1216,548,110,548,'#5E625A',2)
d.line(110,548,110,509,'#5E625A',2);d.line(110,509,104,520,'#5E625A',2);d.line(110,509,116,520,'#5E625A',2)
d.text('A second chance, with a reason to change.',310,586,28,PAPER,'Italic');d.footer(3,MUTED);d.end()
# 04: real prototype scene, no fabricated or translated interface.
d.page(RED);d.header('Live prototype',PAPER);d.text('Live demo',64,128,61,PAPER,'Bold')
d.text('90',54,227,190,INK,'Bold');d.text('seconds',65,417,40,INK,'Bold')
d.block(['Brake. Wait.','Continue when clear.'],67,507,27,PAPER,'Sans',37)
d.rect(520,126,696,428,INK);d.image(ASSET/'gameplay-existing.png',520,126,696,428,crop=(274,335,738,600))
d.text('Actual prototype scene detail, cropped from an earlier capture',521,575,16,PAPER)
d.text('The live build shows the tutorial and feedback.',521,608,23,PAPER);d.footer(4,PAPER);d.end()
# 05: local context in one expansive world image.
d.page();d.header('Local context')
d.block(['Familiar streets.','Different responsibilities.'],64,111,57,INK,leading=63)
d.image(GENERATED/'neighborhood-vehicles.png',0,284,1280,436,anchor=(.5,.49));d.rect(0,559,1280,161,INK,.9)
d.text('Car',65,585,30,PAPER,'Bold');d.text('Seat belt',65,628,21,MUTED)
d.text('Motorcycle',330,585,30,PAPER,'Bold');d.text('Helmet and headlight',330,628,21,MUTED)
d.text('Village, school and market scenarios',689,590,24,PAPER);d.text('AI-generated concept artwork',690,632,14,MUTED)
d.footer(5,MUTED);d.end()
# 06: verified prototype scope, editorial evidence typography.
d.page(INK);d.header('Built today',MUTED);d.text('A playable prototype',64,127,65,PAPER,'Bold')
d.text('Short practice sessions and a longer journey.',67,219,25,MUTED)
for x,num,label,body in [(64,'5','Skill tests',['Focused practice','for individual skills.']),(475,'3','Everyday missions',['Village shopping, school crossing','and market passenger pickup.']),(885,'10','Journey stages',['Preparation through','an ambulance encounter.'])]:
    d.text(num,x-5,293,142,RED,'Bold');d.text(label,x,462,29,PAPER,'Bold');d.block(body,x,510,20,MUTED,'Sans',29)
d.text('Car and motorcycle progress stay separate.',67,613,24,PAPER);d.footer(6,MUTED);d.end()
# 07: planned evaluation, visually distinct from results.
d.page();d.header('Next / validation')
d.block(['Does practice','change the','next decision?'],64,132,63,INK,'Serif',74)
d.text('Learning impact is not yet measured.',68,415,24,RED,'Bold')
d.block(['Next: observe first-time players,','then compare decisions in a new scenario.'],68,475,24,GRAY,'Sans',35)
d.label('Planned evaluation',790,150,RED,13)
for y,num,title,body in [(217,'01','First attempt','Observe the initial decision.'),(353,'02','Practice','Use feedback, example and retry.'),(489,'03','New scenario','Check the next decision.')]:
    d.dot(795,y+19,5,RED);d.text(num,821,y,24,RED,'Bold');d.text(title,878,y-2,29,INK,'Bold');d.text(body,821,y+45,20,GRAY)
d.line(795,242,795,350,'#BBBEB5',1.5);d.line(795,378,795,486,'#BBBEB5',1.5);d.footer(7);d.end()
# 08: the opening background returns for the invitation.
d.page(INK);d.image(GENERATED/'road-crossing-hero.png',0,0,1280,720);d.rect(0,0,760,720,INK,.5)
d.label('RoadSense',64,50,PAPER);d.block(['Every road decision','deserves practice.'],64,191,61,PAPER,'Bold',73)
d.text('Try the prototype.',68,407,36,PAPER,'Italic');d.label('We Will Handle',68,578,PAPER,19);d.text('Thank you',68,620,21,MUTED)
d.rect(858,666,358,32,INK,.72);d.text('AI-generated concept artwork',875,675,13,PAPER);d.end();c.save()
# Render actual PDF pages; review artifacts remain outside the source folder.
doc=fitz.open(PDF);thumbs=[];report={'pages':len(doc),'page_size':[WIDTH,HEIGHT],'text_bounds_errors':[]}
for i,p in enumerate(doc):
    pix=p.get_pixmap(matrix=fitz.Matrix(1,1),alpha=False);path=OUT/f'slide-{i+1:02}.png';pix.save(path)
    im=Image.open(path);im.thumbnail((640,360));thumbs.append(im.copy())
    for block in p.get_text('dict')['blocks']:
        if block['type']!=0:continue
        for ln in block['lines']:
            for span in ln['spans']:
                x0,y0,x1,y1=span['bbox']
                if x0<0 or y0<0 or x1>WIDTH+.5 or y1>HEIGHT+.5:report['text_bounds_errors'].append({'page':i+1,'text':span['text'],'bbox':span['bbox']})
sheet=Image.new('RGB',(1320,1520),'#D6D7D0');draw=ImageDraw.Draw(sheet)
for i,im in enumerate(thumbs):
    x=10+(i%2)*660;y=10+(i//2)*380;sheet.paste(im,(x,y));draw.text((x,y+363),f'{i+1:02}',fill='#191B19')
sheet.save(OUT/'contact-sheet.jpg',quality=93);(OUT/'review.json').write_text(json.dumps(report,indent=2))
(OUT/'slide-text.txt').write_text('\n\n'.join(f'SLIDE {i+1}\n'+p.get_text() for i,p in enumerate(doc)))
if '--desktop' in sys.argv:shutil.copy2(PDF,Path.home()/'Desktop/RoadSense-Pitch.pdf')
print(json.dumps({'pdf':str(PDF),'review':str(OUT),'pages':len(doc),'bounds_errors':report['text_bounds_errors']},indent=2))
