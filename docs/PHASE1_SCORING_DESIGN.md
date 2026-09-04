# CrossDrive — Phase 1 Scoring & Incentive Design

Status: **candidate designs for playtesting. Nothing here is locked.**
Role of this document: three falsifiable scoring models, a halftime recommendation, round timing, and a playtest protocol that produces behavioural evidence.

> Naming note: this folder and brief say **CrossDrive**; `README.md` and `docs/*` say **Crosswire**. Pick one before the first build — it appears in the demo video and the Shipaton submission.

---

## 0. Shared assumptions (constant across all three models)

These are the arena constants. They are held identical across models so playtest differences are attributable to scoring, not to arena tuning.

| Element | Value | Reason |
|---|---|---|
| Cars | 4 | Phase 1 lock |
| Gems on field | 6, respawn at a new node 2s after pickup | Sparse enough to force contested lanes |
| Pits | 3, positioned on the shortest lines between gem clusters | Makes the fast route the risky route |
| Respawn after pit | 3s at the arena edge | The universal time cost; several models rely on it |
| Boost | ~1.4x for 0.5s, 4s cooldown | The shove/escape verb |
| Bump | Physics only unless a model says otherwise | Keeps event surface small |
| Camera | One fixed shot, whole arena visible, no follow | **Non-negotiable.** The premise only works if you can watch your own car being driven badly |

**The camera constraint is a design requirement, not a preference.** If a player cannot see their own car at all times, the entire emotional loop of CrossDrive is invisible to them and every scoring model below degrades to a solo score-attack.

### Scoring event surface Codex needs to expose

All three models are expressible as these events plus point deltas. Nothing else is required.

```
GemCollected(carId, driverId, ownerId)
CarEnteredPit(carId, ownerId, driverId, lastContactCarId, lastContactAgeSeconds)
CarBump(carA, carB, impulse)
CarRespawned(carId)
PhaseChanged(ANONYMOUS | REVEAL | KNOWN)
Tick(secondsRemaining)
```

Plus four HUD slots: `myScore`, `standings[4]`, `myCarStatusLine`, `modelSpecificCounter`. That is the entire integration contract.

### The reference cycle used everywhere below

```
Rohit owns BLUE     drives ORANGE (Aman's)
Aman  owns ORANGE   drives GREEN  (Karan's)
Karan owns GREEN    drives PINK   (Dev's)
Dev   owns PINK     drives BLUE   (Rohit's)
```

Cycle: `Rohit → Aman → Karan → Dev → Rohit`.

**Structural fact that drives everything in this document:** in a cycle, *the person hurting you is never someone you can hurt back through the steering wheel.* Dev is ruining Rohit's car; Rohit cannot touch Dev's car, because Karan drives it. Rohit's only routes are (a) physically intercepting with the car he drives, or (b) making a deal with Karan. This is not a flaw. It is the single best property the cycle has, and it is why the reversal option in §5 is a trap.

---

## 1. MODEL A — COMMISSION

### One-sentence rule
> "Gems pay the car's **owner 3** and its **driver 1**. Falling in a pit costs the **owner 2** and the **driver 1**. One leaderboard."

**Explain time: ~12 seconds.**

| | |
|---|---|
| **What the owner wants** | Their car driven fast, greedily, and never into a pit. |
| **What the driver wants** | To keep collecting for the +1 — *unless* the owner is beating them, at which point every gem is a terrible trade. |
| **Where goals align** | Collecting always pays both. Crashing always costs both. Baseline behaviour is competence. |
| **Where goals conflict** | The single leaderboard. Feeding a leader +3 for your own +1 is relative suicide. The driver can withhold — and withholding costs the driver 1/3 of what it costs the owner. |

### The nine questions

1. **Rohit driving ORANGE:** farm gems for +1 each, while watching Aman's total. Early round: farm flat out. Late round, if Aman is ahead: stop.
2. **Dev driving BLUE:** the same — but Dev is the *only* person who can throttle Rohit's income. If Rohit leads, Dev's best move is not to crash BLUE but to **stop scoring with it**. A driver's strike costs Dev 1/gem-forgone and costs Rohit 3.
3. **Rohit wants his own car** farming continuously and never falling. He has zero control over this and can only shout.
4. **Dev deliberately pits BLUE:** Rohit −2, Dev −1, Dev also loses 3s of farming. Against Rohit that is a +1 relative gain; against Karan and Aman it is −1 each. **So targeted griefing is only rational when the owner is your specific closest rival.** That is exactly the right shape.
5. **Rohit collects a gem in ORANGE:** Aman +3, Rohit +1.
6. **Bump:** no points. Bumps are used to steal a gem line, deny an approach, or push a car pit-ward.
7. **Why Rohit helps Aman:** every gem is real money for Rohit (+1), and points are scarce. Also indirect: Aman drives GREEN, so an engaged Aman suppresses Karan.
8. **Why Rohit hurts Aman:** the moment Aman is first, Rohit's +1 is buying Aman +3. Rohit parks, or shoves ORANGE toward a pit before bailing.
9. **Why Dev can't just grief:** each pit costs him a point and three seconds, and hands the other two players relative ground. Griefing is a purchase, not a freebie.

