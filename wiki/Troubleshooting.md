# Troubleshooting

**Check it loaded.** `BepInEx/LogOutput.log` should mention J.O.O.N.A.S. Joyride when the game starts. No line at all
means BepInEx didn't load it: check the DLL is at `BepInEx/plugins/JoonasJoyride/JoonasJoyride.dll`, that
**MoonFramework** is installed, and that BepInEx itself works.

**E does nothing.** Look straight at a J.O.O.N.A.S. close by, then press E. Chat or a menu open takes the key.

**A J.O.O.N.A.S. can't be mounted after moving between stations.** Walk away and back, or reload the moon; if it keeps
happening, turn on `Diagnostics` ([[Settings]]) and report it with the log lines.

**Running it with LunarLink and nothing happens.** A new hot plugin starts **off**: F9 → HOT PLUGINS → **Enable**. If F9
says it was refused, a second copy is still in `BepInEx/plugins/`: remove one.

**Start over.** Delete `BepInEx/config/JoonasJoyride.preferences` with the game closed to forget your riding mode and camera.

Still stuck? [Open an issue](https://github.com/handofhate/JoonasJoyride/issues/new/choose) with the log lines (with `Diagnostics` on).
