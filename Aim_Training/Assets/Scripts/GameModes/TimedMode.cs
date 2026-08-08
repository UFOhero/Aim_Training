using UnityEngine;

/// <summary>
/// 计时模式：在限定时间内尽可能多命中。
///
/// 规则：
///   - 可选择时长：0.5 / 1 / 2 / 5 分钟。
///   - 命中 +1 分。
///   - 连续命中（连击 >= 2）额外 +1 分。
///   - 未命中：不得分，且连击清零。
///   - 倒计时归零则结束。
/// </summary>
public class TimedMode : ModeBase
{
    [Header("计时模式设置")]
    [Tooltip("游戏时长（秒）")]
    public float durationSeconds = 60f;

    // 运行时状态
    private float timeRemaining;
    private int currentCombo;
    private int maxCombo;
    private int score;

    public override string ModeName => "计时模式";

    /// <summary>剩余时间（秒）</summary>
    public float TimeRemaining => timeRemaining;

    /// <summary>当前连击</summary>
    public int CurrentCombo => currentCombo;

    /// <summary>最高连击</summary>
    public int MaxCombo => maxCombo;

    /// <summary>当前得分</summary>
    public int Score => score;

    /// <summary>当前模式是否结束（倒计时到 0）</summary>
    public bool IsTimeUp => timeRemaining <= 0f;

    public override void StartMode()
    {
        base.StartMode();
        timeRemaining = durationSeconds;
        currentCombo = 0;
        maxCombo = 0;
        score = 0;
    }

    public override void OnHit()
    {
        base.OnHit();

        // 连击 +1
        currentCombo++;
        if (currentCombo > maxCombo) maxCombo = currentCombo;

        // 计分：基础 1 分 + 连击奖励（连击 >= 2 时额外 +1）
        int gained = 1;
        if (currentCombo >= 2)
        {
            gained += 1;
        }
        score += gained;

        Debug.Log($"[TimedMode] 命中！连击 {currentCombo}，+{gained} 分，总分 {score}");
    }

    public override void OnMiss()
    {
        base.OnMiss();

        // 未命中：连击清零，不得分
        if (currentCombo > 0)
        {
            Debug.Log($"[TimedMode] 未命中，连击中断（曾达 {currentCombo}）");
        }
        currentCombo = 0;
    }

    public override void TickMode()
    {
        if (IsEnded) return;

        // 倒计时
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndMode();
            Debug.Log($"[TimedMode] 时间到！最终得分 {score}，最高连击 {maxCombo}，命中率 {Accuracy:P0}");
        }
    }
}
