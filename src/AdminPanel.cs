using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace CanEmAllMod
{
    // Runtime state and ongoing effects only. The UI is NativePanel (uGUI).
    // No Unity IMGUI code or asset allocation belongs in this class.
    internal static class AdminPanel
    {
        internal static float Speed=1f,Jump=1f,Conveyor=1f,Damage=1f,Ammo=1f;
        internal static bool InfiniteStamina,InfiniteAmmo,FreezeClock;
        private static float frozenClock,nextAmmoRefresh,nextErrorLog;
        private static int frozenGameId=int.MinValue;

        internal static void OnOpen() { }
        internal static void OnClose(){ }
        internal static void Tick()
        {
            TrainerSettings.Tick();
            if(FreezeClock)
            {
                try
                {
                    var g=GameManager.Instance;
                    if(g!=null)
                    {
                        int id=g.GetInstanceID();
                        if(id!=frozenGameId){frozenGameId=id;frozenClock=g.Timer;}
                        else g.Timer=frozenClock;
                    }
                }
                catch(Exception ex){Throttle("Freeze clock",ex);}
            }
            else frozenGameId=int.MinValue;

            if(InfiniteAmmo && Time.realtimeSinceStartup>=nextAmmoRefresh)
            {
                // Never force RestoreAmmo on ALL weapons every 170ms: this
                // breaks weapon-specific reload coroutines and melee animations.
                nextAmmoRefresh=Time.realtimeSinceStartup+5f;
                try{NativeActions.SafeRangedAmmo();}
                catch(Exception ex){Throttle("Ranged ammo supply",ex);}
            }
        }
        private static void Throttle(string stage,Exception ex)
        {
            if(Time.realtimeSinceStartup<nextErrorLog)return;
            nextErrorLog=Time.realtimeSinceStartup+10f;
            MelonLogger.Warning(stage+": "+ex.GetBaseException().Message);
        }
    }
}
