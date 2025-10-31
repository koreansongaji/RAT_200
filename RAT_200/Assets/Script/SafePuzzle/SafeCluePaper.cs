using UnityEngine;
using TMPro;

public class SafeCluePaper : MonoBehaviour
{
    [System.Serializable] struct Line { public CardSuit suit; public TMP_Text text; }

    [Header("문양별 텍스트(TMP)")]
    [SerializeField] Line[] lines;

    void Start()
    {
        var reg = SafeCodeRegistry.Instance;
        if (!reg) return;
        foreach (var l in lines)
        {
            if (!l.text) continue;
            int v = reg.Get(l.suit);
            // 예: "♥ : 7"
            l.text.text = SuitToString(l.suit) + " : " + v.ToString();
        }
    }

    string SuitToString(CardSuit s) =>
        s switch
        {
            CardSuit.Spade => "♠",
            CardSuit.Heart => "♥",
            CardSuit.Diamond => "♦",
            CardSuit.Club => "♣",
            _ => "?"
        };
}
