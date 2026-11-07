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
            int stars=PracticeRating.Stars(true,counts);
            AddLabel(parent, PracticeRating.Symbols(stars), "heading");
            AddLabel(parent,GameText.Get("rating.result",stars,PracticeRating.Total(counts),PracticeRating.Serious(counts)),"copy");
            AddLabel(parent,GameText.Get("rating.best",best),"objective-detail");
            AddLabel(parent,GameText.Get("rating.criteria"),"copy");
            AddLabel(parent,GameText.Get("rating.disclaimer"),"copy");
            for(int i=0;i<counts.Length;i++) if(counts[i]>0)
                AddLabel(parent,counts[i]+"× "+Lesson.All[i].Title,"objective-detail");
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
