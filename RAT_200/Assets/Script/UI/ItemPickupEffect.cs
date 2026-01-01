using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 필수

public class ItemPickupEffect : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] ItemDatabase database;

    [Header("Refs")]
    [SerializeField] PlayerInteractor player;
    [SerializeField] Canvas parentCanvas;
    [SerializeField] Image flyingIconPrefab;

    [Header("Animation Settings")]
    [SerializeField] float appearDuration = 0.5f;
    [SerializeField] float moveDuration = 0.6f;
    [SerializeField] Ease appearEase = Ease.OutBack;
    [SerializeField] Ease moveEase = Ease.InBack;

    Camera _cam;

    void Awake() { _cam = Camera.main; }

    void OnEnable()
    {
        if (player) player.OnItemAdded += PlayEffect;
    }

    void OnDisable()
    {
        if (player) player.OnItemAdded -= PlayEffect;
    }

    void PlayEffect(string itemId, Vector3 worldPos)
    {
        if (database == null) return;
        var data = database.GetItem(itemId);
        if (data == null || data.icon == null) return;

        // 1. 아이콘 생성
        Image iconObj = Instantiate(flyingIconPrefab, parentCanvas.transform);
        iconObj.sprite = data.icon;
        iconObj.preserveAspect = true;
        RectTransform rt = iconObj.rectTransform;

        // 좌표 변환
        Vector2 screenPos = _cam.WorldToScreenPoint(worldPos);
        rt.position = screenPos;
        rt.localScale = Vector3.zero;

        // 2. DOTween 시퀀스 생성
        Sequence seq = DOTween.Sequence();

        // ★★★ [핵심 수정 1] SetLink 추가 ★★★
        // "iconObj가 파괴되면 이 시퀀스도 즉시 중단하라"는 명령입니다.
        // 이걸 넣어야 'Destroy'된 오브젝트에 접근하려는 에러가 사라집니다.
        seq.SetLink(iconObj.gameObject, LinkBehaviour.KillOnDestroy);

        // [Phase 1] 등장
        seq.Append(rt.DOScale(1.2f, appearDuration).SetEase(appearEase));
        seq.Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 100f, appearDuration).SetEase(Ease.OutQuad));

        // [Phase 2] 대기
        seq.AppendInterval(0.2f);

        // [Phase 3] 이동
        seq.AppendCallback(() => {
            // ★★★ [핵심 수정 2] null 체크 ★★★
            // 대기 시간 중에 씬이 바뀌거나 해서 player나 iconObj가 사라졌을 수도 있음
            if (player == null || iconObj == null) return;

            Vector3 targetWorld = player.transform.position /*+ Vector3.up * 1.5f*/;
            Vector2 targetScreen = _cam.WorldToScreenPoint(targetWorld);

            // 이동 애니메이션 추가 (이미 시작된 시퀀스에 동적으로 트윈을 붙이는 건 아님,
            // 여기선 별도의 트윈을 실행하거나, 위 구조를 유지하려면 아래 방식 추천)

            // *Callback 안에서 새 트윈을 만들 때는 그 트윈도 Link를 걸어야 함*
            rt.DOMove(targetScreen, moveDuration).SetEase(moveEase)
              .SetLink(iconObj.gameObject);
        });

        // ※ 주의: AppendCallback 내부 로직과 별개로, 
        // 시퀀스 타임라인 상에서 동시에 실행하고 싶다면 Join을 밖에서 써야 하는데,
        // 목표 지점이 '나중에' 결정되어야 한다면 위처럼 콜백 방식이 맞습니다.
        // 다만, 간단하게 하기 위해 여기서는 '작아지는 연출'을 Join으로 미리 예약합니다.

        // 이동하는 시간(moveDuration) 동안 작아지게 예약 (등장+대기 시간 뒤에 실행됨)
        // Insert를 사용해 정확한 타이밍 계산
        float startTime = appearDuration + 0.2f;
        seq.Insert(startTime, rt.DOScale(0f, moveDuration).SetEase(Ease.InQuad));

        // [Phase 4] 삭제
        seq.OnComplete(() => {
            if (iconObj != null) Destroy(iconObj.gameObject);
        });
    }
}