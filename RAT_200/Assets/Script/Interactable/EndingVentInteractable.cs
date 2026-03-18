using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class EndingVentInteractable : BaseInteractable
{
    [Header("Ending Settings")]
    [Tooltip("���� �� Ȱ��ȭ�� �� ��ü�� ���ߴ� ���̵� ī�޶�")]
    public CinemachineCamera roomWideCamera;
    [Tooltip("ī�޶� ������ ��ȯ�Ǵ� �� �ɸ��� �ð�")]
    public float cameraTransitionDelay = 2.0f;

    [Header("Audio Assets")]
    [Tooltip("��Ʈ Ŭ�� �� ���۵� ���� BGM")]
    public AudioClip endingBgm;
    [Tooltip("�÷� �̹����� ���� �� ����� ȿ����")]
    public AudioClip endingSfx;

    [Header("UI References (Canvas Groups)")]
    [Tooltip("������� ������ �÷� �̹����� (�� 4�� ����: 1, 2, 3, 4�� ����)")]
    public CanvasGroup[] endingImages; // �� �迭�� �����

    public CanvasGroup whiteCurtainGroup;   // ��� ���
    public CanvasGroup creditsGroup;        // Credits �ؽ�Ʈ
    public CanvasGroup thankYouGroup;       // Thank You �ؽ�Ʈ
    public CanvasGroup blackCurtainGroup;   // ������ ����

    [Header("Timing Settings")]
    [Tooltip("�� �̹����� ���̵� �� �Ǵ� �ð�")]
    public float overlayFadeDuration = 2.0f;

    [Tooltip("���� �̹����� ������ ������ ����ϴ� �ð�")]
    public float imageInterval = 2.0f;

    [Tooltip("4��° �̹����� ����! �ϰ� ���̴� ª�� �ð�")]
    public float flashDuration = 0.15f;

    [Tooltip("3��° �̹����� �ٽ� ���� �� ��� Ŀư ������ ���� �ð�")]
    public float finalImageHoldDuration = 3.0f;

    public float whiteFadeDuration = 2.0f;
    public float textFadeDuration = 1.0f;
    public float textStayDuration = 2.5f;
    public float finalBlackFadeDuration = 2.0f;

    private bool _isEndingStarted = false;
    private ResearcherController _researcher;

    void Awake()
    {
        _researcher = FindFirstObjectByType<ResearcherController>();
    }

    void Start()
    {
        // �̹��� �迭 �ʱ�ȭ
        if (endingImages != null)
        {
            foreach (var img in endingImages) InitCanvasGroup(img);
        }

        InitCanvasGroup(whiteCurtainGroup);
        InitCanvasGroup(creditsGroup);
        InitCanvasGroup(thankYouGroup);
        InitCanvasGroup(blackCurtainGroup);

        if (roomWideCamera) roomWideCamera.Priority = 10;
    }

    void InitCanvasGroup(CanvasGroup cg)
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
            cg.blocksRaycasts = false;
        }
    }

    public override bool CanInteract(PlayerInteractor i) => !_isEndingStarted;

    public override void Interact(PlayerInteractor i)
    {
        if (_isEndingStarted) return;

        CleanUpGameEnvironment();
        StartCoroutine(Routine_EndingSequence(i.gameObject));
    }

    void CleanUpGameEnvironment()
    {
        if (_researcher != null && _researcher.CurrentState != ResearcherController.State.Idle)
        {
            _researcher.ForceLeave();
        }

        if (AudioManager.Instance)
        {
            AudioManager.Instance.KillAllSounds();
        }
    }

    IEnumerator Routine_EndingSequence(GameObject playerObj)
    {
        _isEndingStarted = true;

        // Steam achievement: Escape The Lab
        SteamAchievementManager.UnlockAchievement(SteamAchievementIds.EscapeTheLab);
        
        // 1. ī�޶� ��ȯ & BGM
        if (playerObj) playerObj.SetActive(false);
        if (roomWideCamera) roomWideCamera.Priority = 999;

        if (AudioManager.Instance && endingBgm)
        {
            AudioManager.Instance.PlayBgmWithFade(endingBgm, 1.0f);
        }

        yield return new WaitForSeconds(cameraTransitionDelay);

        // 2. SFX ���
        if (AudioManager.Instance && endingSfx)
        {
            AudioManager.Instance.Play(endingSfx, AudioManager.Sound.Effect);
        }

        // 3. �̹��� ���� (1 -> 2 -> 3 -> 4(Flash) -> 3)
        // ���� ��ġ: �迭�� ��������� �н�
        if (endingImages != null && endingImages.Length >= 4)
        {
            // [Image 1] Fade In
            endingImages[0].gameObject.SetActive(true);
            yield return endingImages[0].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 2] Fade In (1�� ���� ���)
            endingImages[1].gameObject.SetActive(true);
            yield return endingImages[1].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 3] Fade In (2�� ���� ���)
            endingImages[2].gameObject.SetActive(true);
            yield return endingImages[2].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 4] Flash! (3�� ���� ����)
            endingImages[3].gameObject.SetActive(true);
            endingImages[3].alpha = 1f; // ���̵� ���� ��� ����

            // �����̴� ������ �ð� ���
            yield return new WaitForSeconds(flashDuration);

            // 4�� ���� -> 3���� �ٽ� ����
            endingImages[3].gameObject.SetActive(false);

            // 3�� �̹��� ���� �ð�
            yield return new WaitForSeconds(finalImageHoldDuration);
        }
        else
        {
            Debug.LogWarning("[Ending] �̹��� �迭�� 4�� �̸��Դϴ�! �ν����͸� Ȯ���ϼ���.");
            yield return new WaitForSeconds(2.0f);
        }

        // 4. ȭ��Ʈ Ŀư (����)
        if (whiteCurtainGroup)
        {
            whiteCurtainGroup.gameObject.SetActive(true);
            yield return whiteCurtainGroup.DOFade(1f, whiteFadeDuration).WaitForCompletion();
        }

        // ���� �̹����� ��� ���� (����ȭ)
        if (endingImages != null)
        {
            foreach (var img in endingImages) if (img) img.gameObject.SetActive(false);
        }

        // 5. Credits
        if (creditsGroup)
        {
            creditsGroup.gameObject.SetActive(true);
            yield return creditsGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return creditsGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            creditsGroup.gameObject.SetActive(false);
        }

        // 6. Thank You
        if (thankYouGroup)
        {
            thankYouGroup.gameObject.SetActive(true);
            yield return thankYouGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return thankYouGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            thankYouGroup.gameObject.SetActive(false);
        }

        // 7. Black Curtain (����)
        if (blackCurtainGroup)
        {
            blackCurtainGroup.gameObject.SetActive(true);
            yield return blackCurtainGroup.DOFade(1f, finalBlackFadeDuration).WaitForCompletion();
        }

        // 8. ���� �� ���ε�
        if (AudioManager.Instance) AudioManager.Instance.KillAllSounds();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}