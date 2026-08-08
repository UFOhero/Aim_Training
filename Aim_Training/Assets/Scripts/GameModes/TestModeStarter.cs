using UnityEngine;

/// <summary>
/// 临时测试启动器：启动时自动开始一个模式，方便在没有 UI 的情况下测试。
/// 测试完三种模式后，接入正式 UI 时可删除。
/// </summary>
public class TestModeStarter : MonoBehaviour
{
    [Header("测试模式")]
    [Tooltip("启动时自动开始的模式: Free / Timed / Count")]
    public string modeToStart = "Timed";

    [Tooltip("模式参数：Timed 填秒数，Count 填目标数，Free 忽略")]
    public float modeParam = 30f;

    private void Start()
    {
        ModeManager mgr = GetComponent<ModeManager>();
        if (mgr == null)
        {
            mgr = FindFirstObjectByType<ModeManager>();
        }

        if (mgr != null)
        {
            Debug.Log($"[TestModeStarter] 自动开始模式: {modeToStart}, 参数: {modeParam}");
            mgr.SetModeAndStart(modeToStart, modeParam);
        }
        else
        {
            Debug.LogError("[TestModeStarter] 找不到 ModeManager！");
        }
    }
}
