using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PC SDK 3.0 모듈 별 Using 구문이 필요합니다.
using static Stove.PCSDK.Base;

using static Stove.PCSDK.IAP;		// IAPSDK_NET 연동시


public class STOVEPCSDK3Manager : MonoBehaviour
{
    // 클래스 상단에 필요한 변수를 선언합니다.
    
    // 초기화 여부를 저장하기 위한 변수
    private bool _isInitialized;
    
    // 코루틴 실행 주기를 저장하기 위한 변수
    private float _runCallbackInternval = 1.0f;
    
    // RunCallbackLoop 코루틴을 저장하기 위한 변수
    private Coroutine _runCallbackCoroutine;

    // 오브젝트를 Singleton 형태로 사용히가 위한 정적 변수
    private static STOVEPCSDK3Manager _instance;
    private static object _lockObject = new object();

    public static STOVEPCSDK3Manager Instance
    {
        get
        {
            lock (_lockObject)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<STOVEPCSDK3Manager>();

                    if (_instance == null)
                    {
                        _instance = new GameObject().AddComponent<STOVEPCSDK3Manager>();
                        _instance.name = "STOVEPCSDK3Manager";
                    }
                }
            }

            return _instance;
        }
    }

    #region

    // DontDestroyOnLoad 처리를 진행
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    // OnDestroy 에서 UnInitialize 호출
    private void OnDestroy()
    {
        if (_isInitialized)
        {
            UnInitialize();
        }
    }

    #endregion        
    
    #region Coroutine
    
    // RunCallback을 처리하기 위한 코루틴을 작성
    private IEnumerator RunCallbackCoroutine()
    {
        var wfs = new WaitForSeconds(_runCallbackInternval);

        while (true)
        {        		
            Base_RunCallback();

            yield return wfs;
        }
    }

    #endregion
    
    #region STOVEPCSDK3Manager public methods

    // Result 구조체 출력 메서드
    public void PrintResult(Result r)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.sdkName : {r.sdkName}");
        sb.AppendLine($" - Result.methodCode : {r.methodCode}");
        sb.AppendLine($" - Result.resultCode : {r.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {r.exceptionMessage}");

        Debug.Log(sb.ToString());
    }

    // CallbackResult 구조체 출력 메서드
    public void PrintCallbackResult(CallbackResult cr)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - CallbackResult.Result.sdkName : {cr.result.sdkName}");
        sb.AppendLine($" - CallbackResult.Result.methodCode : {cr.result.methodCode}");
        sb.AppendLine($" - CallbackResult.Result.resultCode : {cr.result.resultCode}");
        sb.AppendLine($" - CallbackResult.Result.exceptionMessage : {cr.result.exceptionMessage}");
        sb.AppendLine($" - CallbackResult.message : {cr.errorMessage}");
        sb.AppendLine($" - CallbackResult.externalError : {cr.externalError}");

        Debug.Log(sb.ToString());
    }

    // 모듈 통합 초기화를 위한 Initialize 메소드 작성
    public void Initialize(string shopKey)
    {
        StartRunCallbackLoop();

        StovePCInitializeParam initParam;
        initParam.environment = "LIVE";
        initParam.gameId = "GM-275C-6959EF1A_IND";
        initParam.applicationKey = "fa1b9c6bfb0c5ed3141ec75996b4217b9a5537d673fc9af80d34f0eb0ca1afd9";

        Base_Initialize(initParam, (CallbackResult callbackResult) =>
        {
            // Print CallbackResult
            PrintCallbackResult(callbackResult);

            if (callbackResult.result.IsSuccessful())
            {
                Result result = default;

                result = IAP_Initialize(shopKey);
                PrintResult(result);

                _isInitialized = true;
            }
            else
            {
                Debug.Log("Fail to initialize Base SDK");
            }
        });
    }

    // 모듈 통합 정리를 위한 UnInitialize 메소드 작성
    public void UnInitialize()
    {
        Result result;

        this.StopRunCallbackLoop();
        
        result = IAP_UnInitialize();
        PrintResult(result);
        
        result = Base_UnInitialize();
        PrintResult(result);
        
        _isInitialized = false;
    }
    
    // RunCallback을 주기적으로 호출하기 위한 메소드 작성
    public void StartRunCallbackLoop()
    {
        if (_runCallbackCoroutine == null)
        {
            Debug.Log("Start RunCallbackLoop");

            _runCallbackCoroutine = StartCoroutine(RunCallbackCoroutine());
        }
    }

    // Coroutine을 중지하기 위한 메소드 작성
    public void StopRunCallbackLoop()
    {
        if (_runCallbackCoroutine != null)
        {
            Debug.Log("Stop RunCallbackLoop");

            StopCoroutine(_runCallbackCoroutine);
            _runCallbackCoroutine = null;
        }
    }

    #endregion
}
