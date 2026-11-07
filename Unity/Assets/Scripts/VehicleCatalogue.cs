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
            new("hatchback", "Hatchback", VehicleMode.Car, "Traffic/city_hatchback", "Mobil kota dengan bodi, lampu, dan roda terperinci."),
            new("mpv", "MPV keluarga", VehicleMode.Car, "Traffic/family_mpv", "MPV perkotaan dengan kamera kejar dan lampu aktif."),
            new("city", "Mobil kota", VehicleMode.Car, "Car_City", "Mobil ringkas untuk jalan perkotaan."),
            new("sedan", "Sedan", VehicleMode.Car, "Car_Sedan", "Sedan empat pintu dengan kabin tertutup."),
            new("beat-red", "motor bebek merah-putih", VehicleMode.Motorcycle, "BeatRed", "Skuter dengan bodi merah-putih dan detail lengkap.", true),
            new("beat-blue", "motor bebek biru", VehicleMode.Motorcycle, "BeatBlue", "Skuter biru dengan tampilan sederhana.", true),
            new("classic", "Skuter klasik", VehicleMode.Motorcycle, "Motorcycle", "Skuter latihan dengan pengendara dan penumpang.")
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
