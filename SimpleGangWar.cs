using RDR2;
using RDR2.Native;
using RDR2.Math;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

public class SimpleGangWar : Script {
    // Settings defined on script variables serve as fallback for settings not defined (or invalid) on .ini config file

    // Peds: https://github.com/Saltyq/ScriptHookRDR2DotNet/blob/master/source/scripting_v3/RDR2/Entities/Peds/PedHash.cs
    // Weapons: https://github.com/Saltyq/ScriptHookRDR2DotNet/blob/2d3fbb501bc138554fd42aca9e12aba4c763f0f9/source/scripting_v3/RDR2/Weapons/Weapon.cs#L103
    private static string[] pedsAlliesHashesStrings = { "G_M_M_UniAfricanAmericanGang_01", "G_M_M_UniCriminals_01", "G_M_M_UniCriminals_02", "G_M_M_UniGrays_01", "G_M_M_UniGrays_02" };
    private static string[] weaponsAlliesHashesStrings = { };
    private static string[] pedsEnemiesHashesStrings = { "U_M_M_ValSheriff_01", "S_M_M_Army_01", "S_M_Y_Army_01" };
    private static string[] weaponsEnemiesHashesStrings = { };
    private static readonly char[] StringSeparators = { ',', ';' };

    private static int healthAllies = 120;
    private static int healthEnemies = 120;
    private static int accuracyAllies = 5;
    private static int accuracyEnemies = 5;

    private static int maxPedsPerTeam = 10;
    private static Keys hotkey = Keys.F9;
    private static Keys spawnHotkey = Keys.Z;
    private static bool noWantedLevel = true;
    private static bool showBlipsOnPeds = true;
    private static bool dropWeaponOnDead = false;
    private static bool removeDeadPeds = true;
    private static int idleInterval = 500;
    private static int battleInterval = 500;
    private static int maxPedsAllies;
    private static int maxPedsEnemies;

    // From here, hidden variables - can be changed only here, not exposed on the .ini file

    private static bool processOtherRelationshipGroups = false;  // This setting may crash the game
    private static float fightDistanceMultiplier = 1.5f;
    private static BlipType spawnpointBlipType = BlipType.BLIP_STYLE_WAYPOINT;
    private static BlipModifier spawnpointAlliesBlipColor = BlipModifier.BLIP_MODIFIER_MP_COLOR_1;  // blue
    private static BlipModifier spawnpointEnemiesBlipColor = BlipModifier.BLIP_MODIFIER_MP_COLOR_2;  // red
    private static BlipType pedAlliedBlipType = BlipType.BLIP_STYLE_FRIENDLY;
    private static BlipType pedEnemyBlipType = BlipType.BLIP_STYLE_ENEMY;

    // From here, internal script variables - do not change!

    private List<PedHash> pedsAlliesHashes;
    private List<WeaponHash> weaponsAlliesHashes;
    private List<PedHash> pedsEnemiesHashes;
    private List<WeaponHash> weaponsEnemiesHashes;

    private int relationshipGroupAllies;
    private int relationshipGroupEnemies;
    private int originalWantedLevel;

    private List<Ped> spawnedAllies = new List<Ped>();
    private List<Ped> spawnedEnemies = new List<Ped>();
    private List<Ped> deadPeds = new List<Ped>();
    private List<Ped> pedsRemove = new List<Ped>();
    private List<int> processedRelationshipGroups = new List<int>();

    private bool spawnEnabled = true;
    private Stage stage = Stage.Initial;

    private Vector3 spawnpointAllies;
    private Vector3 spawnpointEnemies;
    private float spawnpointsDistance;

    private Blip spawnpointBlipAllies;
    private Blip spawnpointBlipEnemies;

    private static Relationship[] allyRelationships = { Relationship.Companion, Relationship.Like, Relationship.Respect };
    private static Relationship[] enemyRelationships = { Relationship.Hate, Relationship.Dislike };

    private static BlipModifier blipModifierBlink = BlipModifier.BLIP_MODIFIER_FLASH_FOREVER;

    private int relationshipGroupPlayer;
    private static Random random;

