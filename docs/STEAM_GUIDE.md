# Steam Guide — Can 'Em All In-Game Trainer (English / Français)

## Steam editor metadata

**Title:** How to Install the Can 'Em All In-Game Trainer (EN/FR, MelonLoader)

**Short description:** Free single-player in-game trainer for Can 'Em All. Install with MelonLoader, change language between English and French, and use money, weapons, spawns, factory and upgrade settings.

**Suggested category:** Modding or Configuration (choose the closest category Steam offers).

**Cover:** A real screenshot of the trainer in-game. The existing French screenshot is suitable because the trainer supports FR and EN. No additional screenshots are required. Add a caption noting that the screenshot uses French mode.

**Do not post a false Nexus Mods status.** The mod is currently released on GitHub; Nexus Mods is undecided.

---

## Chapter 1 — About this mod

I put together an unofficial in-game trainer for the full Windows release of Can 'Em All. It adds a small menu directly in the game, without needing a separate trainer executable or account.

You can adjust money and player stats, spawn creatures and cans, unlock weapons, tweak factory equipment, and experiment with the game's progression.

The mod is free and intended for single-player use. It is not affiliated with the game's developer.

**English and French are both included.** The menu starts in English on a new installation. Switch between **EN** and **FR** at the top right whenever you like. Your choice is saved.

[Attach the real screenshot from docs/images/trainer-fr-v7.2.png. Caption: "Older v7.2 French menu screenshot; v7.3 adds an EN / FR switch beside Close."]

## Chapter 2 — Requirements and download

You'll need:

- The **full Windows game** on Steam — not the separate demo.
- **MelonLoader 0.7.3** installed for the game.
- A backup of your game save. A trainer can affect your progression and achievements.

**Download the trainer v7.3.0 (EN/FR):**

https://github.com/BreezeDelegate/can-em-all-trainer/releases/tag/v7.3.0

**Get MelonLoader 0.7.3:**

https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3

Download the Windows ZIP from the GitHub Release's **Assets** section. The archive contains the DLL and a README, not original game files.

## Chapter 3 — Installation

1. In Steam, right-click **Can 'Em All** and select **Properties → Installed Files → Browse**.
2. Install MelonLoader 0.7.3 for the **full game's executable** in that folder. Start the game once, then exit.
3. Open the trainer ZIP and extract it into the **game's main folder**. It already contains a Mods subfolder.
4. Check that the file **Mods/CanEmAllFullMod.dll** exists.
5. Start the game.

You don't need to run a separate trainer program or copy any files into the MelonLoader directory.

## Chapter 4 — Opening the menu and choosing a language

Press the **physical keyboard key immediately below Esc and to the left of 1**. On a French AZERTY layout, this is **²**; on many QWERTY layouts, it's the backtick/tilde key.

Press the same key to close the menu.

At the upper-right of the trainer, beside **Close**, click **EN** for English or **FR** for French. You can switch without restarting the game, and the language will be saved for future sessions.

Other controls:
- Click the money field, enter a number, and press Enter to apply.
- Hold Shift while clicking + or - to change values by 10 instead of 1.
- In World, open Equipment for extra vacuum, flamethrower and turret settings.

## Chapter 5 — Features

| Tab | What it controls |
| --- | --- |
| **Player** | Money, stamina, speed, jump and damage |
| **Creatures** | Select and spawn available creatures |
| **Cans** | Spawn cans through the game's pool |
| **Weapons** | Unlock/equip weapons, add ammo, toggle safe supply |
| **Chances** | Combat and production probabilities |
| **World** | Day/night timing, conveyor, truck count and equipment |
| **Progression** | Normal skill tree and available weapons |
| **Save** | View save statistics |
| **Casino** | Slot machine odds, when the casino is available |

**World → Equipment** contains turret ammo supply, vacuum capacity and throw rate, and flamethrower range.

## Chapter 6 — Known limitations

- The trainer **does not force-unlock the casino** or change the permanent prestige upgrade system.
- The final exit/ultimate unlock is intentionally excluded from "unlock all upgrades."
- Automatic ammo supply avoids weapons with delicate reload behavior, particularly the rocket launcher and melee weapons.
- Not all settings have been interactively checked in every game state or across future game updates.
- Make a save backup before experimenting.

The trainer displays some sprites from the installed game's runtime. No original game asset bundles are included.

## Chapter 7 — Uninstalling, troubleshooting and updates

Close the game, then remove **Mods/CanEmAllFullMod.dll**. Uninstalling does not undo progression or money changes already written into your save.

If the menu won't open, make sure MelonLoader targets the full game and check **MelonLoader/Logs** for errors. Include your game build, MelonLoader version, and steps to reproduce when reporting a bug.

**Code, updates and issue reports:**

https://github.com/BreezeDelegate/can-em-all-trainer

---

## Steam posting checks

- Use the screenshot as the guide cover. If it's only on the clipboard, save it first using Paint or your usual screenshot application.
- Paste the seven chapters individually using Steam's **Add Section** or equivalent control.
- Choose a short description and cover independently from the chapter content.
- Use direct GitHub links. Never obfuscate URLs to evade Steam filtering.
- Preview the guide and confirm that the GitHub Release link opens. Steam can apply moderation to links, so don't claim it was validated until it is live.
- Publish one relevant Steam guide; avoid spammed promotional discussion threads.
- No Nexus Mods "publication in progress" claim: no submission was made.
