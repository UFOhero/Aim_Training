using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 准星系统 —— 运行时自动创建准星 UI，无需手动搭建 Canvas。
/// 支持：样式（十字四条臂）、颜色（含透明度）、尺寸（臂长/粗细/间隙，可为负数）、
///       中心点开关、黑色描边（CS2 准星常带轮廓）。
///
/// 用法：把本组件挂到场景中任意物体上（例如 Player），运行后自动生效。
/// Inspector 中可直接输入精确小数，效果即时刷新。
/// </summary>
public class CrosshairManager : MonoBehaviour
{
    [Header("准星样式")]
    [Tooltip("勾选 = 十字四条臂；取消 = 仅中心点")]
    public bool useCrossStyle = true;

    [Header("颜色")]
    [Tooltip("准星主体颜色（含透明度，A 值 0~1）")]
    public Color crosshairColor = new Color(1f, 0f, 0f, 0.78f); // 红 255,0,0 透明度 200/255

    [Header("描边（CS2 轮廓）")]
    [Tooltip("是否启用黑色描边")]
    public bool useOutline = true;
    [Tooltip("描边宽度（像素，方向分量）")]
    public float outlineWidth = 1f;

    [Header("尺寸（像素，可直接输入小数）")]
    [Tooltip("每条臂的长度（像素）")]
    public float armLength = 5f;
    [Tooltip("每条臂的粗细（像素）")]
    public float armThickness = 2f;
    [Tooltip("臂与中心点的间隙（像素，可为负值：负值让臂更贴近/重叠中心点）")]
    public float gap = 0f;

    [Header("中心点")]
    public bool showCenterDot = true;
    [Tooltip("中心点尺寸（像素）")]
    public float centerDotSize = 3f;

    // 运行时组件引用
    private Canvas canvas;
    private RectTransform[] armRects = new RectTransform[4]; // 上右下左
    private RectTransform centerRect;

    private void Awake()
    {
        BuildCrosshair();
    }

    private void OnValidate()
    {
        // 编辑器里调整参数时实时刷新（仅当已经构建过才刷新，避免未初始化报错）
        if (canvas != null)
        {
            ApplySettings();
        }
    }

    private void BuildCrosshair()
    {
        // 1. 创建 Canvas（Screen Space - Overlay，始终显示在最上层）
        GameObject canvasGO = new GameObject("CrosshairCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 保证准星在所有 UI 之上

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. 创建中心点
        centerRect = CreateRect("CenterDot", new Vector2(centerDotSize, centerDotSize), crosshairColor);

        // 3. 创建四条臂
        armRects[0] = CreateRect("Arm_Top", new Vector2(armThickness, armLength), crosshairColor);
        armRects[1] = CreateRect("Arm_Right", new Vector2(armLength, armThickness), crosshairColor);
        armRects[2] = CreateRect("Arm_Bottom", new Vector2(armThickness, armLength), crosshairColor);
        armRects[3] = CreateRect("Arm_Left", new Vector2(armLength, armThickness), crosshairColor);

        // 4. 应用一次设置（定位、颜色、显隐、描边）
        ApplySettings();
    }

    /// <summary>创建单个准星矩形块（父物体为 Canvas），可选加描边</summary>
    private RectTransform CreateRect(string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // 准星不拦截鼠标点击

        // 黑色描边（CS2 风格轮廓）
        if (useOutline)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 1f);
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
            outline.useGraphicAlpha = true;
        }

        return rect;
    }

    /// <summary>应用所有设置（定位、颜色、显隐、描边开关）</summary>
    private void ApplySettings()
    {
        if (centerRect == null || canvas == null) return;

        // 中心点
        centerRect.sizeDelta = new Vector2(centerDotSize, centerDotSize);
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.gameObject.SetActive(showCenterDot);
        centerRect.GetComponent<Image>().color = crosshairColor;

        // 四条臂
        bool showArms = useCrossStyle;
        float halfLen = armLength * 0.5f;
        float halfThick = armThickness * 0.5f;
        // offset = 间隙 + 臂的一半长度，负 gap 会让臂贴近中心甚至重叠
        float offset = gap + halfLen;

        ApplyArm(0, "Arm_Top", new Vector2(armThickness, armLength), new Vector2(0f, offset), showArms);
        ApplyArm(1, "Arm_Right", new Vector2(armLength, armThickness), new Vector2(offset, 0f), showArms);
        ApplyArm(2, "Arm_Bottom", new Vector2(armThickness, armLength), new Vector2(0f, -offset), showArms);
        ApplyArm(3, "Arm_Left", new Vector2(armLength, armThickness), new Vector2(-offset, 0f), showArms);
    }

    /// <summary>应用单条臂的设置</summary>
    private void ApplyArm(int index, string name, Vector2 size, Vector2 pos, bool show)
    {
        RectTransform rect = armRects[index];
        rect.gameObject.name = name;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        rect.gameObject.SetActive(show);

        Image img = rect.GetComponent<Image>();
        img.color = crosshairColor;

        // 同步描边设置（重建时已添加，这里仅刷新宽度/开关）
        Outline outline = rect.GetComponent<Outline>();
        if (useOutline && outline == null)
        {
            outline = rect.gameObject.AddComponent<Outline>();
        }
        if (outline != null)
        {
            outline.enabled = useOutline;
            outline.effectColor = new Color(0f, 0f, 0f, 1f);
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
        }
    }
}
