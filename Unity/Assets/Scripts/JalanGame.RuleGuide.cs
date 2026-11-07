using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        // Original vector artwork. See docs/design/RULE_GUIDE_ASSETS.md.
        sealed class RuleGuideIcon : VisualElement
        {
            readonly Rule rule;
            public RuleGuideIcon(Rule rule)
            {
                this.rule = rule;
                AddToClassList("rule-guide-icon");
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }
            void Draw(MeshGenerationContext context)
            {
                float size = Mathf.Min(contentRect.width, contentRect.height);
                if (size < 1) return;
                var p = context.painter2D;
                p.strokeColor = resolvedStyle.color;
                p.fillColor = resolvedStyle.color;
                p.lineWidth = size / 22f;
                p.lineCap = LineCap.Round;
                Vector2 V(float x, float y) => new Vector2(x * size, y * size);
                void Line(float x, float y, float a, float b) {
                    p.BeginPath(); p.MoveTo(V(x,y)); p.LineTo(V(a,b)); p.Stroke();
                }
                void Circle(float x,float y,float radius) {
                    p.BeginPath(); p.Arc(V(x,y),radius*size,0f,360f); p.Stroke();
                }
                void Box(float x,float y,float w,float h) {
                    p.BeginPath(); p.MoveTo(V(x,y)); p.LineTo(V(x+w,y)); p.LineTo(V(x+w,y+h));
                    p.LineTo(V(x,y+h)); p.ClosePath(); p.Stroke();
                }
                void Arrow(float x,float y,float a,float b) {
                    Line(x,y,a,b); var d = (V(x,y)-V(a,b)).normalized;
                    var side = new Vector2(-d.y,d.x);
                    p.BeginPath(); p.MoveTo(V(a,b)+d*size*.14f+side*size*.12f);
                    p.LineTo(V(a,b)); p.LineTo(V(a,b)+d*size*.14f-side*size*.12f); p.Stroke();
                }
                void Person(float x,float y) {
                    Circle(x,y,.075f); Line(x,y+.09f,x,y+.32f);
                    Line(x-.12f,y+.18f,x+.12f,y+.18f);
                    Line(x,y+.32f,x-.1f,y+.49f); Line(x,y+.32f,x+.1f,y+.49f);
                }
                void Car(float x,float y) { Box(x,y,.24f,.35f); Line(x+.03f,y+.09f,x+.21f,y+.09f); }
                switch(rule)
                {
                    case Rule.SeatBelt:
                        Circle(.5f,.2f,.09f); Line(.32f,.36f,.27f,.8f); Line(.68f,.36f,.73f,.8f);
                        Line(.32f,.36f,.68f,.36f); Line(.29f,.73f,.7f,.73f);
                        p.lineWidth=size/13f; Line(.73f,.29f,.32f,.76f); Box(.42f,.59f,.12f,.12f); break;
                    case Rule.Helmet:
                        p.BeginPath(); p.Arc(V(.5f,.46f),size*.31f,180f,360f); p.Stroke();
                        Line(.19f,.46f,.19f,.67f); Line(.19f,.67f,.5f,.75f);
                        Line(.81f,.46f,.55f,.46f); Line(.55f,.46f,.61f,.63f); Line(.61f,.63f,.8f,.63f);
                        Line(.5f,.75f,.61f,.63f); Line(.35f,.73f,.45f,.86f); Line(.45f,.86f,.65f,.77f); break;
                    case Rule.Headlight:
                        p.BeginPath(); p.Arc(V(.57f,.5f),size*.28f,270f,450f); p.ClosePath(); p.Stroke();
                        for(int i=0;i<3;i++) Line(.12f,.3f+i*.2f,.43f,.3f+i*.2f); break;
                    case Rule.Signal: case Rule.RightArrow:
                        Box(.32f,.12f,.36f,.76f); Circle(.5f,.29f,.075f); Circle(.5f,.5f,.075f);
                        if(rule==Rule.RightArrow) Arrow(.39f,.72f,.61f,.72f); else Circle(.5f,.71f,.075f); break;
                    case Rule.Stop:
                        p.BeginPath(); for(int i=0;i<8;i++) { float a=(22.5f+45*i)*Mathf.Deg2Rad; var v=V(.5f+.36f*Mathf.Cos(a),.5f+.36f*Mathf.Sin(a)); if(i==0)p.MoveTo(v); else p.LineTo(v); }
                        p.ClosePath(); p.Stroke(); Line(.32f,.5f,.68f,.5f); break;
                    case Rule.Pedestrian:
                        Person(.5f,.19f); for(int i=0;i<4;i++)Line(.18f+i*.21f,.82f,.27f+i*.21f,.82f); break;
                    case Rule.Indicator: case Rule.TurnSignal: case Rule.ReturnLeft:
                        Arrow(.46f,.37f,.14f,.37f); Arrow(.54f,.63f,.86f,.63f); break;
                    case Rule.NoEntry: case Rule.NoStopping: case Rule.Phone:
                        Circle(.5f,.5f,.36f);
                        if(rule==Rule.Phone) { Box(.38f,.27f,.24f,.45f); Line(.45f,.65f,.55f,.65f); }
                        else Line(.29f,.5f,.71f,.5f);
                        if(rule!=Rule.NoEntry) Line(.24f,.23f,.76f,.77f); break;
                    case Rule.Speed: case Rule.BoulevardSpeed:
                        p.BeginPath(); p.Arc(V(.5f,.57f),size*.34f,180f,360f); p.Stroke();
                        Line(.16f,.59f,.84f,.59f); Line(.5f,.57f,.66f,.32f); Circle(.5f,.57f,.04f);
                        Line(.23f,.4f,.29f,.44f); Line(.5f,.23f,.5f,.3f); break;
                    case Rule.Gap: case Rule.Collision:
                        Car(.16f,.32f); Car(.63f,.32f); Line(.45f,.42f,.58f,.42f); Line(.45f,.57f,.58f,.57f); break;
                    case Rule.PickupPlace: case Rule.PickupIncomplete:
                        Car(.16f,.4f); Person(.7f,.22f); Arrow(.66f,.84f,.32f,.84f); break;
                    case Rule.Railway:
                        Line(.2f,.16f,.2f,.86f); Line(.8f,.16f,.8f,.86f);
                        for(int i=0;i<4;i++)Line(.14f,.25f+i*.17f,.86f,.25f+i*.17f);
                        Line(.12f,.14f,.88f,.66f); break;
                    case Rule.Emergency:
                        Box(.16f,.33f,.68f,.42f); Circle(.3f,.77f,.075f); Circle(.72f,.77f,.075f);
                        Line(.5f,.42f,.5f,.63f); Line(.4f,.52f,.6f,.52f);
                        Line(.4f,.22f,.6f,.22f); Line(.5f,.1f,.5f,.14f); break;
                    case Rule.StopBox:
                        Box(.22f,.16f,.56f,.25f); Line(.13f,.48f,.87f,.48f); Car(.38f,.57f); break;
                    case Rule.Junction:
                        Line(.13f,.3f,.87f,.3f); Line(.13f,.51f,.38f,.51f); Line(.62f,.51f,.87f,.51f);
                        Line(.38f,.51f,.38f,.88f); Line(.62f,.51f,.62f,.88f); Arrow(.5f,.79f,.5f,.57f); break;
                    case Rule.KeepLeft: case Rule.MovementLane: case Rule.OffRoad:
                        Line(.15f,.16f,.15f,.85f); Line(.85f,.16f,.85f,.85f);
                        for(int i=0;i<3;i++)Line(.5f,.18f+i*.26f,.5f,.3f+i*.26f);
                        Arrow(.32f,.75f,.32f,.28f); break;
                }
            }
        }
    }
}
