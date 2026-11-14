using System;
using MoonCleaners;
using UnityEngine;

namespace JoonasPassenger;

public sealed class JoonasPassengerSeat : MonoBehaviour
{
    public Transform Anchor { get; private set; }
    public Transform TankAnchor { get; private set; }

    public JoonasPassengerSeat(IntPtr pointer) : base(pointer) { }

    internal static void AttachToLoadedJoonas()
    {
        var rigs = UnityEngine.Object.FindObjectsOfType<MoonJoonasRig>(true);
        Plugin.Diagnostic($"Model-only scan found {rigs.Length} J.O.O.N.A.S. rig(s).");
        foreach (var rig in rigs)
        {
            if (rig == null || rig.GetComponent<JoonasPassengerSeat>() != null) continue;
            rig.gameObject.AddComponent<JoonasPassengerSeat>();
        }
    }

    public void Awake()
    {
        BuildSeat();
    }

    private void BuildSeat()
    {
        if (Anchor != null) return;

        Anchor = new GameObject("Passenger Surf Anchor").transform;
        Anchor.SetParent(transform, false);
        Anchor.localPosition = new Vector3(0f, 3.05f, -0.2f);
        Anchor.localRotation = Quaternion.identity;

        TankAnchor = new GameObject("Passenger Tank Anchor").transform;
        TankAnchor.SetParent(transform, false);
        TankAnchor.localPosition = new Vector3(0f, 1.55f, -0.2f);
        TankAnchor.localRotation = Quaternion.identity;
    }

    internal Transform MountAnchor(bool tankMode) => tankMode ? TankAnchor : Anchor;
}
