using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Stove.PCSDK.Base;
using static Stove.PCSDK.GameSupport;
using static Stove.PCSDK.IAP;

public class STOVEPCSDK3Manager : MonoBehaviour
{
    [Header("STOVE SDK")]
    [SerializeField] private bool autoInitializeOnAwake = true;
    [SerializeField] private bool logUserProfileOnInitialize = true;
    [SerializeField] private string environment = "LIVE";
    [SerializeField] private string gameId = "GM-275C-6959EF1A_IND";
    [SerializeField] private string applicationKey = "fa1b9c6bfb0c5ed3141ec75996b4217b9a5537d673fc9af80d34f0eb0ca1afd9";
    [SerializeField] private string shopKey = string.Empty;
    [SerializeField] private bool initializeIap;
    [SerializeField] private float runCallbackInterval = 1.0f;

    private bool _isInitialized;
    private bool _isInitializing;
    private bool _gameSupportInitialized;
    private bool _iapInitialized;
    private Coroutine _runCallbackCoroutine;
    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    private static STOVEPCSDK3Manager _instance;
    private static readonly object LockObject = new object();

    public static STOVEPCSDK3Manager Instance
    {
        get
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<STOVEPCSDK3Manager>();

                    if (_instance == null)
                    {
                        var managerObject = new GameObject("STOVEPCSDK3Manager");
                        _instance = managerObject.AddComponent<STOVEPCSDK3Manager>();
                    }
                }
            }

            return _instance;
        }
    }

    public bool IsInitialized => _isInitialized;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoInitializeOnAwake)
        {
            Initialize();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (_isInitialized || _isInitializing)
        {
            UnInitialize();
        }
    }

    private IEnumerator RunCallbackCoroutine()
    {
        var wait = new WaitForSeconds(runCallbackInterval);

        while (true)
        {
            Base_RunCallback();
            yield return wait;
        }
    }

    public void PrintResult(Result result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.sdkName : {result.sdkName}");
        sb.AppendLine($" - Result.methodCode : {result.methodCode}");
        sb.AppendLine($" - Result.resultCode : {result.resultCode}");
        sb.AppendLine($" - Result.resultCodeName : {ToBaseResultCodeName(result.resultCode)}");
        sb.AppendLine($" - Result.exceptionMessage : {result.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    public void PrintCallbackResult(CallbackResult callbackResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - CallbackResult.Result.sdkName : {callbackResult.result.sdkName}");
        sb.AppendLine($" - CallbackResult.Result.methodCode : {callbackResult.result.methodCode}");
        sb.AppendLine($" - CallbackResult.Result.resultCode : {callbackResult.result.resultCode}");
        sb.AppendLine($" - CallbackResult.Result.resultCodeName : {ToBaseResultCodeName(callbackResult.result.resultCode)}");
        sb.AppendLine($" - CallbackResult.Result.exceptionMessage : {callbackResult.result.exceptionMessage}");
        sb.AppendLine($" - CallbackResult.message : {callbackResult.errorMessage}");
        sb.AppendLine($" - CallbackResult.externalError : {callbackResult.externalError}");
        Debug.Log(sb.ToString());
    }

    public void Initialize()
    {
        Initialize(shopKey);
    }

    public void Initialize(string overrideShopKey)
    {
        if (_isInitialized || _isInitializing)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
        {
            Debug.LogWarning($"[STOVE] Initialize skipped on unsupported platform: {Application.platform}");
            return;
        }

        _isInitializing = true;
        StartRunCallbackLoop();

        StovePCInitializeParam initParam;
        initParam.environment = environment;
        initParam.gameId = gameId;
        initParam.applicationKey = applicationKey;

        Base_Initialize(initParam, callbackResult =>
        {
            PrintCallbackResult(callbackResult);

            if (!callbackResult.result.IsSuccessful())
            {
                Debug.LogError($"[STOVE] Base SDK initialize failed. code={callbackResult.result.resultCode} ({ToBaseResultCodeName(callbackResult.result.resultCode)}), message={callbackResult.errorMessage}, exception={callbackResult.result.exceptionMessage}");
                _isInitializing = false;
                return;
            }

            var result = GameSupport_Initialize();
            PrintResult(result);
            _gameSupportInitialized = result.IsSuccessful();

            if (!_gameSupportInitialized)
            {
                Debug.LogError($"[STOVE] GameSupport initialize failed. code={result.resultCode} ({ToBaseResultCodeName(result.resultCode)}), exception={result.exceptionMessage}");
                _isInitializing = false;
                return;
            }

            var targetShopKey = string.IsNullOrWhiteSpace(overrideShopKey) ? shopKey : overrideShopKey;
            if (initializeIap && !string.IsNullOrWhiteSpace(targetShopKey))
            {
                result = IAP_Initialize(targetShopKey);
                PrintResult(result);
                _iapInitialized = result.IsSuccessful();
            }
            else
            {
                Debug.Log("[STOVE] IAP initialize skipped.");
                _iapInitialized = false;
            }

            _isInitialized = true;
            _isInitializing = false;

            if (logUserProfileOnInitialize)
            {
                LogCurrentUserProfile();
            }

            FlushPendingActions();
        });
    }

    public void RunWhenInitialized(Action action)
    {
        if (action == null)
        {
            return;
        }

        if (_isInitialized)
        {
            action.Invoke();
            return;
        }

        _pendingActions.Enqueue(action);
        Initialize();
    }

    public void UnInitialize()
    {
        StopRunCallbackLoop();

        if (_iapInitialized)
        {
            var result = IAP_UnInitialize();
            PrintResult(result);
        }

        if (_gameSupportInitialized)
        {
            var result = GameSupport_UnInitialize();
            PrintResult(result);
        }

        var baseResult = Base_UnInitialize();
        PrintResult(baseResult);

        _isInitialized = false;
        _isInitializing = false;
        _gameSupportInitialized = false;
        _iapInitialized = false;
        _pendingActions.Clear();
    }

    public void StartRunCallbackLoop()
    {
        if (_runCallbackCoroutine != null)
        {
            return;
        }

        Debug.Log("[STOVE] Start RunCallbackLoop");
        _runCallbackCoroutine = StartCoroutine(RunCallbackCoroutine());
    }

    public void StopRunCallbackLoop()
    {
        if (_runCallbackCoroutine == null)
        {
            return;
        }

        Debug.Log("[STOVE] Stop RunCallbackLoop");
        StopCoroutine(_runCallbackCoroutine);
        _runCallbackCoroutine = null;
    }

    [ContextMenu("Log STOVE User Profile")]
    public void LogCurrentUserProfile()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[STOVE] Cannot read user profile before SDK initialization.");
            return;
        }

        StovePCUser user = default;
        var result = Base_GetUser(ref user);
        PrintResult(result);

        if (!result.IsSuccessful())
        {
            Debug.LogError($"[STOVE] Base_GetUser failed. code={result.resultCode} ({ToBaseResultCodeName(result.resultCode)}), exception={result.exceptionMessage}");
            return;
        }

        Debug.Log($"[STOVE] User Profile: nickname={user.nickname}, gameUserId={user.gameUserId}");
    }

    private void FlushPendingActions()
    {
        while (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();
            action?.Invoke();
        }
    }

    private static string ToBaseResultCodeName(uint resultCode)
    {
        try
        {
            var enumValue = (BaseSDKResultCode)resultCode;
            return Enum.IsDefined(typeof(BaseSDKResultCode), enumValue) ? enumValue.ToString() : "UNKNOWN_CODE";
        }
        catch
        {
            return "UNKNOWN_CODE";
        }
    }
}
