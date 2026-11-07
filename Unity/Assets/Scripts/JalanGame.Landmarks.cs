using UnityEngine;
using UnityEngine.UIElements;
namespace JalanKita
{
    public sealed partial class JalanGame
    {
        LandmarkDrive landmark;
        readonly LandmarkDrive[] landmarkScenes=new LandmarkDrive[2];
        JakartaPlace? landmarkPlace;
        void ShowLandmarks()
        {
            CloseCityPreview();SetPage(Page.Locations);
            AddLabel(dialog,"JELAJAH JAKARTA","kicker");AddLabel(dialog,"Pilih lokasi","heading");
            AddLabel(dialog,"Gunakan kendaraan pilihanmu untuk menjelajahi tiga lokasi Jakarta. C mengganti kamera; di HI dan Monas tersedia sudut pandang ke landmark.","copy");
            AddButton(dialog,"Sopo Del · kantor Sea  /  ruas 250 m",StartJaksa,"option primary");
            AddButton(dialog,"Bundaran HI · Monumen Selamat Datang  /  jalan melingkar",()=>StartLandmark(JakartaPlace.BundaranHI),"option primary");
            AddButton(dialog,"Monas · taman dan jalan Medan Merdeka",()=>StartLandmark(JakartaPlace.Monas),"option primary");
            AddLabel(dialog,"Jalan dan tapak bangunan berdasarkan data peta. Fasad, vegetasi, detail patung, dan sebagian dimensi jalan merupakan perkiraan untuk permainan.","rule-source");
            AddButton(Element(dialog,"row actions"),"Kembali",ShowMenu,"quiet");
        }
        void StartLandmark(JakartaPlace place)
        {
            CloseCityPreview();pitchDemo=false;learner=null;practiceStart=-1;
            trainingRoot.SetActive(false);if(jaksa!=null)jaksa.Street.Root.SetActive(false);
            if(landmark!=null)landmark.Environment.Root.SetActive(false);
            landmark=landmarkScenes[(int)place]??=new LandmarkDrive(transform,place);
            landmarkPlace=place;
            landmark.Reset(mode);progress.text="Jeda / menu  [Esc]";
            root.Q<UnityEngine.UIElements.Label>(className:"controls").text="W / ↑ Gas    S / Spasi Rem    A / D Setir    Q / E Sein    C Kamera    Z / X Lihat    R Ulang    Esc Jeda";
            SetPage(Page.Landmark);Show(root.Q("steps"),false);Show(phone,false);Show(toast,false);Show(root.Q("pickup"),false);
        }
        bool LandmarkInput()
        {
            if(page==Page.LandmarkPause){if(Input.GetKeyDown(KeyCode.Escape))SetPage(Page.Landmark);return true;}
            if(page!=Page.Landmark)return false;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Tab)){ShowLandmarkPause();return true;}
            if(Input.GetKeyDown(KeyCode.C))landmark.ToggleCamera();
            if(Input.GetKeyDown(KeyCode.Q))landmark.ToggleIndicator(-1);
            if(Input.GetKeyDown(KeyCode.E))landmark.ToggleIndicator(1);
            if(Input.GetKeyDown(KeyCode.L))landmark.ToggleHeadlight();
            if(Input.GetKeyDown(KeyCode.R))landmark.Reset(mode);return true;
        }
        void LandmarkFixedUpdate()
        {
            float turn=(Pressed(KeyCode.D,KeyCode.RightArrow)?1:0)-(Pressed(KeyCode.A,KeyCode.LeftArrow)?1:0);
            landmark.Step(Time.fixedDeltaTime,Pressed(KeyCode.W,KeyCode.UpArrow),Pressed(KeyCode.S,KeyCode.DownArrow)||Input.GetKey(KeyCode.Space),turn);
        }
        void RefreshLandmarkHud()
        {
            location.text="JAKARTA PUSAT  /  "+(landmarkPlace==JakartaPlace.BundaranHI?"THAMRIN":"MEDAN MERDEKA");title.text=landmark.Environment.Name;
            instruction.text=landmark.TrafficContact?"Jaga jarak. Tunggu ruang aman di depan.":landmark.CurbContact?"Tepi jalan. Arahkan kembali ke lajur.":"Ikuti arah jalan melingkar · C untuk kamera pengemudi atau landmark";
            detail.text=Mathf.RoundToInt(landmark.Distance)+" M DIJELAJAHI  ·  KAMERA "+new[]{"BELAKANG","PENGEMUDI","LANDMARK"}[landmark.CameraMode];
            routeCount.text="JELAJAH BEBAS · "+Mathf.RoundToInt(landmark.Environment.Route.Length)+" M / PUTARAN";
            speed.text=Mathf.RoundToInt(landmark.Speed*3.6f).ToString("00");speed.EnableInClassList("speed-alert",false);
            bool blink=Time.unscaledTime%.7f<.42f;
            signalLeft.On=landmark.Indicator<0&&blink;signalRight.On=landmark.Indicator>0&&blink;headlight.On=mode==VehicleMode.Motorcycle&&landmark.Headlight;
            Show(root.Q("practiceHint"),false);
        }
        void ShowLandmarkPause()
        {
            SetPage(Page.LandmarkPause);AddLabel(dialog,landmark.Environment.Name.ToUpperInvariant(),"kicker");AddLabel(dialog,"Jeda penjelajahan","heading");
            AddLabel(dialog,"C mengganti kamera belakang, pengemudi, dan sudut pandang landmark. Ruas permainan mengikuti jalan melingkar; taman dan bagian luar jalan menjadi pemandangan.","copy");
            var actions=Element(dialog,"row actions");AddButton(actions,"Lanjutkan",()=>SetPage(Page.Landmark),"primary");
            AddButton(actions,"Ulangi lokasi",()=>{landmark.Reset(mode);SetPage(Page.Landmark);},"quiet");
            AddButton(actions,"Pindah lokasi",ShowLandmarks,"quiet");
            AddButton(actions,"Menu utama",ShowMenu,"quiet");
            AddLabel(dialog,"Data © OpenStreetMap contributors · ODbL. Material Poly Haven · CC0. Detail arsitektur dan prop adalah perkiraan.","rule-source");
        }
    }
}
