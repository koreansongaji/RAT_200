using UnityEngine;
using Steamworks;

public class SteamAchievementManager : MonoBehaviour
{
    public static void UnlockAchievement(string achievementId)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam이 초기화되지 않았습니다.");
            return;
        }

        bool success = SteamUserStats.SetAchievement(achievementId);
        SteamUserStats.StoreStats();

        Debug.Log($"Achievement Unlock Try: {achievementId}, Success: {success}");
    }
}

public static class SteamAchievementIds
{
    public const string SafeCracker = "SAFE_CRACKER";
    public const string ColdOpen = "COLD_OPEN";
    public const string CrispyRat = "CRIPPY_RAT";
    public const string FuseFixed = "FUSE_FIXED";
    public const string AGreatBeginning = "A_GREAT_BEGINNING";
    public const string EscapeTheRoom = "ESCAPE_THE_ROOM";
}