namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void ShowCreditsPage(bool tutorialCompleted = false)
        {
            string completion = tutorialCompleted && demoTutorial != null
                ? GameText.Get(demoTutorial.Scenario.CompletionSummaryKey) : null;
            PrepareMenuScene();
            SetPage(Page.Credits);
            var content = ScrollDialog();
            if (tutorialCompleted)
                AddLabel(content, GameText.Get("credits.completed"), "kicker credits-complete");
            AddLabel(content, GameText.Get("credits.title"), "heading");
            if (!string.IsNullOrEmpty(completion))
                AddLabel(content, completion, "copy credits-summary");

            var team = Element(content, "credits-team");
            AddLabel(team, GameText.Get("credits.subtitle"), "kicker");
            AddLabel(team, "We Will Handle", "heading credits-team-name");
            AddLabel(team, "Jonathan Yudi Gunawan", "credits-person");
            AddLabel(team, "Rizky Eko Putra", "credits-person");
            AddLabel(team, "Imre Nagi", "credits-person");

            var actions = Element(content, "credits-actions");
            AddButton(actions, GameText.Get("credits.explore"), () => ShowPlayChooser(true), "primary credits-explore");
            AddButton(actions, GameText.Get("credits.menu"), ShowMenu, "quiet");
        }
    }
}
