using System.Collections.Generic;
using UnityEngine;
using UjianSim.Game;
using static JalanKita.RouteStage;

namespace JalanKita
{
    public sealed partial class District
    {
        readonly List<(int stage, int arm, string prefix, Dictionary<string, Material> mats)> boulevardLights = new();
        readonly List<(Transform arm, Quaternion rest, Renderer left, Renderer right)> barriers = new();
        readonly List<GameObject> train = new();
        GameObject boulevardWalker, ambulance, angkot;
        Renderer[] ambulanceRenderers;

        void BuildBoulevard()
        {
            SurfaceBox("Tanah distrik besar", new Vector3(-90, -.55f, 760), new Vector3(570, .6f, 820), "Grass");
            BigRoad(new Vector2(0, 390), new Vector2(0, 710), new[] { 480f, 640f });
            BigRoad(new Vector2(-85, 480), new Vector2(85, 480), new[] { 0f });
            BigRoad(new Vector2(-260, 640), new Vector2(85, 640), new[] { -180f, 0f });
            BigRoad(new Vector2(-180, 565), new Vector2(-180, 1165), new[] { 640f, 800f });
            for (int stage = 5; stage <= 7; stage++) BuildBigJunction(stage);
            for (int i = 5; i < All.Length; i++)
            {
                Vector2 d = new(Mathf.Sin(All[i].Heading*Mathf.Deg2Rad), Mathf.Cos(All[i].Heading*Mathf.Deg2Rad));
                Vector2 r = new(d.y, -d.x);
                for (int stripe = -2; stripe <= 2; stripe++)
                    RoadPaint("Checkpoint " + (i+1), All[i].Checkpoint + r * stripe * .6f, r, .3f, .35f, "2fae64");
            }
            Model("Sign_Speed50", new Vector3(-12.4f, .12f, 420));
            Model("Sign_Speed50", new Vector3(-192.4f, .12f, 855));
            StreetName(-12.5f, 435, "thamrin");
            StreetName(-75, 626.5f, "diponegoro");
            StreetName(-192.5f, 730, "ahmadyani");
            BuildRailway();
            boulevardWalker = Model("Person", Vector3.zero, 0, actors);
            ambulance = Model("Ambulance", Vector3.zero, 180, actors);
            ambulanceRenderers = ambulance.GetComponentsInChildren<Renderer>();
            angkot = Model("Angkot", new Vector3(-189, 0, 914), 180, actors);
        }

        void BigRoad(Vector2 start, Vector2 end, float[] junctions)
        {
            bool vertical = Mathf.Abs(end.y - start.y) > Mathf.Abs(end.x - start.x);
            Vector2 d = (end-start).normalized, r = new(d.y, -d.x);
            float length = Vector2.Distance(start,end);
            var middle = (start+end)*.5f;
            SurfaceBox("Jalan besar 3 lajur tiap arah", World(middle) + Vector3.down * .042f,
                vertical ? new Vector3(21.6f,.08f,length) : new Vector3(length,.08f,21.6f), "Asphalt");
            for (float along = 2; along < length - 2; along += 4)
            {
                var p = start+d*along;
                float coordinate = vertical ? p.y : p.x;
                bool near = false;
                foreach (float junction in junctions) if (Mathf.Abs(coordinate-junction) < 17) near = true;
                if (near) continue;
                RoadPaint("Garis tengah",p,d,4,.16f,"e9e9e2");
                foreach (int side in Side)
                {
                    RoadPaint("Garis tepi",p+r*side*10.5f,d,4,.12f,"e9e9e2");
                    for (int lane = 1; lane <= 2; lane++)
                    {
                        bool solid = false;
                        foreach (float j in junctions)
                            if (Mathf.Abs(coordinate-j) < 56) solid = true;
                        RoadPaint(solid ? "Marka utuh" : "Marka putus", p+r*side*lane*3.6f,d,solid ? 4 : 2,.12f,"e9e9e2");
                    }
                    var curb = p+r*side*10.9f;
                    SurfaceBox("Trotoar jalan besar",World(p+r*side*12.2f)+Vector3.up*.03f,
                        vertical ? new Vector3(2.4f,.18f,4) : new Vector3(4,.18f,2.4f),"Paving");
                    for (int k = 0; k < 4; k++)
                    {
                        var tile = curb + d*(k-1.5f);
                        Box("Kanstin",World(tile)+Vector3.up*.06f,
                            vertical ? new Vector3(.22f,.22f,1) : new Vector3(1,.22f,.22f), k%2==0 ? "eeeeea" : "25272b");
                    }
                    if ((int)along % 28 == 2)
                    {
                        Model("StreetLamp",World(p+r*side*12),vertical ? side==1 ? 90 : -90 : side==1 ? 0 : 180);
                        Model("Tree_Palm",World(p+r*side*13.3f),along*17);
                    }
                    if ((int)along % 8 == 2)
                    {
                        // Keep the rail corridor and junction sight lines clear.
                        if (vertical && Mathf.Abs(p.y-800)<30) continue;
                        float facing = vertical ? side==1 ? 90 : -90 : side==1 ? 180 : 0;
                        var shop = Model(Shops[(int)along/8%4],World(p+r*side*15),facing);
                        shop.transform.localScale = new Vector3(7.8f/4.5f,1,.85f);
                    }
                }
            }
        }

