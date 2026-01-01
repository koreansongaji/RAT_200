using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public string id;       // 예: "Sodium"
        public string label;    // 예: "나트륨 (Na)"
        public Sprite icon;     // 아이콘 이미지
    }

    public List<ItemData> items = new List<ItemData>();

    // ID로 아이템 데이터 찾기 (헬퍼 함수)
    public ItemData GetItem(string id)
    {
        return items.Find(x => x.id == id);
    }
}