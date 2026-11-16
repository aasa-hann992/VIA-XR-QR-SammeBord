# Samme bord, samme verden

**Øvelse i tracking til Meta Quest 3: en QR-kode som fælles anchor for to headsets, uden netværk.**

Undervisningsmateriale til XR-teknologier, VIA University College · Andreas Steen Simmelkiær Sørensen · aasa@via.dk

| | |
|---|---|
| **Branch `main`** | Opgaven: projektet virker, men der er huller (`TODO OPGAVE …`), som I udfylder |
| **Branch `loesning`** | Løsningen: alle huller udfyldt, plus udvidelse A (`TwoQrOrigin`) og C (`blocks`) |
| **QR-generator** | <https://aasa-hann992.github.io/VIA-XR-QR-SammeBord/>: lav QR-koder med jeres eget id |
| **Printbare ark** | [`Print/QR_ark.pdf`](Print/QR_ark.pdf): ark 1–8 (én pr. gruppe), P (fysik-demo), L + R (udvidelse A) |

---

## Hvad handler det om?

To personer med hver sit headset har hver sit koordinatsystem. Headsettet bestemmer selv, hvor (0,0,0)
er, ud fra hvor du stod, da appen startede. Placerer I begge en klods i `(0, 1, 2)`, står den to
forskellige steder i rummet.

I løser problemet med en **fysisk ting, I begge kan se: en printet QR-kode**. Begge headsets finder koden,
og begge siger "her er nulpunktet". Så står indholdet samme sted for jer begge, uden at headsetsene
udveksler en eneste byte.

Når I er færdige, kan I:

- læse en QR-kodes **pose** og **payload** med Meta MR Utility Kit (MRUK)
- filtrere QR-koder, så **kun jeres headsets** reagerer på koder med jeres id
- lave et fælles **rum-koordinatsystem**: lås til tyngdekraften, og midl målingerne
- holde indholdet fast med en **spatial anchor**
- vise det samme på begge headsets ud fra et **seed** og **fælles tid**, og forklare, hvorfor **fysik** ikke kan deles på den måde
- **måle** fejlen i millimeter

## Det skal I bruge

- 2 × **Meta Quest 3 eller 3S** (Quest Pro kan ikke læse QR-koder med MRUK, se *Quest Pro-variant*)
- **Unity 6000.4.3f1** med Android Build Support (OpenJDK, SDK & NDK)
- Et printet ark fra `Print/QR_ark.pdf` (print i **100 %**, tjek at målestokken er 100 mm), eller koden vist på en skærm, fx en laptop.
- En lineal

> **Gør det hjemmefra:** klon repoet, og åbn det én gang i Unity (platform: **Android**).
> Første import af Meta-pakkerne og første Gradle-build tager 10–20 minutter. Det skal ikke ske i timen.

```bash
git clone https://github.com/aasa-hann992/VIA-XR-QR-SammeBord.git
```

## Projektet

```
Assets/Scenes/1_QR_SammeBord.unity            ← øvelsen
Assets/Scenes/2_Fysik_IkkeDeterministisk.unity ← demoen (X-knappen skifter scene)
Assets/XRV/Scripts/
  QrCode.cs            opgave 2a – reagér kun på koder med jeres id
  SharedOrigin.cs      opgave 2b + 2c – tyngdekraft-lås og midling
  PayloadContent.cs    opgave 3a – byg indhold ud fra et seed
  QrDebugVisualizer.cs viser akser, payload og opdateringsrate på hver QR-kode
  SharedClock.cs       fælles tid uden netværk (+ ClockSpin, ClockBlink)
  PhysicsTowerDemo.cs  demoen: samme tårn, samme sted, samme tid – forskelligt fald
  ControllerCalibration.cs  Quest Pro-variant / udvidelse B
  StatusHud.cs         teksten nederst i synsfeltet
Assets/XRV/Editor/     VIA XR-menuen: byg scener og APK
docs/index.html        QR-generatoren
Print/                 printbare ark + Python-script til at lave flere
```

**Byg:** *File › Build Profiles › Android › Build And Run*, eller menuen **VIA XR › Byg APK til Quest 3**.

**Knapper i headsettet:** **B** = lås nulpunktet igen · **X** = næste scene · **venstre stick-klik** = skjul tekst

## Hvad står der i QR-koden?

Koderne er almindelig, læsbar tekst:

```
XRV;id=7;model=tower;seed=42;h=10
```