        void RoadPaint(string name, Vector2 point, Vector2 direction, float length, float width, string color)
        {
            var painted = Box(name,World(point)+Vector3.up*.014f,new Vector3(width,.018f,length),color);
            painted.transform.rotation = Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg,0);
        }

        void BuildBigJunction(int stage)
        {
            var center = Boulevard.Center(stage);
            SurfaceBox("Permukaan persimpangan",World(center)+Vector3.down*.039f,new Vector3(34,.08f,34),"Asphalt");
            for (int arm = 0; arm < 4; arm++)
            {
                float heading = (stage == 7 ? 270 : 0) + arm*90;
                Vector2 d = new(Mathf.Sin(heading*Mathf.Deg2Rad),Mathf.Cos(heading*Mathf.Deg2Rad));
                Vector2 r = new(d.y,-d.x);
                Vector2 W(float u,float v) => center+r*u+d*v;
                RoadPaint("Garis henti mobil",W(-5.4f,-26),r,10.6f,.3f,"e9e9e2");
                RoadPaint("Ruang henti motor",W(-5.4f,-22),d,7.6f,10.5f,"a83230");
                RoadPaint("Garis henti motor",W(-5.4f,-18),r,10.6f,.3f,"e9e9e2");
                for (int k = -10; k <= 10; k++) RoadPaint("Zebra cross",W(k,-14),d,3,.5f,"e9e9e2");
                string[] arrows = { "LaneArrow_Left","LaneArrow_Straight","LaneArrow_Right" };
                for (int lane = 0; lane < 3; lane++)
                {
                    Model(arrows[lane],World(W(-9+lane*3.6f,-39))+Vector3.up*.025f,heading+180);
                    Model("RoadSymbol_Motorcycle",World(W(-9+lane*3.6f,-22))+Vector3.up*.03f,heading+180);
                }
                // Reuse the original lens materials and protected-right arrow model.
                SignalModel(stage, arm, "TrafficLight", "M_Light_", W(-11.8f,-18), heading);
                SignalModel(stage, arm, "TrafficLightArrow", "M_Arrow_", W(-.55f,-18), heading);
                SignalModel(stage, arm, "PedLight", "M_Ped_", W(-12,-12), heading+90);
                Model("Plate_LeftFollowLight",World(W(-11.8f,-18))+Vector3.up*2.7f,heading);
            }
        }

        void SignalModel(int stage,int arm,string model,string prefix,Vector2 pos,float heading)
        {
            var go = Models.Spawn(model,actors,out var mats);
            go.transform.SetPositionAndRotation(World(pos),Quaternion.Euler(0,heading+180,0));
            boulevardLights.Add((stage,arm,prefix,mats));
        }

