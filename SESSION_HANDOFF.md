# Hackathon product discussion and handoff

Recorded: 7 November 2026. This document captures the product discussion in the initial Codex session. Read it alongside `CONTEXT.md`; the product direction below developed after that original brief. Technical choices remain unresolved.

## Working context and collaboration preferences

- We are participating in a one-day hackathon.
- The user wants a proactive, thoughtful collaborator who challenges assumptions and makes useful decisions independently.
- Ask when something important is unclear. Avoid unnecessary confirmation and premature technical discussion.
- The latest emphasis is the foundational problem, why someone would spend time using the product, and a credible pitch.
- This file is a portable handoff for another Codex session or a new ChatGPT Project. Upload or paste it there; do not assume that another session automatically has this conversation or local repository access.

## Original idea from CONTEXT.md

The original audience was Indonesian car drivers and motorcycle riders, especially first-time license applicants, newly licensed road users, and existing drivers refreshing their knowledge. Driving schools and road-safety educators were potential secondary users.

The problem: knowing vehicle controls or memorizing test answers does not necessarily prepare someone to apply traffic rules in real situations. Learners need a safe place to make mistakes and understand the correct response.

The proposed experience was a 3D driving game in an Indonesian street setting, with left-hand traffic, recognizable signs, markings, intersections, pedestrians, and other vehicles. Both cars and motorcycles were in the original scope.

Original learning loop:

**Drive → receive immediate feedback → understand the rule → retry → apply it in a different situation.**

Original five mission groups:

1. Before departure: rider/passenger helmets and car-occupant seat belts.
2. Signs and signals: stop signs, stopping positions, and traffic lights.
3. Intersections and crossings: priority and pedestrians.
4. Road direction and lane discipline: wrong-way shortcuts, markings, and lane changes.
5. Speed and attention: speed limits and phone distractions.

Feedback would explain the action in plain Bahasa Indonesia, show a reviewed legal source, and demonstrate the correct response. Explicit game logic would determine violations consistently. Legal requirements and additional defensive-driving advice should be distinguished. Source versions should be recorded so lessons can be reviewed when regulations change.

The original brief also proposed progression, achievements, recurring-mistake summaries, recommended practice, and before/after knowledge checks. These are proposed features, not implemented or validated outcomes.

## Foundational critique discussed

The original brief explained why society needs safer roads more clearly than why an individual would voluntarily use this product today.

Missing foundations identified:

- A specific initial user, a triggering moment, and a useful outcome.
- Evidence that the behavior being addressed comes from missing knowledge rather than panic, deliberate rule-breaking, time pressure, or incentives.
- A clear reason to use an interactive scenario instead of a video or quiz.
- A definition of success beyond completing missions or collecting achievements.
- Evidence of demand from users or organizations.

Audience/motivation hypotheses:

| Audience | Possible motivation |
| --- | --- |
| First-time license applicant | Prepare for the examination. |
| New driver or rider | Practice situations that make them nervous before facing traffic. |
| Experienced driver | Check whether their instincts are correct. |
| Instructor | Teach situations that are difficult to practice safely. |

These motivations were hypotheses, not interview findings. Experienced drivers who knowingly disregard rules may be difficult voluntary users because they may not perceive a learning need.

The strongest interaction hypothesis: someone may know a rule but still make the wrong decision when several things happen together. A scenario should expose that gap, explain it, and check understanding in a variation. Trivia placed inside a 3D environment would offer a weaker reason to spend time.

Daily retention is not necessarily the objective. Completing useful preparation and mastering weak areas can be success even if the learner stops using the product afterward.

## Product direction introduced by the user

The user raised concerns about people obtaining Indonesian driving licenses through intermediaries known as “calo,” potentially bypassing testing. They proposed an additional screening step for online motorcycle taxi drivers (ojol/ojek online), because a license alone may be insufficient evidence of road judgment.

This shifted the concept toward **driver assessment and training for platform onboarding**, with two stakeholders:

| Stakeholder | Reason to participate |
| --- | --- |
| Ride-hailing/delivery platform or fleet operator | Identify applicants’ road-judgment gaps and target training before onboarding. |
| Driver applicant | Complete onboarding, demonstrate competence, and qualify for earning opportunities. |

The user later added a second use case: **license applicants practicing at home before spending time visiting a police station**, including preparation for simulator examinations where those are available.

Both use cases were incorporated into the latest pitch. The initial priority between them remains undecided.

Important boundaries discussed:

- Do not infer that an individual obtained a license improperly or is incompetent merely because licensing shortcuts are a concern.
- A simulation could assess hazard recognition, decisions, and rule understanding. It cannot by itself establish physical riding skill or guarantee real-world behavior.
- Platform demand, adoption, and willingness to pay have not been validated.
- A score needs relevant scenarios, consistent scoring, resistance to simple memorization, and ultimately evidence that it measures something useful outside the app.
- The assistant recommended **screen → identify gaps → train → reassess**, with a learning path after a failed attempt. This is a recommendation, not a finalized eligibility policy.
- High-stakes rejection of driver applicants would require stronger validation and a fair review process.

