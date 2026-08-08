using UnityEngine;

/// <summary>
/// 练枪模式抽象基类。
/// 定义所有模式共有的生命周期：开始 / 命中 / 未命中 / 结束。
/// 三种模式（自由练习、计时、计数）继承并实现自己的规则。
/// </summary>
public abstract class ModeBase : MonoBehaviour
{
    /// <summary>模式是否已结束</summary>
    public bool IsEnded { get; protected set; }

    /// <summary>模式名称（用于 UI 显示）</summary>
    public abstract string ModeName { get; }

    /// <summary>当前命中次数</summary>
    public int HitCount { get; protected set; }

    /// <summary>当前未命中次数</summary>
    public int MissCount { get; protected set; }

    /// <summary>命中率（0~1）</summary>
    public float Accuracy
    {
        get
        {
            int total = HitCount + MissCount;
            return total == 0 ? 0f : (float)HitCount / total;
        }
    }

    /// <summary>模式结束时触发（参数=是否正常结束）</summary>
    public event System.Action<ModeBase> OnModeEnded;

    /// <summary>开始模式</summary>
    public virtual void StartMode()
    {
        IsEnded = false;
        HitCount = 0;
        MissCount = 0;
    }

    /// <summary>命中回调（由 ModeManager 调用）</summary>
    public virtual void OnHit()
    {
        HitCount++;
    }

    /// <summary>未命中回调（由 ModeManager 调用）</summary>
    public virtual void OnMiss()
    {
        MissCount++;
    }

    /// <summary>结束模式</summary>
    public virtual void EndMode()
    {
        if (IsEnded) return;
        IsEnded = true;
        OnModeEnded?.Invoke(this);
    }

    /// <summary>每帧更新（子类可覆写，用于倒计时等）</summary>
    public virtual void TickMode() { }
}