### 40-second simulated round

| t | Action | Deltas | Table talk |
|---|---|---|---|
| 0:00 | All four break for the centre gems | — | — |
| 0:04 | Rohit(ORANGE) takes gem | Aman +3, Rohit +1 | Aman: "whoever's got orange, I love you" |
| 0:07 | Dev(BLUE) takes gem | Rohit +3, Dev +1 | Rohit: "ok my guy is actually playing" |
| 0:09 | Aman(GREEN) beats Karan(PINK) to a contested gem | Karan +3, Aman +1 | Karan: "that's mine!" |
| 0:12 | *Standing: Rohit 4, Aman 4, Karan 3, Dev 1* | | |
| 0:14 | Dev is last and Rohit is tied first. Dev **stops farming BLUE** and starts circling an empty corner | 0 | — |
| 0:18 | Rohit's score has not moved in 11s | — | Rohit: "why is my car just DRIVING IN A CIRCLE" ← **first accusation** |
| 0:21 | Rohit retaliates the only way available: stops farming ORANGE, starts ramming BLUE toward a gem to force a pickup | 0 | Aman: "wait, now MY car stopped scoring too" |
| 0:26 | Karan quietly keeps farming PINK | Dev +3, Karan +1 | Karan says nothing |
| 0:31 | Aman, starved and annoyed, boosts GREEN into BLUE; BLUE falls in a pit | Rohit −2, Dev −1 | Rohit: "AMAN THAT'S MY CAR" / Aman: "then tell your driver to drive" |
| 0:38 | *Standing: Aman 4, Karan 4, Dev 3, Rohit 2* | | Dev, still anonymous, says nothing at all |

**What the sim exposes:** Model A's live wire is not sabotage, it is **the driver's strike** — withholding, not wrecking. That is good for grief-resistance and bad for spectacle: a striking player produces a car doing nothing. Also note the punishment problem at 0:31 — hitting a striking driver's car also hits its innocent owner, so the striker is partly shielded by their own victim's protest. Funny once, potentially confusing.

### Dominant strategy test
**Boring optimum: "farm steadily, never crash."** For roughly the first 40% of a round that is genuinely correct, and if the HUD does not make live standings unmissable, players will never leave it — Model A collapses into Bad Extreme A (cheerful mutual competence).

- **The 3:1 ratio is the strike dial.** Higher owner:driver ratio → more withholding and more parked cars. Lower (e.g. 2:1) → more cooperation. 3:1 is the test point; do not tune it before data.
- Secondary failure: strikes are visually dead. If groups discover the strike and the game becomes four idling cars, A is broken even though the maths is sound.

### Fairness test
1. **Under my control:** weak. Score ≈ 3×(gems my driver got) + 1×(gems I got). **Three-quarters of my upside is in a stranger's hands.** This is Model A's worst number.
2. **Depends on someone else:** ~75%.
3. **Can one bad player ruin my round?** Yes — a striker or an incompetent driver flatlines me and I have no mechanical answer.
4. **Skill expression:** moderate. Good driving earns +1s and denies others.
5. **Can a weak player win?** Easily — by being owned-by-a-good-driver. That is luck, not comedy.
6. **Funny or frustrating unfairness?** Borderline. "My driver is sandbagging me" is funny for one round and frustrating by the third.

---

## 2. MODEL B — SIPHON

### One-sentence rule
> "Every gem you grab in someone else's car pays **you 2** and costs **its owner 1**. A pit costs the **owner 1** and costs **you three seconds**. So the way to hurt someone is to drive their car *brilliantly*."

**Explain time: ~15 seconds.**

| | |
|---|---|
| **What the owner wants** | Their own car to be *useless* — slow, stuck, badly driven. Not destroyed: destruction also bills them. |
| **What the driver wants** | Maximum extraction, maximum speed, zero wasted seconds. |
| **Where goals align** | Neither wants the car in a pit. Both want it above ground and mobile. That is the only overlap. |
| **Where goals conflict** | Everywhere else, structurally and continuously. Competence *is* the attack. |

