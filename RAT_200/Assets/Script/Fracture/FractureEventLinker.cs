using UnityEngine;
using DinoFracture;

public class FractureEventLinker : MonoBehaviour
{
    private void Start()
    {
        // 씬 시작 시 미리 유효성 검사를 끝내버립니다 (폭발 시 지연 방지)
        var geoms = FindObjectsByType<FractureGeometry>(FindObjectsSortMode.None);
        foreach (var g in geoms)
        {
            g.ForceValidGeometry(); 
        }
    }

    public void OnFractureCompleted(OnFractureEventArgs args)
    {
        if (args == null) return;
        FracturePieceHandler.SetBlastPoint(args.OriginalObject.transform.position);
    }
}
