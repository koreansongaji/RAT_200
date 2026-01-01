using UnityEngine;
using System.Collections;

public class RatExecutionDirector : MonoBehaviour
{
    [Header("Actors")]
    public ResearcherController researcher;
    public Transform npcRat;       // 희생될 쥐
    public GameObject bloodDecal;  // 핏자국

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxSwitchOff;       // 탁! (암전)
    public AudioClip sfxCrunch;          // 으직! (사망)

    // 중복 실행 방지용
    private bool _isEventExecuted = false;

    void Start()
    {
        if (bloodDecal) bloodDecal.SetActive(false);

        if (researcher)
        {
            // 연구원에게 "이 쥐도 타겟이야"라고 알려줌
            researcher.npcTarget = npcRat;
            // 연구원이 "NPC 잡았어!"라고 외치면 OnRatFound 실행
            researcher.OnNpcCaught.AddListener(OnRatFound);
        }
    }

    // ★ [삭제됨] OnTriggerEnter : 이제 위치 트리거로 연구원을 부르지 않습니다.

    // 연구원이 NPC를 발견하면(OnNpcCaught) 이 함수가 실행됨
    void OnRatFound()
    {
        if (_isEventExecuted) return;
        _isEventExecuted = true;

        // 연구원 기억 소거 (더 이상 타겟 추적 X)
        if (researcher)
        {
            researcher.npcTarget = null;
            researcher.OnNpcCaught.RemoveListener(OnRatFound);
        }

        StartCoroutine(Routine_KillSequence());
    }

    IEnumerator Routine_KillSequence()
    {
        Debug.Log("[Director] 동료 쥐 발견! 처형 시퀀스 시작.");

        // 1. 잠시 대기 (연구원이 쳐다보는 시간)
        yield return new WaitForSeconds(1.5f);

        // 2. 탁! (완전 암전)
        if (sfxSwitchOff) audioSource.PlayOneShot(sfxSwitchOff);

        // 연구원 라이트 끄기 (어둠 속으로)
        if (researcher.spotLight) researcher.spotLight.enabled = false;

        // 3. 으직! (사망 소리)
        yield return new WaitForSeconds(0.4f);
        if (sfxCrunch) audioSource.PlayOneShot(sfxCrunch);

        // 4. 바꿔치기 (쥐 사라지고 핏자국 생성)
        yield return new WaitForSeconds(1.0f);
        if (npcRat) npcRat.gameObject.SetActive(false);
        if (bloodDecal) bloodDecal.SetActive(true);

        // 5. 상황 종료 및 퇴근
        if (researcher)
        {
            // 색상 복구 불필요 (붉게 안 바꿨으므로)
            // researcher.spotLight.color = Color.white; 

            researcher.ForceLeave(); // 연구원 퇴근 (이때 Noise도 0이 됨)
        }

        Debug.Log("[Director] 시퀀스 종료.");
    }
}