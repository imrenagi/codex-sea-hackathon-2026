1. Stakeholder: Who is this for?

Indonesian car drivers and motorcycle riders, especially first-time license applicants, newly licensed road users, and existing drivers who need to refresh their understanding of traffic rules. Both cars and motorcycles will be supported from the initial prototype, with shared road scenarios and vehicle-specific lessons.

Driving schools and road-safety educators could also use the application to help learners connect traffic regulations with everyday decisions.

2. Challenge: What problem are they facing?

Knowing how to operate a car or motorcycle does not automatically mean understanding how to behave safely and legally on the road. Everyday situations require judgment: Where should we stop at a stop sign? Who has priority at an intersection? When can we change lanes? How should we respond to pedestrians or a one-way street?

Our project starts with frustration at unsafe driving and disregard for traffic rules in Indonesia. We want to address a practical learning gap: people may learn vehicle controls or prepare for licensing tests without developing a strong understanding of how rules apply in realistic traffic situations. Concerns about shortcuts in obtaining licenses reinforce the importance of making practical road education accessible independently of the licensing process.

Our hypothesis is that interactive practice, immediate explanations, and repeated exposure to realistic situations can improve understanding and reinforce better decisions. Learners need a place to make mistakes, see their consequences, and practice the correct response without putting other road users at risk.

3. Result: What outcome do you want for the stakeholders?

We want drivers and riders to recognize traffic situations, understand the applicable rules, and practice making safer, lawful decisions through an engaging 3D game.

When a player breaks a rule, the application will immediately identify the action, explain the relevant rule in plain Bahasa Indonesia, show its legal source, and demonstrate the correct response. Players can replay the situation and then attempt a variation to check whether they have understood the lesson.

Progression, achievements, and targeted practice will reward responsible driving. A progress screen will highlight recurring mistakes and recommend lessons.

During the hackathon, we will assess learning through short before-and-after knowledge checks and changes in repeated mistakes across scenario attempts. Our longer-term goal is to support safer road habits and greater consideration for other road users.

4. Approach: How will you build the solution?

We plan to build a browser-based 3D driving game using Three.js and WebGL, with playable car and motorcycle modes. (Update, 2026-10-29: the game is now built with Unity 6 URP and released for desktop and the web through WebGL; the Three.js prototype was removed.) The environment will recreate a small Indonesian street setting with left-hand traffic, recognizable signs, road markings, intersections, pedestrians, and other vehicles. Camera perspectives, steering, braking, indicators, and traffic interactions will make the decisions feel grounded in an actual road experience.

To keep the hackathon deliverable focused, we will build one reusable environment with five short missions:

• Before departure: helmet checks for motorcycle riders and passengers, and seat-belt checks for car occupants.
• Signs and signals: stop signs, stopping positions, and traffic lights.
• Intersections and crossings: priority decisions and responding to pedestrians.
• Road direction and lane discipline: avoiding wrong-way shortcuts, following markings, and changing lanes.
• Speed and attention: responding to posted speed limits and resisting phone distractions.

The core learning loop will be: drive → receive immediate feedback → understand the rule → retry → apply it in a new situation.

The distinguishing feature is that scenario logic will be grounded in applicable Indonesian laws and regulations. Each lesson will connect a traffic situation, expected behavior, violation condition, and explanation to a reviewed legal provision. We will record the source and version so lessons can be reviewed when regulations change. Our starting sources will include UU No. 22 Tahun 2009, its applicable amendments, and relevant implementing regulations. Legal requirements and additional defensive-driving advice will be clearly distinguished.

We will use Codex to help implement and debug the 3D environment, vehicle interactions, scenario logic, and rule checks. We will use ChatGPT’s web-search and document-analysis features to help research official regulations and prepare scenario drafts, Bahasa Indonesia explanations, and follow-up learning questions. Our team will verify the legal interpretation before publishing each lesson, while explicit game logic will determine violations consistently.

Our hackathon deliverable will be a playable 3D prototype supporting both vehicle types, five learning missions, immediate source-linked feedback, and a progress dashboard.