### The nine questions

1. **Rohit driving ORANGE:** extract as fast as possible. Every gem is +2 to him and −1 to Aman simultaneously. Secondary job: body-block BLUE, because every gem BLUE picks up costs Rohit 1.
2. **Dev driving BLUE:** extract, and block PINK (his own car) when convenient.
3. **Rohit wants his own car** parked, cornered, or wall-pinned — **not** pitted (a pit still bills him 1).
4. **Dev deliberately pits BLUE:** Rohit −1, Dev forfeits 3s of farming and gains nothing. **Farming for those same 3 seconds would have cost Rohit ~1 anyway *and* paid Dev +2.** Griefing is therefore *strictly dominated by playing well.* This is the strongest anti-grief property of any of the three models and it needs no penalty rule at all.
5. **Rohit collects a gem in ORANGE:** Rohit +2, Aman −1.
6. **Bump:** no points. Bumps deny gem lines — and the signature move is bumping *your own car* away from a gem.
7. **Why Rohit helps Aman:** almost never directly, only as a traded favour ("I'll idle for ten seconds if you go sit on BLUE").
8. **Why Rohit hurts Aman:** it is the default loop; he does not have to decide to.
9. **Why Dev can't grief:** see 4 — grief is dominated. The only griefing that survives is *spite after the reveal*, which is precisely when spite is dramatically appropriate.

### 40-second simulated round

| t | Action | Deltas | Table talk |
|---|---|---|---|
| 0:00 | All four sprint at the nearest gem | — | — |
| 0:03 | Rohit(ORANGE) gem | Rohit +2, Aman −1 | Aman's HUD: `ORANGE HAS COST YOU −1` |
| 0:05 | Dev(BLUE) gem | Dev +2, Rohit −1 | Rohit: "someone is bleeding me already" |
| 0:07 | Rohit turns ORANGE around and body-checks BLUE off the next gem | 0 | Rohit: "get AWAY from my car" (to nobody in particular) |
| 0:10 | Cost of that block: Rohit forfeited his own +2 to deny Dev +2 and save himself 1. Near break-even *at best* — and he is now out of position | — | — |
| 0:12 | Aman(GREEN) takes two gems in four seconds | Aman +4, Karan −2 | Karan: "somebody is EATING me alive" |
| 0:15 | Karan is across the map and cannot reach GREEN. He opens a negotiation into the void | — | Karan: "whoever has green — ease up and I'll leave your car alone" |
| 0:16 | Dev, who is not driving GREEN, lies anyway to muddy the water | — | Dev: "not me, I've barely scored" (he is second) ← **bluff** |
| 0:19 | Rohit boosts ORANGE into BLUE and shoves **his own car** into a pit | Rohit −1, Dev loses 3s | Table erupts. Rohit: "worth it" |
| 0:24 | *Standing: Aman 3, Dev 2, Rohit 0, Karan −2* | | |
| 0:28 | Karan pins GREEN against a wall. Both he and Aman stop scoring; Rohit and Dev farm freely | Rohit +2/−1, Dev +2, Aman −1 | Aman: "you're killing us both, genius" |
| 0:34 | Aman boosts out; Karan is out of position and out of pace | Aman +2, Karan −1 | |
| 0:40 | *Standing: Aman 4, Dev 4, Rohit 1, Karan −3* | | Karan, pre-reveal, is already 90% sure it is Aman |

**What the sim exposes:** the social layer arrives *without* anyone behaving badly. Karan's anger at 0:12 is generated purely by someone else playing well, which is a much cleaner comedy engine than griefing. The 0:19 self-pit is the best single demo beat in this document.

**But the corrected ledger contradicts the design intent.** Rohit blocks at 0:07 and self-pits at 0:19 — the two most entertaining acts in the round — and finishes third with 1 point. Aman and Dev, who did nothing but drive, finish on 4. **In Model B, every socially interesting act is a net loss.** That is the model's real defect, and it is more serious than the legibility problem. The dial is to make interception *pay*: e.g. a car that has not collected a gem for 5s stops billing its owner, so successful blocking converts directly into stopped bleed. Do not pre-tune this — but expect the playtest to demand it.

### Dominant strategy test
**Boring optimum: "farm at maximum speed and ignore everyone."** This is the real risk. Blocking is deliberately near break-even, which keeps it situational — but "situational" can also mean "never worth the detour," in which case Model B becomes a four-way time trial with insults layered on top.

