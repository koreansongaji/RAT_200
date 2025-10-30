using UnityEngine;
using System.Collections.Generic;

public enum CardSuit { Spade, Heart, Diamond, Club }

public class SafeCodeRegistry : MonoBehaviour
{
    public static SafeCodeRegistry Instance { get; private set; }

    [Header("Options")]
    [Tooltip("서로 다른 숫자만 사용")] public bool uniqueDigits = true;
    [Tooltip("디버그 고정 시드(-1=랜덤)")] public int debugSeed = -1;

    [Header("Runtime (read-only)")]
    public Dictionary<CardSuit, int> code = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (code.Count == 0) Generate();
    }

    [ContextMenu("Regenerate (Play Mode)")]
    public void Generate()
    {
        code.Clear();
        if (debugSeed >= 0) Random.InitState(debugSeed);

        List<int> bag = new(10);
        for (int i = 0; i < 10; i++) bag.Add(i);

        void Assign(CardSuit s)
        {
            int v;
            if (uniqueDigits)
            {
                int idx = Random.Range(0, bag.Count);
                v = bag[idx];
                bag.RemoveAt(idx);
            }
            else
            {
                v = Random.Range(0, 10);
            }
            code[s] = v;
        }

        Assign(CardSuit.Spade);
        Assign(CardSuit.Heart);
        Assign(CardSuit.Diamond);
        Assign(CardSuit.Club);

        Debug.Log($"[SafeCodeRegistry] Code: S{code[CardSuit.Spade]} H{code[CardSuit.Heart]} D{code[CardSuit.Diamond]} C{code[CardSuit.Club]}");
    }

    public int Get(CardSuit s) => code.TryGetValue(s, out var v) ? v : 0;
}
