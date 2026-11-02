using System;
using CanEmAllMod;
internal static class Program
{
    static int passed;
    static void Assert(string label,bool value)
    {
        if(!value)throw new Exception("FAIL "+label);
        passed++;
        Console.WriteLine("PASS "+label);
    }
    static void Main()
    {
        Assert("default English",Localization.Language=="en");
        Assert("English menu",Localization.Tr("JOUEUR")=="PLAYER");
        Assert("English weapons",Localization.Tr("ARMES")=="WEAPONS");
        Assert("English toggle",Localization.Tr("OUI")=="ON" && Localization.Tr("NON")=="OFF");
        Assert("English money status",Localization.Tr("Argent applique : 10")=="Money set: 10");
        Assert("English spawn status",Localization.Tr("Creature creee : Water")=="Creature spawned: Water");
        Assert("English research status",Localization.Tr("251 achats; autres ameliorations debloquees (Master exclu).")=="251 purchases; other upgrades unlocked (final upgrade excluded).");
        Assert("English research remaining",Localization.Tr("4 achats; 3 ameliorations encore verrouillees (Master exclu).")=="4 purchases; 3 upgrades still locked (final upgrade excluded).");
        Assert("English ammo",Localization.Tr("1000 munitions ajoutees.")=="1000 ammo added.");
        Assert("English label casino",Localization.Tr("Machine indisponible dans cette scene.")=="Slot machine unavailable in this scene.");
        Localization.Set("fr");
        Assert("French menu",Localization.Tr("JOUEUR")=="JOUEUR" && Localization.Tr("ARMES")=="ARMES");
        Assert("French dynamic",Localization.Tr("Creature creee : Water")=="Creature creee : Water");
        Assert("French labels",Localization.Tr("TOUT DEBLOQUER")=="TOUT DEBLOQUER");
        Localization.Set("xx");
        Assert("unknown defaults to English",Localization.English && Localization.Tr("JOUEUR")=="PLAYER");
        Localization.Set("FR");
        Assert("case insensitive French",Localization.Language=="fr");
        Console.WriteLine("LOCALIZATION_TESTS="+passed);
    }
}