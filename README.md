# Can 'Em All - In-Game Trainer

An unofficial **MelonLoader mod for the full Windows release of Can 'Em All**. It adds a nine-tab menu directly inside the game. No separate trainer executable or account is required.

**[Download the latest release](https://github.com/BreezeDelegate/can-em-all-trainer/releases/latest)** · [Installation](#installation) · [Controls](#controls) · [Known limitations](#known-limitations)

> **Language:** The menu supports **English and French** in one DLL. English is the default on fresh installs. Choose **English** or **Français** with the flag icons beside Close. Your choice is saved.
>
> **Single-player use.** This is an independent community project, not affiliated with LuisDev.

## Menu preview

![French-language menu captured in the game](docs/images/trainer-fr-v7.2.png)

*Real in-game screenshot of the v7.2 French interface. Starting with v7.4.1, the menu has a flag-based English/Français selector beside Close. The image shows an older menu; it does not reflect the v7.4.2 flags, icons or alignment refinements.*

## What it does

| Menu tab | Functions |
| --- | --- |
| **Player** (Joueur) | Money, stamina, player speed, jumping and damage |
| **Creatures** (Créatures) | Select and spawn available creature models |
| **Cans** (Canettes) | Spawn cans using the game's object pool |
| **Weapons** (Armes) | Unlock/select weapons, add ammunition, safe automatic ammo supply |
| **Chances** (Probabilities) | Adjust critical hits and selected combat/production odds |
| **World** (Monde) | Adjust day/night duration, conveyor, truck count and clock; equipment controls |
| **Progression** | Purchase normal research upgrades and send the native factory-sale offer email |
| **Save** (Sauvegarde) | View statistics and set the 100,000 children-fed count for Steam progression |
| **Casino** | Send the native casino unlock email and adjust slot-machine win/near-miss odds |

**Monde → Équipement** also offers turret ammo supply, vacuum capacity/throw rate and flamethrower range.

**Steam guide download compatibility:** The existing v7.3.0 and v7.2.0 ZIP links both download the current v7.4.2 trainer. Old ZIP file names and Git tags are retained to avoid broken guide links. The source tag v7.4.2 remains available for source provenance.

## Installation

**Requirements:** full Steam game on **Windows**, with **MelonLoader 0.7.3**. This mod does **not** target the separately released demo.

1. Back up your game save before changing any progression.
2. Install [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3) for the **full game's executable**. Run the game once and exit to initialize the loader.
3. Download **[current trainer v7.4.2, Steam-compatible download](https://github.com/BreezeDelegate/can-em-all-trainer/releases/download/v7.3.0/CanEmAll-Trainer-v7.3.0-Windows.zip)**.
4. Extract the ZIP **into the game's root folder** (Steam → the game → Properties → Installed Files → Browse). The final file must be `<game folder>/Mods/CanEmAllFullMod.dll`.
5. Start the game and press **F8** or the **physical key immediately below Esc** to open/close the trainer (² on French AZERTY keyboards, backtick/tilde on many QWERTY keyboards). Select **English** or **Français** beside Close.

The game is typically installed in `steamapps/common/Can 'Em All/`. Do not copy the DLL into `MelonLoader/` or the game's demo directory.

## Controls

- **F8** or **the physical key below Esc**: open or close the trainer.
- **English / Français** with flag icons in the top-right corner: change language without restarting; the choice is saved.
- **Click the money field** - edit a value. Use Enter to apply; Ctrl+A / Ctrl+V for selection/paste.
- **Shift + click** on numerical +/- controls - change the value by 10 instead of 1.
- **Monde → Équipement** - optional controls for turret supply, vacuum and flamethrower.
- **Progression > Unlock upgrades**: purchase normal research upgrades. The ultimate exit unlock is intentionally excluded.
- **Progression > Send Factory Offer**: receive the company offer email; open it on the in-game computer and select Accept to unlock factory sales.
- **Casino > Send Casino Email**: receive Feeling Lucky, then select Accept in the game mail to unlock the casino.
- **Save > Set 100,000 Fed**: set the current and lifetime children-fed counters to 100,000 and request the corresponding Steam achievement.
- **Casino odds**: Near Miss (Almost Win, Still Lose) means a close-looking but losing spin, not an additional winning chance.

## Known limitations

- The UI includes English and French in one release. Other languages are not supported yet.
- The casino and factory-sale options unlock through their original in-game emails. The trainer delivers those emails, but **you must accept them in the game**.
- Permanent prestige upgrades still require selling the factory and spending earned points. The final exit upgrade is not granted directly.
- Automatic ammo supply deliberately avoids melee weapons and weapons with delicate reload sequences, including the rocket launcher.
- Not every feature has been interactively tested across all game versions, weapons, scenes or save states. Compatibility after game updates is not guaranteed.
- The mod can affect progression, achievements and save-state expectations. **Back up your save.**

## Uninstall and troubleshooting

With the game closed, remove `Mods/CanEmAllFullMod.dll`. Uninstalling the trainer does **not** undo gameplay or progression changes. Optional preferences may remain in `UserData/CanEmAllTrainer.ini`.

If it doesn't load, verify you're playing the **full game**, MelonLoader is installed correctly, and only one copy of this trainer DLL exists. Logs are in `MelonLoader/Logs/`.

When reporting a bug, include the game's build/version, the MelonLoader version, the affected tab or weapon, and relevant log lines (remove personal paths first).

## Source, verification and attribution

The `src/` directory contains the mod's C# source. The release archive contains only **our mod DLL and installation instructions**; it does not bundle MelonLoader, Unity assemblies, save files or proprietary game art. Game UI sprites are used from the user's installed game at runtime.

Version 7.4.2 passed 10 hotkey regression cases, 20 localization tests, managed settings tests, and a headless Windows/Unity smoke of 180 tab navigations with 20/20 official icon references available. The automated smoke does not prove the result of every gameplay-changing action or substitute for interactive screenshot review.

The project was developed with substantial AI-assisted programming and iterative runtime testing. No repository license has yet been selected; publication of source is not a blanket permission for redistribution or derivative works.
