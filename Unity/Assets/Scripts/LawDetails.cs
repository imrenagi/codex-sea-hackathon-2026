namespace JalanKita
{
    // Summaries of UU 22/2009; fines read with UU 1/2026 Pasal II (3)-(4).
    // No fixed fine is inferred where the simulated event does not establish an offence.
    public readonly struct LawDetails
    {
        public const string Amendment = "https://peraturan.bpk.go.id/Details/337869/uu-no-1-tahun-2026";
        public readonly string Meaning, Sanction;
        LawDetails(string meaning,string sanction) { Meaning=meaning; Sanction=sanction; }
        static string Fine(string article,string amount) => article+" · Denda paling banyak Rp"+amount+".";
        public static LawDetails For(Rule rule) => rule switch
        {
            Rule.SeatBelt => new("Drivers and passengers seated beside the driver in vehicles with four or more wheels must wear seat belts.",Fine("Pasal 289","250.000")),
            Rule.Helmet => new("Motorcycle riders and passengers must wear helmets that meet Indonesian National Standards (SNI).",Fine("Pasal 291 ayat (1)–(2)","250.000")+" Applies to riders without a helmet or riders who allow their passenger to ride without one."),
            Rule.Headlight => new("In addition to nighttime and other required lighting, motorcycle riders must use headlights during the day.",Fine("Pasal 293 ayat (2)","100.000")),
            Rule.NoStopping => new("Vehicles may not stop where signs or markings prohibit stopping (Pasal 118 huruf a). A passenger pickup request does not override this restriction.",Fine("Pasal 287 ayat (1), for violating prohibitory signs","500.000")),
            Rule.Stop or Rule.NoEntry or Rule.MovementLane => new("Drivers must obey regulatory signs and road markings. In this exercise, STOP and no-entry signs, lane arrows, and solid lines determine permitted movements.",Fine("Pasal 287 ayat (1)","500.000")),
            Rule.Signal or Rule.RightArrow => new("Drivers must obey traffic signals (APILL). At junctions with separate signals, green for straight traffic does not permit a right turn while the right arrow is red.",Fine("Pasal 287 ayat (2)","500.000")),
            Rule.StopBox => new("Drivers must obey road markings and traffic lights. Cars wait behind the car stop line; the stop box ahead is reserved for motorcycles.",Fine("Pasal 287 ayat (1), for markings; ayat (2), for traffic signals","500.000")+" The applicable provision depends on the offense; penalties are not added automatically."),
            Rule.Junction => new("At an unsignalized junction, vehicles from a smaller road yield to main-road traffic. At a perpendicular T-junction, through traffic has priority.","Pasal 113 does not directly specify a fine amount. This exercise does not assign an automatic fine for this event."),
            Rule.Pedestrian => new("Drivers must prioritize pedestrian and cyclist safety. Pedestrians have priority when using a crossing.",Fine("Pasal 284","500.000")),
            Rule.KeepLeft => new("Road users keep left. The right lane may be used for overtaking or as directed by an officer under Pasal 108.","Pasal 108 does not directly specify a fine. A marking violation may fall under Pasal 287 ayat (1), with a maximum fine of Rp500.000, if the offense requirements are met."),
            Rule.Indicator => new("Before changing lanes or moving sideways, drivers must check traffic ahead, beside, and behind them, and signal.",Fine("Pasal 295, for moving without signaling","250.000")),
            Rule.Gap => new("Pasal 112 ayat (2) requires observation before a lane change. The game's safe-gap threshold is a training measure, not a distance specified by that provision.","Pasal 295 covers moving without signaling (maximum fine Rp250.000). A small gap alone does not automatically establish that offense."),
            Rule.TurnSignal => new("Before turning or making a U-turn, drivers must check traffic ahead, beside, and behind them, and signal using an indicator or hand signal.",Fine("Pasal 294","250.000")),
            Rule.Speed or Rule.BoulevardSpeed => new("Drivers must obey maximum and minimum speed limits; exceeding the maximum is prohibited. Obey the 30 or 50 km/h limits posted on these practice roads.",Fine("Pasal 287 ayat (5)","500.000")),
            Rule.Phone => new("Drivers must drive responsibly and with full concentration. The explanation of Pasal 106 ayat (1) includes distraction caused by phone use.",Fine("Pasal 283","750.000")),
            Rule.Railway => new("At a level crossing, drivers must stop when a warning sounds, barriers begin closing, or another warning is given. Give trains priority and yield to vehicles already crossing the tracks.",Fine("Pasal 296, for failing to stop at a warning","750.000")),
            Rule.Emergency => new("Ambulances carrying patients have priority. Priority vehicles use a police escort and/or red or blue warning lights and a siren as specified in Pasal 135.",Fine("Pasal 287 ayat (4)","250.000")),
            _ => new("This is a practice objective or safety recommendation.","This exercise does not assign an automatic fine.")
        };
    }
}
