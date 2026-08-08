using UnityEngine;

/// <summary>
/// 自由练习模式：无时间限制，无限练习。
/// 只统计命中/未命中/命中率，不结束（玩家主动退出）。
/// </summary>
public class FreeMode : ModeBase
{
    public override string ModeName => "自由练习";

    public override void StartMode()
    {
        base.StartMode();
        Debug.Log("[FreeMode] 自由练习开始，无时间限制");
    }

    public override void OnHit()
    {
        base.OnHit();
        Debug.Log($"[FreeMode] 命中！累计命中 {HitCount}，命中率 {Accuracy:P0}");
    }

    public override void OnMiss()
    {
        base.OnMiss();
        Debug.Log($"[FreeMode] 未命中！累计未命中 {MissCount}，命中率 {Accuracy:P0}");
    }
}
