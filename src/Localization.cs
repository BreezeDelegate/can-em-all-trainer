using System;
using System.Collections.Generic;

namespace CanEmAllMod
{
    // Self-contained language table, no external Unity assets or font downloads.
    internal static class Localization
    {
        internal static string Language="en";
        internal static bool English=>Language=="en";
        internal static void Set(string language)
        {
            Language=string.Equals(language,"fr",StringComparison.OrdinalIgnoreCase)?"fr":"en";
        }

        private static readonly Dictionary<string,string> En =new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["JOUEUR"]="PLAYER",["CREATURES"]="CREATURES",["CANETTES"]="CANS",
            ["ARMES"]="WEAPONS",["CHANCES"]="CHANCES",["MONDE"]="WORLD",
            ["PROGRESSION"]="PROGRESSION",["SAUVEGARDE"]="SAVE",["CASINO"]="CASINO",
            ["FERMER"]="CLOSE",["TRAINER  /  VERSION COMPLETE"]="TRAINER  /  FULL GAME",
            ["TOUCHE ²  /  REGLAGES SAUVEGARDES AUTOMATIQUEMENT"]="KEY BELOW ESC  /  SETTINGS AUTO-SAVED",
            ["Action indisponible."]="Action unavailable.",
            ["OUI"]="ON",["NON"]="OFF",
            ["Joueur"]="Player",["Argent et modifications du personnage"]="Money and player modifications",
            ["Argent"]="Money",["APPLIQUER"]="APPLY",["Montant invalide."]="Invalid amount.",
            ["Chiffres, Ctrl+A/V, Retour, Entree pour appliquer."]="Enter digits, Ctrl+A/V, Backspace, Enter to apply.",
            ["Endurance infinie"]="Infinite stamina",["Vitesse"]="Speed",["Saut"]="Jump",["Degats"]="Damage",
            ["Creatures"]="Creatures",["Selectionner un modele disponible, puis le faire apparaitre."]="Choose an available creature, then spawn it.",
            ["Aucun modele charge."]="No creature models loaded.",["FAIRE APPARAITRE"]="SPAWN",
            ["PREC."]="PREV.",["SUIV."]="NEXT",
            ["Canettes"]="Cans",["Creer une canette standard a l'endroit vise."]="Spawn a standard can where you aim.",
            ["Creer une canette standard devant le joueur."]="Spawn a standard can in front of the player.",
            ["Les variantes dependent des machines et ameliorations du jeu."]="Variants depend on the game's machines and upgrades.",
            ["Arsenal"]="Weapons",["Debloquer une arme precise, puis choisir les munitions."]="Unlock and equip a weapon, then adjust its ammo.",
            ["Reserve auto (armes a feu)"]="Auto ammo supply (guns)",
            ["Arsenal indisponible."]="Weapon inventory unavailable.",
            ["DEBLOQUER / EQUIPER"]="UNLOCK / EQUIP",["+100 MUNITIONS"]="+100 AMMO",
            ["MAJ + CLIC : +1000"]="SHIFT + CLICK : +1000",
            ["RECHARGER TOUT"]="REFILL ALL",
            ["Probabilites"]="Probabilities",["Valeurs du combat et des recompenses. -- : valeur d'origine."]="Combat and reward odds. -- = default.",
            ["Coup critique"]="Critical hit",["Mort instantanee"]="Instant kill",["Balle gratuite"]="Free bullet",
            ["Balle explosive"]="Explosive bullet",["Electrocution"]="Electric shock",
            ["Canette bonus"]="Bonus can",["Creature brillante"]="Shiny creature",["Recompense x2"]="Double reward",
            ["Monde"]="World",["Duree des cycles et production."]="Day/night cycles and production.",
            ["EQUIPEMENT"]="EQUIPMENT",["Aucune partie active."]="No active game session.",
            ["Figer l'horloge"]="Freeze time",["Convoyeur"]="Conveyor",
            ["Capacite munitions"]="Ammo capacity",["Duree jour (sec)"]="Day duration (sec)",
            ["Duree nuit (sec)"]="Night duration (sec)",["Creatures / camion"]="Creatures / truck",
            ["MAJ + CLIC : +/- 10"]="SHIFT + CLICK : +/- 10",
            ["CONFIRMER"]="CONFIRM",["ANNULER"]="CANCEL",["ELIMINER TOUS LES MONSTRES"]="KILL ALL CREATURES",
            ["Equipement"]="Equipment",["Ajustements d'armes automatiques et de production."]="Weapon and production equipment adjustments.",
            ["RETOUR AU MONDE"]="BACK TO WORLD",["Tourelles ravitaillees"]="Auto-refill turrets",
            ["Capacite du vacuum"]="Vacuum capacity",["Cadence du vacuum"]="Vacuum throw rate",
            ["Portee lance-flammes"]="Flamethrower range",
            ["Pas de rechargement force du lance-roquettes ni des armes de melee."]="No forced rocket launcher or melee weapon reloads.",
            ["Les options actives sont conservees entre les sessions."]="Enabled settings persist across sessions.",
            ["Progression"]="Progression",["Armes et arbre de recherche du jeu complet."]="Weapons and full-game skill tree.",
            ["Debloquer les competences disponibles."]="Unlock available upgrades.",
            ["TOUT DEBLOQUER"]="UNLOCK UPGRADES",["Debloquer toutes les armes."]="Unlock all weapons.",
            ["TOUTES LES ARMES"]="ALL WEAPONS",
            ["Sauvegarde"]="Save",["Consultation des statistiques de la partie, sans modification de fichier."]="View game statistics without editing save files.",
            ["Aucune sauvegarde chargee."]="No save loaded.",["Jour"]="Day",
            ["Creatures vaincues"]="Creatures defeated",["Canettes produites"]="Cans produced",
            ["Casino"]="Casino",["Probabilites natives de la machine a sous."]="Native slot machine probabilities.",
            ["Machine indisponible dans cette scene."]="Slot machine unavailable in this scene.",
            ["Chance de gagner"]="Win chance",["Quasi-gain"]="Near miss",
            ["Tirage en cours"]="Spinning",["Machine au repos"]="Machine idle",
            ["Les valeurs sont appliquees en dehors de l'interface."]="Values apply outside the menu.",
            ["Indisponible"]="Unavailable",["Sans nom"]="Unnamed",
            ["Patiente une seconde."]="Wait a second.",
            ["Camera du joueur indisponible."]="Player camera unavailable.",
            ["Montant hors limites."]="Amount out of range.",
            ["Partie indisponible."]="Game unavailable.",["Modification impossible."]="Modification failed.",
            ["Prefab de canette indisponible."]="Can prefab unavailable.",
            ["Limite de canettes atteinte."]="Can limit reached.",
            ["Le pool n'a rien cree."]="The object pool returned nothing.",
            ["Creation impossible, voir journal."]="Spawn failed; check logs.",
            ["Canette creee."]="Can spawned.",["Canette inactive, voir journal."]="Can inactive; check logs.",
            ["Creature inactive (journal)."]="Creature inactive; check logs.",
            ["Creature indisponible."]="Creature unavailable.",
            ["Limite de 75 creatures."]="Limit of 75 creatures reached.",
            ["Prefab introuvable."]="Prefab not found.",
            ["Inventaire indisponible."]="Inventory unavailable.",
            ["Arme introuvable."]="Weapon not found.",
            ["Arme non ajoutee a l'inventaire."]="Weapon not added to inventory.",
            ["Arme indisponible (journal)."]="Weapon unavailable; check logs.",
            ["Quantite invalide."]="Invalid amount.",
            ["Munitions indisponibles."]="Ammo unavailable.",
            ["Munitions rechargees."]="Ammo refilled.",
            ["Recharge impossible."]="Reload failed.",
            ["Creatures eliminees."]="Creatures eliminated.",
            ["Action impossible."]="Action failed.",
            ["Arbre indisponible."]="Skill tree unavailable.",
            ["Quantite invalide"]="Invalid amount",
            ["Duree"]="Duration",
        };