## Proposed hackathon demonstration

The assistant recommended a focused demonstration:

1. An applicant makes decisions in realistic riding situations.
2. The assessment identifies a specific weakness.
3. The applicant receives an explanation and targeted practice.
4. A different scenario checks understanding.
5. An onboarding reviewer can inspect the evidence behind the result.

For a one-day build, the assistant suggested proving one excellent learning loop before expanding to five missions and two vehicle types. The user has not confirmed a reduced scope.

## Pitch evolution and latest wording

Initial operator-focused positioning:

> A scenario-based assessment and training tool that helps ride-hailing operators identify gaps in drivers’ road judgment before onboarding.

Latest pitch delivered after adding at-home preparation:

> A driving license alone doesn’t tell us how someone will respond to a dangerous situation on the road. We’re building an interactive platform where Indonesian drivers can practice realistic traffic scenarios, understand their mistakes, and improve their road judgment. For license applicants, it offers preparation at home before visiting a police station—including for simulator assessments where available. For ride-hailing and delivery operators, it provides additional evidence beyond a license to identify learning gaps and support onboarding. Inspired by Indonesia’s earlier investment in SIM simulators, we aim to make scenario-based practice more accessible, with clear explanations grounded in Indonesian traffic rules and targeted training before reassessment.

This is a product ambition, not a claim of demonstrated learning or safety effectiveness. Do not promise that home practice prevents repeat visits or exactly reproduces an official examination without supporting evidence.

## Research: Polri’s Simulator SIM project

The user asked when the police simulator project started, what it was for, and what happened. Web research identified the major **2011 Korlantas procurement** and evidence of earlier simulator use. Distinguish the procurement scandal from the wider use of simulator testing.

### Timeline

| Period | Finding and qualification |
| --- | --- |
| Before 2010 | A supplier chronology reported maintenance of 54 existing Korean-made simulators in October 2009 and development of a moving local prototype. This is a participant’s account, not an independently established national launch date. |
| November 2010 | Contemporary reporting described a public demonstration at Indo Defence in Jakarta and 100 units distributed to pilot locations: 15 Jakarta, 75 West Java, and 10 Banten. |
| 2011 | The major procurement covered 700 motorcycle and 556 car simulators. A DPR research briefing reported a budget of approximately Rp198.7 billion. |
| July 2012 onward | KPK investigated the procurement; subsequent proceedings resulted in corruption convictions. |
| 2013–2014 | Equipment/audit testimony documented significant problems. Djoko Susilo’s sentence was increased to 18 years on appeal; a later Finance Ministry account identifies Supreme Court decision 537 K/Pid.Sus/2014 dated 4 June 2014. |
| 2021 | The original text of Perpol No. 5 Tahun 2021 included simulator examinations and allowed physical equipment or virtual simulation. It provided for theory and practical tests alone where simulator examinations were unavailable. This describes that historical text, not a verified consolidated statement of current law. |
| April 2025 | A Polres Ternate service report described simulator examinations among its SIM processing stages. This does not establish nationwide availability or whether its machines were from the 2011 procurement. |

### Intended purpose

Physical motorcycle rigs and car cockpits presented simulated road environments for assessment within the SIM examination process. Polri’s 2018 operational reference listed reaction, anticipation, judgment/estimation, concentration, and driving attitude among simulator assessment topics.

### Documented results

- Equipment was distributed, but expert testimony described failures involving hydraulics, sound, microcontrollers, and software, as well as limitations in measuring the intended competencies.
- A BPK auditor’s July 2013 court testimony described checks at 272 locations in 33 provinces, equipment that did not meet contractual specifications, and many nonfunctioning units. This does not mean every unit was unusable.
- KPK publicly reported approximately Rp121 billion in state losses in April 2013. Budget, contract, and loss figures differ across reports and should not be treated as interchangeable.
- Former Korlantas chief Djoko Susilo received an 18-year sentence after appeal for corruption and money laundering. The broader money-laundering proceedings should not be reduced to a claim that every seized asset came directly from this procurement.
- A study published through Polri’s research journal reported that police offices studied tended to lack simulators despite examination provisions. This was not a nationwide inventory.
- Later regulations and local service reports show that the simulator concept was not entirely abandoned.
- **The search did not find a credible evaluation establishing that the original project reduced crashes or reliably predicted real-world driving safety.** This is a limit of the research, not proof that no evaluation exists.

### Research sources

