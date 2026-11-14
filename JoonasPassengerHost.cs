using System;
using UnityEngine;

namespace JoonasPassenger;

// Normal BepInEx fallback host. Lunar Link uses Plugin.Runtime directly so hot reloads do not
// repeatedly register custom IL2CPP types.
public sealed class JoonasPassengerHost : MonoBehaviour
{
    public JoonasPassengerHost(IntPtr pointer) : base(pointer) { }

    public void Awake() => Plugin.Diagnostic("Passenger host Awake.");

    public void Update() => Plugin.Runtime?.Update();

    public void LateUpdate() => Plugin.Runtime?.LateUpdate();
}