    private enum Stage {
        Initial = 0,
        DefiningEnemySpawnpoint = 1,
        EnemySpawnpointDefined = 2,
        Running = 3,
        StopKeyPressed = 4
    }

    private class SettingsHeader {
        public static readonly string Allies = "ALLIED_TEAM";
        public static readonly string Enemies = "ENEMY_TEAM";
        public static readonly string General = "SETTINGS";
    }


    public SimpleGangWar() {
        Tick += MainLoop;
        KeyUp += OnKeyUp;
        Interval = idleInterval;

        ScriptSettings config = ScriptSettings.Load("scripts\\SimpleGangWar.ini");
        string configString;

        healthAllies = config.GetValue<int>(SettingsHeader.Allies, "Health", healthAllies);
        healthEnemies = config.GetValue<int>(SettingsHeader.Enemies, "Health", healthEnemies);

        accuracyAllies = config.GetValue<int>(SettingsHeader.Allies, "Accuracy", accuracyAllies);
        accuracyEnemies = config.GetValue<int>(SettingsHeader.Enemies, "Accuracy", accuracyEnemies);

        configString = config.GetValue<string>(SettingsHeader.Allies, "Weapons", "");
        weaponsAlliesHashesStrings = ArrayParse(configString, weaponsAlliesHashesStrings);
        weaponsAlliesHashes = EnumArrayParse<WeaponHash>(weaponsAlliesHashesStrings);

        configString = config.GetValue<string>(SettingsHeader.Enemies, "Weapons", "");
        weaponsEnemiesHashesStrings = ArrayParse(configString, weaponsEnemiesHashesStrings);
        weaponsEnemiesHashes = EnumArrayParse<WeaponHash>(weaponsEnemiesHashesStrings);

        configString = config.GetValue<string>(SettingsHeader.Allies, "Models", "");
        pedsAlliesHashesStrings = ArrayParse(configString, pedsAlliesHashesStrings);
        pedsAlliesHashes = EnumArrayParse<PedHash>(pedsAlliesHashesStrings);
        if (pedsAlliesHashes.Count == 0) {
            ThrowException("No valid allied ped models defined!");
        }

        configString = config.GetValue<string>(SettingsHeader.Enemies, "Models", "");
        pedsEnemiesHashesStrings = ArrayParse(configString, pedsEnemiesHashesStrings);
        pedsEnemiesHashes = EnumArrayParse<PedHash>(pedsEnemiesHashesStrings);
        if (pedsEnemiesHashes.Count == 0) {
            ThrowException("No valid enemy ped models defined!");
        }

        configString = config.GetValue<string>(SettingsHeader.General, "Hotkey", "");
        hotkey = EnumParse<Keys>(configString, hotkey);
        configString = config.GetValue<string>(SettingsHeader.General, "SpawnHotkey", "");
        spawnHotkey = EnumParse<Keys>(configString, spawnHotkey);

        maxPedsPerTeam = config.GetValue<int>(SettingsHeader.General, "MaxPedsPerTeam", maxPedsPerTeam);
        noWantedLevel = config.GetValue<bool>(SettingsHeader.General, "NoWantedLevel", noWantedLevel);
        showBlipsOnPeds = config.GetValue<bool>(SettingsHeader.General, "ShowBlipsOnPeds", showBlipsOnPeds);
        dropWeaponOnDead = config.GetValue<bool>(SettingsHeader.General, "DropWeaponOnDead", dropWeaponOnDead);
        removeDeadPeds = config.GetValue<bool>(SettingsHeader.General, "RemoveDeadPeds", removeDeadPeds);
        idleInterval = config.GetValue<int>(SettingsHeader.General, "IdleInterval", idleInterval);
        battleInterval = config.GetValue<int>(SettingsHeader.General, "BattleInterval", battleInterval);

        maxPedsAllies = config.GetValue<int>(SettingsHeader.Allies, "MaxPeds", maxPedsPerTeam);
        maxPedsEnemies = config.GetValue<int>(SettingsHeader.Enemies, "MaxPeds", maxPedsPerTeam);

        relationshipGroupAllies = World.AddRelationshipGroup("simplegangwar_allies");
        relationshipGroupEnemies = World.AddRelationshipGroup("simplegangwar_enemies");
        relationshipGroupPlayer = Game.Player.Character.RelationshipGroup;
        SetRelationshipBetweenGroups(Relationship.Hate, relationshipGroupAllies, relationshipGroupEnemies);
        SetRelationshipBetweenGroups(Relationship.Respect, relationshipGroupAllies, relationshipGroupAllies);
        SetRelationshipBetweenGroups(Relationship.Respect, relationshipGroupEnemies, relationshipGroupEnemies);
        SetRelationshipBetweenGroups(Relationship.Respect, relationshipGroupAllies, relationshipGroupPlayer);
        SetRelationshipBetweenGroups(Relationship.Hate, relationshipGroupEnemies, relationshipGroupPlayer);
        processedRelationshipGroups.Add(relationshipGroupPlayer);
        processedRelationshipGroups.Add(relationshipGroupAllies);
        processedRelationshipGroups.Add(relationshipGroupEnemies);

        random = new Random();

        PrintSubtitle("SimpleGangWar loaded");
    }


