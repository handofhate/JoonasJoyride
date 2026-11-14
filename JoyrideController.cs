using System;
using System.Collections.Generic;
using MoonCleaners;
using MoonFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JoonasPassenger;

// Shared gameplay logic. Normal BepInEx mode calls this through JoonasPassengerHost; Lunar Link calls it directly from
// its hot-plugin lifecycle, so reloading does not inject a new IL2CPP MonoBehaviour type.
internal sealed class JoyrideController
{
    private sealed class SeatAnchors
    {
        internal readonly Transform Rig;
        internal readonly Transform Surf;
        internal readonly Transform Tank;
        internal SeatAnchors(Transform rig, Transform surf, Transform tank) { Rig = rig; Surf = surf; Tank = tank; }
        internal Transform MountAnchor(bool tankMode) => tankMode ? Tank : Surf;
    }

    private readonly Dictionary<int, SeatAnchors> seats = new();
    private float nextScan;
    private MoonWalker walker;
    private MoonJoonasRig mountedJoonas;
    private Transform mountedVehicle;
    private Vector3 mountedExitLocal;
    private string activeScene;
    private bool riding;
    private bool tankMode;
    private MoonWalker.CameraView cameraViewToRestore;
    private bool seatedFirstPersonToRestore;
    private int cameraRestoreFrames;
    private SeatAnchors mountedSeat;
    private Vector3 safeExitPosition;
    private Vector3 safeExitFacing;
    private bool updateReported;
    private float nextDiagnostic;
    private bool running;

    internal void Start() { if (!running) { running = true; Moon.WorldLoaded += OnWorldLoaded; } }

    internal string Stop()
    {
        if (!running) return "joonas joyride stopped";
        Moon.WorldLoaded -= OnWorldLoaded;
        if (riding && walker != null)
        {
            try { Dismount(); }
            catch (Exception exception) { Plugin.Diagnostic($"Hot stop dismount failed: {exception.GetType().Name}: {exception.Message}"); }
        }
        seats.Clear(); running = false;
        return "joonas joyride stopped";
    }

    internal void Update()
    {
        if (!running) return;
        if (!updateReported) { updateReported = true; Plugin.Diagnostic("Passenger controller Update is running."); }
        if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 1f; ScanJoonas(); }
        if (Keyboard.current == null) return;

        RestoreMountCamera();
        if (riding && cameraRestoreFrames <= 0 && walker != null) Plugin.SavePreferences(tankMode, walker.seatedFirstPerson);
        if (Keyboard.current.fKey.wasPressedThisFrame && riding && mountedSeat != null) ToggleMountedMode();
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (FindWalker() == null) return;
        if (riding || mountedJoonas != null || walker.Passenger) { Dismount(); return; }
        if (walker.Mounted || walker.Building || walker.UIBlocked) return;
        var camera = Camera.main ?? Camera.current;
        if (camera == null) return;
        var best = FindBestJoonas(camera);
        if (best == null) return;
        var seat = EnsureSeat(best);
        if (seat == null) return;

