# Violation explanations and sanctions

The game displays a plain-language summary and a separately labelled sanction
in **Dasar hukum dan sanksi**. Amounts are maximum fines, not fixed ticket fees.
The UI reads `LawDetails.For(rule)`; violation detection remains in `Journey`.

Sources checked on 7 November 2026:

- [UU 22/2009, official BPK PDF](https://peraturan.bpk.go.id/Download/27961/UU%20Nomor%2022%20Tahun%202009.pdf), obligations in Articles 106–135 and sanctions in Articles 283–296.
- [UU 1/2026, official JDIH PDF](https://jdih.bp2mi.go.id/uploads/20260310/20260310124650_889000746_UU_Nomor_1_Tahun_2026_compressed.pdf), Article II paragraphs (3)–(4), printed page 4: removes the confinement component of combined confinement/fine provisions; the fine adjustment exception preserves UU 22/2009 amounts.
- [BPHN explanation of the effect on Article 287(1)](https://literasihukum.bphn.go.id/konsultasi-hukum/31161).
- Ujian SIM `content/rules.json` and `docs/traffic-rules-and-laws.md` supplied the starting mapping. Their pre-2026 confinement wording is not reproduced as a current penalty.

| Game rule | Sanction article | Maximum fine |
|---|---|---|
| Seat belt | 289 | Rp250.000 |
| SNI helmet | 291(1)–(2) | Rp250.000 |
| Motorcycle daytime headlight | 293(2) | Rp100.000 |
| STOP, no entry, lane markings | 287(1) | Rp500.000 |
| Red light, red right arrow | 287(2) | Rp500.000 |
| Motorcycle stop box | 287(1) markings / 287(2) signal, according to the offence | Rp500.000, not automatically summed |
| Pedestrian priority | 284 | Rp500.000 |
| Lane change without signalling | 295 | Rp250.000 |
| Turn without signalling | 294 | Rp250.000 |
| Speed limit | 287(5) | Rp500.000 |
| Phone / concentration | 283 | Rp750.000 |
| Failure to stop at railway warning | 296 | Rp750.000 |
| Ambulance priority | 287(4) | Rp250.000 |

No direct fixed sanction is assigned to the junction-priority or keep-left
simulation checks. Marking violations can separately fall under 287(1).
A small lane-change gap alone does not establish the no-signal offence in
Article 295. Collision, off-road and unfinished practice checks remain safety
advice / exercise feedback, without invented automatic fines. Human legal
review has not been asserted.

# Signal presentation

The combined original TrafficLightArrow rig is mirrored horizontally and
placed at the left kerb, so the arm extends over the lanes and its arrow points
to the driver's right. Both its round and arrow lenses are updated from the
simulation. Lens geometry is enlarged 40%; saturated base/emission colours
replace the excessive glow that washed the active lens toward white.

A live camera close-up appears at each traffic-light approach (mission 2 and
missions 6–8), with separate main/right-turn text states. It hides after the
junction and while paused. Unsignalised priority junctions are not presented
as having traffic lights. The close-up uses two 256×256 textures/cameras for the separate heads with no
additional audio listener; pause and destruction release its work/resources.
