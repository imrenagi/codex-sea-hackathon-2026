using System;
using System.Linq;
using UnityEngine;

namespace JalanKita
{
    public sealed class VehicleDefinition
    {
        public readonly string Id, Model;
        readonly string labelKey, descriptionKey;
        public string Label => GameText.Get(labelKey);
        public string Description => GameText.Get(descriptionKey);
        public readonly VehicleMode Mode;
        public readonly bool Beat;
        public VehicleDefinition(string id, string label, VehicleMode mode, string model, string description, bool beat = false)
        { Id = id; labelKey = label; Mode = mode; Model = model; descriptionKey = description; Beat = beat; }
    }

    public static class VehicleCatalogue
    {
        public static readonly VehicleDefinition[] All =
        {
            new("mpv", "text.family_mpv", VehicleMode.Car, "Traffic/family_mpv", "text.an_urban_mpv_with_a_chase_camera_and_working"),
            new("hatchback", "vehicles.hatchback.name", VehicleMode.Car, "Traffic/city_hatchback", "text.a_city_car_with_detailed_bodywork_lights_and_wheels"),
            new("city", "text.city_car", VehicleMode.Car, "Car_City", "text.a_compact_car_for_city_streets"),
            new("sedan", "vehicles.sedan.name", VehicleMode.Car, "Car_Sedan", "text.a_four_door_sedan_with_an_enclosed_cabin"),
            new("beat-red", "text.red_and_white_scooter", VehicleMode.Motorcycle, "BeatRed", "text.a_detailed_scooter_with_red_and_white_bodywork", true),
            new("beat-blue", "text.blue_scooter", VehicleMode.Motorcycle, "BeatBlue", "text.a_blue_scooter_with_a_simple_design", true),
            new("classic", "text.classic_scooter", VehicleMode.Motorcycle, "Motorcycle", "text.a_practice_scooter_with_a_rider_and_passenger")
        };

        public static VehicleDefinition Selected(VehicleMode mode)
        {
            string id = PlayerPrefs.GetString("JalanKita.Vehicle." + mode, "");
            return All.FirstOrDefault(v => v.Mode == mode && v.Id == id) ?? All.First(v => v.Mode == mode);
        }

        public static void Select(VehicleDefinition definition)
        {
            if (!All.Contains(definition)) throw new ArgumentException("Unknown vehicle selection");
            PlayerPrefs.SetString("JalanKita.Vehicle." + definition.Mode, definition.Id);
            PlayerPrefs.Save();
        }
    }
}
