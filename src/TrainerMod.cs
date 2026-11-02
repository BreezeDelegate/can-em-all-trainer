using System;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;

#if FULL_GAME
[assembly: MelonInfo(typeof(CanEmAllMod.TrainerMod), "Can 'Em All - Full Admin", "7.3.0", "BreezeDelegate")]
[assembly: MelonGame("LuisDev", "Can 'Em All")]
#else
[assembly: MelonInfo(typeof(CanEmAllMod.TrainerMod), "Can 'Em All - Demo Admin", "7.3.0", "BreezeDelegate")]
[assembly: MelonGame("LuisDev", "Can 'Em All Demo")]
#endif

namespace CanEmAllMod
{
    public sealed class TrainerMod : MelonMod
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint code,uint mapType);
        private int menuKey = 0xDF; // physical French ² key (scan 0x29)

        private static bool menuOpen;

        private bool prevKey;
        private bool cursorSaved;
        private CursorLockMode originalCursorLock;
        private bool originalCursorVisible;

        private MoneyManager money;
        private Player player;
        private GameManager game;
        private int playerId = int.MinValue, gameId = int.MinValue;
        private float nextProbe, nextDiagnostic;
        private int moneyProbes, playerProbes, gameProbes;
        private string message = "";
        private bool savedStamina, originalStamina;
        private float speed = 1, jump = 1, conveyor = 1, damage = 1, ammo = 1;
        private float baseWalk, baseRun, baseJump, baseConveyor, baseDamage, baseAmmo;
        private bool havePlayerBaseline, haveGameBaseline;
        private float lastSpeed=1f, lastJump=1f, lastConveyor=1f, lastDamage=1f, lastAmmo=1f;
        private bool lastStamina;
        private bool moneyErrorLogged;
        private HarmonyLib.Harmony inputHarmony;

        public override void OnInitializeMelon()
        {
            TrainerSettings.Initialize();
#if !FULL_GAME
            DemoUnlocks.Install();
#endif
            try
            {
                uint mapped=MapVirtualKey(0x29,3);
                if(mapped>0 && mapped<256)menuKey=(int)mapped;
                MelonLogger.Msg("Toggle key: scan=0x29 vk=0x"+menuKey.ToString("X2"));
                inputHarmony = new HarmonyLib.Harmony("breezedelegate.canemall.trainer.input");
                var gate = new HarmonyMethod(typeof(TrainerMod).GetMethod(nameof(PausePlayerInput), BindingFlags.Public | BindingFlags.Static));
                int patched = 0;
                // Gameplay-only updates. Other systems keep running, unlike Time.timeScale=0.
                foreach (string method in new[] { "Update", "FixedUpdate" })
                {
                    MethodInfo target = AccessTools.DeclaredMethod(typeof(Player), method);
                    if (target == null) continue;
                    try { inputHarmony.Patch(target, prefix: gate); patched++; }
                    catch (Exception ex) { MelonLogger.Warning("Input gate " + method + ": " + ex.GetBaseException().Message); }
                }
                // Player.Update does not own all combat actions. Inventory and
                // active weapons have their own input loops, and must not attack
                // when the native trainer overlay is accepting pointer clicks.
                foreach(Type type in new[]{typeof(Inventory),typeof(Knife),typeof(BaseballBat),
                    typeof(RocketLauncher),typeof(Vacuum),typeof(FlameThrower),typeof(Shotgun)})
                {
                    MethodInfo combat=AccessTools.DeclaredMethod(type,"Update");
                    if(combat==null)continue;
                    try{inputHarmony.Patch(combat,prefix:gate);patched++;}
                    catch(Exception ex){MelonLogger.Warning("Weapon input gate "+type.Name+": "+ex.GetBaseException().Message);}
                }
                // Blocks code paths that query the game's own "may accept input" property.
                MethodInfo canInput = AccessTools.PropertyGetter(typeof(GameManager), "CanPlayerInput");
                if (canInput != null)
                {
                    try
                    {
                        var prefix = new HarmonyMethod(typeof(TrainerMod).GetMethod(nameof(PauseGameInput), BindingFlags.Public | BindingFlags.Static));
                        inputHarmony.Patch(canInput, prefix: prefix);
                        patched++;
                    }
                    catch (Exception ex) { MelonLogger.Warning("CanPlayerInput gate: " + ex.GetBaseException().Message); }
                }
                MelonLogger.Msg("Trainer v7.3 loaded. Player input patches: " + patched + ". ²: menu.");
            }
            catch (Exception ex) { MelonLogger.Error("Input patches unavailable: " + ex); }
        }

        public static bool PausePlayerInput() { return !menuOpen; }
        public static bool PauseGameInput(ref bool __result)
        {
            if (!menuOpen) return true;
            __result = false;
            return false;
        }

        private static T FindComponent<T>() where T : Component
        {
            try
            {
                // Non-generic Unity API avoids missing AOT generic instantiations on IL2CPP.
                var obj = UnityEngine.Object.FindFirstObjectByType(Il2CppType.Of<T>(), FindObjectsInactive.Include);
                return obj as T;
            }
            catch { return null; }
        }

        private void ProbeManagers()
        {
            if (Time.realtimeSinceStartup < nextProbe) return;
            nextProbe = Time.realtimeSinceStartup + 1.0f;

            try
            {
                if (money == null)
                {
                    money = MoneyManager.Instance;
                    if (money == null) money = FindComponent<MoneyManager>();
                    moneyProbes++;
                }
                if (player == null)
                {
                    player = Player.Instance;
                    if (player == null) player = FindComponent<Player>();
                    playerProbes++;
                }
                if (game == null)
                {
                    game = GameManager.Instance;
                    if (game == null) game = FindComponent<GameManager>();
                    gameProbes++;
                }

                if (player != null)
                {
                    int current = player.GetInstanceID();
                    if (!havePlayerBaseline || current != playerId)
                    {
                        playerId = current;
                        baseWalk = player.walkSpeed;
                        baseRun = player.runSpeed;
                        baseJump = player.jumpForce;
                        lastSpeed = lastJump = 1f;
                        lastStamina = false;
                        originalStamina = player.infiniteStamina;
                        savedStamina = true;
                        havePlayerBaseline = true;
                        MelonLogger.Msg("Player ready. ID=" + current + " base run=" + baseRun);
                    }
                }
                if (game != null)
                {
                    int current = game.GetInstanceID();
                    if (!haveGameBaseline || current != gameId)
                    {
                        gameId = current;
                        baseConveyor = game.conveyorSpeed;
                        baseDamage = game.damageMultiplier;
                        baseAmmo = game.ammoCapacityMultiplier;
                        lastConveyor = lastDamage = lastAmmo = 1f;
                        haveGameBaseline = true;
                        MelonLogger.Msg("GameManager ready. ID=" + current + " base conveyor=" + baseConveyor);
                    }
                }

                if (money != null && !moneyErrorLogged)
                {
                    moneyErrorLogged = true;
                    MelonLogger.Msg("MoneyManager ready. Current money=" + money.Money + " (using game native SetMoney).");
                }
                if (money == null && Time.realtimeSinceStartup >= nextDiagnostic)
                {
                    nextDiagnostic = Time.realtimeSinceStartup + 10f;
                    MelonLogger.Warning("MoneyManager unresolved (singleton + scene search); probes=" + moneyProbes + "; player=" + (player != null) + "; game=" + (game != null));
                }
            }
            catch (Exception ex)
            {
                if (Time.realtimeSinceStartup >= nextDiagnostic)
                {
                    nextDiagnostic = Time.realtimeSinceStartup + 10f;
                    MelonLogger.Error("Scene binding failed: " + ex);
                }
            }
        }

        public override void OnUpdate()
        {
            bool pressed = (GetAsyncKeyState(menuKey) & 0x8000) != 0;
            if (pressed && !prevKey) Toggle();
            prevKey = pressed;
            ProbeManagers();
#if !FULL_GAME
            DemoUnlocks.Update();
#endif
            AdminPanel.Tick();
            NativeActions.Tick();
            NativePanel.Tick(menuOpen,Toggle);

            if (menuOpen) EnforceCursor();
            try
            {
                speed=AdminPanel.Speed; jump=AdminPanel.Jump; conveyor=AdminPanel.Conveyor;
                damage=AdminPanel.Damage; ammo=AdminPanel.Ammo;
                bool requestedStamina=AdminPanel.InfiniteStamina;
                if (player != null && havePlayerBaseline)
                {
                    if (speed > 1f || lastSpeed > 1f)
                    {
                        player.walkSpeed = baseWalk * speed;
                        player.runSpeed = baseRun * speed;
                    }
                    if (jump > 1f || lastJump > 1f) player.jumpForce = baseJump * jump;
                    if (savedStamina && (requestedStamina || lastStamina)) player.infiniteStamina = requestedStamina || originalStamina;
                    lastSpeed = speed; lastJump = jump; lastStamina = requestedStamina;
                }
                if (game != null && haveGameBaseline)
                {
                    if (conveyor > 1f || lastConveyor > 1f) game.conveyorSpeed = baseConveyor * conveyor;
                    if (damage > 1f || lastDamage > 1f) game.damageMultiplier = baseDamage * damage;
                    if (ammo > 1f || lastAmmo > 1f) game.ammoCapacityMultiplier = baseAmmo * ammo;
                    lastConveyor = conveyor; lastDamage = damage; lastAmmo = ammo;
                }
            }
            catch (Exception ex) { message = "Valeur indisponible : " + ex.GetBaseException().Message; }
        }

        private void Toggle()
        {
            menuOpen = !menuOpen;
            if (menuOpen)
            {
                AdminPanel.OnOpen();
                originalCursorLock = Cursor.lockState;
                originalCursorVisible = Cursor.visible;
                cursorSaved = true;
                EnforceCursor();
                CaptureEventSystem();
                NativePanel.Show(true);
            }
            else { NativePanel.Show(false); AdminPanel.OnClose(); TrainerSettings.Save(); RestoreEventSystem(); RestoreCursor(); }
        }

        private void EnforceCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CaptureEventSystem();
        }

        private void CaptureEventSystem()
        {
            // The uGUI panel uses the existing EventSystem. A fullscreen raycast
            // shield blocks clicks behind the overlay; Harmony gates game input.
            // Disabling the EventSystem would also disable the trainer's buttons.
            try
            {
                var events=EventSystem.current;
                if(events!=null && !events.enabled)events.enabled=true;
            }
            catch(Exception ex){MelonLogger.Warning("EventSystem: "+ex.GetBaseException().Message);}
        }
        private void RestoreEventSystem() { }
        private void RestoreCursor()
        {
            if (!cursorSaved) return;
            Cursor.lockState = originalCursorLock;
            Cursor.visible = originalCursorVisible;
            cursorSaved = false;
        }

        public override void OnApplicationQuit()
        {
            menuOpen=false;
            AdminPanel.OnClose();
            NativePanel.Destroy();
            NativeActions.RestoreSoundOnExit();
            TrainerSettings.Save();
#if !FULL_GAME
            DemoUnlocks.SetEnabled(false);
#endif
            RestoreEventSystem();
            RestoreCursor();
        }
    }
}