| Felt | Betyder |
|---|---|
| `XRV` | Det er en kode til øvelsen. Alle andre QR-koder i lokalet ignoreres. |
| `id` | **Jeres id.** Kun headsets, hvor `myId` på SharedOrigin er samme tal, reagerer på koden. Begge headsets i parret bruger samme id. |
| `model` | Hvad der skal stå på koden: `tower` (tårn), `orbit` (planeter), `blocks` (jeres eget level) eller `physics` (fysik-demoen). |
| `seed` | Starttallet for tilfældigheden. Samme seed giver præcis det samme »tilfældige« tårn på begge headsets. |
| `h` | Tårnets højde i klodser. |

Lav jeres egne koder på QR-generatoren, eller med en hvilken som helst QR-generator: det er bare tekst.

---

## Trin 1: Hvad ser headsettet? (ca. 20 min)

Byg og kør scenen `1_QR_SammeBord`, og læg jeres ark (fx ark **1**) fladt på bordet. Over koden kommer et akse-kryds
(rød = X, grøn = Y, blå = Z) og en gul tekst. Undersøg og skriv ned:

1. Hvilken akse peger **ud af papiret**? Hvilken peger mod **"KODENS TOP"**?
2. Sidder origin i kodens **midte** eller i et **hjørne**?
3. Hvor tit opdateres posen? (»sidst efter … s«)
4. Hvad sker der med »Tracked«, når I dækker koden?
5. Hvor meget hopper »Pos«, når arket ligger stille?
6. Hvor langt væk og hvor skråt kan I stå?

> QR-tracking i MRUK opdaterer ca. **1 gang i sekundet**. Det er lavet til ting, der står stille, og det er
> netop dét, vi skal bruge: et fast punkt i rummet.

## Trin 2: Et fælles nulpunkt (ca. 30 min)

Vi definerer et fælles **rum-koordinatsystem**:

```
origin = QR-kodens midte
+Y     = lodret op (tyngdekraften, IKKE kodens egen hældning)
+Z     = mod kodens top (væk fra en, der står ved arkets nederste kant)
+X     = mod højre
```

**2a – Reagér kun på jeres id** (`QrCode.cs - TryRead`)
Lige nu reagerer appen på **alle** XRV-koder, også de andre gruppers. Prøv det først! Implementér så
`TryRead`, så den kun returnerer `true`, når feltet `id` er det samme som `myId`.
Skriv jeres id i `myId` på SharedOrigin i inspectoren.

**2b – Lås til tyngdekraften** (`SharedOrigin.cs - GravityAlignedPose`)
Brug kun position + drejning om lodret akse (4 DoF). Skriv jeres svar fra trin 1 ind i `qrLocalNormal` og
`qrLocalTop` på SharedOrigin i inspectoren. Værktøjer: `Vector3.ProjectOnPlane`, `Quaternion.LookRotation`.

**2c – Midl målingerne** (`SharedOrigin.cs - Average`)
Sæt først `mode = Follow`, og se støjen. Implementér så `Average`, og sæt `mode = LockWhenStable`.
Man kan ikke midle vinkler som tal: 359° og 1° giver ikke 180°.

**Tjek:** de to lilla kugler skal sidde præcis på de printede kryds. Mål afvigelsen med linealen.
Prøv `useSpatialAnchor` til og fra, gå en tur rundt i 2 minutter, og mål igen.

## Trin 3: Samme oplevelse fra samme kode (ca. 25 min)

**3a – Det samme tårn på begge headsets** (`PayloadContent.cs - BuildTower`)
Lige nu bygges tårnet med `UnityEngine.Random`: hvert headset får sit **eget** tilfældige tårn. Se det med to
headsets. Skift til `new System.Random(seed)`, og brug seed fra koden. Nu skal I se **præcis** samme tårn.

- Hvorfor giver samme seed det samme tårn?
- Hvorfor ikke bare `UnityEngine.Random.InitState(seed)`?
- **Klæd det ud:** byt klodserne ud med jeres egne prefabs, fx fra stable-spillet.

**Fælles tid:** lav en kode med `model=orbit`. Planeterne styres af uret (`SharedClock`), ikke af `Time.time`.
Bevæger de sig synkront på begge headsets?

## Trin 4: Mål det (ca. 20 min)

| Test | Sådan gør I | Skriv ned |
|---|---|---|
| Kryds | Mål fra lilla kugle til printet kryds | mm, venstre og højre |
| Lås igen | Tryk B, og mål igen | Er fejlen den samme? |
| Uden / med anchor | Gå rundt i 2 min uden at se koden | mm efter turen |
| Fingertest | A rører tårnets top, B sammenligner med sit tårn | cm mellem jer |
| Afstand | Gå 3 m væk, og kig tilbage | Flytter tårnet sig? |

## Demo: fysik er ikke deterministisk