    /// <summary>
    /// The main script loop runs at the frequency delimited by the Interval, which varies depending if the battle is running or not.
    /// The loop only spawn peds and processes them as the battle is running. Any other actions that happen outside a battle are processed by Key event handlers.
    /// </summary>
    private void MainLoop(object sender, EventArgs e)
    {
        if (stage >= Stage.Running) {
            try {
                SpawnPeds(true);
                SpawnPeds(false);

                SetUnmanagedPedsInRelationshipGroups();
                ProcessSpawnedPeds(true);
                ProcessSpawnedPeds(false);
            } catch (FormatException exception) {
                RDR2.UI.Screen.ShowSubtitle("(SimpleGangWar) Error! " + exception.Message);
            }
        }
    }


    /// <summary>
    /// Key event handler for key releases.
    /// </summary>
    private void OnKeyUp(object sender, KeyEventArgs e) {
        if (e.KeyCode == hotkey) {
            switch (stage) {
                case Stage.Initial:
                    RDR2.UI.Screen.ShowSubtitle("Welcome to SimpleGangWar!\nGo to the enemy spawnpoint and press the hotkey again to define it.");
                    stage = Stage.DefiningEnemySpawnpoint;
                    break;
                case Stage.DefiningEnemySpawnpoint:
                    DefineSpawnpoint(false);
                    RDR2.UI.Screen.ShowSubtitle("Enemy spawnpoint defined! Now go to the allied spawnpoint and press the hotkey again to define it.");
                    stage = Stage.EnemySpawnpointDefined;
                    break;
                case Stage.EnemySpawnpointDefined:
                    DefineSpawnpoint(true);
                    SetupBattle();
                    RDR2.UI.Screen.ShowSubtitle("The battle begins NOW!");
                    stage = Stage.Running;
                    break;
                case Stage.Running:
                    RDR2.UI.Screen.ShowSubtitle("Do you really want to stop the battle? Press the hotkey again to confirm.");
                    stage = Stage.StopKeyPressed;
                    break;
                case Stage.StopKeyPressed:
                    RDR2.UI.Screen.ShowSubtitle("The battle has ended!");
                    stage = Stage.Initial;
                    Teardown();
                    break;
            }
        } else if (e.KeyCode == spawnHotkey) {
            spawnEnabled = !spawnEnabled;
            BlinkSpawnpoint(true);
            BlinkSpawnpoint(false);
        }
    }


    /// <summary>
    /// After the spawnpoints are defined, some tweaks are required just before the battle begins.
    /// </summary>
    private void SetupBattle() {
        Interval = battleInterval;
        spawnpointsDistance = spawnpointEnemies.DistanceTo(spawnpointAllies);

        if (noWantedLevel) {
            originalWantedLevel = Game.MaxWantedLevel;
            Game.MaxWantedLevel = 0;
        }
    }

