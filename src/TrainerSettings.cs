using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace CanEmAllMod
{
    // Managed-only persistent preferences. Does not serialize Unity / IL2CPP objects.
    internal static class TrainerSettings
    {
        internal static readonly Dictionary<string,float> Chances =
            new Dictionary<string,float>(StringComparer.Ordinal);
        private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        private static string settingsPath="";
        private static string lastSnapshot="";
        private static float nextSave, nextReplay, nextWarning;
        private static int previousGameId=int.MinValue, previousTruckId=int.MinValue,
            previousRewardId=int.MinValue,previousMoneyId=int.MinValue,previousSlotId=int.MinValue;
        internal static float CasinoObservedWin=.6f, CasinoObservedNear=.3f;
        internal static bool CasinoAvailable, CasinoSpinning;
        internal static long? PinnedMoney;
        internal static int? DaySeconds, NightSeconds, TruckCount;
        internal static bool TurretAmmo;
        internal static float VacuumCapacity=1f,VacuumFire=1f,FlameRange=1f;

        private static void Warn(string stage,Exception error)
        {
            if(UnityEngine.Time.realtimeSinceStartup<nextWarning)return;
            nextWarning=UnityEngine.Time.realtimeSinceStartup+10f;
            MelonLogger.Warning("Settings "+stage+": "+error.GetBaseException().Message);
        }
        private static float ParseFloat(Dictionary<string,string> values,string key,float initial,float min,float max)
        {
            string raw;
            float f;
            if(values.TryGetValue(key,out raw) && float.TryParse(raw,NumberStyles.Float,Invariant,out f)
               && !float.IsNaN(f) && !float.IsInfinity(f))
                return Math.Max(min,Math.Min(max,f));
            return initial;
        }
        private static bool ParseBool(Dictionary<string,string> values,string key)
        {
            string s;
            return values.TryGetValue(key,out s)&&s=="1";
        }
        private static int? ParseInt(Dictionary<string,string> v,string k,int min,int max)
        {
            string raw;
            int i;
            if(v.TryGetValue(k,out raw)&&int.TryParse(raw,NumberStyles.Integer,Invariant,out i))
                return Math.Max(min,Math.Min(max,i));
            return null;
        }
        internal static void Initialize()
        {
            try
            {
                string executable=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                string gameRoot=Path.GetDirectoryName(executable);
                if(string.IsNullOrEmpty(gameRoot))gameRoot=Environment.CurrentDirectory;
                settingsPath=Path.Combine(gameRoot,"UserData","CanEmAllTrainer.ini");
                if(File.Exists(settingsPath))
                {
                    var v=new Dictionary<string,string>(StringComparer.Ordinal);
                    foreach(string line in File.ReadAllLines(settingsPath))
                    {
                        int split=line.IndexOf('=');
                        if(split<=0)continue;
                        v[line.Substring(0,split)]=line.Substring(split+1);
                    }
                    AdminPanel.Speed=ParseFloat(v,"Speed",1,1,5);
                    AdminPanel.Jump=ParseFloat(v,"Jump",1,1,5);
                    AdminPanel.Damage=ParseFloat(v,"Damage",1,1,10);
                    AdminPanel.Conveyor=ParseFloat(v,"Conveyor",1,1,5);
                    AdminPanel.Ammo=ParseFloat(v,"Ammo",1,1,10);
                    AdminPanel.InfiniteStamina=ParseBool(v,"InfiniteStamina");
                    AdminPanel.InfiniteAmmo=ParseBool(v,"InfiniteAmmo");
                    AdminPanel.FreezeClock=ParseBool(v,"FreezeClock");
                    TurretAmmo=ParseBool(v,"TurretAmmo");
                    VacuumCapacity=ParseFloat(v,"VacuumCapacity",1,1,10);
                    VacuumFire=ParseFloat(v,"VacuumFire",1,1,3);
                    FlameRange=ParseFloat(v,"FlameRange",1,1,5);
                    var money=ParseInt64(v,"PinnedMoney",0,9000000000000L);
                    if(money.HasValue)PinnedMoney=money.Value;
                    DaySeconds=ParseInt(v,"DaySeconds",30,3600);
                    NightSeconds=ParseInt(v,"NightSeconds",15,1800);
                    TruckCount=ParseInt(v,"TruckCount",1,60);
                    foreach(string k in new[]{"crit","instant","free","explosive","shock","bonus","shiny","double","casino.win","casino.near"})
                    {
                        if(v.ContainsKey("chance."+k))
                            Chances[k]=ParseFloat(v,"chance."+k,0,0,1);
                    }
                    MelonLogger.Msg("Trainer settings loaded from "+settingsPath);
                }
                else MelonLogger.Msg("Trainer settings path: "+settingsPath);
            }
            catch(Exception ex)
            {
                Warn("load",ex);
                if(string.IsNullOrEmpty(settingsPath))
                  settingsPath=Path.Combine(Environment.CurrentDirectory,"UserData","CanEmAllTrainer.ini");
            }
            lastSnapshot=File.Exists(settingsPath)?BuildSnapshot():"";
        }
        private static long? ParseInt64(Dictionary<string,string> v,string key,long min,long max)
        {
            string raw;
            long number;
            if(v.TryGetValue(key,out raw)&&long.TryParse(raw,NumberStyles.Integer,Invariant,out number))
                return Math.Max(min,Math.Min(max,number));
            return null;
        }
        internal static void SetChance(string key,float value)
        {
            Chances[key]=Math.Max(0,Math.Min(1,value));
            previousGameId=previousTruckId=previousRewardId=previousSlotId=int.MinValue;
        }
        internal static void RecordMoney(long money){PinnedMoney=money;}
        internal static void RecordDay(int v){DaySeconds=v;}
        internal static void RecordNight(int v){NightSeconds=v;}
        internal static void RecordTruck(int v){TruckCount=v;}

        private static string BuildSnapshot()
        {
            var b=new StringBuilder();
            b.AppendLine("# CanEmAll Trainer - managed settings, full game");
            b.AppendLine("Speed="+AdminPanel.Speed.ToString("R",Invariant));
            b.AppendLine("Jump="+AdminPanel.Jump.ToString("R",Invariant));
            b.AppendLine("Damage="+AdminPanel.Damage.ToString("R",Invariant));
            b.AppendLine("Conveyor="+AdminPanel.Conveyor.ToString("R",Invariant));
            b.AppendLine("Ammo="+AdminPanel.Ammo.ToString("R",Invariant));
            b.AppendLine("InfiniteStamina="+(AdminPanel.InfiniteStamina?"1":"0"));
            b.AppendLine("InfiniteAmmo="+(AdminPanel.InfiniteAmmo?"1":"0"));
            b.AppendLine("FreezeClock="+(AdminPanel.FreezeClock?"1":"0"));
            b.AppendLine("TurretAmmo="+(TurretAmmo?"1":"0"));
            b.AppendLine("VacuumCapacity="+VacuumCapacity.ToString("R",Invariant));
            b.AppendLine("VacuumFire="+VacuumFire.ToString("R",Invariant));
            b.AppendLine("FlameRange="+FlameRange.ToString("R",Invariant));
            if(PinnedMoney.HasValue)b.AppendLine("PinnedMoney="+PinnedMoney.Value.ToString(Invariant));
            if(DaySeconds.HasValue)b.AppendLine("DaySeconds="+DaySeconds.Value.ToString(Invariant));
            if(NightSeconds.HasValue)b.AppendLine("NightSeconds="+NightSeconds.Value.ToString(Invariant));
            if(TruckCount.HasValue)b.AppendLine("TruckCount="+TruckCount.Value.ToString(Invariant));
            foreach(string k in new[]{"crit","instant","free","explosive","shock","bonus","shiny","double","casino.win","casino.near"})
                if(Chances.ContainsKey(k))b.AppendLine("chance."+k+"="+Chances[k].ToString("R",Invariant));
            return b.ToString();
        }
        internal static void Save()
        {
            try
            {
                string next=BuildSnapshot();
                if(next==lastSnapshot)return;
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                string tmp=settingsPath+".tmp";
                File.WriteAllText(tmp,next,new UTF8Encoding(false));
                if(File.Exists(settingsPath))File.Replace(tmp,settingsPath,null);
                else File.Move(tmp,settingsPath);
                lastSnapshot=next;
            }
            catch(Exception ex){Warn("save",ex);}
        }
        internal static void Tick()
        {
            if(Time.realtimeSinceStartup>=nextReplay)
            {
                nextReplay=Time.realtimeSinceStartup+1.2f;
                ApplyToNewScene();
            }
            if(Time.realtimeSinceStartup>=nextSave)
            {
                nextSave=Time.realtimeSinceStartup+2f;
                Save();
            }
        }
        private static void ApplyToNewScene()
        {
            try
            {
                bool needGame=DaySeconds.HasValue || NightSeconds.HasValue || PinnedMoney.HasValue ||
                    Chances.ContainsKey("crit") || Chances.ContainsKey("instant") ||
                    Chances.ContainsKey("free") || Chances.ContainsKey("explosive") ||
                    Chances.ContainsKey("shock") || Chances.ContainsKey("bonus");
                var gm=needGame?GameManager.Instance:null;
                if(gm!=null)
                {
                    int id=gm.GetInstanceID();
                    if(id!=previousGameId)
                    {
                        previousGameId=id;
                        if(DaySeconds.HasValue)gm.dayDuration=DaySeconds.Value;
                        if(NightSeconds.HasValue)gm.nightShiftDuration=NightSeconds.Value;
                        float v;
                        if(Chances.TryGetValue("crit",out v))gm.criticProbability=v;
                        if(Chances.TryGetValue("instant",out v))gm.instantDeathProbability=v;
                        if(Chances.TryGetValue("free",out v))gm.freeBulletProbability=v;
                        if(Chances.TryGetValue("explosive",out v))gm.explosiveBulletProbability=v;
                        if(Chances.TryGetValue("shock",out v))gm.shockProbability=v;
                        if(Chances.TryGetValue("bonus",out v))gm.holeInOneProbability=v;
                    }
                    var cash=MoneyManager.Instance;
                    if(cash!=null && gm!=null && Time.realtimeSinceStartup>8f)
                    {
                        int moneyId=cash.GetInstanceID();
                        if(moneyId!=previousMoneyId)
                        {
                            previousMoneyId=moneyId;
                            if(PinnedMoney.HasValue)cash.SetMoney(PinnedMoney.Value);
                        }
                    }
                }
                var truck=(TruckCount.HasValue || Chances.ContainsKey("shiny"))?Truck.Instance:null;
                if(truck!=null)
                {
                    int id=truck.GetInstanceID();
                    if(id!=previousTruckId)
                    {
                        previousTruckId=id;
                        if(TruckCount.HasValue)truck.enemyCount=TruckCount.Value;
                        float value;
                        if(Chances.TryGetValue("shiny",out value))truck.shinyProbability=value;
                    }
                }
                var reward=Chances.ContainsKey("double")?RewardMachine.Instance:null;
                if(reward!=null)
                {
                    int id=reward.GetInstanceID();
                    if(id!=previousRewardId)
                    {
                        previousRewardId=id;
                        float value;
                        if(Chances.TryGetValue("double",out value))reward.duplicateMoneyProbability=value;
                    }
                }

                // Casino interop was verified with an isolated MelonLoader probe:
                // read and write-back on the main game's native SlotMachine succeeded.
                // Never call this from Unity OnGUI, only from throttled OnUpdate.
                var slot=(Time.realtimeSinceStartup>5f && GameManager.Instance!=null)?SlotMachine.Instance:null;
                if(slot!=null)
                {
                    int id=slot.GetInstanceID();
                    CasinoAvailable=true;
                    CasinoSpinning=slot.IsSpinning;
                    if(id!=previousSlotId)
                    {
                        previousSlotId=id;
                        float val;
                        if(Chances.TryGetValue("casino.win",out val))slot.winChance=val;
                        if(Chances.TryGetValue("casino.near",out val))slot.nearMissChance=val;
                    }
                    CasinoObservedWin=slot.winChance;
                    CasinoObservedNear=slot.nearMissChance;
                }
                else
                {
                    CasinoAvailable=false;
                    CasinoSpinning=false;
                    previousSlotId=int.MinValue;
                }
            }
            catch(Exception ex){Warn("replay",ex);}
        }
    }
}