- **The falsifiable question is: does interception actually happen?** Threshold: ≥3 deliberate block/ram-your-own-car attempts per round. Below that, B has failed as a *social* game even if it scores well on fun.
- Second risk: **score legibility.** Players must track a number that goes down because of a car they are not driving. The HUD must carry an explicit bleed line (`YOUR CAR HAS COST YOU −4`) or nobody will understand why they are losing.

### Fairness test
1. **Under my control:** strong. Score = 2×(my extraction) − 1×(my car's extraction) − 1×(my car's falls). **Two-thirds of the magnitude is my own hands.** Best agency of the three.
2. **Depends on someone else:** ~33%, and it is capped by half-weighting.
3. **Can one bad player ruin my round?** No. A hostile driver of my car can only bleed me at half rate while I extract at full rate elsewhere.
4. **Skill expression:** highest of the three — routing, denial, boost timing all pay directly.
5. **Can a weak player win?** Only if they happen to be owned by a weak driver. That correlation is mild.
6. **Funny or frustrating?** Funny. The unfairness is *legible* ("Aman is farming me") and *answerable* (go block him, or out-extract him).

---

## 3. MODEL C — SPLIT PURSE

### One-sentence rule
> "Gems pay the car's **owner 3**. Shoving another car into a pit pays **whoever is driving the car that shoved 3**. Falling in a pit costs the **owner 2**. Every car has a farmer's job and a hitman's job, and two different people want different ones."

**Explain time: ~18 seconds.** Longest of the three, still inside budget.

Implementation note: shove credit = `lastContactCarId` within 1.0s of `CarEnteredPit`. Self-inflicted falls (no recent contact) pay nobody; the owner is still billed 2.

| | |
|---|---|
| **What the owner wants** | Their car quietly farming gems and staying out of pits. |
| **What the driver wants** | Their car healthy, fast, and hunting — gems pay them nothing. |
| **Where goals align** | Both want the car *alive and mobile*. The driver needs it as a weapon; the owner needs it as a mule. Neither ever wants it in a pit. |
| **Where goals conflict** | Every single second, over the same steering wheel. This is the only model where the conflict is continuous and visible on screen rather than positional or accounting-based. |

### The nine questions

1. **Rohit driving ORANGE:** hunt cars into pits (+3 each), ignore gems entirely, and pick the current leader as prey. He will collect a gem only as a bargaining chip.
2. **Dev driving BLUE:** hunt. Which means Rohit's owner-income is structurally zero unless he can *buy* it.
3. **Rohit wants his own car** farming gems he cannot make it farm. This is the model's central hook: **his score depends on persuading an anonymous stranger to do the boring job.**
4. **Dev deliberately drives BLUE into a pit:** Rohit −2, Dev +0 (no shove credit for self-inflicted), Dev loses 3s of hunting while everyone else banks +3 shoves. Pure spite with a real price.
5. **Rohit collects a gem in ORANGE:** Aman +3, Rohit +0. He does this only to pay for something.
6. **Bump:** no points on its own; a bump that puts a car in a pit inside 1s pays the bumping car's *driver* +3 and bills the bumped car's *owner* −2.
7. **Why Rohit helps Aman:** currency. Aman drives GREEN and can body-block whoever is hunting BLUE. "Two gems for orange if you screen me for ten seconds."
8. **Why Rohit hurts Aman:** he does not have to. **The default behaviour already starves Aman.** Active harm (wrecking ORANGE) costs Rohit hunting time, so it is rare and deliberate.
9. **Why Dev can't grief:** self-crashing pays 0 and burns 3s. The efficient way to hurt Rohit is simply *not to farm* — which is what Dev wants to do anyway. Model C's grief problem is not vandalism, it is **neglect**.

### 40-second simulated round

