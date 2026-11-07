using System;
using System.Linq;
using UnityEngine;

namespace JalanKita
{
    public sealed class VehicleDefinition
    {
        public readonly string Id, Label, Model, Description;
        public readonly VehicleMode Mode;
        public readonly bool Beat;
        public VehicleDefinition(string id, string label, VehicleMode mode, string model, string description, bool beat = false)
        { Id = id; Label = label; Mode = mode; Model = model; Description = description; Beat = beat; }
    }

    public static class VehicleCatalogue
    {
        public static readonly VehicleDefinition[] All =
        {
            new("hatchback", "Hatchback", VehicleMode.Car, "Traffic/city_hatchback", "A city car with detailed bodywork, lights, and wheels."),
            new("mpv", "Family MPV", VehicleMode.Car, "Traffic/family_mpv", "An urban MPV with a chase camera and working lights."),
            new("city", "City car", VehicleMode.Car, "Car_City", "A compact car for city streets."),
            new("sedan", "Sedan", VehicleMode.Car, "Car_Sedan", "A four-door sedan with an enclosed cabin."),
            new("beat-red", "Red-and-white scooter", VehicleMode.Motorcycle, "BeatRed", "A detailed scooter with red-and-white bodywork.", true),
            new("beat-blue", "Blue scooter", VehicleMode.Motorcycle, "BeatBlue", "A blue scooter with a simple design.", true),
            new("classic", "Classic scooter", VehicleMode.Motorcycle, "Motorcycle", "A practice scooter with a rider and passenger.")
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
