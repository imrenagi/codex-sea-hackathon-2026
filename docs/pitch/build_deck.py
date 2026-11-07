"""Build the RoadSense PDF deck aligned with PITCH.md.
Usage: python docs/pitch/build_deck.py [--desktop]
Dependencies: reportlab, pymupdf, pillow. Fonts: macOS Arial and Georgia.
Text and diagrams remain editable here; generated artwork lives in assets/.
"""
from pathlib import Path
import json,shutil,tempfile,sys,hashlib
from reportlab.pdfgen import canvas
import pymupdf as fitz
from PIL import Image,ImageDraw
from template import Deck,register_fonts,WIDTH,HEIGHT,INK,PAPER,RED,MUTED,GRAY
HERE=Path(__file__).resolve().parent
ASSET=HERE/'assets';GENERATED=ASSET/'generated'
OUT=Path(tempfile.gettempdir())/'roadsense-pitch-review';OUT.mkdir(parents=True,exist_ok=True)
PDF=HERE/'RoadSense-Pitch-Draft.pdf'
PITCH=HERE/'PITCH.md'
pitch_source=PITCH.read_text()
SOURCE='https://peraturan.bpk.go.id/Download/189970/Perpres%20Nomor%201%20Tahun%202022%20-%20Lampiran.pdf'
register_fonts();c=canvas.Canvas(str(PDF),pagesize=(WIDTH,HEIGHT),pageCompression=1)
c.setTitle('RoadSense - Learn to give way');c.setAuthor('We Will Handle')
c.setSubject('Six-minute pitch aligned with PITCH.md. Live demo 2:15-4:20; close by 5:45.')
d=Deck(c)
def source_link(y):
    d.text('Source: Perpres 1/2022, annex p. 1. Estimate uses 2020 GDP.',66,y,16,MUTED)
    c.linkURL(SOURCE,(64,HEIGHT-y-20,785,HEIGHT-y+3),relative=0,thickness=0)

# 01 / 0:00-0:35: open on the frustration, without an extra introduction.
d.page(INK);d.image(GENERATED/'road-crossing-hero.png',0,0,1280,720)
d.rect(0,0,740,720,INK,.48)
d.label('RoadSense',64,50,PAPER)
d.block(['We are frustrated.','They have a license.','But who gives way?'],64,105,59,PAPER,'Bold',72)
d.block(['Cutting into lanes.','Ignoring crossings.','Blocking junctions.'],68,370,28,PAPER,'Sans',43)
d.label('We Will Handle',68,604,PAPER,18)
d.rect(858,666,358,32,INK,.72);d.text('AI-generated concept artwork',875,675,13,PAPER);d.end()

# 02 / 0:35-1:25: preserve the editorial image/text split.
d.page();d.image(GENERATED/'pedestrian-decision.png',628,0,652,720,anchor=(.74,.5))
d.header('The testing gap')
d.block(['Practice for','real traffic'],64,143,62,INK,leading=72)
d.label('Vehicle control',68,345,RED,13)
d.text('Braking and turning',68,378,28,INK)
d.label('Road decisions',68,450,RED,13)
d.block(['Notice people. Know who goes first.','Give way when it matters.'],68,483,24,INK,'Sans',35)
d.block(['The 2023 reform replaced zig-zags','and figure-eight tracks.'],68,592,17,GRAY,'Sans',24)
d.label('We Will Handle',64,677,GRAY,11);d.label('02 / 08',498,677,GRAY,11)
d.rect(913,666,303,32,INK,.72);d.text('AI-generated concept artwork',928,675,13,PAPER);d.end()

# 03 / 1:25-1:45: the national loss, with its historical baseline visible.
d.page(INK);d.header('The national cost',MUTED)
d.text('Road crashes cost Indonesia',64,128,55,PAPER,'Bold')
d.text('About',68,240,25,MUTED)
d.text('Rp450',58,279,146,PAPER,'Bold')
d.text('trillion a year',68,450,53,PAPER,'Bold')
d.line(784,247,784,519,'#5E625A',1.5)
d.text('3%',844,275,113,RED,'Bold')
d.text('of GDP, roughly',850,414,26,PAPER)
d.text('2020 economic baseline',850,464,20,MUTED)
d.text('Hospital bills. Lost income. Lost lives.',68,558,27,PAPER)
source_link(622);d.footer(3,MUTED);d.end()