| t | Action | Deltas | Table talk |
|---|---|---|---|
| 0:00 | Nobody drives toward a gem. All four cars turn toward each other | — | Immediate laughter at the shape of it |
| 0:04 | Rohit(ORANGE) boosts GREEN pit-ward; GREEN survives on the lip | — | Karan: "whoa whoa" |
| 0:06 | Dev(BLUE) shoves GREEN into the pit | Dev +3, Karan −2 | Karan: "WHO IS DRIVING BLUE" ← **accusation at 6 seconds** |
| 0:09 | Rohit does the arithmetic on his own zero and opens the bribe market | — | Rohit: "whoever has BLUE — grab one gem and I won't touch your car all half" |
| 0:13 | Dev (anonymous) takes the deal, buys safety cheaply | Rohit +3 | Rohit now knows something true about his driver. Deduction begins |
| 0:16 | Karan(PINK) shoves ORANGE into the pit | Karan +3, Aman −2 | Aman: "oh so it's open season on me" |
| 0:20 | Rohit respawns and wants revenge on Karan — but hurting Karan means wrecking **GREEN**, the car Karan *owns*, not PINK, the car Karan *drives*. Pre-reveal, Rohit is not certain which is which | — | **Structural: targeted revenge is near-impossible before halftime in this model** |
| 0:25 | Aman tries the same bribe; the market is already saturated | — | Aman: "same deal, anyone?" / silence |
| 0:31 | Dev shoves PINK — his own car — into the pit for +3 and −2 | Dev net +1 | Big laugh; Dev: "I'll take a point off myself, sure" |
| 0:38 | *Standing: Dev 4, Rohit 3, Karan 1, Aman −2.* **Gems collected all round: 2 of ~9 spawned** | | |

**What the sim exposes:** C produces the fastest accusation, the loudest table, and the most natural negotiation of the three — and it very nearly kills the gem economy in the process.

### Dominant strategy test
**Boring optimum: "everybody hunts, nobody farms, all gems rot."** Then score = shove count and CrossDrive is a demolition derby wearing an ownership costume. The bribe market is the only thing holding the farming channel open, and bribes devalue fast (see 0:25).

- **Concrete falsification metric: gem pickup rate.** If **fewer than 30% of spawned gems are collected** in a round, Model C has structurally collapsed and the ownership layer is decorative.
- Secondary risk: shove credit is fiddly to feel. A player who set up a shove and got the credit stolen by an incidental last-touch will feel cheated. The 1.0s window needs tuning against real bumper physics.
- Tertiary risk: gems are worth 3 to a person with no ability to collect them. That is a channel where **agency is exactly zero**, offset only by negotiation skill.

### Fairness test
1. **Under my control:** split. Shove income is 100% mine; gem income is 0% mine and 100% purchasable.
2. **Depends on someone else:** ~50%, but it is *negotiable* dependence rather than passive dependence — which is a materially better kind.
3. **Can one bad player ruin my round?** Yes, cheaply, by simply refusing to farm. Neglect is free.
4. **Skill expression:** good — shoving is a real, learnable, watchable skill.
5. **Can a weak player win?** Yes, off two lucky shoves. High variance.
6. **Funny or frustrating?** Funny in the moment, potentially frustrating in aggregate: the player nobody bribes ends the round on a negative score with nothing to show.

---

## 4. Head-to-head

| | A — Commission | B — Siphon | C — Split Purse |
|---|---|---|---|
| Conflict comes from | Relative standing | The act of playing well | Two jobs, one steering wheel |
| Griefing is | A priced purchase | Strictly dominated | Free, but takes the form of neglect |
| Failure mode | Cheerful co-op / dead parked cars | Solo time trial with insults | Demolition derby, gems rot |
| Agency (own hands) | ~25% | ~66% | ~50% |
| Loudest table | Low | Medium | High |
| Easiest to explain | Yes | Middle | No |
| Best demo beat | The strike | Throwing your own car in a pit | Bribing your unknown driver |

**These three genuinely disagree about the answer to the brief.** A says the conflict should come from the scoreboard, B says it should come from competence, C says it should come from the car itself. They cannot all be right, which is the point.

---

## 5. Halftime structure

### Does the locked structure produce enough social pressure without changing drivers?

**Yes — and the anonymity is doing more work than the reveal.**

- **Phase A (anonymous)** suppresses *targeted* aggression. You cannot revenge someone you cannot name, so the first half is about cars, not people. This is correct: it lets players learn the scoring model before the social game starts. It also produces the premise's signature line — "who is driving my car?" — which is the single most important thing to observe in playtesting.
- **The reveal** does not need to change any mechanic to change behaviour. It changes the *negotiation graph* from anonymous to named. That is enough.
- **Phase B (same mapping)** then hits the structural fact from §0: Rohit now knows Dev is bleeding him and *still cannot touch Dev's car*. His options are to intercept Dev physically with the car he drives, or to go make a deal with Karan. **The cycle converts revenge into diplomacy**, which is exactly the emergent behaviour the brief asks for and is far more interesting than direct retaliation.

### The three options

**A. Same mappings after reveal — RECOMMENDED for the first playtest.**
- Preserves the asymmetric revenge graph, which is the cycle's best property.
- The information you paid 40 seconds of mystery to build stays *actionable*. Learning who drove you matters only if they are still driving you.
- Zero new architecture. Zero re-learning cost.
- Weakness: if the second half feels flat, it will feel flat.

