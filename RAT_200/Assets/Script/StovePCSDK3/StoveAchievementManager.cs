using UnityEngine;
using static Stove.PCSDK.Base;
using static Stove.PCSDK.GameSupport;

public static class StoveAchievementManager
{
    public static void UnlockByStat(string statId)
    {
        GameSupport_ModifyStat(statId, 1, (CallbackResult callbackResult, StovePCModifyStatValue statValue) =>
        {
            if (!callbackResult.result.IsSuccessful())
            {
                Debug.LogError($"[STOVE] ModifyStat failed: {statId}, resultCode={callbackResult.result.resultCode}, error={callbackResult.errorMessage}");
                return;
            }

            Debug.Log($"[STOVE] ModifyStat success: {statId}, updated={statValue.updated}, currentValue={statValue.currentValue}");
        });
    }

    public static void RequestAchievementState(string achievementId)
    {
        GameSupport_Achievement(achievementId, (CallbackResult callbackResult, StovePCAchievement achievement) =>
        {
            if (!callbackResult.result.IsSuccessful())
            {
                Debug.LogError($"[STOVE] GetAchievement failed: {achievementId}, resultCode={callbackResult.result.resultCode}, error={callbackResult.errorMessage}");
                return;
            }

            Debug.Log($"[STOVE] Achievement: id={achievement.achievementId}, name={achievement.name}, status={achievement.status}, value={achievement.value}");
        });
    }
}

public static class StoveAchievementStatIds
{
    public const string SuccessfulExperiment = "SUCCESSFUL_EXPERIMENT";
    public const string SafeCracker = "SAFE_CRACKER";
    public const string ColdOpen = "COLD_OPEN";
    public const string CrispyRat = "CRISPY_RAT";
    public const string FuseFixed = "FUSE_FIXED";
    public const string AGreatBeginning = "A_GREAT_BEGINNING";
    public const string EscapeTheLab = "ESCAPE_THE_LAB";
}