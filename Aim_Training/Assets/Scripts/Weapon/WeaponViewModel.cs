using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 枪械视角层 —— 2D 贴图枪（当前用色块拼出抽象枪占位）。
///
/// ========== v6 版本说明 ==========
/// v6 = v5（用户认可：枪口对准准星）+ 修复左右手切换 Y 漂移。
///
/// 左右手切换 Y 漂移的根因（v5）：
///   部件在 Awake 时按初始 handSide 镜像，但运行时切换 handSide 时
///   部件不重建，而 pivot/角度按新 handSide 计算 -> 错配导致漂移。
///
/// v6 修复（最小改动，3 处）：
///   1) 部件不再镜像（统一朝 +X）：运行时切换无需重建部件。
///   2) pivot 恒定（不随左右手变化）：握把 Y 天然不动。
///   3) 去掉左手 -180° 补偿：部件不镜像后，左手角度自然指向准星。
///
/// 效果：左右手切换时，握把仅镜像水平位置，Y 完全不变。
///       右手观感与 v5 完全相同。
/// </summary>
public class WeaponViewModel : MonoBehaviour
{
    public enum HandSide
    {
        Right, // 右手持枪：枪在屏幕右下，枪口朝左上指向准星
        Left   // 左手持枪：枪在屏幕左下，枪口朝右上指向准星
    }

    [Header("枪械显示")]
    public bool showWeapon = true;

    [Tooltip("持枪手：右手 / 左手")]
    public HandSide handSide = HandSide.Right;

    [Tooltip("握把相对屏幕中心的位置偏移（像素）。X 水平，Y 垂直")]
    public Vector2 handOffset = new Vector2(320f, -240f);

    [Tooltip("枪的整体缩放（绕握把点缩放）")]
    [Range(0.1f, 3f)]
    public float weaponScale = 1f;

    [Header("手持摆动")]
    [Tooltip("鼠标移动时枪身摆动幅度（0=不摆动）")]
    [Range(0f, 30f)]
    public float swayAmount = 6f;

    [Tooltip("摆动平滑速度")]
    [Range(1f, 20f)]
    public float swaySmooth = 8f;

    private Canvas canvas;
    private RectTransform weaponRoot;
    private Vector2 currentSway = Vector2.zero;
    private float currentSwayRot = 0f;

    // 握把在枪框局部坐标中的位置（恒定，不随左右手变化）
    // 用于计算 pivot，使旋转/缩放绕握把
    private static readonly Vector2 GripLocalPos = new Vector2(-5f, 25f);

    // 部件布局 —— 与 v5/第二版一致，但部件统一朝 +X（不再镜像）
    private static readonly (string, Vector2, Vector2, Color)[] GunParts =
    {
        ("Barrel", new Vector2(220f, 22f), new Vector2(60f, 40f),  new Color(0.15f, 0.15f, 0.18f)), // 枪管
        ("Body",   new Vector2(120f, 55f), new Vector2(15f, 70f),  new Color(0.22f, 0.22f, 0.26f)), // 枪身
        ("Grip",   new Vector2(40f, 80f),  new Vector2(-5f, 25f),  new Color(0.12f, 0.12f, 0.15f)), // 握把
        ("Stock",  new Vector2(80f, 18f),  new Vector2(-55f, 40f), new Color(0.10f, 0.10f, 0.13f))  // 枪托
    };

    private void Awake()
    {
        BuildWeapon();
    }

    private void Update()
    {
        if (weaponRoot == null) return;
        weaponRoot.gameObject.SetActive(showWeapon);
        if (!showWeapon) return;

        // 屏幕中心（Overlay Canvas 左下角为原点）
        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;

        // 左右手符号：只影响握把水平位置，Y 不受影响
        float side = (handSide == HandSide.Right) ? 1f : -1f;
        Vector2 handScreen = screenCenter + new Vector2(handOffset.x * side, handOffset.y);

        // 鼠标摆动（反向、平滑）
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        Vector2 targetSway = -mouseDelta * swayAmount;
        currentSway = Vector2.Lerp(currentSway, targetSway, swaySmooth * Time.deltaTime);
        float targetSwayRot = Mathf.Clamp(-mouseDelta.x * 0.5f, -6f, 6f);
        currentSwayRot = Mathf.Lerp(currentSwayRot, targetSwayRot, swaySmooth * Time.deltaTime);

        // pivot 恒定（不随左右手变）
        ApplyPivotForGrip();

        // 位置 = 握把屏幕位置 + 摆动
        weaponRoot.anchoredPosition = handScreen + currentSway;

        // 瞄准角 = 握把 -> 屏幕中心 的方向角（部件统一朝右，无需 180° 补偿）
        Vector2 dir = screenCenter - handScreen;
        float aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        weaponRoot.localRotation = Quaternion.Euler(0f, 0f, aimAngle + currentSwayRot);

        // 缩放（绕握把）
        weaponRoot.localScale = Vector3.one * weaponScale;
    }

    /// <summary>
    /// 计算 pivot，使旋转/缩放支点落在"握把"部件上。
    /// pivot 恒定，不随左右手变化（部件不镜像）。
    /// </summary>
    private void ApplyPivotForGrip()
    {
        if (weaponRoot == null) return;

        Vector2 size = weaponRoot.sizeDelta;
        if (size.x <= 0f || size.y <= 0f) return;

        weaponRoot.pivot = new Vector2(
            (GripLocalPos.x + size.x * 0.5f) / size.x,
            (GripLocalPos.y + size.y * 0.5f) / size.y
        );
    }

    private void BuildWeapon()
    {
        // 1. 专属 Canvas
        GameObject canvasGO = new GameObject("WeaponCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // 2. 武器根节点（锚点左下角）
        GameObject rootGO = new GameObject("WeaponRoot");
        rootGO.transform.SetParent(canvas.transform, false);
        weaponRoot = rootGO.AddComponent<RectTransform>();
        weaponRoot.anchorMin = Vector2.zero;
        weaponRoot.anchorMax = Vector2.zero;
        weaponRoot.sizeDelta = new Vector2(400f, 300f);

        // 初始 pivot 设为握把位置（恒定）
        ApplyPivotForGrip();

        // 3. 生成枪身部件（统一朝 +X，不镜像）
        foreach (var part in GunParts)
        {
            CreatePart(part.Item1, part.Item2, part.Item3, part.Item4);
        }

        weaponRoot.gameObject.SetActive(showWeapon);
    }

    private void CreatePart(string name, Vector2 size, Vector2 localPos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(weaponRoot, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = localPos; // 不镜像

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }
}
