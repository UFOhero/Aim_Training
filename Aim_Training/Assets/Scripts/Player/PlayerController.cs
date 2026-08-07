using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第一人称视角控制器
/// 负责：鼠标移动 -> 相机旋转（水平Yaw / 垂直Pitch），俯仰角限制 ±89°
/// 手感核心：使用新输入系统 Mouse.current.delta 获取鼠标移动量，
///           配合 CS2 灵敏度换算（见 SensitivitySettings）。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("灵敏度设置")]
    [Tooltip("CS2 风格灵敏度数值，直接填你在 CS2 里使用的灵敏度即可")]
    [Range(0.01f, 20f)]
    public float sensitivity = 1.7f;

    [Tooltip("鼠标 DPI（仅用于计算并显示 cm/360 参考值）")]
    [Range(100f, 10000f)]
    public float mouseDpi = 2400f;

    [Tooltip("是否反转垂直视角（鼠标上移=向下看），大多数玩家保持关闭")]
    public bool invertY = false;

    [Header("视角限制")]
    [Tooltip("俯仰角限制（度），±90 会翻转镜头，FPS 通常限制在 ±89")]
    [Range(1f, 89f)]
    public float pitchLimit = 89f;

    // 相机组件引用（运行时自动获取，无需手动指定）
    private Camera playerCamera;

    // 当前累计的俯仰角（绕 X 轴），用于限制范围
    private float pitch = 0f;

    /// <summary>
    /// 计算 cm/360：鼠标滑过该距离（厘米），视角刚好旋转 360°。
    /// 公式与 CS2 完全一致：
    ///   countsPer360 = 360 / (sensitivity * 0.022)   （鼠标计数）
    ///   cmPer360     = countsPer360 / dpi * 2.54
    /// </summary>
    public float CmPer360
    {
        get
        {
            float counts = 360f / (sensitivity * 0.022f);
            float cm = counts / mouseDpi * 2.54f;
            return cm;
        }
    }

    // 在 Inspector 中显示 cm/360（编辑时通过 OnValidate 刷新，运行时在 Update 刷新）
    [SerializeField]
    private float cmPer360Display;

    private void OnValidate()
    {
        cmPer360Display = CmPer360;
    }

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("PlayerController 需要一个子物体挂 Camera！请把相机放在玩家物体下面。");
        }

        // 确保新输入系统已启用鼠标设备（编辑器/打包后都生效）
        if (Mouse.current != null)
        {
            InputSystem.EnableDevice(Mouse.current);
        }

        // 锁定鼠标到屏幕中央并隐藏光标（FPS 标准行为，Esc 可解锁）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 运行时同步显示 cm/360（方便调节灵敏度时实时观察）
        cmPer360Display = CmPer360;

        // 如果处于编辑器的 UI 输入框焦点中，暂停视角旋转
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 读取鼠标移动量（像素），单位为"点数"
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // 水平旋转：鼠标横向移动 -> 绕 Y 轴旋转
        // yaw = 点数 × 灵敏度 × 0.022 度（Source 引擎的 yaw 系数）
        float yaw = mouseDelta.x * sensitivity * 0.022f;
        transform.Rotate(0f, yaw, 0f, Space.World);

        // 垂直旋转：鼠标纵向移动 -> 修改 pitch 并应用
        // 注意：Unity 屏幕坐标 Y 向上为正，但 3D 中"向上看"需绕 X 轴负方向旋转，
        // 因此标准 FPS 要将 Y 增量取反：鼠标上移 -> 视角上抬
        float pitchDelta = mouseDelta.y * sensitivity * 0.022f;
        if (invertY)
        {
            pitchDelta = -pitchDelta;
        }
        pitch -= pitchDelta;

        // 限制俯仰角在 ±pitchLimit 内，避免镜头翻转
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        // 应用到相机本地旋转（只绕 X 轴），保持相机相对玩家的前后朝向正确
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}
