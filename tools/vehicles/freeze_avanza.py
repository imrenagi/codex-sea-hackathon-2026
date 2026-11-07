"""Freeze a source-only Avanza visual handoff; never writes under Unity."""
import difflib, hashlib, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'assets/vehicles/avanza_inspired'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return str(p.relative_to(ROOT))
mat=ROOT/'Unity/Assets/Resources/Traffic/Materials/family_mpv_0.mat'
s=mat.read_text()
for key in ['_BaseColor','_Color']:
    s,n=re.subn(r'('+re.escape('- '+key)+r': )\{[^\n]*\}',r'\g<1>{r: 0.77254903, g: 0.7921569, b: 0.8039216, a: 1}',s)
    assert n==1,key
(OUT/'unity/family_mpv_0.mat').write_text(s)
cat=ROOT/'Unity/Assets/Scripts/TrafficCatalogue.cs';old=cat.read_text()
lines=old.splitlines(keepends=True);changed=[]
for line in lines:
    if 'new("family_mpv"' in line:
        assert '"DCE1DD"' in line or '"C5CACD"' in line
        line=line.replace('"DCE1DD"','"C5CACD"')
    changed.append(line)
new=''.join(changed)
patch=''.join(difflib.unified_diff(old.splitlines(keepends=True),new.splitlines(keepends=True),fromfile='a/'+rel(cat),tofile='b/'+rel(cat)))
(OUT/'unity/TrafficCatalogue.silver.patch').write_text(patch)
# Preserve the replacement when the original pack's Unity derivation is rerun.
prepare=ROOT/'tools/traffic/prepare.py';prepare_old=prepare.read_text()
anchor="    dest=OUT/spec['id'];dest.mkdir(exist_ok=True)\n"
replacement=anchor+"""    authored=ROOT/'assets/vehicles/avanza_inspired'
    if spec['id']=='family_mpv' and (authored/'asset_stats.json').exists():
        # Keep the authored Avanza visual while preserving the original source pack.
        stats=json.loads((authored/'asset_stats.json').read_text())
        for lod,fn in [(0,'Body.fbx'),(1,'Body_LOD1.fbx')]:
            shutil.copy2(authored/'unity'/fn,dest/fn)
            report.append(dict(model=spec['id'],lod=lod,triangles=stats['lods'][lod]['triangles']))
        continue
"""
assert prepare_old.count(anchor)==1
prepare_new=prepare_old if "authored=ROOT/'assets/vehicles/avanza_inspired'" in prepare_old else prepare_old.replace(anchor,replacement)
prepare_patch=''.join(difflib.unified_diff(prepare_old.splitlines(keepends=True),prepare_new.splitlines(keepends=True),fromfile='a/'+rel(prepare),tofile='b/'+rel(prepare)))
(OUT/'unity/TrafficPrepare.avanza.patch').write_text(prepare_patch)
replace=[]
for src,dst in [('Body.fbx','family_mpv/Body.fbx'),('Body_LOD1.fbx','family_mpv/Body_LOD1.fbx'),('family_mpv_0.mat','Materials/family_mpv_0.mat')]:
    source=OUT/'unity'/src;target=ROOT/'Unity/Assets/Resources/Traffic'/dst;meta=Path(str(target)+'.meta')
    replace.append(dict(source=rel(source),target=rel(target),source_sha256=sha(source),baseline_sha256=sha(target),preserve_meta=rel(meta),meta_sha256=sha(meta)))
deps=[]
for p in sorted((ROOT/'Unity/Assets/Resources/Traffic/Materials').glob('*')):
    if p.is_file() and (p.name.startswith('family_mpv_') or p.stem.split('.')[0] in ['Atlas','HeadOn','TailOn','BrakeOn','IndicatorOn']):deps.append(p)
deps+=sorted(p for p in (ROOT/'Unity/Assets/Resources/Traffic/Textures').glob('*') if p.is_file())
for n in ['TrafficVehicleView','PlayerVehicleView','TrafficCatalogue','VehicleCatalogue']:
    for ext in ['.cs','.cs.meta']:deps.append(ROOT/'Unity/Assets/Scripts'/(n+ext))
deps += [ROOT/'assets/vehicles/indonesia_traffic_pack/build.py']
files=[p for p in OUT.rglob('*') if p.is_file() and p.name!='handoff_manifest.json']
files += [ROOT/'tools/vehicles'/n for n in ['build_avanza.py','inspect_avanza.py','freeze_avanza.py']]
manifest=dict(schema_version=1,state='frozen',label='Avanza-inspired MPV',scope='Visual geometry and default paint only; no Unity execution',replacements=replace,
    patch=dict(file=rel(OUT/'unity/TrafficCatalogue.silver.patch'),target=rel(cat),baseline_sha256=sha(cat),result_sha256=hashlib.sha256(new.encode()).hexdigest(),change='family_mpv first paint DCE1DD -> C5CACD only'),
    generation_patch=dict(file=rel(OUT/'unity/TrafficPrepare.avanza.patch'),target=rel(prepare),baseline_sha256=sha(prepare),result_sha256=hashlib.sha256(prepare_new.encode()).hexdigest(),change='Prefer authored family_mpv FBXs during traffic regeneration'),
    preserved_contract=dict(selection_id='mpv',resource_id='family_mpv',wheelbase_metres=2.75,wheel_radius_metres=.32,renderer_count=11,materials=['Traffic_Atlas_PBR','Body_Paint'],runtime_textures='existing shared traffic atlas; standalone textures are authoring/export only'),
    dependencies=[dict(path=rel(p),sha256=sha(p),action='preserve') for p in deps],
    new_files=[dict(path=rel(p),sha256=sha(p),bytes=p.stat().st_size,baseline='absent') for p in sorted(files)])
(OUT/'handoff_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('AVANZA_HANDOFF_FROZEN',len(files),'files',len(deps),'preserved dependencies')