        private static readonly KeyValuePair<string,string>[] Prefixes={
            new KeyValuePair<string,string>("Selection : ","Selected: "),
            new KeyValuePair<string,string>("Creature creee : ","Creature spawned: "),
            new KeyValuePair<string,string>("Argent applique : ","Money set: "),
            new KeyValuePair<string,string>("Equipee : ","Equipped: "),
            new KeyValuePair<string,string>("Armes traitees : ","Weapons processed: "),
            new KeyValuePair<string,string>("100 munitions ajoutees.","100 ammo added."),
            new KeyValuePair<string,string>("1000 munitions ajoutees.","1000 ammo added."),
        };

        internal static string Tr(string french)
        {
            if(Language=="fr"||string.IsNullOrEmpty(french))return french;
            if(En.TryGetValue(french,out var translated))return translated;
            foreach(var prefix in Prefixes)
                if(french.StartsWith(prefix.Key,StringComparison.Ordinal))
                    return prefix.Value+french.Substring(prefix.Key.Length);
            // Research action returns quantities followed by a short French status.
            string r=french.Replace(" achats; autres ameliorations debloquees (Master exclu).",
                    " purchases; other upgrades unlocked (final upgrade excluded).")
                .Replace(" achats avant interruption (voir journal).",
                    " purchases before interruption (check logs).")
                .Replace(" achats; "," purchases; ")
                .Replace(" ameliorations encore verrouillees (Master exclu).",
                    " upgrades still locked (final upgrade excluded).")
                .Replace(" munitions ajoutees."," ammo added.");
            return r;
        }
    }
}