    /// <summary>
    /// Spawn peds on the given team, until the ped limit for that team is reached.
    /// </summary>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>
    private void SpawnPeds(bool alliedTeam) {
        List<Ped> spawnedPedsList = alliedTeam ? spawnedAllies : spawnedEnemies;
        int maxPeds = alliedTeam ? maxPedsAllies : maxPedsEnemies;

        //if (spawnEnabled && spawnedPedsList.Count < maxPeds) {
        while (spawnEnabled && spawnedPedsList.Count < maxPeds) {
            SpawnRandomPed(alliedTeam);
        }
    }

    /// <summary>
    /// Spawns a ped on the given team, ready to fight.
    /// </summary>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>
    /// <returns>The spawned ped</returns>
    private Ped SpawnRandomPed(bool alliedTeam) {
        Vector3 pedPosition = alliedTeam ? spawnpointAllies : spawnpointEnemies;
        List<PedHash> pedHashes = alliedTeam ? pedsAlliesHashes : pedsEnemiesHashes;
        PedHash pedHash = RandomChoice(pedHashes);

        Ped ped = World.CreatePed(pedHash, pedPosition);

        ped.Health = ped.MaxHealth = alliedTeam ? healthAllies : healthEnemies;
        ped.Accuracy = alliedTeam ? accuracyAllies : accuracyEnemies;
        ped.RelationshipGroup = alliedTeam ? relationshipGroupAllies : relationshipGroupEnemies;
        ped.DropsWeaponsOnDeath = dropWeaponOnDead;

        if (showBlipsOnPeds) {
            BlipType blipType = alliedTeam ? pedAlliedBlipType : pedEnemyBlipType;
            Blip blip = ped.AddBlip(blipType);
            blip.Label = alliedTeam ? "Ally team member" : "Enemy team member";
        }

        SetPedTask(ped);
        SetPedWeapon(ped, alliedTeam);
        (alliedTeam ? spawnedAllies : spawnedEnemies).Add(ped);

        return ped;
    }

    /// <summary>
    /// Processes the spawned peds of the given team, featuring:
    ///   - Deleting dead peds
    ///   - Avoiding allies from attacking the player, by resetting their task
    /// </summary>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>
    private void ProcessSpawnedPeds(bool alliedTeam) {
        Ped player = Game.Player.Character;
        List<Ped> pedList = alliedTeam ? spawnedAllies : spawnedEnemies;

        foreach (Ped ped in pedList) {
            if (ped.IsDead) {
                ped.CurrentBlip.Delete();
                pedsRemove.Add(ped);
                deadPeds.Add(ped);
                if (removeDeadPeds) ped.MarkAsNoLongerNeeded();
            } else {
                // Avoid allies from attacking player
                if (alliedTeam && ped.IsInCombatAgainst(player)) {
                    SetPedTask(ped);
                }
                // Give custom weapons to ped
                SetPedWeapon(ped, alliedTeam);
            }
        }

        foreach (Ped ped in pedsRemove) {
            pedList.Remove(ped);
        }

        pedsRemove.Clear();
    }

    /// <summary>
    /// Set the task to the given ped, to start fighting against hated targets within the spawnpoint distance.
    /// This method clears the current ped task, so can be used to reset rogue peds.
    /// </summary>
    /// <param name="ped">ped to process</param>
    private void SetPedTask(Ped ped) {
        ped.Task.ClearAllImmediately();
        ped.Task.FightAgainstHatedTargets(spawnpointsDistance * fightDistanceMultiplier);
        ped.AlwaysKeepTask = true;
    }

    /// <summary>
    /// Give a weapon to the given ped on the given team, if required.
    /// A weapon is only given if the ped does not currently have any of the weapons on the weapons array for its team.
    /// The weapon is chosen randomly from the team weapons list. If no weapons are configured, no weapon is given.
    /// </summary>
    /// <param name="ped">ped to process</param>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>

    private void SetPedWeapon(Ped ped, bool alliedTeam) {
        List<WeaponHash> weaponHashes = alliedTeam ? weaponsAlliesHashes : weaponsEnemiesHashes;

        if (weaponHashes.Count > 0 && !weaponHashes.Contains(ped.Weapons.Current.Hash) && !weaponHashes.Contains(ped.Weapons.BestWeapon)) {
            WeaponHash weaponGive = RandomChoice(weaponHashes);
            ped.Weapons.Current.Remove();
            ped.GiveWeapon(weaponGive, 1000, true, false);
            ped.Weapons.Current.InfiniteAmmo = true;
        }
    }