        void BuildRailway()
        {
            SurfaceBox("Ballast rel",new Vector3(-180,-.02f,800),new Vector3(240,.1f,4.6f),"Paving");
            for (int x = -300; x <= -60; x++)
                Box("Bantalan rel",new Vector3(x,.045f,800),new Vector3(.22f,.08f,2.1f),"5a4635");
            foreach (float z in new[] { 799.3f,800.7f })
                Box("Rel baja",new Vector3(-180,.11f,z),new Vector3(240,.12f,.07f),"8f8a85");
            RoadPaint("Garis henti kereta",new Vector2(-185.4f,790),Vector2.right,10.6f,.3f,"e9e9e2");
            RoadPaint("Garis henti kereta",new Vector2(-174.6f,810),Vector2.right,10.6f,.3f,"e9e9e2");
            Model("Sign_RailCross",new Vector3(-192.5f,.12f,765));
            foreach (int side in Side)
            {
                var go = Models.Spawn("RailCrossing",actors);
                go.transform.SetPositionAndRotation(new Vector3(-180+side*11.5f,.1f,800+side*7),Quaternion.Euler(0,side<0?180:0,0));
                go.transform.localScale = new Vector3(-2.1f,1,1);
                var arm = Models.Find(go.transform,"Barrier_Arm");
                var left = Models.Find(go.transform,"Lamp_L")?.GetComponent<Renderer>();
                var right = Models.Find(go.transform,"Lamp_R")?.GetComponent<Renderer>();
                Models.OwnMaterials(left); Models.OwnMaterials(right);
                barriers.Add((arm,arm ? arm.localRotation : Quaternion.identity,left,right));
            }
            for (int i=0;i<4;i++) train.Add(Model(i==0?"Train_Loco":"Train_Coach",Vector3.zero,0,actors));
        }

        void PresentBoulevard(Journey run, bool driving)
        {
            Pose(boulevardWalker,run.BoulevardPedestrian,run.BoulevardPedestrianHeading);
            Pose(ambulance,run.Ambulance,0);
            bool flash = Time.unscaledTime % .8f < .4f;
            foreach (var item in boulevardLights)
            {
                int phase = item.stage == run.Stage ? item.prefix == "M_Arrow_" ? run.ArrowPhase
                    : item.prefix == "M_Ped_" ? run.BoulevardPedestrian.HasValue ? 2 : 0 : run.MainPhase : 0;
                if (item.arm % 2 == 1 && item.stage == run.Stage)
                    phase = item.prefix == "M_Light_" && run.MainPhase == 0 && run.ArrowPhase == 0 ? 2 : 0;
                string[] colors = { "Red","Amber","Green" };
                for (int i=0;i<3;i++) if (item.mats.TryGetValue(item.prefix+colors[i],out var mat))
                    MaterialLibrary.SetGlow(mat,phase==i?3:0);
            }
            foreach (var barrier in barriers)
            {
                if (barrier.arm) barrier.arm.localRotation=barrier.rest*Quaternion.Euler(0,0,(1-run.BarrierDown)*82);
                if (barrier.left) MaterialLibrary.SetGlow(barrier.left.sharedMaterial,run.RailWarning&&flash?2.5f:0);
                if (barrier.right) MaterialLibrary.SetGlow(barrier.right.sharedMaterial,run.RailWarning&&!flash?2.5f:0);
            }
            for (int i=0;i<train.Count;i++)
            {
                train[i].SetActive(run.TrainVisible);
                train[i].transform.SetPositionAndRotation(new Vector3(run.TrainX-run.TrainDirection*(9+i*18),0,800),
                    Quaternion.Euler(0,run.TrainDirection>0?-90:90,0));
            }
            foreach (var renderer in ambulanceRenderers)
                foreach (var mat in renderer.sharedMaterials)
                {
                    string name=MaterialLibrary.BaseName(mat.name);
                    if (name=="M_Siren_Red" || name=="M_Siren_Blue")
                        MaterialLibrary.SetGlow(mat,(name=="M_Siren_Red"?flash:!flash)?2.5f:0);
                }
        }
    }
}