**B. Legal random reshuffle after reveal.**
- **This throws away the reveal.** "Dev was driving you — but he isn't any more" is information with no use. The twist becomes trivia.
- Costs 3–5 seconds of re-orientation in a 35-second half. In a 79-second round that is unaffordable.
- Only worth considering if playtests show players *hard-locked* into detente and needing a shuffle to break it.

**C. Full reverse of the cycle.**
- The brief's suspicion is correct, and it is worse than suspected. Reversal creates mutual pairs: Rohit drives Dev's car while Dev drives Rohit's. Symmetric retaliation with a 35-second horizon is a textbook mutually-assured-destruction setup, and MAD's equilibrium is **peace**. Both players work out in about four seconds that hitting each other is net-negative, and settle.
- Worse: it *removes* the need for diplomacy. Rohit no longer needs Karan for anything. Negotiation — the thing that makes CrossDrive more than a bumper game — is designed out.
- Reversal makes the second half calmer and simpler. That is the opposite of what a second half is for. **Do not test it first.**

### Optional dial, if the second half tests flat
**Double all point values in Phase B.** One line in a scoring-strategy architecture. It creates comeback potential, makes the reveal feel like stakes rising rather than information arriving, and does not touch the mapping. Try this before ever touching mappings.

---

## 6. Round timing

| Segment | Duration | Note |
|---|---|---|
| Assignment card (pre-round) | 4s | `YOU ARE / YOU DRIVE / YOUR DRIVER: ???` + 3-2-1 |
| **Phase A — anonymous** | **40s** | Long enough to learn the scoring model and form a suspicion |
| **Halftime reveal** | **4s** | Frozen arena, not a menu — see §8 |
| **Phase B — known** | **35s** | Deliberately shorter. Urgency, not a second full round |
| Results | 8s | Final standings, then straight into the rematch prompt |
| **In-round total** | **79s** | |
| **Door to door** | **~91s** | Inside the 60–100s target |

Rationale for the asymmetry: the anonymous half must be long enough to generate a grievance; the known half must be short enough that the grievance has to be acted on immediately rather than planned around. 40/35 is the test point. If groups reach a stable detente in Phase B, shorten it to 30s before changing anything else.

---

## 7. First-half information design

Show **all owner identities. Hide only the control mapping.** One secret, not two.

| Element | Shown in Phase A? | Why |
|---|---|---|
| Owner name floating over every car | **Yes** | You must know that ORANGE is Aman's or the scoring rules are unreadable |
| Car colours | Yes | Primary identity |
| **Your own car** | Yes — coloured ground ring + `YOU` tag | |
| **The car you drive** | Yes — white caret above it + screen-edge glow in that car's colour | |
| **Who owns the car you drive** | **Yes** | See below |
| Who drives any car | **No** | The one secret |
| Control tethers | No (Phase A) — yes for 2s at reveal | |

**Use two different visual languages for "mine" and "the one I drive."** A ground ring for ownership, an overhead caret for control. Never the same shape in two colours — that is the single most likely source of Phase-1 confusion, and confusion will be misread as the premise not working.