1. [Supplier chronology, detik, 2 August 2012](https://news.detik.com/berita/d-1982051/kisah-perkenalan-sukotjo-dengan-budi-susanto-proyek-simulator-sim) — earlier equipment and prototype history; participant account.
2. [Mengenal Kendaraan Simulator Ujian SIM, detik, 31 July 2012](https://news.detik.com/berita/d-1979114/mengenal-kendaraan-simulator-ujian-sim) — public demonstration, pilot distribution, intended use.
3. [DPR research briefing, August 2012](https://berkas.dpr.go.id/pusaka/files/info_singkat/Info%20Singkat-IV-15-I-P3DI-Agustus-2012-24.pdf) — procurement quantities, budget, investigation context.
4. [Polri’s 2018 traffic-police reference](https://k3i.korlantas.polri.go.id/api/web/uploads/regulation_doc/VADEMIKUM%20POLANTAS%20TAHUN%202018.pdf) — intended assessment dimensions.
5. [842 Simulator SIM Kondisinya Bermasalah, JPNN, 19 July 2013](https://www.jpnn.com/news/842-simulator-sim-kondisinya-bermasalah) — reporting of ITB expert testimony. The search returned excerpts; the subsequent full-page fetch failed.
6. [BPK audit testimony, detik, 19 July 2013](https://news.detik.com/berita/d-2308433/kasus-simulator-sim-nilai-final-kerugian-negara-rp-121-m) — physical checks, specification problems, reported losses.
7. [KPK spokesperson on losses, detik, 1 April 2013](https://news.detik.com/berita/d-2208570/kpk-kerugian-kasus-simulator-sim-rp-121-m).
8. [Djoko Susilo appeal sentence, detik, 19 December 2013](https://news.detik.com/berita/d-2446122/irjen-djoko-susilo-dibui-18-tahun-pengacara-kami-pelajari-dulu).
9. [Finance Ministry/DJKN account of asset auctions, December 2018](https://www.djkn.kemenkeu.go.id/berita_media/baca/12814/Lelang-Ditawarkan-Secara-Online.html) — references the final Supreme Court decision; a media report republished on the ministry website.
10. [Polri research publication: Perspektif Pungutan Liar pada Layanan SSB](https://jlp.puslitbang.polri.go.id/jlp/index.php/LitbangPOLRI/article/download/18/18/) — implementation and equipment availability, particularly PDF pages 37–38. Publication year was not confirmed during this session.
11. [Official original Perpol No. 5 Tahun 2021](https://korlantas.polri.go.id/wp-content/uploads/2021/03/PERPOL-NO-5-TH-2021-TTG-PENERBITAN-DAN-PENANDAAN-SIM.pdf) — Articles 13 and 16; check amendments before using as current legal guidance.
12. [Polres Ternate service report, 17 April 2025](https://polresternate.com/satpas-polres-ternate-gelar-pelayanan-penerbitan-sim-wujud-komitmen-meningkatkan-tertib-berlalu-lintas/) — later reported simulator use.

### Implications for our positioning

The history supports institutional interest in simulation-based assessment. It does not validate our product’s effectiveness or prove a market.

Recommended differentiation: accessible practice, understandable evidence of decisions, transparent scoring, targeted training, and reassessment. Simulation itself is not a new invention.

Suggested historical framing:

> Indonesia has previously invested in simulator-based driving assessment, but documented equipment and implementation problems limited that effort. We’re exploring an accessible, scenario-based approach for ride-hailing onboarding, with transparent assessment and targeted training.

Avoid claiming that Polri abandoned all simulator testing, that simulation was proven ineffective, or that our product already identifies safe drivers reliably.

## Open questions and next validation

Product questions:

- Which is the first audience: platform onboarding or individual license preparation?
- Can we speak with a platform onboarding team, fleet operator, instructor, or ojol community?
- How do operators currently assess and train applicants, and what do existing processes miss?
- Is the main gap knowledge, applying knowledge under pressure, or incentives to follow rules?
- What recent confusing or frightening road situation can prospective users describe, and how did they seek help?
- What would make an operator trust and act on assessment results?
- What is the initial competency and how will improvement be demonstrated?
- Which official examinations and local simulator installations would at-home preparation realistically support?
- Who reviews legal interpretation, scenario validity, and scoring?

Build questions raised earlier, still unanswered:

- How many hours remain and what must the final demo deliver: browser link, desktop build, live presentation, or a combination?
- What team roles, skills, assets, and working code exist?
- Are both vehicle types and all five missions mandatory?
- Where is the implementation? At the time of this discussion, the repository contained only `CONTEXT.md` before this handoff was added.
- `CONTEXT.md` contains an older Three.js plan and an update claiming a switch to Unity 6 URP/WebGL. No Unity project was present to verify that statement. The user explicitly asked to defer technical specifications, so do not treat either stack as a settled implementation choice.

## Suggested continuation prompt

Read CONTEXT.md and SESSION_HANDOFF.md. We are doing a one-day hackathon. Continue from the latest product direction: scenario-based road-judgment assessment and training for Indonesian drivers, with ojol onboarding and at-home SIM preparation as candidate use cases. Help us choose a focused, credible demonstration. Distinguish user decisions, recommendations, research findings, and unvalidated hypotheses. Be proactive and ask only questions that materially affect the next step.
