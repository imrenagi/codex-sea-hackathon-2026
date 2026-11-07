"""Original tileable road and wear maps generated mathematically, no reference pixels."""
import bpy,numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Unity/Assets/Resources/StreetLook/Surfaces';OUT.mkdir(parents=True,exist_ok=True)
n=1024;rng=np.random.default_rng(7143)
def noise(scale):
 a=rng.normal(size=(n,n));fy=np.fft.fftfreq(n)[:,None];fx=np.fft.fftfreq(n)[None,:]
 f=np.fft.ifft2(np.fft.fft2(a)*np.exp(-(fx*fx+fy*fy)*scale*scale)).real
 return (f-f.mean())/f.std()
def save(name,data,color=False):
 data=np.clip(data,0,1).astype(np.float32)
 if data.ndim==2:data=np.stack([data,data,data,np.ones_like(data)],axis=-1)
 if data.shape[-1]==3:data=np.concatenate([data,np.ones((n,n,1),dtype=np.float32)],axis=-1)
 # Byte image pixels are already display encoded; normal/mask channels stay linear.
 im=bpy.data.images.new(name,width=n,height=n,alpha=True,float_buffer=False)
 if not color:im.colorspace_settings.name='Non-Color'
 im.pixels.foreach_set(data.ravel());im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save();bpy.data.images.remove(im)
fine=noise(2.5);grain=noise(10);broad=noise(100)
h=.6*fine+.4*grain
v=(91+fine*5+grain*4+broad*3)/255
save('asphalt_albedo',np.stack([v*.97,v,v*1.015],axis=-1),True)
dx=(np.roll(h,-1,1)-np.roll(h,1,1))*.21;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*.21
nn=np.stack([-dx,-dy,np.ones_like(dx)],axis=-1);nn/=np.sqrt((nn*nn).sum(axis=-1,keepdims=True));save('asphalt_normal',nn*.5+.5)
save('asphalt_mask',np.stack([np.zeros_like(v),np.clip(.94-h*.025,.7,1),np.zeros_like(v),np.clip(.14+grain*.015,.08,.22)],axis=-1))
# Worn road paint: small irregular exposed aggregate, not repeated large cracks.
mask=(noise(5)+noise(38)*.45)>-2.0
rgba=np.ones((n,n,4));rgba[:,:,:3]=(.93+noise(5)[:,:,None]*.025);rgba[:,:,3]=mask.astype(float);save('marking_albedo',rgba,True)
# Plaster with subtle rain runoff, mottling and mineral deposit variation.
streak=noise(7);streak=np.mean(np.stack([np.roll(streak,i,0) for i in range(-100,101,10)]),axis=0);streak/=streak.std()
p=(177+noise(3)*3+noise(80)*5-streak*3)/255
save('weathered_plaster_albedo',np.stack([p,p*.98,p*.93],axis=-1),True)
h=noise(3)*.35+noise(28)*.2;dx=(np.roll(h,-1,1)-np.roll(h,1,1))*.18;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*.18
nn=np.stack([-dx,-dy,np.ones_like(dx)],axis=-1);nn/=np.sqrt((nn*nn).sum(axis=-1,keepdims=True));save('weathered_plaster_normal',nn*.5+.5)
print('ORIGINAL_SURFACES_READY')
