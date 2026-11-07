using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JalanKita
{
    public sealed partial class SopoDelEnvironment
    {
        readonly List<Rect> occupiedLots=new(),roadReservations=new();
        Material dryGround,shrubLeaves;
        Material[] grasses;
        SopoDelDressingStats dressing;

        void PrepareDressing(Func<string,string,string,float,float,Material> mat)
        {
            dryGround=mat("Sopo uneven dry ground","SopoDel/withered_grass","bcb797",.12f,0);
            grasses=new[]{"62764b","84905a","929267","526543"}.Select((c,i)=>mat("Sopo wild grass "+i,null,c,.05f,0)).ToArray();
            foreach(var material in grasses)material.SetFloat("_Cull",0);
            shrubLeaves=mat("Sopo shrub foliage",null,"b1bc85",.08f,0);
            shrubLeaves.SetTexture("_BaseMap",Resources.Load<Texture2D>("Jaksa/leaves"));
            shrubLeaves.SetFloat("_AlphaClip",1);shrubLeaves.SetFloat("_Cutoff",.45f);
            shrubLeaves.SetFloat("_Cull",0);shrubLeaves.EnableKeyword("_ALPHATEST_ON");shrubLeaves.renderQueue=2450;
            foreach(var b in data.buildings)
                occupiedLots.Add(Rect.MinMaxRect(b.points.Min(p=>p.x)-2,b.points.Min(p=>p.y)-2,b.points.Max(p=>p.x)+2,b.points.Max(p=>p.y)+2));
            foreach(var r in data.surroundingRoads)
                for(int i=1;i<r.points.Length;i++)
                {
                    var a=r.points[i-1];var b=r.points[i];float margin=Mathf.Max(1,r.width*.5f)+3;
                    roadReservations.Add(Rect.MinMaxRect(Mathf.Min(a.x,b.x)-margin,Mathf.Min(a.y,b.y)-margin,
                        Mathf.Max(a.x,b.x)+margin,Mathf.Max(a.y,b.y)+margin));
                }
            // The visual road extends beyond the playable boundary; reserve that continuation too.
            for(float z=-30;z<365;z+=5)
                roadReservations.Add(Rect.MinMaxRect(Mathf.Min(roadX(z),roadX(z+5))-6,z,
                    Mathf.Max(roadX(z),roadX(z+5))+6,z+5));
            dressing=root.gameObject.AddComponent<SopoDelDressingStats>();
        }

        bool OfficeForecourt(Rect lot)=>lot.yMax>101&&lot.yMin<247&&lot.xMax>roadX(lot.center.y)+4&&lot.xMin<190;
        bool FreeLot(Rect lot)
        {
            if(OfficeForecourt(lot))return false;
            return !occupiedLots.Any(b=>b.Overlaps(lot))&&!roadReservations.Any(r=>r.Overlaps(lot));
        }

        void FillVacantLand()
        {
            // Authored infill intentionally fills unmapped gaps, while mapped towers/roads keep their positions.
            // Prioritize the street view, then close the distant skyline around the finite dataset.
            var candidates=new List<Rect>();
            for(float z=-80;z<430;z+=22)
                for(float x=-225;x<280;x+=22)
                    candidates.Add(new Rect(x+Rand(-2,2),z+Rand(-2,2),Rand(11,18),Rand(12,19)));
            foreach(var lot in candidates.OrderBy(r=>Mathf.Abs(r.center.x-roadX(r.center.y))+.15f*Mathf.Abs(r.center.y-125)))
            {
                if(dressing.InfillBuildings>=125)break;
                if(!FreeLot(lot))continue;
                int floors=Rand(0,1)<.7f?random.Next(2,5):random.Next(5,9);
                InfillBuilding(lot,floors);
                occupiedLots.Add(new Rect(lot.x-2,lot.y-2,lot.width+4,lot.height+4));
                dressing.InfillLots.Add(lot);dressing.InfillBuildings++;
            }
            // Vegetation is deliberately irregular: dry patches, clumps, low shrubs and taller trees.
            for(float z=-30;z<365;z+=1.6f)
                for(float offset=-105;offset<106;offset+=1.6f)
                {
                    float x=roadX(z)+offset+Rand(-.75f,.75f),zz=z+Rand(-.75f,.75f);
                    var lot=new Rect(x-.55f,zz-.55f,1.1f,1.1f);
                    if(!FreeLot(lot))continue;
                    Vector3 p=new(x,.045f,zz);
                    Grass(p,Rand(.3f,.85f));dressing.GrassClumps++;
                    float choice=Rand(0,1);
                    if(choice<.006f&&Mathf.Abs(offset)>9)
                    {
                        tree(p,Rand(6,10));dressing.AddedTrees++;
                    }
                    else if(choice<.1f)Shrub(p,Rand(.7f,1.5f));
                    if(choice>.96f)mesh.Box(pavement,p+Vector3.up*.07f,new Vector3(Rand(.15f,.4f),.16f,Rand(.15f,.4f)),Quaternion.Euler(0,Rand(0,180),0));
                }
            for(float z=-10;z<350;z+=12)
                foreach(int side in new[]{-1,1})
                {
                    float x=roadX(z)+side*Rand(9,16);
                    var lot=new Rect(x-1.8f,z-2.5f,3.6f,5);
                    if(!FreeLot(lot))continue;
                    mesh.Box(dryGround,new Vector3(x,-.006f,z),new Vector3(3.6f,.025f,5),repeat:2.5f);
                    for(int i=0;i<18;i++)Grass(new Vector3(x+Rand(-1.65f,1.65f),.03f,z+Rand(-2.4f,2.4f)),Rand(.2f,.6f));
                }
            Debug.Log($"Sopo Del dressing: {dressing.InfillBuildings} infill buildings, {dressing.GrassClumps} grass clumps, {dressing.AddedTrees} additional trees.");
        }

        void InfillBuilding(Rect lot,int floors)
        {
            var points=new[]{new Vector2(lot.xMin,lot.yMin),new Vector2(lot.xMax,lot.yMin),
                new Vector2(lot.xMax,lot.yMax),new Vector2(lot.xMin,lot.yMax)};
            ContextBuilding(new JaksaStreet.Building {name="",id="authored-infill",points=points,levels=floors});
            float h=floors*3.5f;
            mesh.Box(white,new Vector3(lot.center.x,h+.15f,lot.center.y),new Vector3(lot.width+.3f,.3f,lot.height+.3f));
            mesh.Box(stone,new Vector3(lot.center.x,h+.65f,lot.center.y),new Vector3(3.3f,1,2.5f));
            mesh.Tube(silver,new Vector3(lot.center.x+2,h,lot.center.y),new Vector3(lot.center.x+2,h+2.6f,lot.center.y),.035f);
            if(Mathf.Abs(lot.center.x-roadX(lot.center.y))>85)return;
            int side=lot.center.x>roadX(lot.center.y)?-1:1;
            float face=side<0?lot.xMin:lot.xMax;
            Vector3 n=Vector3.right*side;
            mesh.Box(pavement,new Vector3(face+side*.7f,.08f,lot.center.y),new Vector3(1.4f,.16f,lot.height));
            mesh.Box(contextWalls[random.Next(contextWalls.Length)],new Vector3(face+side*.9f,2.9f,lot.center.y),new Vector3(2,.15f,lot.height+.3f),Quaternion.Euler(0,0,side*4));
            for(int j=0;j<3;j++)
            {
                float z=Mathf.Lerp(lot.yMin+2,lot.yMax-2,j/2f);
                mesh.Box(glazing[1],new Vector3(face+side*.06f,1.4f,z),new Vector3(.1f,2.6f,2.4f));
                mesh.Box(silver,new Vector3(face+side*.14f,1.4f,z),new Vector3(.1f,2.7f,.06f));
                mesh.Box(white,new Vector3(face+side*.2f,5.4f,z),new Vector3(.55f,.55f,.85f));
            }
            if(floors<=4)
            {
                mesh.Box(dark,new Vector3(face+side*.12f,3.45f,lot.center.y),new Vector3(.12f,.72f,5));
                Text(new[]{"COFFEE","GENERAL STORE","LAUNDRY","LOCAL SHOP"}[random.Next(4)],new Vector3(face+side*.18f,3.45f,lot.center.y),n,.17f,Color.white);
            }
        }

        void Grass(Vector3 p,float height)
        {
            for(int i=0;i<9;i++)
            {
                float a=Rand(0,Mathf.PI*2),h=height*Rand(.55f,1),w=Rand(.025f,.065f);
                Vector3 across=new(Mathf.Cos(a)*w,0,Mathf.Sin(a)*w);
                Vector3 bottom=p+new Vector3(Rand(-.3f,.3f),0,Rand(-.3f,.3f));
                Vector3 tip=bottom+new Vector3(Mathf.Sin(a)*h*.27f,h,Mathf.Cos(a)*h*.27f);
                mesh.Quad(grasses[random.Next(grasses.Length)],bottom-across,bottom+across,tip,tip);
            }
        }

        void Shrub(Vector3 p,float size)
        {
            for(int i=0;i<7;i++)
            {
                Quaternion rotation=Quaternion.Euler(Rand(-12,12),i*51,0);
                Vector3 u=rotation*Vector3.right*size*.65f,v=Vector3.up*size*.5f;
                Vector3 c=p+new Vector3(Rand(-.3f,.3f),size*.45f,Rand(-.3f,.3f));
                mesh.Quad(shrubLeaves,c-u-v,c+u-v,c+u+v,c-u+v,1,new Rect(0,0,1,1));
            }
        }

        void ForecourtDetails()
        {
            foreach(float z in new[]{119f,170f,195f})
            {
                float x=roadX(z)+12;
                mesh.Box(stone,new Vector3(x,.25f,z),new Vector3(.6f,.5f,2.6f));
                mesh.Box(contextWalls[0],new Vector3(x,.53f,z),new Vector3(.7f,.13f,2.7f));
                mesh.Box(contextWalls[0],new Vector3(x+.28f,.9f,z),new Vector3(.12f,.7f,2.7f));
                mesh.Box(dark,new Vector3(x-1.2f,.52f,z+2),new Vector3(.55f,1.04f,.55f));
                mesh.Box(silver,new Vector3(x-1.2f,1.07f,z+2),new Vector3(.61f,.08f,.61f));
            }
            foreach(float z in new[]{112f,158f,207f})
            {
                float x=roadX(z)+5.55f;
                mesh.Tube(silver,new Vector3(x,.24f,z),new Vector3(x,1.1f,z),.07f,sides:8);
                mesh.Box(paint,new Vector3(x,.95f,z),new Vector3(.17f,.06f,.17f));
            }
            // These offices' small service details are approximate, like the added vegetation.
            mesh.Box(stone,new Vector3(roadX(83)-7.5f,.65f,83),new Vector3(1.2f,1.3f,.7f));
            for(int i=0;i<7;i++)mesh.Box(silver,new Vector3(roadX(83)-6.89f,.35f+i*.12f,83),new Vector3(.03f,.035f,.5f));
        }
    }

    // Scene diagnostics allow integrated checks to verify decorative infill stays off the driving corridor.
    public sealed class SopoDelDressingStats : MonoBehaviour
    {
        public int InfillBuildings,GrassClumps,AddedTrees;
        public readonly List<Rect> InfillLots=new();
    }
}
