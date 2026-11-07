namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void ShowCreditsPage()
        {
            PrepareMenuScene();
            SetPage(Page.Credits);
            var content = ScrollDialog();
            AddLabel(content, GameText.Get("credits.title"), "heading");

            var team = Element(content, "credits-team");
            AddLabel(team, GameText.Get("credits.subtitle"), "kicker");
            AddLabel(team, "We Will Handle", "heading credits-team-name");
            AddLabel(team, "Jonathan Yudi Gunawan", "credits-person");
            AddLabel(team, "Rizky Eko Putra", "credits-person");
            AddLabel(team, "Imre Nagi", "credits-person");

            var tools = Element(content, "credits-team");
            AddLabel(tools, GameText.Get("credits.codex"), "subhead");
            AddLabel(tools, GameText.Get("credits.hackathon"), "copy");

            AddLabel(content, GameText.Get("text.map_data_openstreetmap_contributors_odbl_surface_materials_poly_haven"), "rule-source");
            var actions = Element(content, "credits-actions");
            AddButton(actions, GameText.Get("navigation.return_menu"), ShowMenu, "quiet");
        }
    }
}
