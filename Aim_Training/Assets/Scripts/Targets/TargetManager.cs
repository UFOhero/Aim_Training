using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 靶球管理器 —— 负责"场上常驻 3 个小球，打掉一个随机补一个"。
///
/// 生成方式：在【世界坐标固定】的长方体内随机生成。
///   - 长方体以 TargetManager 所在物体（transform.position）为中心。
///   - 球的相对位置固定，不随玩家视角变化。
///   - 玩家通过转动视角在区域内寻找并击打小球。
///
/// 如何摆放：
///   - 把挂 TargetManager 的空物体放到场景中你想放置小球区域的位置
///     （例如玩家前方 12 米、略高于视线处）。
///   - 通过 Inspector 调整 boxWidth / boxHeight / boxDepth / centerOffset。
///   - 在 Scene 视图能看到半透明线框标出的区域。
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("靶球预制体")]
    [Tooltip("靶球预制体（含 TargetBall 组件的球）")]
    public GameObject targetBallPrefab;

    [Header("场上球数")]
    [Tooltip("场上常驻的靶球数量")]
    public int ballCount = 3;

    [Header("生成长方体（世界固定，以本物体位置为中心）")]
    [Tooltip("长方体宽度（米，X 轴）")]
    public float boxWidth = 10f;

    [Tooltip("长方体高度（米，Y 轴）")]
    public float boxHeight = 5f;

    [Tooltip("长方体深度（米，Z 轴）")]
    public float boxDepth = 4f;

    [Tooltip("长方体中心相对本物体位置的偏移（米）")]
    public Vector3 centerOffset = Vector3.zero;

    [Tooltip("最小间距（避免球重叠，米）")]
    public float minSpacing = 2.5f;

    [Header("调试")]
    [Tooltip("是否在 Console 输出调试日志")]
    public bool debugLog = true;

    [Tooltip("是否在 Scene 视图显示生成长方体线框")]
    public bool showBoundsGizmo = true;

    // 内部状态
    private int currentCount = 0;
    private readonly System.Collections.Generic.List<TargetBall> activeBalls = new System.Collections.Generic.List<TargetBall>();
    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        if (playerCamera == null)
        {
            Debug.LogError("[TargetManager] 找不到场景中的 Camera！");
        }
    }

    private void Start()
    {
        if (targetBallPrefab == null)
        {
            Debug.LogError("[TargetManager] 未指定 targetBallPrefab！请在 Inspector 中拖入靶球预制体。");
            return;
        }

        Log("初始化，生成 " + ballCount + " 个靶球");
        for (int i = 0; i < ballCount; i++)
        {
            SpawnOneBall();
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShootRaycast();
        }
    }

    /// <summary>从屏幕中心（准星）发射射线，检测是否命中靶球</summary>
    private void TryShootRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            TargetBall ball = hit.collider.GetComponentInParent<TargetBall>();
            if (ball != null)
            {
                Log("命中靶球！位置: " + ball.transform.position);
                Destroy(ball.gameObject);
                activeBalls.Remove(ball);
                currentCount--;
                SpawnOneBall();
            }
            else
            {
                Log("射线命中物体，但不是靶球: " + hit.collider.name);
            }
        }
        else
        {
            Log("射线未命中任何物体");
        }
    }

    /// <summary>
    /// 在【世界坐标固定】的长方体内随机生成一个小球。
    /// 长方体中心 = transform.position + centerOffset。
    /// </summary>
    private void SpawnOneBall()
    {
        Vector3 boxCenter = transform.position + centerOffset;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            // 在世界坐标长方体内随机取点
            Vector3 pos = boxCenter + new Vector3(
                Random.Range(-boxWidth * 0.5f, boxWidth * 0.5f),
                Random.Range(-boxHeight * 0.5f, boxHeight * 0.5f),
                Random.Range(-boxDepth * 0.5f, boxDepth * 0.5f)
            );

            // 检查与已有球是否重叠
            bool overlap = false;
            foreach (TargetBall b in activeBalls)
            {
                if (b != null && Vector3.Distance(pos, b.transform.position) < minSpacing)
                {
                    overlap = true;
                    break;
                }
            }
            if (overlap) continue;

            // 生成球
            GameObject go = Instantiate(targetBallPrefab, pos, Quaternion.identity, transform);
            TargetBall ball = go.GetComponent<TargetBall>();
            if (ball == null)
            {
                ball = go.AddComponent<TargetBall>();
            }
            ball.Init(this);

            activeBalls.Add(ball);
            currentCount++;
            return;
        }

        LogWarning("无法找到合适的生成位置（尝试 30 次后放弃）。");
    }

    // 在 Scene 视图画出长方体线框，方便摆放区域
    private void OnDrawGizmosSelected()
    {
        if (!showBoundsGizmo) return;

        Vector3 center = transform.position + centerOffset;
        Vector3 size = new Vector3(boxWidth, boxHeight, boxDepth);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireCube(center, size);
    }

    private void Log(string msg)
    {
        if (debugLog) Debug.Log("[TargetManager] " + msg);
    }

    private void LogWarning(string msg)
    {
        if (debugLog) Debug.LogWarning("[TargetManager] " + msg);
    }
}
