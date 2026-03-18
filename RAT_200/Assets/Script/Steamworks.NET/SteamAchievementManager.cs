using UnityEngine;
using Steamworks;

public static class SteamAchievementManager
{
    public static bool UnlockAchievement(string achievementId)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError($"Steam 초기화 안 됨: {achievementId}");
            return false;
        }

        if (SteamUserStats.GetAchievement(achievementId, out bool isUnlocked) && isUnlocked)
        {
            Debug.Log($"이미 해금된 업적: {achievementId}");
            return false;
        }

        bool success = SteamUserStats.SetAchievement(achievementId);
        if (!success)
        {
            Debug.LogError($"업적 설정 실패: {achievementId}");
            return false;
        }

        SteamUserStats.StoreStats();
        Debug.Log($"업적 해금 성공: {achievementId}");
        return true;
    }
}

public static class SteamAchievementIds
{
    public const string SuccessfulExperiment = "SUCCESSFUL_EXPERIMENT";
    public const string SafeCracker = "SAFE_CRACKER";
    public const string ColdOpen = "COLD_OPEN";
    public const string CrispyRat = "CRISPY_RAT";
    public const string FuseFixed = "FUSE_FIXED";
    public const string AGreatBeginning = "A_GREAT_BEGINNING";
    public const string EscapeTheLab = "ESCAPE_THE_LAB";
}