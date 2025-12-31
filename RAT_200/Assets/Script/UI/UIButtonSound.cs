using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip _hoverSound;
    [SerializeField] private AudioClip _clickSound;

    private List<Button> _subscribedButtons = new List<Button>();

    private void Awake()
    {
        // 리소스 로드 (경로가 정확한지 확인해 주세요)
        if (_hoverSound == null) _hoverSound = Resources.Load<AudioClip>("Sounds/Effect/UI/UI_hover");
        if (_clickSound == null) _clickSound = Resources.Load<AudioClip>("Sounds/Effect/UI/UI_click");

        SubscribeChildButtons();
    }

    /// <summary>
    /// 모든 자식 버튼을 찾아 클릭 이벤트를 구독합니다.
    /// </summary>
    public void SubscribeChildButtons()
    {
        // 기존 구독 해제 (중복 방지)
        UnsubscribeAll();

        // 비활성화된 버튼까지 모두 포함하여 검색
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            btn.onClick.AddListener(PlayClickSound);
            _subscribedButtons.Add(btn);
        }
        
        Debug.Log($"{gameObject.name}: {_subscribedButtons.Count}개의 자식 버튼 구독 완료");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 올라간 대상이 버튼(또는 버튼의 자식)인지 확인
        Button targetButton = eventData.pointerEnter?.GetComponentInParent<Button>();
        
        // 버튼이 없거나, 비활성화(interactable = false) 상태면 소리 안 냄
        if (targetButton == null || !targetButton.interactable) return;

        if (_hoverSound != null)
        {
            // Hover는 피치 변화를 작게 (0.98 ~ 1.02) 주어 귀가 편안하게 설정
            float randomPitch = Random.Range(0.98f, 1.02f);
            AudioManager.Instance.Play(_hoverSound, AudioManager.Sound.Effect, randomPitch, 0.5f);
        }
    }

    private void PlayClickSound()
    {
        if (_clickSound != null)
        {
            // Click은 피치 변화를 조금 더 넓게 (0.95 ~ 1.05) 주어 매번 다른 느낌을 줌
            float randomPitch = Random.Range(0.95f, 1.05f);
            AudioManager.Instance.Play(_clickSound, AudioManager.Sound.Effect, randomPitch);
        }
    }

    private void UnsubscribeAll()
    {
        foreach (var btn in _subscribedButtons)
        {
            if (btn != null)
                btn.onClick.RemoveListener(PlayClickSound);
        }
        _subscribedButtons.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
    }
}