        safeExitPosition = walker.transform.position;
        safeExitFacing = walker.transform.forward;
        mountedVehicle = best.transform;
        mountedExitLocal = best.transform.InverseTransformPoint(safeExitPosition);
        mountedSeat = seat;
        tankMode = Plugin.SavedTankMode;
        walker.MountPassenger(seat.MountAnchor(tankMode), best.transform);
        mountedJoonas = best; riding = true;
        cameraViewToRestore = walker.cameraView;
        seatedFirstPersonToRestore = Plugin.SavedFirstPerson;
        cameraRestoreFrames = 8;
        RestoreMountCamera();
        Plugin.Diagnostic($"Mounted {(tankMode ? "tank" : "surf")} mode on J.O.O.N.A.S.");
    }

    internal void LateUpdate() => RestoreMountCamera();

    private void OnWorldLoaded(MoonWorld _) { activeScene = null; seats.Clear(); }

    private void Dismount()
    {
        mountedSeat = null;
        foreach (var passenger in UnityEngine.Object.FindObjectsOfType<MoonVehiclePassenger>(true))
        {
            if (passenger == null || passenger.walker != walker) continue;
            passenger.GetOut(); break;
        }

        var exit = safeExitPosition;
        var facing = safeExitFacing.sqrMagnitude > 0.01f ? safeExitFacing : Vector3.forward;
        if (mountedVehicle != null)
        {
            var local = mountedExitLocal;
            var lateral = new Vector3(local.x, 0f, local.z);
            if (lateral.sqrMagnitude < 0.25f) lateral = Vector3.forward * 2f;
            lateral = lateral.normalized * Mathf.Max(2f, lateral.magnitude);
            var up = mountedVehicle.up.sqrMagnitude > 0.01f ? mountedVehicle.up.normalized : Vector3.up;
            var vehicleExit = mountedVehicle.TransformPoint(new Vector3(lateral.x, 0.5f, lateral.z));
            exit = FindGroundedExit(vehicleExit, up, mountedVehicle); facing = mountedVehicle.forward;
        }

        Plugin.SavePreferences(tankMode, walker.seatedFirstPerson);
        try { walker.LeavePrototype(exit, Vector3.zero); }
        catch (Exception exception) { Debug.LogWarning($"[J.O.O.N.A.S. Joyride] Native cleanup failed: {exception.GetType().Name}: {exception.Message}"); }
        walker.Passenger = false; walker.Resting = false; walker.UIBlocked = false; walker.Held = false;
        walker.vehicleSeat = null; walker.vehicleCameraRoot = null; walker.seatedFirstPerson = false; walker.enabled = true;
        walker.Teleport(exit, true); walker.StandUp(exit, facing); walker.Place(exit, facing, walker.LookPitch);
        mountedJoonas = null; mountedVehicle = null; riding = false;
        Plugin.Diagnostic($"Exit completed at {exit}; passenger={walker.Passenger} mounted={walker.Mounted} resting={walker.Resting}.");
    }

    private void ToggleMountedMode()
    {
        if (walker == null || mountedSeat == null || mountedVehicle == null) return;
        cameraViewToRestore = walker.cameraView; seatedFirstPersonToRestore = walker.seatedFirstPerson; cameraRestoreFrames = 8;
        tankMode = !tankMode; walker.MountPassenger(mountedSeat.MountAnchor(tankMode), mountedVehicle); RestoreMountCamera();
        Plugin.SavePreferences(tankMode, seatedFirstPersonToRestore);
        Plugin.Diagnostic($"Switched mounted mode to {(tankMode ? "Tank" : "Surf")}.");
    }

    private void RestoreMountCamera()
    {
        if (cameraRestoreFrames <= 0 || walker == null || !riding) return;
        walker.cameraView = cameraViewToRestore; walker.seatedFirstPerson = seatedFirstPersonToRestore; cameraRestoreFrames--;
    }

    private static Vector3 FindGroundedExit(Vector3 candidate, Vector3 up, Transform vehicle)
    {
        var hits = Physics.RaycastAll(candidate + up * 8f, -up, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        var nearestDistance = float.PositiveInfinity; var nearestPoint = Vector3.zero; var found = false;
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == vehicle || hit.collider.transform.IsChildOf(vehicle) || hit.distance >= nearestDistance) continue;
            nearestDistance = hit.distance; nearestPoint = hit.point; found = true;
        }
        if (found) return nearestPoint + up * 0.25f;
        var worldHits = Physics.RaycastAll(candidate + Vector3.up * 8f, Vector3.down, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        nearestDistance = float.PositiveInfinity; found = false;
        foreach (var hit in worldHits)
        {
            if (hit.collider == null || hit.collider.transform == vehicle || hit.collider.transform.IsChildOf(vehicle) || hit.distance >= nearestDistance) continue;
            nearestDistance = hit.distance; nearestPoint = hit.point; found = true;
        }
        if (found) return nearestPoint + Vector3.up * 0.25f;
        return candidate + up * 1f;
    }

    private void ScanJoonas()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, scene, StringComparison.Ordinal)) { activeScene = scene; seats.Clear(); }
        var rigs = UnityEngine.Object.FindObjectsOfType<MoonJoonasRig>(true);
        if (Time.unscaledTime >= nextDiagnostic)
        {
            nextDiagnostic = Time.unscaledTime + 5f; var details = new List<string>();
            foreach (var rig in rigs) if (rig != null) details.Add($"{rig.name}:seat={EnsureSeat(rig) != null}:active={rig.gameObject.activeInHierarchy}:pos={rig.transform.position}");
            Plugin.Diagnostic($"Scan scene={scene} rigs={rigs.Length} anchors={seats.Count} details={string.Join(" | ", details)}");
        }
        foreach (var rig in rigs) if (rig != null) EnsureSeat(rig);
    }

    private SeatAnchors EnsureSeat(MoonJoonasRig rig)
    {
        if (rig == null) return null;
        var id = rig.GetInstanceID();
        if (seats.TryGetValue(id, out var existing) && existing.Rig == rig.transform && existing.Surf != null && existing.Tank != null) return existing;
        var surf = FindOrCreateAnchor(rig.transform, "Passenger Surf Anchor", new Vector3(0f, 3.05f, -0.2f));
        var tank = FindOrCreateAnchor(rig.transform, "Passenger Tank Anchor", new Vector3(0f, 1.55f, -0.2f));
        var result = new SeatAnchors(rig.transform, surf, tank); seats[id] = result; return result;
    }

    private static Transform FindOrCreateAnchor(Transform parent, string name, Vector3 localPosition)
    {
        var anchor = parent.Find(name);
        if (anchor != null) return anchor;
        anchor = new GameObject(name).transform; anchor.SetParent(parent, false); anchor.localPosition = localPosition; anchor.localRotation = Quaternion.identity;
        return anchor;
    }

    private MoonJoonasRig FindBestJoonas(Camera camera)
    {
        MoonJoonasRig best = null; var bestAngle = 20f; var details = new List<string>();
        foreach (var rig in UnityEngine.Object.FindObjectsOfType<MoonJoonasRig>(true))
        {
            if (rig == null) continue;
            if (EnsureSeat(rig) == null) { details.Add($"{rig.name}:no-seat"); continue; }
            var surfaceUp = rig.transform.up.sqrMagnitude > 0.01f ? rig.transform.up.normalized : Vector3.up;
            var point = rig.transform.position + surfaceUp * 1.4f; var to = point - camera.transform.position; var distance = to.magnitude;
            if (distance > 12f || distance < 0.5f) { details.Add($"{rig.name}:distance={distance:0.00}"); continue; }
            var angle = Vector3.Angle(camera.transform.forward, to);
            if (angle > bestAngle) { details.Add($"{rig.name}:angle={angle:0.0}"); continue; }
            if (Physics.Raycast(camera.transform.position, to.normalized, out var hit, distance + 0.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                !hit.transform.IsChildOf(rig.transform) && hit.transform != rig.transform) { details.Add($"{rig.name}:blocked={hit.transform.name}"); continue; }
            bestAngle = angle; best = rig; details.Add($"{rig.name}:eligible distance={distance:0.00} angle={angle:0.0}");
        }
        Plugin.Diagnostic($"E target scene={SceneManager.GetActiveScene().name} selected={(best == null ? "none" : best.name)} details={string.Join(" | ", details)}");
        return best;
    }

    private MoonWalker FindWalker()
    {
        if (walker != null) return walker;
        var camera = Camera.main ?? Camera.current;
        if (camera != null)
        {
            walker = camera.GetComponentInParent<MoonWalker>();
            if (walker == null) walker = camera.transform.root.GetComponentInChildren<MoonWalker>();
        }
        if (walker == null)
        {
            var all = UnityEngine.Object.FindObjectsOfType<MoonWalker>(true);
            if (all.Length > 0) walker = all[0];
        }
        return walker;
    }
}
