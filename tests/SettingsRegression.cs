using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace UnityEngine { public static class Time { public static float realtimeSinceStartup {get;set;} } }
namespace MelonLoader { public static class MelonLogger { public static void Msg(string s) {} public static void Warning(string s) { Console.WriteLine(s); } } }
namespace Il2Cpp {
 public class GameManager {
  public static GameManager Instance {get;set;}
  public int Id=1;public int GetInstanceID()=>Id;
  public int dayDuration=100,nightShiftDuration=60;
  public float criticProbability=.1f,instantDeathProbability=.2f,freeBulletProbability=.3f,explosiveBulletProbability=.4f,shockProbability=.5f,holeInOneProbability=.6f;
 }
 public class MoneyManager {
  public static MoneyManager Instance {get;set;}
  public int Id=4; public int GetInstanceID()=>Id;
  public long money=10;
  public void SetMoney(long v) {money=v;}
 }
 public class Truck {
  public static Truck Instance {get;set;}
  public int Id=2;public int GetInstanceID()=>Id;public int enemyCount=4; public float shinyProbability=.1f;
 }
 public class RewardMachine {
  public static RewardMachine Instance {get;set;}
  public int Id=3;public int GetInstanceID()=>Id;public float duplicateMoneyProbability=.2f;
 }
 public class SlotMachine {
  public static SlotMachine Instance {get;set;}
  public int Id=5;public int GetInstanceID()=>Id;
  public bool IsSpinning=false;
  public float winChance=.6f,nearMissChance=.3f;
 }
}
namespace CanEmAllMod {
 static class AdminPanel {
  internal static float Speed=1,Jump=1,Damage=1,Conveyor=1,Ammo=1;
  internal static bool InfiniteStamina,InfiniteAmmo,FreezeClock;
 }
 class Program {
  static void Check(bool v,string why) {if(!v)throw new Exception("FAIL "+why); Console.WriteLine("PASS "+why);}
  static void Main() {
   // A sandboxed test uses dotnet.exe's own install dir, never a real game's UserData.
   var dir=Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
   var cfg=Path.Combine(dir,"UserData","CanEmAllTrainer.ini");
   Directory.CreateDirectory(Path.GetDirectoryName(cfg));
   var initial=File.Exists(cfg)?File.ReadAllText(cfg):null;
   try {
    if(File.Exists(cfg))File.Delete(cfg);
    TrainerSettings.Initialize();
    AdminPanel.Speed=3.5f;AdminPanel.InfiniteAmmo=true;AdminPanel.FreezeClock=true;
    TrainerSettings.RecordMoney(54000);
    TrainerSettings.RecordDay(423);
    TrainerSettings.RecordNight(71);
    TrainerSettings.RecordTruck(13);
    TrainerSettings.SetChance("crit",0.42f);
    TrainerSettings.SetChance("shiny",0.22f);
    TrainerSettings.SetChance("double",0.81f);
    TrainerSettings.SetChance("casino.win",0.87f);
    TrainerSettings.SetChance("casino.near",0.12f);
    TrainerSettings.Save();
    Check(File.Exists(cfg),"first-run config file");
    Check(File.ReadAllText(cfg).Contains("Language=en"),"English default persisted");
    Localization.Set("fr");
    TrainerSettings.Save();
    Check(File.ReadAllText(cfg).Contains("Language=fr"),"French choice persisted");
    Localization.Set("en");
    string text=File.ReadAllText(cfg);
    Check(text.Contains("Speed=3.5")&&text.Contains("PinnedMoney=54000"),"serialized values");
    // Force divergent values before reload.
    AdminPanel.Speed=1;AdminPanel.InfiniteAmmo=false;AdminPanel.FreezeClock=false;
    TrainerSettings.Chances.Clear();TrainerSettings.PinnedMoney=null;
    TrainerSettings.Initialize();
    Check(Localization.Language=="fr","French choice restored on launch");
    Check(AdminPanel.Speed==3.5f&&AdminPanel.InfiniteAmmo&&AdminPanel.FreezeClock,"load persistent flags/multiplier");
    Check(TrainerSettings.PinnedMoney==54000 && TrainerSettings.DaySeconds==423 && TrainerSettings.TruckCount==13,"load money/durations");
    Check(Math.Abs(TrainerSettings.Chances["crit"]-.42f)<.001f,"load chance probability");
    Il2Cpp.GameManager.Instance=new Il2Cpp.GameManager();
    Il2Cpp.MoneyManager.Instance=new Il2Cpp.MoneyManager();
    Il2Cpp.Truck.Instance=new Il2Cpp.Truck();
    Il2Cpp.RewardMachine.Instance=new Il2Cpp.RewardMachine();
    Il2Cpp.SlotMachine.Instance=new Il2Cpp.SlotMachine();
    UnityEngine.Time.realtimeSinceStartup=16;
    TrainerSettings.Tick();
    Check(Il2Cpp.GameManager.Instance.dayDuration==423&&Il2Cpp.GameManager.Instance.nightShiftDuration==71,"replay time on fresh scene");
    Check(Math.Abs(Il2Cpp.GameManager.Instance.criticProbability-.42f)<.001,"replay combat chance on fresh scene");
    Check(Il2Cpp.MoneyManager.Instance.money==54000,"replay money on fresh scene");
    Check(Il2Cpp.Truck.Instance.enemyCount==13&&Math.Abs(Il2Cpp.Truck.Instance.shinyProbability-.22f)<.001,"replay truck on fresh scene");
    Check(Math.Abs(Il2Cpp.RewardMachine.Instance.duplicateMoneyProbability-.81f)<.001,"replay reward on fresh scene");
    Check(TrainerSettings.CasinoAvailable,"native slot instance discovered during update");
    Check(Math.Abs(Il2Cpp.SlotMachine.Instance.winChance-.87f)<.001f && Math.Abs(Il2Cpp.SlotMachine.Instance.nearMissChance-.12f)<.001f,"casino native values replayed from persisted settings");
    Il2Cpp.GameManager.Instance=new Il2Cpp.GameManager {Id=19};
    Il2Cpp.MoneyManager.Instance=new Il2Cpp.MoneyManager {Id=20};
    UnityEngine.Time.realtimeSinceStartup=20;
    TrainerSettings.Tick();
    Check(Il2Cpp.GameManager.Instance.dayDuration==423&&Il2Cpp.MoneyManager.Instance.money==54000,"replay after recreated scene");
    Il2Cpp.SlotMachine.Instance=new Il2Cpp.SlotMachine {Id=30};
    UnityEngine.Time.realtimeSinceStartup=24;
    TrainerSettings.Tick();
    Check(Math.Abs(Il2Cpp.SlotMachine.Instance.winChance-.87f)<.001f,"casino settings restored after scene recreation");
    File.WriteAllText(cfg,"Speed=9999\nJump=bad\nInfiniteAmmo=0\nchance.crit=3\nPinnedMoney=-42\n");
    TrainerSettings.Chances.Clear();TrainerSettings.Initialize();
    Check(Localization.Language=="en","Legacy config defaults to English");
    Check(AdminPanel.Speed==5 && AdminPanel.Jump==1 && TrainerSettings.Chances["crit"]==1 && TrainerSettings.PinnedMoney==0,"malformed input clamped");
    Console.WriteLine("ALL MANAGED INTEGRATION CHECKS PASSED");
   } finally {
    if(initial==null){if(File.Exists(cfg))File.Delete(cfg);}
    else File.WriteAllText(cfg,initial);
   }
  }
 }
}