    /// <summary>
    /// Set the spawnpoint for the given team on the position where the player is at.
    /// </summary>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>
    private void DefineSpawnpoint(bool alliedTeam) {
        Vector3 position = Game.Player.Character.Position;
        Blip blip = World.CreateBlip(position, spawnpointBlipType);

        if (alliedTeam) {
            blip.ModifierAdd(spawnpointAlliesBlipColor);
            spawnpointAllies = position;
            spawnpointBlipAllies = blip;
            blip.Label = "Ally spawnpoint";
        } else {
            blip.ModifierAdd(spawnpointEnemiesBlipColor);
            spawnpointEnemies = position;
            spawnpointBlipEnemies = blip;
            blip.Label = "Enemy spawnpoint";
        }

        BlinkSpawnpoint(alliedTeam);
    }

    /// <summary>
    /// Blink or stop blinking the spawnpoint blip of the given team, depending on if the spawn is disabled (blink) or not (stop blinking).
    /// </summary>
    /// <param name="alliedTeam">true=ally team / false=enemy team</param>
    private void BlinkSpawnpoint(bool alliedTeam) {
        Blip blip = alliedTeam ? spawnpointBlipAllies : spawnpointBlipEnemies;
        if (blip == null) return;

        if (spawnEnabled) {
            blip.ModifierRemove(blipModifierBlink);
        } else {
            blip.ModifierAdd(blipModifierBlink);
        }
    }

