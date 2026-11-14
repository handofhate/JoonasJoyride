using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Injection;
using MoonFramework;
using System;
using System.IO;
using UnityEngine;

namespace JoonasPassenger;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MoonFramework.FrameworkPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "tylerun.moonsweepers.joonaspassenger";
    public const string PluginName = "J.O.O.N.A.S. Joyride";
    public const string PluginVersion = "1.0.3";

    private static GameObject host;
    private static ManualLogSource logger;
    private static ConfigFile config;
    private static bool savedTankMode;
    private static bool savedFirstPerson = true;
    private static ConfigEntry<bool> diagnosticsEnabled;
    internal static JoyrideController Runtime { get; private set; }

    internal static bool SavedTankMode => savedTankMode;
    internal static bool SavedFirstPerson => savedFirstPerson;
    internal static bool DiagnosticsEnabled => diagnosticsEnabled?.Value ?? false;

    internal static void SavePreferences(bool tankMode, bool firstPerson)
    {
        if (savedTankMode == tankMode && savedFirstPerson == firstPerson) return;
        savedTankMode = tankMode;
        savedFirstPerson = firstPerson;
        SavePreferenceFile();
    }

    internal static void Diagnostic(string message)
    {
        if (!DiagnosticsEnabled) return;
        try
        {
            var path = Path.Combine(Paths.BepInExRootPath, "JoonasJoyride-diagnostic.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never affect gameplay.
        }
    }

    public override void Load()
    {
        StartRuntime(Log, Config, registerHotHost: true);
        logger.LogInfo($"{PluginName} {PluginVersion} loaded normally.");
    }

    internal static void StartHot()
    {
        logger = BepInEx.Logging.Logger.CreateLogSource(PluginName);
        config = new ConfigFile(Path.Combine(Paths.ConfigPath, PluginGuid + ".cfg"), true);
        StartRuntime(logger, config, registerHotHost: false);
        logger.LogInfo($"{PluginName} {PluginVersion} loaded through Lunar Link.");
    }

    private static void StartRuntime(ManualLogSource source, ConfigFile settings, bool registerHotHost)
    {
        logger = source;
        config = settings;
        LoadPreferences();
        diagnosticsEnabled = BindDiagnosticsSetting();
        Diagnostic($"Plugin loaded: {PluginVersion}");
        Runtime = new JoyrideController();
        Runtime.Start();
        if (registerHotHost) ClassInjector.RegisterTypeInIl2Cpp<JoonasPassengerHost>();
        if (registerHotHost) EnsureHost();
    }

    internal static string StopHot()
    {
        Runtime?.Stop();
        Runtime = null;
        if (host != null)
        {
            UnityEngine.Object.Destroy(host);
            host = null;
        }
        var oldLogger = logger;
        logger = null;
        config = null;
        diagnosticsEnabled = null;
        if (oldLogger != null) BepInEx.Logging.Logger.Sources.Remove(oldLogger);
        return "joonas joyride stopped";
    }

    private static void EnsureHost()
    {
        if (host != null && host.GetComponent<JoonasPassengerHost>() != null) return;
        host = new GameObject("J.O.O.N.A.S. Joyride Host");
        UnityEngine.Object.DontDestroyOnLoad(host);
        var component = host.AddComponent<JoonasPassengerHost>();
        logger?.LogInfo($"Passenger host component created: {component != null}");
    }

    private static ConfigEntry<bool> BindDiagnosticsSetting()
    {
        const string oldSection = "Passenger";
        const string section = "J.O.O.N.A.S. Joyride";
        var legacy = config.Bind(oldSection, "Diagnostics", false,
            "Legacy setting migrated to J.O.O.N.A.S. Joyride.");
        var olderLegacy = config.Bind(oldSection, "Enable Diagnostics", legacy.Value,
            "Legacy setting migrated to J.O.O.N.A.S. Joyride.");
        var value = legacy.Value || olderLegacy.Value;
        config.Remove(new ConfigDefinition(oldSection, "Diagnostics"));
        config.Remove(new ConfigDefinition(oldSection, "Enable Diagnostics"));
        return config.Bind(section, "Diagnostics", value,
            "Enable verbose troubleshooting logs.");
    }

    private static void LoadPreferences()
    {
        const string section = "Passenger";
        var legacyTank = config.Bind(section, "LastTankMode", false,
            "Legacy preference migrated to private Joyride storage.");
        var legacyFirstPerson = config.Bind(section, "LastFirstPerson", true,
            "Legacy preference migrated to private Joyride storage.");

        var path = PreferenceFilePath();
        var loaded = false;
        if (File.Exists(path))
        {
            try
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var fields = line.Split('=', 2);
                    if (fields.Length != 2) continue;
                    if (string.Equals(fields[0], "TankMode", StringComparison.OrdinalIgnoreCase) &&
                        bool.TryParse(fields[1], out var tank))
                    {
                        savedTankMode = tank;
                        loaded = true;
                    }
                    else if (string.Equals(fields[0], "FirstPerson", StringComparison.OrdinalIgnoreCase) &&
                             bool.TryParse(fields[1], out var firstPerson))
                    {
                        savedFirstPerson = firstPerson;
                        loaded = true;
                    }
                }
            }
            catch
            {
                loaded = false;
            }
        }

        if (!loaded)
        {
            savedTankMode = legacyTank.Value;
            savedFirstPerson = legacyFirstPerson.Value;
            SavePreferenceFile();
        }

        config.Remove(new ConfigDefinition(section, "LastTankMode"));
        config.Remove(new ConfigDefinition(section, "LastFirstPerson"));
        config.Save();
    }

    private static void SavePreferenceFile()
    {
        try
        {
            var path = PreferenceFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, new[]
            {
                "# J.O.O.N.A.S. Joyride persistent preferences",
                $"TankMode={savedTankMode}",
                $"FirstPerson={savedFirstPerson}",
            });
        }
        catch
        {
            // Preference persistence must never affect gameplay.
        }
    }

    private static string PreferenceFilePath()
    {
        return Path.Combine(Paths.BepInExRootPath, "config", "JoonasJoyride.preferences");
    }

    internal static void UpdateHot() => Runtime?.Update();
    internal static void LateUpdateHot() => Runtime?.LateUpdate();
    internal static string UpdateFeed() => "github:handofhate/JoonasJoyride";
}

// Lunar Link discovers this public lifecycle class by method name. The game can still load Plugin normally from
// BepInEx/plugins; this class adds hot-folder support without taking a dependency on Lunar Link.
public sealed class JoonasJoyrideHot
{
    public void Start(string state) => Plugin.StartHot();
    public string Stop() => Plugin.StopHot();
    public void Update() => Plugin.UpdateHot();
    public void LateUpdate() => Plugin.LateUpdateHot();
    public string UpdateFeed() => Plugin.UpdateFeed();
}
