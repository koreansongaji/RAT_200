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
    public AudioClip sfxSwitchOff;       // 탁!
    public AudioClip sfxCrunch;          // 으직!

    // ★ [추가] 중복 실행 방지용 플래그
    private bool _isEventExecuted = false;
    private bool _triggered = false;

    void Start()
    {
        if (bloodDecal) bloodDecal.SetActive(false);

        if (researcher)
        {
            // 연구원에게 타겟 등록
            researcher.npcTarget = npcRat;
            researcher.OnNpcCaught.AddListener(OnRatFound);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 이미 이벤트가 끝났으면 다시는 발동 안 함
        if (_isEventExecuted) return;

        if (!_triggered && other.CompareTag("Player"))
        {
            _triggered = true;
            Debug.Log("[Director] Player entered trigger. Summoning Researcher...");
            researcher.StartSummon();
        }
    }

    // 연구원이 쥐를 발견했을 때 호출됨
    void OnRatFound()
    {
        // ★ [핵심] 이미 실행 중이거나 끝났으면 무시
        if (_isEventExecuted) return;
        _isEventExecuted = true; // "이제 끝났다"고 표시

        // ★ [핵심] 연구원의 기억 소거
        // 이제 연구원은 NPC를 더 이상 신경 쓰지 않게 됩니다.
        if (researcher)
        {
            researcher.npcTarget = null; // 타겟 해제
            researcher.OnNpcCaught.RemoveListener(OnRatFound); // 이벤트 리스너 끊기
        }

        StartCoroutine(Routine_KillSequence());
    }

    IEnumerator Routine_KillSequence()
    {
        Debug.Log("[Director] Rat Found! Starting Execution.");

        // 1. 조명 붉게 변경
        if (researcher.spotLight) researcher.spotLight.color = Color.red;

        // 2. 2초 대기 (붉은 시선)
        yield return new WaitForSeconds(2.0f);

        // 3. 탁! (완전 암전)
        if (sfxSwitchOff) audioSource.PlayOneShot(sfxSwitchOff);

        // 연구원 라이트 끄기
        if (researcher.spotLight) researcher.spotLight.enabled = false;

        // 4. 으직! (사망)
        yield return new WaitForSeconds(0.2f);
        if (sfxCrunch) audioSource.PlayOneShot(sfxCrunch);

        // 5. 바꿔치기
        yield return new WaitForSeconds(1.0f);
        if (npcRat) npcRat.gameObject.SetActive(false);
        if (bloodDecal) bloodDecal.SetActive(true);

        // 6. 상황 종료 및 퇴근
        if (researcher)
        {
            researcher.spotLight.color = Color.white; // 색상 복구
            researcher.ForceLeave(); // 연구원 퇴근
        }

        Debug.Log("[Director] Sequence Finished.");
    }
}