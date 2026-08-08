using UnityEngine;

/// <summary>
/// 模式管理器 —— 在三种练枪模式之间切换，并把靶球事件连接到当前模式。
///
/// 用法：
///   - 在场景中创建一个空物体挂 ModeManager。
///   - 在 Inspector 中把三个模式脚本（FreeMode/TimedMode/CountMode）
///     以组件形式挂到同一个物体上，并拖到对应槽位。
///   - 把 TargetManager 拖到 targetManager 槽。
///   - 通过 SetModeAndStart(...) 切换模式并开始。
/// </summary>
public class ModeManager : MonoBehaviour
{
    [Header("模式脚本引用")]
    public FreeMode freeMode;
    public TimedMode timedMode;
    public CountMode countMode;

    [Header("靶球管理器")]
    public TargetManager targetManager;

    // 当前激活的模式
    private ModeBase currentMode;

    /// <summary>当前模式</summary>
    public ModeBase CurrentMode => currentMode;

    /// <summary>模式结束时触发</summary>
    public event System.Action<ModeBase> OnModeEnded;

    private void Start()
    {
        // 订阅靶球事件
        if (targetManager != null)
        {
            targetManager.OnHit += HandleHit;
            targetManager.OnMiss += HandleMiss;
        }
    }

    private void Update()
    {
        // 每帧驱动当前模式的计时逻辑
        if (currentMode != null && !currentMode.IsEnded)
        {
            currentMode.TickMode();
        }
    }

    private void OnDestroy()
    {
        if (targetManager != null)
        {
            targetManager.OnHit -= HandleHit;
            targetManager.OnMiss -= HandleMiss;
        }
    }

    /// <summary>
    /// 切换并开始一个模式。
    /// type: "Free" / "Timed" / "Count"
    /// 可选的模式参数：时长(秒)/目标数量，根据类型设置。
    /// </summary>
    public void SetModeAndStart(string type, float param = 0f)
    {
        // 结束当前模式
        if (currentMode != null && !currentMode.IsEnded)
        {
            currentMode.EndMode();
        }

        // 选择新模式
        switch (type)
        {
            case "Free":
                currentMode = freeMode;
                break;
            case "Timed":
                currentMode = timedMode;
                if (param > 0f) timedMode.durationSeconds = param;
                break;
            case "Count":
                currentMode = countMode;
                if (param > 0f) countMode.targetCount = (int)param;
                break;
            default:
                Debug.LogError($"[ModeManager] 未知模式类型: {type}");
                return;
        }

        if (currentMode == null)
        {
            Debug.LogError("[ModeManager] 模式脚本未在 Inspector 中赋值！");
            return;
        }

        // 订阅模式结束事件
        currentMode.OnModeEnded += HandleModeEnded;

        // 开始模式
        currentMode.StartMode();
        Debug.Log($"[ModeManager] 开始模式: {currentMode.ModeName}");
    }

    /// <summary>处理靶球命中事件</summary>
    private void HandleHit(Vector3 hitPos)
    {
        Debug.Log($"[ModeManager] 收到命中事件，currentMode={(currentMode != null ? currentMode.ModeName : "NULL")}, IsEnded={(currentMode != null ? currentMode.IsEnded.ToString() : "?")}");
        if (currentMode != null && !currentMode.IsEnded)
        {
            currentMode.OnHit();
        }
    }

    /// <summary>处理靶球未命中事件</summary>
    private void HandleMiss()
    {
        if (currentMode != null && !currentMode.IsEnded)
        {
            currentMode.OnMiss();
        }
    }

    private void HandleModeEnded(ModeBase mode)
    {
        // 取消订阅，避免重复
        mode.OnModeEnded -= HandleModeEnded;
        Debug.Log($"[ModeManager] 模式结束: {mode.ModeName}");
        OnModeEnded?.Invoke(mode);
    }
}
