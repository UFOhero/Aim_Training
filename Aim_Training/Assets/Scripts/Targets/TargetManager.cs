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

    [Header("生成长方体（跟随本物体位置与朝向）")]
    [Tooltip("长方体宽度（米，沿本物体的左右方向）。值越大，球在左右方向的分布范围越宽")]
    public float boxWidth = 10f;

    [Tooltip("长方体高度（米，沿本物体的上下方向）。值越大，球在上下方向的分布范围越高")]
    public float boxHeight = 5f;

    [Tooltip("长方体深度（米，沿本物体的前后方向）。值越大，球在远近方向的分布范围越深")]
    public float boxDepth = 4f;

    [Tooltip("长方体中心相对本物体位置的偏移（米）。X=左右，Y=上下，Z=前后")]
    public Vector3 centerOffset = Vector3.zero;

    [Header("启动自动定位")]
    [Tooltip("勾选后，启动游戏时自动把生成长方体放到相机正前方（省去手动摆放）。取消勾选则使用本物体在场景中的位置")]
    public bool autoPlaceInFrontOfCamera = true;

    [Tooltip("自动定位时，长方体中心离相机的距离（米）")]
    public float autoPlaceDistance = 10f;

    [Tooltip("勾选后，启动时自动旋转玩家（相机所在物体），使其正对生成长方体中心")]
    public bool autoRotatePlayerToFaceArea = true;

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

    /// <summary>命中事件：命中一个靶球时触发（参数=该球的位置）</summary>
    public System.Action<Vector3> OnHit;

    /// <summary>未命中事件：点击但没打中任何靶球时触发</summary>
    public System.Action OnMiss;

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

        // 启动时自动把生成长方体定位到相机正前方（可选）
        if (autoPlaceInFrontOfCamera && playerCamera != null)
        {
            // 只取相机前方水平方向（忽略俯仰），避免区域随抬头低头而抬高
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            // 定位到相机前方 autoPlaceDistance 米处
            transform.position = playerCamera.transform.position + forward * autoPlaceDistance;

            // 让本物体朝向相机前方（水平方向），使长方体的"前后"对齐相机视向
            if (forward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            Log("自动定位生成长方体到相机前方，距离 " + autoPlaceDistance + " 米");
        }

        // 自动旋转玩家，使其正对生成长方体中心（可选）
        if (autoRotatePlayerToFaceArea)
        {
            // 计算玩家应面向的方向：区域中心 -> 玩家位置
            Vector3 areaCenter = transform.position;
            Vector3 playerPos = playerCamera.transform.position;
            Vector3 toArea = areaCenter - playerPos;
            toArea.y = 0f;
            toArea.Normalize();

            if (toArea.sqrMagnitude > 0.0001f)
            {
                // 旋转玩家根物体（Player），使相机朝向区域中心
                Transform playerRoot = playerCamera.transform.parent != null
                    ? playerCamera.transform.parent
                    : playerCamera.transform;

                Quaternion targetRot = Quaternion.LookRotation(toArea, Vector3.up);
                playerRoot.rotation = targetRot;

                // 相机是子物体，若相机有本地旋转则保留（此处场景相机本地旋转为 0）
                Log("自动旋转玩家，正对生成长方体中心");
            }
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
                Vector3 hitPos = ball.transform.position;
                Log("命中靶球！位置: " + hitPos);

                // 通知外部（模式管理器）命中事件
                OnHit?.Invoke(hitPos);

                Destroy(ball.gameObject);
                activeBalls.Remove(ball);
                currentCount--;
                SpawnOneBall();
            }
            else
            {
                Log("射线命中物体，但不是靶球: " + hit.collider.name);
                OnMiss?.Invoke();
            }
        }
        else
        {
            Log("射线未命中任何物体");
            OnMiss?.Invoke();
        }
    }

    /// <summary>
    /// 在【本物体局部坐标】的长方体内随机生成一个小球。
    /// 长方体跟随本物体的位置与旋转：
    ///   - 宽度沿物体右方向（localX）
    ///   - 高度沿物体上方向（localY）
    ///   - 深度沿物体前方向（localZ）
    /// 这样旋转物体时，长方体跟着转，"宽=左右、高=上下、深=前后"的语义始终成立。
    /// </summary>
    private void SpawnOneBall()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            // 在物体局部坐标的长方体内随机取点
            Vector3 localPos = centerOffset + new Vector3(
                Random.Range(-boxWidth * 0.5f, boxWidth * 0.5f),
                Random.Range(-boxHeight * 0.5f, boxHeight * 0.5f),
                Random.Range(-boxDepth * 0.5f, boxDepth * 0.5f)
            );

            // 局部坐标 -> 世界坐标（跟随物体的位置、旋转、缩放）
            Vector3 pos = transform.TransformPoint(localPos);

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

    // 在 Scene 视图画出长方体线框，方便摆放区域（跟随物体旋转）
    private void OnDrawGizmosSelected()
    {
        if (!showBoundsGizmo) return;

        Vector3 center = transform.TransformPoint(centerOffset);
        Vector3 size = new Vector3(boxWidth, boxHeight, boxDepth);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix; // 让线框跟随物体旋转
        Gizmos.DrawWireCube(centerOffset, size);
        Gizmos.matrix = Matrix4x4.identity;
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