    /// <summary>
    /// Get all the relationship groups from foreign peds (those that are not part of SimpleGangWar), and set the relationship between these groups and the SimpleGangWar groups.
    /// NOTE: This method may crash the game while battle runs. Currently is not supported.
    /// </summary>
    private void SetUnmanagedPedsInRelationshipGroups() {
        if (!processOtherRelationshipGroups) return;

        foreach (Ped ped in World.GetAllPeds()) {
            if (ped.IsHuman && !ped.IsPlayer)  {
                Relationship pedRelationshipWithPlayer = ped.GetRelationshipWithPed(Game.Player.Character);
                int relationshipGroup = ped.RelationshipGroup;

                if (relationshipGroup != relationshipGroupAllies && relationshipGroup != relationshipGroupEnemies && relationshipGroup != relationshipGroupPlayer) {
                    if (allyRelationships.Contains(pedRelationshipWithPlayer)) {
                        SetRelationshipBetweenGroups(Relationship.Respect, relationshipGroup, relationshipGroupAllies);
                        SetRelationshipBetweenGroups(Relationship.Hate, relationshipGroup, relationshipGroupEnemies);
                    } else if (enemyRelationships.Contains(pedRelationshipWithPlayer)) {
                        SetRelationshipBetweenGroups(Relationship.Respect, relationshipGroup, relationshipGroupEnemies);
                        SetRelationshipBetweenGroups(Relationship.Hate, relationshipGroup, relationshipGroupAllies);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Physically delete the peds from the given list from the game world.
    /// </summary>
    /// <param name="pedList">List of peds to teardown</param>
    private void TeardownPeds(List<Ped> pedList) {
        foreach (Ped ped in pedList) {
            if (ped.Exists()) ped.Delete();
        }
    }

    /// <summary>
    /// Manage the battle teardown on user requests. This brings the game to an initial state, before battle start and spawnpoint definition.
    /// </summary>
    private void Teardown() {
        Interval = idleInterval;
        spawnpointBlipAllies.Delete();
        spawnpointBlipEnemies.Delete();

        TeardownPeds(spawnedAllies);
        TeardownPeds(spawnedEnemies);
        TeardownPeds(deadPeds);

        spawnedAllies.Clear();
        spawnedEnemies.Clear();
        deadPeds.Clear();
        pedsRemove.Clear();
        processedRelationshipGroups.Clear();

        if (noWantedLevel) Game.MaxWantedLevel = originalWantedLevel;
    }

    /// <summary>
    /// Set the relationship between two given groups. The relationship is set twice, on both possible combinations.
    /// </summary>
    /// <param name="relationship">Relationship to set between the groups</param>
    /// <param name="groupA">One group</param>
    /// <param name="groupB">Other group</param>
    private void SetRelationshipBetweenGroups(Relationship relationship, int groupA, int groupB) {
        World.SetRelationshipBetweenGroups(relationship, groupA, groupB);
        World.SetRelationshipBetweenGroups(relationship, groupB, groupA);
    }

    /// <summary>
    /// Choose a random item from a given array, containing objects of type T
    /// </summary>
    /// <typeparam name="T">Type of objects in the array</typeparam>
    /// <param name="array">Array to choose from</param>
    /// <returns>A random item from the array</returns>
    private T RandomChoice<T>(T[] array) {
        return array[random.Next(0, array.Length)];
    }

    /// <summary>
    /// Choose a random item from a given List, containing objects of type T
    /// </summary>
    /// <typeparam name="T">Type of objects in the List</typeparam>
    /// <param name="list">List to choose from</param>
    /// <returns>A random item from the List</returns>
    private T RandomChoice<T>(List<T> list) {
        return list[random.Next(0, list.Count)];
    }

    /// <summary>
    /// Given a string key for an enum, return the referenced enum object.
    /// </summary>
    /// <typeparam name="EnumType">The whole enum object, to choose an option from</typeparam>
    /// <param name="enumKey">The enum key as string</param>
    /// <returns>The chosen enum option, or null if the given key does not match any item from the enum</returns>
    private EnumType? EnumParse<EnumType>(string enumKey) where EnumType : struct {
        EnumType returnValue;
        if (!Enum.TryParse<EnumType>(enumKey, true, out returnValue)) return null;
        return returnValue;
    }

    /// <summary>
    /// Given a string key for an enum, return the referenced enum object.
    /// </summary>
    /// <typeparam name="EnumType">The whole enum object, to choose an option from</typeparam>
    /// <param name="enumKey">The enum key as string</param>
    /// <param name="defaultValue">What enum option to return if the referenced enum key does not exist in the enum</param>
    /// <returns>The chosen enum option</returns>
    private EnumType EnumParse<EnumType>(string enumKey, EnumType defaultValue) where EnumType : struct {
        EnumType? returnValue = EnumParse<EnumType>(enumKey);
        if (returnValue == null) returnValue = defaultValue;
        return (EnumType)returnValue;
    }

    /// <summary>
    /// Given an array of string keys for an enum, return the referenced enum objects on a List.
    /// Elements (keys) that do not match any element from the enum are not returned.
    /// </summary>
    /// <typeparam name="EnumType">The whole enum object, to choose an option from</typeparam>
    /// <param name="enumKey">The enum keys as string array</param>
    /// <returns>List of enum options</returns>
    private List<EnumType> EnumArrayParse<EnumType>(string[] enumKeys) where EnumType : struct {
        List<EnumType> parsedValues = new List<EnumType>();

        foreach (string key in enumKeys) {
            EnumType? returnValue = EnumParse<EnumType>(key);
            if (returnValue != null) {
                parsedValues.Add((EnumType)returnValue);
            }
        }

        return parsedValues;
    }

    /// <summary>
    /// Given a string of words to be split, split them and return a string array.
    /// </summary>
    /// <param name="stringInput">Input string</param>
    /// <param name="defaultArray">Array to return if the input string contains no items</param>
    /// <returns>A string array</returns>
    private string[] ArrayParse(string stringInput, string[] defaultArray) {
        string[] resultArray = stringInput.Replace(" ", string.Empty).Split(StringSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (resultArray.Length == 0) resultArray = defaultArray;
        return resultArray;
    }

    /// <summary>
    /// Throws a new Exception, with the given message. Also prints an in-game subtitle beforehand.
    /// </summary>
    /// <param name="message">Subtitle and Exception text</param>
    private void ThrowException(string message) {
        PrintSubtitle(message);
        throw new Exception(message);
    }

    /// <summary>
    /// Show an in-game subtitle.
    /// </summary>
    /// <param name="message">Subtitle text</param>
    private void PrintSubtitle(string message) {
        RDR2.UI.Screen.ShowSubtitle(message);
    }
}
