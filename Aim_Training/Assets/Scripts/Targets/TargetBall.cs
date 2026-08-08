using UnityEngine;

/// <summary>
/// 靶球 —— 被射线命中的小球。
/// 负责：被击中时的反馈（缩放消失动画），并通知 TargetManager 命中事件。
/// </summary>
public class TargetBall : MonoBehaviour
{
    [Header("靶球外观")]
    [Tooltip("球的大小（半径，米）")]
    public float radius = 0.25f;

    [Tooltip("球体颜色")]
    public Color ballColor = new Color(1f, 0.4f, 0.2f, 1f); // 橙红色

    [Header("命中反馈")]
    [Tooltip("被击中后消失动画时长（秒）")]
    public float destroyAnimDuration = 0.15f;

    // 归属的 TargetManager（用于命中事件）
    private TargetManager owner;

    private void Awake()
    {
        // 确保有 Sphere Collider（射线命中需要）
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }

        // 关键修复：碰撞器半径也受 GameObject Scale 缩放影响。
        // Unity 默认球半径 0.5，若 Scale=0.5 则渲染半径 0.25。
        // 要让碰撞半径 = radius（世界单位），需除以 Scale：
        //   collider.radius / worldScale * worldScale = radius
        float worldScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z)
        );
        if (worldScale <= 0f) worldScale = 1f;

        col.radius = radius / worldScale;
    }

    private void Start()
    {
        // 设置外观（MeshRenderer 颜色）
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = ballColor;
        }
    }

    /// <summary>由 TargetManager 调用，初始化归属</summary>
    public void Init(TargetManager manager)
    {
        owner = manager;
    }

    /// <summary>
    /// 播放被击中的消失动画并销毁。
    /// 命中检测由 TargetManager 的射线完成，这里只负责视觉反馈。
    /// </summary>
    public void PlayHitAndDestroy()
    {
        // 简单实现：直接销毁。若要缩放动画，可扩展协程。
        Destroy(gameObject);
    }
}
