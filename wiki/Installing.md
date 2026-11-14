# Installing

## What you need

- **Moon Sweepers** with **[BepInEx 6.0.0-be.791](https://builds.bepinex.dev/projects/bepinex_be)** (Unity.IL2CPP, win-x64), run once so BepInEx has set itself up.
- The **[MoonSweepers BepInEx Patcher](https://www.nexusmods.com/moonsweepers/mods/9)**, which makes BepInEx work on Moon Sweepers.
- **MoonFramework**, which many Moon Sweepers mods build on.

Only you install it: it runs on your own game, and other players don't need it.

## Install

1. Download the zip from [Nexus Mods](https://www.nexusmods.com/moonsweepers/mods/78) or the [latest GitHub release](https://github.com/handofhate/JoonasJoyride/releases/latest).
2. Close the game and unzip it into the game folder (the one that holds `BepInEx/`). You get
   `BepInEx/plugins/JoonasJoyride/JoonasJoyride.dll`. A mod manager such as Vortex does this for you.
3. Start the game. The BepInEx log (`BepInEx/LogOutput.log`) says "J.O.O.N.A.S. Joyride x.y.z loaded".

To run it hot with LunarLink instead, see [[With LunarLink]].

## Updating

Unzip the new version over the old one with the game closed, or let your mod manager update it. With LunarLink, F9 →
UPDATES does it in place without a restart ([[With LunarLink]]).

## Uninstalling

Close the game and delete `BepInEx/plugins/JoonasJoyride/` (or `BepInEx/hot/JoonasJoyride.dll`). Its preferences file,
`BepInEx/config/JoonasJoyride.preferences`, can go too.
