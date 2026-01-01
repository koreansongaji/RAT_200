using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerInteractor : MonoBehaviour
{
    // ★ 변경: 아이템 ID뿐만 아니라 '획득 위치(Vector3)'도 같이 보냄
    public event Action<string, Vector3> OnItemAdded;

    private readonly HashSet<string> inventory = new();

    public bool HasItem(string id) => inventory.Contains(id);

    // ★ 변경: worldPos 매개변수 추가 (기본값은 0,0,0)
    public void AddItem(string id, Vector3 worldPos = default)
    {
        if (!inventory.Contains(id))
        {
            inventory.Add(id);
        }

        // 만약 위치를 안 넣어서(0,0,0) 왔다면, 그냥 플레이어 위치를 사용하도록 예외 처리 (선택 사항)
        if (worldPos == Vector3.zero) worldPos = transform.position + Vector3.up;

        // ★ 위치 정보와 함께 이벤트 발송
        OnItemAdded?.Invoke(id, worldPos);
    }

    public bool RemoveItem(string id) => inventory.Remove(id);
}