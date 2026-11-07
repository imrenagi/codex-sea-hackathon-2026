# RoadSense — six-minute pitch and live demo

Read the pitch paragraphs aloud. The demo section contains actions and points to emphasize, not a script. Timing assumes about 135–140 spoken words per minute, with short pauses. Finish by 5:45, leaving 15 seconds spare. Do not read the preparation notes aloud.

| Time      | Section                                     | Duration |
| --------- | ------------------------------------------- | -------- |
| 0:00–1:25 | Frustration and the testing gap             | 1:25     |
| 1:25–2:15 | National cost and RoadSense                 | 0:50     |
| 2:15–4:20 | Live demo: pedestrian and junction priority | 2:05     |
| 4:20–4:50 | Built with Codex                            | 0:30     |
| 4:50–5:45 | Proposed uses and closing                   | 0:55     |
| 5:45–6:00 | Spare time                                  | 0:15     |

## 0:00–1:25 | The frustration

We are frustrated with how people drive on our roads in Jakarta.

A rider cuts into your lane. A pedestrian tries to cross, but nobody stops. At a junction, everyone wants to go first. Sometimes they are blocking the intersection during the traffic jam.

These riders may have a licence. But do they know when to give way?

Passing a driving test does not always mean someone is ready for real traffic. That is the gap we want to close.

We want driving preparation to reflect real road situations. The motorcycle test checks skills like braking and turning. On the road, riders also need to notice people, know who goes first, and make safe decisions. We want them to practise those decisions before they drive in real traffic.

## 1:25–2:15 | The cost and the national opportunity

Indonesia's national road-safety plan estimates that road crashes cost about **450 trillion rupiah** in 2020. That is roughly three percent of GDP. The cost is huge. Families pay through hospital bills, lost income, and lost lives.

[Pause. Show: “≈ Rp450 trillion / year · 2020 economic baseline”.]

[Show: “1% lower national crash losses ≈ Rp4.5 trillion / year”, with “Illustrative scenario · 2020 baseline” beneath it.]

If Indonesia reduced those crash costs by just one percent, that would mean about **4.5 trillion rupiah saved a year**. We have not proven that impact yet. It shows why better road training matters.

So we built **RoadSense**: a driving game where people learn the rules by practising real road situations.

## 2:15–4:20 | Live demo: learn to give way

**Mission:** **Demo singkat · Hak utama — Pasar Bunga**, using **Motor**. This single mission covers a pedestrian crossing and giving way at a junction. Start with the game loaded; open the mission from the menu or press F2.

| Time      | What to show                                                                                   | What to emphasize                                                                                                                                         |
| --------- | ---------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2:15–2:30 | Approach the occupied crossing and deliberately fail to give way.                              | The game checks an actual driving decision: whether the rider lets someone cross safely.                                                                  |
| 2:30–2:50 | Show the violation card, **YANG BENAR**, and the legal citation.                               | Immediate feedback: what went wrong, what to do instead, and the law behind it. Give the judges time to read.                                             |
| 2:50–3:10 | Select **Lihat cara benar**. Show the coach stopping and waiting, then return to the feedback. | Learners can see the correct action in the same situation.                                                                                                |
| 3:10–3:45 | Retry manually. Stop before the crossing, wait for a clear path, then continue.                | The learner must perform the correct action themselves.                                                                                                   |
| 3:45–4:10 | At the junction, give way to traffic on the main road, then reach the green checkpoint.        | Who goes first depends on the situation. Show the second right-of-way decision without changing missions.                                                 |
| 4:10–4:20 | Show the completion card and actual session result.                                            | The complete experience: make a mistake, understand it, watch, retry, and finish. The result is practice evidence, not proof of safer real-world driving. |

**Keep the demo focused:** one mission, one deliberate mistake, one explanation, one retry. Briefly explain each point while driving; let the game do most of the talking. Move to Codex at 4:20.

## 4:20–4:50 | What we built with Codex

[Show a genuine Codex task capture alongside its resulting implementation or verification artifact.]

Codex helped us build the driving game, rule checks, explanations, and tests.

We chose Deep Domain AI as our direction. We turn Indonesian traffic rules into situations people can practise. The game checks their actions and explains the result. It also labels what is law and what is safety advice.

## 4:50–5:45 | Where this could go and closing

We want to bring this online in two ways.

First, as a required learning step before the driving test, if licensing authorities adopt it. Applicants would practise road situations and show they understand the rules.

Second, as training for people joining Gojek or Grab as driver partners, and refresher lessons for existing drivers.

We would start with a driving school or a group of drivers. Do they repeat fewer mistakes? Can they handle a new situation? Do they drive more safely afterward?

Passing the game cannot guarantee safe driving. We need to test whether the lessons carry over to the road.

**RoadSense. Learn to give way before someone else pays for your mistake.**

[End by 5:45. Hold the final screen.]

---

## Preparation notes — do not read aloud

### Numbers and sources

- **Economic baseline:** The annex to [Perpres 1/2022, national road-safety plan](https://peraturan.bpk.go.id/Download/189970/Perpres%20Nomor%201%20Tahun%202022%20-%20Lampiran.pdf), first page, cites an ADB estimate of **2.9–3.1% of GDP**, equivalent to **Rp448–478 trillion using 2020 GDP of Rp15,434.2 trillion**. This confirms the estimate in [old-pitch.md](../old-pitch.md). “About Rp450 trillion” is a rounded figure near the lower end, not an updated 2026 estimate.
- **Illustrative improvement:** Rp450 trillion × 0.01 = **Rp4.5 trillion**. This is a hypothetical reduction in total national economic losses, not a product forecast, government-budget saving, or claim that 1% fewer crashes necessarily means 1% lower costs. Real impact depends on adoption, sustained behaviour change, and the severity of crashes avoided.
- **Testing context:** [Polri's explanation of the 2023 motorcycle-test changes](https://tribratanews.polri.go.id/blog/nasional-3/permudah-masyarakat-korlantas-polri-resmi-ubah-skema-ujian-praktik-pembuatan-sim-62007) describes braking, U-turn, S-course, and avoidance exercises. Some official service standards also describe on-road testing, for example this [2025 Satpas standard](https://reshulusungaiselatan.kalsel.polri.go.id/wp-content/uploads/STANDAR-PELAYANAN-SATPAS-2025-REVISI.pdf). The pitch therefore identifies a preparation gap without asserting that no real-road assessment exists or that test design is the proven main cause of crashes.
- **Adoption:** Licensing prerequisites and Gojek/Grab onboarding are proposals, not current integrations, partnerships, or approved requirements. Before using it for eligibility decisions, validate the assessment, identity checks, accessibility, and legal content with the relevant partner. The current demo does not establish those capabilities.

### Live delivery

- If the presenting build still shows **Jalan Kita**, say once: “Jalan Kita is the name on this prototype build.” Present the project as **RoadSense**.
- Rehearse the exact route and build. The demo has **2:05**, including commentary. These times are estimates, not a completed rehearsal. If the route needs more time, cut the paragraph about the old motorcycle test from the opening and begin the demo about 20 seconds earlier; keep the 4:20 exit.
- The completion card counts the actual session's mistakes, including the deliberate violation. Do not call it a measured learning improvement.
- If the demo stalls, switch to a clearly labelled recording and say: “I’ll show a recording of the same sequence.” If there is no recording, show the coach and explicitly call it a demonstration. Only say the route is completed when the completion screen appears.
- Move to the Codex section by **4:20**. If running late, shorten the coach segment after its first clear stop and keep the feedback commentary brief. Start the proposed uses by **4:50** and the closing line by **5:40**.
