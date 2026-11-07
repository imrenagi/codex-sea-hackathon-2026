using System.Linq;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void AddVehiclePicker(UnityEngine.UIElements.VisualElement parent = null)
        {
            var selected = VehicleCatalogue.Selected(mode);
            var picker = Element(parent ?? dialog, "vehicle-picker");
            AddLabel(picker, "KENDARAAN", "kicker");
            var choices = Element(picker, "vehicle-choices");
            foreach (var choice in VehicleCatalogue.All.Where(v => v.Mode == mode))
            {
                var button = AddButton(choices, choice.Label, () =>
                {
                    VehicleCatalogue.Select(choice);
                    if (page == Page.PlayChooser) ShowPlayChooser(); else ShowMenu();
                }, choice == selected ? "vehicle-choice selected" : "vehicle-choice quiet");
                button.tooltip = choice.Description;
            }
            AddLabel(picker, selected.Description, "objective-detail");
        }
    }
}
