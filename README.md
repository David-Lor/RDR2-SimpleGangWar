# RDR 2 SimpleGangWar script

Red Dead Redemption 2 script to create a basic battle between two teams.

## Background

This is a port of [GTA V SimpleGangWar script](https://github.com/David-Lor/GTAV-SimpleGangWar).
Since most functions and methods are the same between GTA5 and RDR2, and the language used is the same (C#), almost all functionalities were ported to RDR2.

The main differences with the GTA V version are: no armor, no combat movement, no combat range, and not run to spawnpoint (as RDR2 peds seem to fight well on wide range).

## Installing

- Download and extract the required dependencies: [ScriptHook RDR2](http://www.dev-c.com/rdr2/scripthookrdr2) & [ScriptHookRDR2 DotNet](https://github.com/Saltyq/ScriptHookRDR2DotNet), and their dependencies
- Download & extract `SimpleGangWar.cs` & `SimpleGangWar.ini` into `Red Dead Redemption 2/scripts` folder

## Usage

The key `F9` ("Hotkey") is used to navigate through all the steps of the script. In-game help popups will describe what to do, but these are the different stages you will find:

1. The script will ask you to move to where the enemies will spawn
2. After pressing the hotkey, you must do the same to define where the allies will spawn
3. Right after defining both spawnpoints, peds from both teams will spawn on their respective spawnpoints, and fight each other
4. Press the hotkey once to enter the "exit mode" (it will ask for confirmation to stop the battle)
5. Pressing the hotkey again will inmediately stop the battle and remove all alive & dead peds from the map

An additional hotkey `Z` ("SpawnHotkey") is used to pause/resume the ped spawning in both teams.

## Settings

Settings can be defined on the `SimpleGangWar.ini` file, being the following:

### ALLIED_TEAM & ENEMY_TEAM

_All lists of items (models & weapons) are separated by comma (`,`) or semi-colon (`;`). Spaces and case ignored._

- `Models`: list of ped models ([Reference](https://github.com/Saltyq/ScriptHookRDR2DotNet/blob/master/source/scripting_v3/RDR2/Entities/Peds/PedHash.cs))
- `Weapons`: list of ped weapons ([Reference](https://github.com/Saltyq/ScriptHookRDR2DotNet/blob/2d3fbb501bc138554fd42aca9e12aba4c763f0f9/source/scripting_v3/RDR2/Weapons/Weapon.cs#L103))
- `Health`: health for peds
- `Accuracy`: accuracy for peds
- `MaxPeds`: maximum alive peds on the team (if not specified, the MaxPedsPerTeam setting will be used)

### SETTINGS

- `Hotkey`: the single hotkey used to iterate over the script stages ([Reference](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.key?view=netcore-3.1#fields))
- `SpawnHotkey`: hotkey used to pause/resume ped spawn in both teams ([Reference](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.key?view=netcore-3.1#fields))
- `MaxPedsPerTeam`: maximum alive peds on each team - teams with the setting MaxPeds will ignore this option
- `NoWantedLevel`: if true, disable wanted level during the battle (true/false)
- `ShowBlipsOnPeds`: if true, each spawned ped will have a blip on the map (true/false)
- `DropWeaponOnDead`: if false, dead peds won't drop their weapons - they will remain stick to their bodies (true/false)
- `RemoveDeadPeds`: if true, mark dead peds as no longer needed, making the game handle their cleanup (true/false)
- `ProcessOtherRelationshipGroups`: (This was not tested on RDR2, and might crash your game if enabled!)
  if true, get all relationship groups from other existing peds and match these groups with the groups of SimpleGangWar peds.
  Set it to true if you experience the spawned peds fighting against other peds (like mission peds) when they should not be (for example, enemy peds of a mission fighting against enemy peds of SimpleGangWar).
- `IdleInterval`: delay between loop runs, when battle is not running, in ms
- `BattleInterval`: delay between loop runs, when battle is running, in ms

## Known bugs

- Ally peds can "switch sides" when no enemies remain, and start shooting the player and other peds in ally team.
  As the ped is still set as an ally, it cannot be shot by the player (only damaged collaterally, with fire or explosives).
- Enabling "ProcessOtherRelationshipGroups" might cause the game to crash.

## TODO

- Set better blips for spawnpoints and peds
- Make spawnpoint blips blink (it depends on their type/sprite?)
- Avoid spawn-killing
- Add winning conditions
- Smooth transition from battle end to cleanup (extra step?)
- Add menu/more hotkeys to improve UX?
- Respawn player on ally spawnpoint after dying
- Organize data, settings, variables - for each teams on the script structurally (struct?)

## Changelog

- 0.0.2
    - Change default hotkey to F9, and default battle loop interval to 500ms
- 0.0.1
    - Initial release (from [GTAV-SimpleGangWar 2.1.1](https://github.com/David-Lor/GTAV-SimpleGangWar/releases/tag/2.1.1))
