using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        int[] practiceScoreBaseline;
        int completedPracticeStage = -1;
        Journey lastScoredJourney;
        int lastScoredStage = -1;
        Journey lastScoredTutorial;

        void BeginPracticeScore()
        {
            practiceScoreBaseline = (int[])journey.Save.mistakes.Clone();
            completedPracticeStage = -1;
        }
        void AddScoreSummary(VisualElement parent, int[] counts, int best)
        {
            int stars = PracticeRating.Stars(true, counts);
            var row = new VisualElement { name = "completionStars" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.Center;
            row.style.alignItems = Align.Center;
            row.style.width = Length.Percent(100);
            row.style.marginTop = 18;
            row.style.marginBottom = 24;
            row.tooltip = stars + "/3";
            for (int i = 0; i < 3; i++) row.Add(new CompletionStar(i < stars, i == 1));
            parent.Add(row);
        }

        // Vector artwork stays crisp and does not depend on a font's star glyph.
        sealed class CompletionStar : VisualElement
        {
            readonly bool earned;
            public CompletionStar(bool earned, bool centre)
            {
                this.earned = earned;
                pickingMode = PickingMode.Ignore;
                style.width = Length.Percent(30);
                style.maxWidth = centre ? 132 : 116;
                style.height = centre ? 142 : 126;
                style.flexShrink = 1;
                generateVisualContent += Draw;
            }
            void Draw(MeshGenerationContext context)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 1 || h < 1) return;
                var pen = context.painter2D;
                float radius = Mathf.Min(w, h) * .43f;
                var centre = new Vector2(w * .5f, h * .5f);
                var points = new Vector2[10];
                for (int i = 0; i < points.Length; i++)
                {
                    float a = (-90 + i * 36) * Mathf.Deg2Rad;
                    float r = radius * (i % 2 == 0 ? 1 : .46f);
                    points[i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                }
                pen.fillColor = new Color(0, 0, 0, .25f);
                pen.BeginPath();
                for (int i = 0; i < 10; i++)
                { var p = points[i] + new Vector2(0, 4); if (i == 0) pen.MoveTo(p); else pen.LineTo(p); }
                pen.ClosePath(); pen.Fill();
                for (int i = 0; i < 10; i++)
                {
                    pen.fillColor = earned
                        ? (i % 2 == 0 ? new Color(1f, .79f, .22f) : new Color(.90f, .56f, .08f))
                        : new Color(.22f, .25f, .28f);
                    pen.BeginPath(); pen.MoveTo(centre); pen.LineTo(points[i]);
                    pen.LineTo(points[(i + 1) % 10]); pen.ClosePath(); pen.Fill();
                }
                pen.strokeColor = earned ? new Color(1f, .91f, .51f) : new Color(.40f, .45f, .48f);
                pen.lineWidth = earned ? 2f : 1.5f;
                pen.BeginPath();
                for (int i = 0; i < 10; i++) { if (i == 0) pen.MoveTo(points[i]); else pen.LineTo(points[i]); }
                pen.ClosePath(); pen.Stroke();
            }
        }
        void ShowPracticeScore(int stage)
        {
            var counts=PracticeRating.Since(journey.Save.mistakes,practiceScoreBaseline);
            string id="checkpoint-"+stage;
            if(lastScoredJourney!=journey || lastScoredStage!=stage)
            { PracticeScore.Record(mode,id,counts); lastScoredJourney=journey; lastScoredStage=stage; }
            completedPracticeStage=stage;
            SetPage(Page.Checkpoint);
            var sheet=Result(GameText.Get("text.mission_complete_405e76"),"good",
                GameText.Get("level.checkpoint.title",stage+1,RouteStage.All[stage].Title));
            var content=ResultBody(sheet);
            AddScoreSummary(content,counts,PracticeScore.Load(mode,id).bestStars);
            var actions=Element(sheet,"sheet-actions");
            bool hasNext=stage+1<RouteStage.All.Length;
            if(hasNext) AddButton(actions,GameText.Get("rating.next"),()=>JumpToMission(stage+1),"big primary");
            AddButton(actions,GameText.Get("rating.retry"),()=>JumpToMission(stage),"big quiet");
            AddButton(actions,GameText.Get("home.practice"),ShowPracticeChooser,hasNext ? "big quiet" : "big primary");
            AddButton(actions,GameText.Get("text.main_menu"),BackToMenu,"big quiet");
        }
        void ShowTutorialScore(VisualElement parent)
        {
            const string id="tutorial";
            if(lastScoredTutorial!=journey)
            { PracticeScore.Record(mode,id,journey.Save.mistakes); lastScoredTutorial=journey; }
            AddScoreSummary(parent,journey.Save.mistakes,PracticeScore.Load(mode,id).bestStars);
        }
        void ContinueAfterViolation()
        {
            if(journey==null || !journey.Failure.HasValue) return;
            journey.ContinueAfterFailure(); // recover safely, retaining the attempt's counted errors
            district.Snap();
            if(journey.Stage==0) ShowPreflight(); else EnterHumanDrive(Page.Drive);
        }
    }
}