# 04 / 1:45-2:15: clearly distinguish the hypothetical saving from results.
d.page();d.header('The opportunity')
d.text('What a 1% reduction could mean',64,125,55,INK,'Bold')
d.text('Rp4.5',57,246,151,RED,'Bold')
d.text('trillion a year',65,422,52,INK,'Bold')
d.block(['We have not proven that impact yet.','It shows why better road','training matters.'],747,287,23,GRAY,'Sans',36)
d.line(66,524,1216,524,'#BBBEB5',1.5)
d.text('RoadSense',66,557,32,INK,'Bold')
d.text('A driving game for practising real road situations.',294,562,25,INK)
d.text('Calculation: 1% of the rounded Rp450 trillion baseline. Perpres 1/2022, annex p. 1.',66,626,16,GRAY)
c.linkURL(SOURCE,(64,HEIGHT-648,1215,HEIGHT-623),relative=0,thickness=0)
d.footer(4);d.end()

# 05 / 2:15-4:20: one live mission, no narration script on the slide.
d.page(RED);d.header('Live prototype',PAPER)
d.text('Live demo',64,128,61,PAPER,'Bold')
d.text('2:05',56,237,139,INK,'Bold')
d.text('Pasar Bunga',66,413,35,INK,'Bold')
d.block(['Pedestrian crossing','Junction priority'],67,477,27,PAPER,'Sans',37)
d.rect(520,126,696,428,INK)
d.image(ASSET/'gameplay-existing.png',520,126,696,428,crop=(274,335,738,600))
d.text('Earlier prototype capture; scene detail only',521,573,16,PAPER)
d.text('Mistake. Explanation. Example. Retry.',521,608,26,PAPER)
d.footer(5,PAPER);d.end()

# 06 / 4:20-4:50: Codex contribution, without invented task screenshots.
d.page(INK);d.header('Deep Domain AI',MUTED)
d.text('Built with Codex',64,127,65,PAPER,'Bold')
d.text('Indonesian traffic rules become situations people can practise.',67,220,28,MUTED)
for x,num,title,body in [
    (66,'01','Driving game',['Road situations','and vehicle actions.']),
    (367,'02','Rule checks',['Check the action','against the rule.']),
    (668,'03','Explanations',['What went wrong','and what to do.']),
    (969,'04','Tests',['Check the result','stays consistent.'])]:
    d.text(num,x,316,49,RED,'Bold');d.text(title,x,398,30,PAPER,'Bold')
    d.block(body,x,451,23,MUTED,'Sans',32)
d.line(66,550,1216,550,'#5E625A',1.5)
d.text('The game labels legal rules and safety advice separately.',67,588,28,PAPER)
d.footer(6,MUTED);d.end()

# 07 / 4:50-5:35: both adoption paths are proposals, with a pilot first.
d.page();d.header('Proposed uses')
d.text('Where RoadSense could help',64,126,52,INK,'Bold')
d.label('01 / Licensing',68,253,RED,16)
d.text('Before the driving test',68,303,36,INK,'Bold')
d.block(['A required learning step,','if licensing authorities adopt it.'],68,364,25,GRAY,'Sans',36)
d.label('02 / Driver partners',699,253,RED,16)
d.text('Gojek or Grab',699,303,36,INK,'Bold')
d.block(['Training for new drivers.','Refresher lessons for existing drivers.'],699,364,25,GRAY,'Sans',36)
d.line(66,465,1216,465,'#BBBEB5',1.5)
d.label('03 / Driving School',68,487,RED,16)
d.text('First: a driving-school or driver-group pilot',68,525,31,INK,'Bold')
d.block(['Fewer repeat mistakes? Better decisions in new situations?',
         'Safer driving afterward?'],68,574,24,GRAY,'Sans',33)
d.text('Proposals for validation; no licensing or platform partnership is claimed.',68,649,16,GRAY)
d.footer(7);d.end()

# 08 / 5:35-5:45: use the exact closing line and hold through the buffer.
d.page(INK);d.image(GENERATED/'road-crossing-hero.png',0,0,1280,720)
d.rect(0,0,790,720,INK,.57)
d.label('RoadSense',64,50,PAPER)
d.block(['Learn to give way','before someone else','pays for your mistake.'],64,170,54,PAPER,'Bold',68)
d.block(['We are looking for a partner','to test this with real learners.'],68,441,27,PAPER,'Sans',37)
d.label('We Will Handle',68,603,PAPER,18)
d.rect(858,666,358,32,INK,.72);d.text('AI-generated concept artwork',875,675,13,PAPER);d.end();c.save()

