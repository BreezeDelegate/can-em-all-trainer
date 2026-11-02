using System;
using System.Globalization;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;

namespace CanEmAllMod
{
    // Gameplay mutations are deliberately separate from the uGUI render tree.
    // Every dangerous native operation is called only by an explicit user action,
    // never by page construction, label updates, or UI layout.
    internal static class NativeActions
    {
        private static readonly CultureInfo I=CultureInfo.InvariantCulture;
        private static float nextSpawn,nextEquipmentTick,nextTurretTick,restoreSoundAt;
        private static int lastVacuumId=int.MinValue,lastFlameId=int.MinValue;
        private static float originalFlameRange=0f,originalVacuumInterval=0f;
        private static int originalVacuumCapacity=0;
        private static bool capacityOverridden,intervalOverridden,flameOverridden;
        private static bool soundMuted;
        private static float previousVolume;
        private static int pendingWeaponIndex=-1;
        private static float nextWeaponCheck;

        internal static void Tick()
        {
            if(soundMuted && Time.realtimeSinceStartup>=restoreSoundAt)
            {
                AudioListener.volume=previousVolume;
                soundMuted=false;
            }
            if(pendingWeaponIndex>=0 && Time.realtimeSinceStartup>=nextWeaponCheck)
            {
                int desired=pendingWeaponIndex;
                pendingWeaponIndex=-1;
                try
                {
                    var inventory=Inventory.Instance;
                    if(inventory!=null&&inventory.unlockedWeapons!=null&&desired<inventory.unlockedWeapons.Count)
                    {
                        inventory.currentWeaponIndex=desired;
                        inventory.UpdateSelection();
                        MelonLogger.Msg("Weapon selection verified index="+inventory.currentWeaponIndex+
                            " target="+Name(inventory.unlockedWeapons[desired]));
                    }
                }
                catch(Exception ex){MelonLogger.Warning("Weapon selection confirmation: "+ex.GetBaseException().Message);}
            }
            if(Time.realtimeSinceStartup>=nextEquipmentTick)
            {
                nextEquipmentTick=Time.realtimeSinceStartup+1.0f;
                try
                {
                    var vacuum=Vacuum.Instance;
                    if(vacuum!=null)
                    {
                        int id=vacuum.GetInstanceID();
                        if(id!=lastVacuumId)
                        {
                            lastVacuumId=id;
                            originalVacuumInterval=vacuum.throwInterval;
                            originalVacuumCapacity=vacuum.maxCapacityMultiplier;
                            capacityOverridden=false;
                            intervalOverridden=false;
                        }
                        if(TrainerSettings.VacuumCapacity>1.01f && originalVacuumCapacity>0)
                        {
                            vacuum.maxCapacityMultiplier=(int)Math.Min(100,Math.Max(1,
                                Math.Round(originalVacuumCapacity*TrainerSettings.VacuumCapacity)));
                            capacityOverridden=true;
                        }
                        else if(capacityOverridden)
                        {
                            vacuum.maxCapacityMultiplier=originalVacuumCapacity;
                            capacityOverridden=false;
                        }
                        if(TrainerSettings.VacuumFire>1.01f && originalVacuumInterval>0)
                        {
                            vacuum.throwInterval=Math.Max(.1f,originalVacuumInterval/TrainerSettings.VacuumFire);
                            intervalOverridden=true;
                        }
                        else if(intervalOverridden)
                        {
                            vacuum.throwInterval=originalVacuumInterval;
                            intervalOverridden=false;
                        }
                    }
                    var flame=UnityEngine.Object.FindFirstObjectByType(Il2CppType.Of<FlameThrower>(),
                        FindObjectsInactive.Include)?.TryCast<FlameThrower>();
                    if(flame!=null)
                    {
                        int id=flame.GetInstanceID();
                        if(id!=lastFlameId)
                        {
                            lastFlameId=id;
                            originalFlameRange=flame.range;
                            flameOverridden=false;
                        }
                        if(TrainerSettings.FlameRange>1.01f && originalFlameRange>0)
                        {
                            flame.range=originalFlameRange*TrainerSettings.FlameRange;
                            flameOverridden=true;
                        }
                        else if(flameOverridden)
                        {
                            flame.range=originalFlameRange;
                            flameOverridden=false;
                        }
                    }
                }
                catch(Exception ex){MelonLogger.Warning("Equipment tuning: "+ex.GetBaseException().Message);}
            }
            if(TrainerSettings.TurretAmmo && Time.realtimeSinceStartup>=nextTurretTick)
            {
                nextTurretTick=Time.realtimeSinceStartup+2.5f;
                try
                {
                    var turrets=Resources.FindObjectsOfTypeAll(Il2CppType.Of<Turret>());
                    int refilled=0;
                    foreach(var item in turrets)
                    {
                        if(item==null)continue;
                        var turret=item.TryCast<Turret>();
                        if(turret==null||!turret.gameObject.activeInHierarchy||
                            turret.maxBulletCount<=0||turret.bulletCount>turret.maxBulletCount/4)continue;
                        turret.RestoreAmmo();
                        refilled++;
                    }
                    if(refilled>0)MelonLogger.Msg("Turrets supplied="+refilled);
                }
                catch(Exception ex){MelonLogger.Warning("Turret ammo: "+ex.GetBaseException().Message);}
            }
        }
        internal static void SafeRangedAmmo()
        {
            // Reserve-only aid, never touch the reload animation of melee,
            // flamethrower, vacuum, or the single-shot rocket launcher.
            var inv=Inventory.Instance;
            if(inv==null||inv.unlockedWeapons==null||inv.currentWeaponIndex<0||
                inv.currentWeaponIndex>=inv.unlockedWeapons.Count)return;
            var weapon=inv.unlockedWeapons[inv.currentWeaponIndex];
            if(weapon==null)return;
            string name=Name(weapon).ToLowerInvariant();
            if(name.Contains("rocket")||name.Contains("knife")||name.Contains("bat")||
               name.Contains("vacuum")||name.Contains("flame")||name.Contains("chain")||
               name.Contains("punch"))return;
            weapon.AddAmmo(100);
        }
        internal static void RestoreSoundOnExit()
        {
            if(soundMuted){AudioListener.volume=previousVolume;soundMuted=false;}
        }

