using UnityEngine;

/// <summary>
/// 计数模式：打满指定数量的小球，记录总用时。
///
/// 规则：
///   - 目标数量可选：30 / 50 / 100 个。
///   - 命中一个球计数 +1。
///   - 打满目标数量则结束，记录总用时。
///   - 未命中不计入目标，只影响命中率。
/// </summary>
public class CountMode : ModeBase
{
    [Header("计数模式设置")]
    [Tooltip("目标命中数量")]
    public int targetCount = 50;

    // 运行时状态
    private float elapsedTime;

    public override string ModeName => "计数模式";

    /// <summary>已过去的时间（秒）</summary>
    public float ElapsedTime => elapsedTime;

    /// <summary>目标数量</summary>
    public int TargetCount => targetCount;

    /// <summary>是否已打满目标</summary>
    public bool IsTargetReached => HitCount >= targetCount;

    public override void StartMode()
    {
        base.StartMode();
        elapsedTime = 0f;
    }

    public override void OnHit()
    {
        base.OnHit();

        Debug.Log($"[CountMode] 命中！{HitCount}/{targetCount}");

        if (IsTargetReached)
        {
            EndMode();
            Debug.Log($"[CountMode] 打满 {targetCount} 个！用时 {elapsedTime:F1} 秒，命中率 {Accuracy:P0}");
        }
    }

    public override void OnMiss()
    {
        base.OnMiss();
        Debug.Log($"[CountMode] 未命中！命中率 {Accuracy:P0}");
    }

    public override void TickMode()
    {
        if (IsEnded) return;
        elapsedTime += Time.deltaTime;
    }
}
