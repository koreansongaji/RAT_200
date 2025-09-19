using Unity.Cinemachine;
using UnityEngine;

public static class CloseupCamManager
{
    static CinemachineCamera _active;
    public const int CloseOn = 20;
    public const int CloseOff = 0;

    public static void Activate(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_active == cam) return;
        if (_active) _active.Priority = CloseOff;
        cam.Priority = CloseOn;
        _active = cam;
    }

    public static void Deactivate(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_active == cam) _active = null;
        cam.Priority = CloseOff;
    }

    public static void DeactivateAll()
    {
        if (_active) { _active.Priority = CloseOff; _active = null; }
    }
}