Tryk **X** for at skifte til scenen `2_Fysik_IkkeDeterministisk`, og brug ark **P** (her reagerer alle headsets, uanset id). Begge headsets bygger det
**samme** skæve tårn på det **samme** sted og slipper det på det **samme** tidspunkt (uret). Alligevel vælter
det forskelligt: HUD'en viser, hvor den øverste klods landede. Sammenlign tallene.
Alt, der kan regnes ud, kan deles uden netværk. Fysik kan ikke regnes ud på den måde.

## Lav jeres egne QR-koder

1. Vælg et **id** til jeres par (fx jeres gruppenummer), og skriv det i `myId` på SharedOrigin på **begge** headsets.
2. Åbn **QR-generatoren**: <https://aasa-hann992.github.io/VIA-XR-QR-SammeBord/>
3. Skriv samme id, vælg model og seed, og vis koden på en skærm eller print arket (100 %).

Når opgave 2a er løst, reagerer **kun jeres headsets** på jeres koder.

## Hjælp til hullerne

| Hul | Slå op her |
|---|---|
| 2a | [Dictionary.TryGetValue](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.trygetvalue) · [String.StartsWith](https://learn.microsoft.com/dotnet/api/system.string.startswith) · `PayloadContent.ParsePayload` i projektet |
| 2b | [Vector3.ProjectOnPlane](https://docs.unity3d.com/ScriptReference/Vector3.ProjectOnPlane.html) · [Quaternion.LookRotation](https://docs.unity3d.com/ScriptReference/Quaternion.LookRotation.html) |
| 2c | [Vector3.ProjectOnPlane](https://docs.unity3d.com/ScriptReference/Vector3.ProjectOnPlane.html) · [Vector3.normalized](https://docs.unity3d.com/ScriptReference/Vector3-normalized.html) · tip: `rotation * Vector3.forward` |
| 3a | [System.Random](https://learn.microsoft.com/dotnet/api/system.random) · [UnityEngine.Random](https://docs.unity3d.com/ScriptReference/Random.html) |
| QR i MRUK | [QR Code Detection](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-qrcode-detection) · [Trackables](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-trackables) |
| Anchors | [Spatial Anchors overview](https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-overview/) |

Sidder I fast, står løsningen i branchen `loesning` og på slides.

## Udvidelser

| | Udvidelse | Idé |
|---|---|---|
| **A** | To koder | Læg ark L og R ca. 1 m fra hinanden (de har `id=1` og `side=L` / `side=R`; lav nye på generatoren med jeres id). Linjen mellem dem giver en meget bedre retning (lang baseline). Løsning: `TwoQrOrigin.cs` på `loesning`. |
| **B** | Controller | Slå `ControllerCalibration` til, og rør de to kryds med controllerens spids (A). Hvad er mest præcist? |
| **C** | Level i koden | `XRV;id=7;model=blocks;b=0,0,0,r/1,0,0,g/0,1,0,b`: design et level, lav koden på generator-siden, og giv det videre. |
| **D** | Spil uden netværk | Hvert 5. sekund lyser den samme kugle på begge headsets: `new System.Random(seed + (int)(SharedClock.Seconds / 5))`. Hvem rører den først? |
| **E** | Auto re-sync | Ser I koden igen, og er I drevet, så glid blødt over, helst mens hovedet bevæger sig. |

## Quest Pro-variant

Quest Pro kan ikke læse QR-koder med MRUK. Slå `ControllerCalibration` til på SharedOrigin. Rør
**venstre** kryds med højre controllers spids, og tryk **A**. Gør det samme med **højre** kryds. Begge i parret
skal bruge samme metode og skrive det samme indhold i `manualPayload`.

## Fejlfinding

| Problem | Mulig årsag |
|---|---|
| »QR-tracking: IKKE understøttet« | Quest Pro, for gammelt Horizon OS, eller ingen tilladelse til rumdata. Indstillinger › Privatliv › Rumdata, eller geninstallér og sig ja. |
| Koden ses, men »Ikke til os« | Koden har et andet id end `myId` på SharedOrigin. |
| Vi reagerer også på de andre gruppers koder | Opgave 2a mangler. |
| Indholdet hælder som papiret | Opgave 2b mangler, eller `qrLocalNormal` / `qrLocalTop` er forkerte. |
| Det låser aldrig | Målingerne er uenige: stå stille, gå tættere på, og sørg for godt lys uden genskin. |
| To forskellige tårne | Opgave 3a mangler (`UnityEngine.Random`), eller I kigger på forskellige koder. |

---

Bygget på Unity 6000.4.3f1, Meta XR Core SDK 207 og Meta MR Utility Kit 207 (OpenXR).
Koden må frit bruges og ændres i undervisning (MIT). Meta-pakkerne har deres egen licens.