# Keep slide notes tied to the exact text rather than maintaining a second script.
notes_header = """# RoadSense: slide cues and speaker notes

Use `PITCH.md` as the spoken script. The slide cues below follow that text.
The six-minute slot includes a 2:05 live demo and a 15-second buffer.

| Slide | Screen | Pitch clock | Duration |
|---|---|---|---|
| 1 | We are frustrated. They have a license. But who gives way? | 0:00-0:35 | 0:35 |
| 2 | Practice for real traffic | 0:35-1:25 | 0:50 |
| 3 | National crash costs | 1:25-1:45 | 0:20 |
| 4 | Illustrative 1% saving and RoadSense | 1:45-2:15 | 0:30 |
| 5 | Live demo; switch to the app | 2:15-4:20 | 2:05 |
| 6 | Built with Codex | 4:20-4:50 | 0:30 |
| 7 | Proposed uses and pilot | 4:50-5:35 | 0:45 |
| 8 | Closing | 5:35-5:45 | 0:10 |
| Hold 8 | Buffer | 5:45-6:00 | 0:15 |

## Presenter cues

- Slide 1: begin with the frustration in Jakarta. Do not add an introduction ahead of the script.
- Slide 2: move here at the sentence about passing a driving test. The old zig-zag/figure-eight course changed in 2023; do not describe it as the current test.
- Slide 3: pause after Rp450 trillion. Say that the estimate uses 2020 figures.
- Slide 4: 1% means lower national economic losses, not measured savings caused by this app. Introduce RoadSense before switching to the demo.
- Slide 5: use only the mission and emphasis points in the script below. Switch back by 4:20. The image is an earlier prototype crop, not the current tutorial or a record of a successful run.
- Slide 6: explain Codex's contribution. Prepare the genuine task capture and matching artifact requested in PITCH.md for display alongside this slide. The four text columns summarize the work; they are not an activity log.
- Slide 7: licensing prerequisites and Gojek/Grab training are proposed uses. The pilot asks whether learners repeat fewer mistakes and can apply a lesson elsewhere.
- Slide 8: say the exact closing line, finish by 5:45, and hold the slide.

## Demo rehearsal check

The sequence in PITCH.md calls for a deliberate pedestrian violation and manual retry. Current `../DEMO_TUTORIAL_SCENARIO.md` describes a guided crossing/junction tutorial that pauses, brakes automatically, and disables penalties during guidance. Verify the prepared build and entry route before presenting. Do not promise a deliberate failure in a mode that prevents it. If only the guided tutorial is available, identify it as guided practice and show feedback/retry separately only if actually available. This deck update does not change the game or establish a completed rehearsal.

## Slide sources

- Slides 1, 2 and 8: existing AI-generated concept artwork; source prompts remain in `assets/PROVENANCE.json`. The visible artwork labels distinguish illustrations from gameplay.
- Slide 2: Polri's 2023 test changes, linked in the preparation notes below.
- Slides 3 and 4: Perpres 1/2022, annex page 1, linked in the preparation notes below. The estimate is Rp448-478 trillion on 2020 GDP; the deck rounds to about Rp450 trillion. The hypothetical 1% saving uses the same baseline.
- Slide 5: `assets/gameplay-existing.png`, an earlier prototype capture cropped to its road/vehicle region. The proposed demo sequence comes from PITCH.md.
- Slides 6 and 7: contribution and proposal wording from PITCH.md. No new test result, partnership, or learner outcome is asserted.

## Full pitch and preparation notes

The following text is copied from PITCH.md by build_deck.py so these notes match the current script.

"""
(HERE/'SPEAKER_NOTES.md').write_text(notes_header+pitch_source)
# Render actual PDF pages; review artifacts remain outside the source folder.
doc=fitz.open(PDF);thumbs=[];report={'pages':len(doc),'page_size':[WIDTH,HEIGHT],
    'pitch_sha256':hashlib.sha256(pitch_source.encode()).hexdigest(),
    'demo_seconds':125,'planned_finish_seconds':345,'buffer_seconds':15,
    'text_bounds_errors':[]}
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
