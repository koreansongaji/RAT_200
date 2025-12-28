using Unity.Cinemachine;
using UnityEngine;

public static class CloseupCamManager
{
    static CinemachineCamera _activeClose;
    static CinemachineCamera _activeMicro;
    public static bool InMicro => _activeMicro != null;

    public const int CloseOn = 20;
    public const int CloseOff = 0;
    public const int MicroOn = 50;
    public const int MicroOff = 0;

    public static void Activate(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_activeClose == cam) return;
        if (_activeClose) _activeClose.Priority = CloseOff;
        cam.Priority = CloseOn;
        _activeClose = cam;
    }

    public static void Deactivate(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_activeClose == cam) _activeClose = null;
        cam.Priority = CloseOff;
    }

    public static void ActivateMicro(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_activeMicro && _activeMicro != cam)
            _activeMicro.Priority = MicroOff;

        cam.Priority = MicroOn;   // 기존 Close(20)는 건드리지 않음
        _activeMicro = cam;
    }

    public static void DeactivateMicro(CinemachineCamera cam)
    {
        if (!cam) return;
        if (_activeMicro == cam) _activeMicro = null;
        cam.Priority = MicroOff;
    }

    public static void DeactivateAll()
    {
        if (_activeClose) { _activeClose.Priority = CloseOff; _activeClose = null; }
        if (_activeMicro) { _activeMicro.Priority = MicroOff; _activeMicro = null; }
    }
}