### Should you see who owns the car you drive? Yes.
It is required for the scoring models to be legible (in Model B, "each gem costs Aman 1" is meaningless if you do not know it is Aman's car), and it leaks exactly the right amount. In a 4-cycle, knowing `Rohit → Aman` leaves two possible cycles, so **your driver is one of two people — a coin flip.** That is enough to accuse and not enough to know. It scales gracefully: 1-in-3 at five players, 1-in-4 at six.

Deduction from driving behaviour ("whoever has my car keeps chasing green") is a *feature*. Do not obfuscate it.

---

## 8. Halftime reveal UX

Requirements: readable with sound off, under 4 seconds, produces an audible reaction, never feels like a menu.

```
0.0s   Physics freeze. Arena desaturates to ~40%. Arena stays fully visible.
0.3s   Four control tethers draw as curved arcs, one after another, ~80ms apart,
       forming the visible loop:  BLUE → ORANGE → GREEN → PINK → BLUE
1.2s   A nameplate slams onto every car simultaneously:  "DRIVEN BY DEV" etc.
       Everyone learns the ENTIRE mapping at once, not just their own.
2.0s   Your own car flashes red. Full-width text, largest type in the game:
              DEV  WAS  DRIVING  YOU
       Device haptic buzz.
3.0s   Text shrinks into the persistent Phase B HUD tag on your car. 3-2-1.
4.0s   Physics resume.
```

Three deliberate choices:
- **Reveal the whole cycle, not just your own driver.** It costs nothing extra, it lets everyone verify or demolish their theory simultaneously, and it makes the loop structure legible for the first time — which is a huge win for demo clarity and for judges watching a 60-second video.
- **Never black out the arena.** Cars stay on screen, frozen mid-collision. A full-screen card reads as a menu; a frozen world reads as a twist.
- **Nameplates persist through Phase B.** The reveal is not a moment, it is a state change.

---

## 9. Playtest protocol

**3 groups × 4 friends. All three models per group. ~45 minutes per group.**

### Order — Latin square (this is the anti-bias design)

| Group | 1st | 2nd | 3rd |
|---|---|---|---|
| G1 | **A** | **C** | **B** |
| G2 | **C** | **B** | **A** |
| G3 | **B** | **A** | **C** |

Every model appears exactly once in each position across the three groups, which cancels first-model learning penalty and last-model recency advantage. With three groups this is the only fair schedule; do not improvise per-group orders.

### Structure per group

| Step | Content | Time |
|---|---|---|
| 0 | Controls-only round, no scoring shown, Model 1's arena | 2 min |
| 1 | Model 1: 1 unrecorded warm-up round + 3 recorded rounds | ~10 min |
| 2 | 60s break. Ask the two questions below. | 2 min |
| 3 | Model 2: same shape | ~10 min |
| 4 | 60s break + two questions | 2 min |
| 5 | Model 3: same shape | ~10 min |
| 6 | **Silent round** — one extra round of whichever model the group liked most, all talking banned | 2 min |
| 7 | **Behavioural vote:** "we have time for two more rounds — which one?" Record the answer, not the reasoning | 1 min |

The **silent round** is the cheapest experiment here. It isolates whether the fun is in the mechanic or in the room. If a model is still fun silent, it survives being played by strangers online.

### What the tester explains
- Controls: auto-drive, steer, one boost.
- "You own one car. You are driving a different player's car. Nobody drives their own."
- The scoring rule, once, in one sentence, exactly as written above.
- "At halftime you find out who's been driving your car."

### What the tester must NOT explain
- That the assignments form a single loop. **Record whether anyone works it out.**
- That sabotage, stalling, blocking, bribery, or lying are possible or allowed.
- Whether talking is allowed (say nothing; let them discover it).
- Any strategy, any hint of the intended optimum, any suggestion that they should protect their own car.
- Do not answer "should I go for gems or crash people?" — say "up to you."

### Observation sheet (one per round)

| Metric | How recorded |
|---|---|
| Time to first laugh | Stopwatch, seconds |
| Time to first accusation | Stopwatch, seconds |
| Number of accusations | Tally |
| **"Who is driving my car?"** (any phrasing) | **Y/N + timestamp — highest-priority signal in the whole test** |
| Negotiations / alliance attempts | Tally + one-line quote |
| Deliberate sabotage acts | Tally |
| Unintentional crashes | Tally |
| Frustration events (visible, audible) | Tally + who |
| Confusion questions about rules/score | Tally |
| Score understanding | After round 3, each player predicts their own placement before results. Record hit rate |
| Dominant strategy discovery | Free text: did anyone find and repeat the boring optimum? |
| Revenge acts after reveal | Tally |
| Spontaneous "one more round?" | Y/N, unprompted only |
| **Accusation accuracy** | Who they blamed vs who actually drove them |
| **Gem pickup rate** (Model C especially) | collected ÷ spawned |
| **Interception attempts** (Model B especially) | Tally |
| Did anyone deduce the loop? | Y/N |

Only two verbal questions, asked at breaks, both deliberately non-evaluative:
1. "Who was driving your car? How do you know?"
2. "What were you trying to do in that last round?"

**Never ask "did you like it?"** Take the answer from step 7 instead.

---

## 10. Scoring rubric (pre-playtest predictions — these exist to be falsified)

| Criterion /10 | A Commission | B Siphon | C Split Purse |
|---|---|---|---|
| Fun | 6 | 8 | 8 |
| Social conflict | 5 | 7 | 9 |
| Fairness | 6 | 8 | 5 |
| Player agency | 4 | 8 | 6 |
| Blame / revenge potential | 6 | 7 | 9 |
| Negotiation potential | 5 | 6 | 9 |
| Understandability | 9 | 6 | 7 |
| Grief resistance | 8 | 9 | 6 |
| Skill expression | 7 | 9 | 7 |
| Randomness balance | 6 | 8 | 5 |
| Implementation simplicity | 9 | 8 | 7 |
| Replayability | 5 | 8 | 6 |
| Demo clarity | 7 | 6 | 9 |
| **Total /130** | **83** | **98** | **93** |

B's score is pulled down by the ledger problem found while checking the simulation arithmetic (§2): every socially interesting act in Model B currently costs the person who performs it. If the "interception pays" dial in §11 works, B goes back up; if it does not, B is a time trial. I am recording B as my prediction so the playtest can prove me wrong. C is the one most likely to beat its prediction in a live room, because the rubric cannot score "everyone shouting at once."

---

## 11. Kill tests

### Model A — Commission
- **GO IF** ≥2 groups discover the driver's strike unprompted, *and* strikes are met with counterplay rather than sulking, *and* ≥2 accusations per round.
- **ITERATE IF** players farm competently and pleasantly with no conflict → drop the owner:driver ratio to 2:1 and re-test, or make live standings far more prominent.
- **KILL IF** two or more groups play three full rounds with zero sabotage and zero accusations. That is Bad Extreme A confirmed, and the model is a co-op game wearing a betrayal skin. Also kill if the strike is discovered and produces four stationary cars.

### Model B — Siphon
- **GO IF** ≥3 deliberate interceptions per round, *and* ≥1 player throws their own car into a pit unprompted, *and* score-prediction accuracy exceeds 50% by round 3.
- **ITERATE IF** the maths confuses people but the behaviour is right → the fix is HUD, not rules. Add an explicit bleed line and a floating `−1` over your car when it is farmed.
- **ITERATE IF** players intercept, enjoy it, and consistently lose for it (the flaw exposed in the simulation above) → make interception pay. Cheapest version: a car that has not collected a gem for 5s stops billing its owner, so a successful block converts directly into stopped bleed.
- **KILL IF** interceptions stay near zero across all three groups. That means the social layer is talk only and B is a four-way time trial.

### Model C — Split Purse
- **GO IF** gem pickup rate >30%, *and* ≥1 successful bribe per round, *and* the bribe market survives to the third round rather than collapsing.
- **ITERATE IF** gems rot but the hunting is genuinely fun → raise gems to +5 for the owner, or make gems also pay the driver +1 (which converts C partway into A and should be treated as a different model, not a tweak).
- **KILL IF** gem pickup rate stays under 30% in all three groups *and* players stop referring to ownership at all. At that point ownership is decorative and CrossDrive has become a bumper-car brawler that any team could have built.

### If all three are structurally weak
The evidence for that would be: **nobody in any group ever asks who is driving their car.** If that happens, the problem is not the economy — it is that the premise is not perceptible in play. The likely causes, in order: (1) the camera does not keep your own car legible; (2) the car you drive and the car you own are visually confusable; (3) 40 seconds is too short to form an attachment to a car you do not steer. Fix those before touching scoring, and re-run the same protocol. If it still happens, the CrossDrive mechanic itself needs rework, not the scoring model.

---

## 12. Honest risks with the core mechanic (independent of scoring)

1. **Attribution ambiguity.** In a bumper-physics game with momentum, players may not be able to tell whether their car did badly because their driver was hostile or because bumper cars are slippery. If blame cannot be assigned confidently, blame is not fun. **Watch accusation accuracy** — if it is near chance in every model, the mechanic is producing noise, not drama.
2. **Attention split.** Every player is asked to watch two cars, one of which they cannot influence. Under time pressure people will drop the one they cannot control — which is their own. That would silently kill the premise. The fixed full-arena camera and a persistent own-car status line are the mitigations.
3. **The premise may be funnier to describe than to play.** This is the real Shipaton risk. The 30-second pitch is excellent; that is not evidence about second-to-second play. The "one more round?" tally in §9 step 7 is the only honest measurement of this.
4. **Four players is the minimum viable cycle for mystery.** At three players, knowing whose car you drive fully determines who drives yours — the mystery collapses to zero. **Do not demo at three players.** This should be a hard floor in matchmaking.

---

## 13. What Codex should build to keep all three testable

- Scoring as a swappable strategy object consuming the six events in §0. No model needs anything else.
- A dev-only hotkey to switch model between rounds without a rebuild — this is what makes a 45-minute playtest session possible at all.
- Per-round JSON telemetry: every event with a timestamp, plus final scores. Half the metrics in §9 can then be counted automatically instead of by a person with a clipboard.
- Point values in a config file, not constants. Every ITERATE branch above is a number change.
