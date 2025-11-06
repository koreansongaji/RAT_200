public interface IMicroSessionHost
{
    bool CanBeginMicro(PlayerInteractor player);
    void OnMicroEnter(PlayerInteractor player);
    void OnMicroExit(PlayerInteractor player);
}

public interface IZoomStateProvider { bool InZoom { get; } }