        internal static string Name(UnityEngine.Object obj)
        {
            if(obj==null)return "Indisponible";
            string name=(obj.name??"Sans nom").Replace("(Clone)","").Trim();
            return name.Length>35?name.Substring(0,32)+"...":name;
        }
        private static bool CanSpawn(out string reason)
        {
            reason="";
            if(Time.realtimeSinceStartup<nextSpawn){reason="Patiente une seconde.";return false;}
            nextSpawn=Time.realtimeSinceStartup+1f;
            return true;
        }
        private static bool Aim(out Vector3 target,out string reason)
        {
            target=Vector3.zero;
            reason="";
            var player=Player.Instance;
            if(player==null||player.mainCamera==null)
            {reason="Camera du joueur indisponible.";return false;}
            var camera=player.mainCamera;
            var pos=camera.transform.position;
            var dir=camera.transform.forward;
            RaycastHit hit;
            target=Physics.Raycast(pos,dir,out hit,35f)
                ?hit.point+hit.normal*.5f+Vector3.up*.8f
                :pos+dir*5f+Vector3.up*.2f;
            if(Vector3.Distance(pos,target)<2f)target=pos+dir*4f;
            return true;
        }
        internal static string SetMoney(long sum)
        {
            if(sum<0||sum>9000000000000L)return "Montant hors limites.";
            try
            {
                var cash=MoneyManager.Instance;
                if(cash==null)return "Partie indisponible.";
                cash.SetMoney(sum);
                TrainerSettings.RecordMoney(sum);
                return "Argent applique : "+sum.ToString("N0",I);
            }
            catch(Exception ex){MelonLogger.Error("Native money: "+ex);return "Modification impossible."; }
        }
        internal static string SpawnCan()
        {
            if(!CanSpawn(out string reason)||!Aim(out Vector3 target,out reason))return reason;
            try
            {
                var gm=GameManager.Instance;
                if(gm==null||gm.pooledCanPrefab==null)return "Prefab de canette indisponible.";
                if(gm.PhysicalCanCount>=Math.Max(10,gm.maxPhysicalCans-1))return "Limite de canettes atteinte.";
                var can=gm.GetCanFromPool(gm.pooledCanPrefab,target,Quaternion.identity);
                if(can==null)return "Le pool n'a rien cree.";
                can.transform.position=target;
                bool active=can.gameObject.activeInHierarchy;
                MelonLogger.Msg("Can spawn id="+can.GetInstanceID()+" active="+active);
                return active?"Canette creee.":"Canette inactive, voir journal.";
            }
            catch(Exception ex){MelonLogger.Error("Spawn can: "+ex);return "Creation impossible, voir journal.";}
        }
        internal static string SpawnEnemy(int index)
        {
            if(!CanSpawn(out string reason)||!Aim(out Vector3 target,out reason))return reason;
            try
            {
                var truck=Truck.Instance;
                if(truck==null||truck.allEnemyPrefabs==null||index<0||index>=truck.allEnemyPrefabs.Count)
                    return "Creature indisponible.";
                if(truck.spawnedEnemies!=null&&truck.spawnedEnemies.Count>=75)return "Limite de 75 creatures.";
                var prefab=truck.allEnemyPrefabs[index];
                if(prefab==null)return "Prefab introuvable.";
                var enemy=truck.GetEnemy(prefab,target,Quaternion.identity);
                if(enemy==null)return "Le pool n'a rien cree.";
                enemy.transform.position=target;
                enemy.gameObject.SetActive(true);
                if(!enemy.gameObject.activeInHierarchy)enemy.transform.SetParent(null,true);
                enemy.SpawnRoaming(Math.Max(4f,truck.roamRadius));
                enemy.transform.position=target;
                if(truck.spawnedEnemies!=null&&!truck.spawnedEnemies.Contains(enemy))
                    truck.spawnedEnemies.Add(enemy);
                var gm=GameManager.Instance;
                if(gm!=null&&gm.registeredEnemies!=null&&!gm.registeredEnemies.Contains(enemy))
                    gm.RegisterEnemy(enemy);
                bool active=enemy.gameObject.activeInHierarchy;
                MelonLogger.Msg("Enemy spawn "+Name(prefab)+" active="+active+" id="+enemy.GetInstanceID());
                return active?"Creature creee : "+Name(prefab):"Creature inactive (journal).";
            }
            catch(Exception ex){MelonLogger.Error("Spawn enemy: "+ex);return "Creation impossible, voir journal.";}
        }
        internal static string UnlockWeapon(int index)
        {
            try
            {
                var inv=Inventory.Instance;
                if(inv==null||inv.allWeapons==null||index<0||index>=inv.allWeapons.Count)
                    return "Inventaire indisponible.";
                var weapon=inv.allWeapons[index];
                if(weapon==null)return "Arme introuvable.";
                inv.UnlockWeapon(weapon);
                int found=-1;
                if(inv.unlockedWeapons!=null)
                    for(int i=0;i<inv.unlockedWeapons.Count;i++)
                        if(inv.unlockedWeapons[i]!=null &&
                           inv.unlockedWeapons[i].GetInstanceID()==weapon.GetInstanceID()){found=i;break;}
                if(found<0)return "Arme non ajoutee a l'inventaire.";
                inv.currentWeaponIndex=found;
                inv.UpdateWeaponIcons();
                inv.UpdateSelection();
                // A game inventory update can otherwise reselect its previous
                // item immediately after the trainer clicked Unlock.
                pendingWeaponIndex=found;
                nextWeaponCheck=Time.realtimeSinceStartup+.08f;
                MelonLogger.Msg("Weapon selected "+Name(weapon)+" index="+found);
                return "Equipee : "+Name(weapon);
            }
            catch(Exception ex){MelonLogger.Error("Unlock weapon: "+ex);return "Arme indisponible (journal).";}
        }
        internal static string AddAmmo(int index,int amount=100)
        {
            try
            {
                var inv=Inventory.Instance;
                if(inv==null||inv.allWeapons==null||index<0||index>=inv.allWeapons.Count)
                    return "Arsenal indisponible.";
                if(amount!=100 && amount!=1000)return "Quantite invalide.";
                inv.allWeapons[index].AddAmmo(amount);
                return amount+" munitions ajoutees.";
            }
            catch(Exception ex){MelonLogger.Error("Add ammo: "+ex);return "Munitions indisponibles.";}
        }
        internal static string Refill()
        {
            try
            {
                var inv=Inventory.Instance;
                if(inv==null)return "Arsenal indisponible.";
                inv.RestoreAllUnlockedWeaponsAmmo();
                return "Munitions rechargees.";
            }
            catch(Exception ex){MelonLogger.Error("Refill ammo: "+ex);return "Recharge impossible.";}
        }
        internal static string KillAll()
        {
            try
            {
                var gm=GameManager.Instance;
                if(gm==null)return "Partie indisponible.";
                gm.KillAll();
                return "Creatures eliminees.";
            }
            catch(Exception ex){MelonLogger.Error("Kill all: "+ex);return "Action impossible.";}
        }
        internal static string UnlockAllWeapons()
        {
            try
            {
                var inv=Inventory.Instance;
                if(inv==null||inv.allWeapons==null)return "Arsenal indisponible.";
                int total=0;
                foreach(Weapon w in inv.allWeapons)
                {
                    if(w==null)continue;
                    try{inv.UnlockWeapon(w);total++;}
                    catch(Exception ex){MelonLogger.Warning("Unlock weapon: "+ex.GetBaseException().Message);}
                }
                return "Armes traitees : "+total;
            }
            catch(Exception ex){MelonLogger.Error("Unlock all weapons: "+ex);return "Action impossible.";}
        }
        internal static string UnlockAllResearch()
        {
            var gm=GameManager.Instance;
            var tree=gm!=null?gm.skillTreeView:null;
            var cash=MoneyManager.Instance;
            if(tree==null||tree.nodes==null||cash==null)return "Arbre indisponible.";
            int purchased=0, remaining=0, master=0, errorCount=0, passes=0;
            long original=cash.Money;
            try
            {
                // We are purchasing via the game's normal node OnClick() path,
                // so prerequisites, effects and save hooks are preserved.
                // The old fixed six-pass loop left deep upgrade chains behind.
                cash.SetMoney(9000000000000L);
                int upper=Math.Min(300,Math.Max(8,tree.nodes.Count+2));
                for(int pass=0;pass<upper;pass++)
                {
                    passes++;
                    tree.RefreshNodeLockStates();
                    int purchasedThisPass=0;
                    for(int i=0;i<tree.nodes.Count;i++)
                    {
                        var node=tree.nodes[i];
                        if(node==null||node.skill==null||node.skill.isPurchased)continue;
                        // The ultimate game upgrade remains player-owned.
                        if(node.skill.skillId==SkillId.MasterKey)continue;
                        if(node.IsDemoLocked || node.IsLocked || !node.AreRequirementsMet())continue;
                        try
                        {
                            node.OnClick();
                            if(node.skill.isPurchased)
                            {
                                purchased++;
                                purchasedThisPass++;
                                if(!soundMuted)
                                {
                                    previousVolume=AudioListener.volume;
                                    AudioListener.volume=0f;
                                    soundMuted=true;
                                }
                                // A newly purchased prerequisite can unlock a
                                // later node in the same pass.
                                tree.RefreshNodeLockStates();
                            }
                        }
                        catch(Exception ex)
                        {
                            errorCount++;
                            if(errorCount<=4)MelonLogger.Warning("Research purchase "+node.skill.skillId+": "+
                                ex.GetBaseException().Message);
                        }
                    }
                    if(purchasedThisPass==0)break;
                }
                tree.RefreshNodeLockStates();
                for(int i=0;i<tree.nodes.Count;i++)
                {
                    var node=tree.nodes[i];
                    if(node==null||node.skill==null||node.skill.isPurchased)continue;
                    if(node.skill.skillId==SkillId.MasterKey){master++;continue;}
                    remaining++;
                }
                MelonLogger.Msg("Research bulk: bought="+purchased+" remaining="+remaining+
                    " masterExcluded="+master+" passes="+passes+" errors="+errorCount);
                if(remaining>0)return purchased+" achats; "+remaining+
                    " ameliorations encore verrouillees (Master exclu).";
                return purchased+" achats; autres ameliorations debloquees (Master exclu).";
            }
            catch(Exception ex)
            {
                MelonLogger.Error("Research unlock: "+ex);
                return purchased+" achats avant interruption (voir journal).";
            }
            finally
            {
                if(soundMuted)restoreSoundAt=Time.realtimeSinceStartup+.35f;
                try{cash.SetMoney(original);}
                catch(Exception ex){MelonLogger.Error("Restore money: "+ex);}
            }
        }
    }
}
