using UnityEngine;

/* ==============================================================================
 * 功能说明(通用拖拽文本功能)
 * 此脚本用于实现 UI 元素（如长文本、列表、面板）的垂直拖拽滚动效果。
 * 兼容 PC 鼠标拖拽和手机触摸滑动。
 * 包含移动速度调节和边界限制功能，防止 UI 被拖出屏幕外。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：准备 UI 对象
 * 1. 在 Canvas 下创建一个 Panel 或 Text（确保该物体有 RectTransform 组件）。
 * 2. 确保该物体的大小合适，且内容超出显示范围（这样才能看到拖拽效果）。
 * 
 * 第二步：挂载脚本
 * 1. 选中你需要拖拽的那个 UI 物体。
 * 2. 在 Inspector 面板点击 "Add Component"。
 * 3. 搜索 "gdui" 并添加此脚本。
 *    *注意：脚本会自动要求 RectTransform 组件，如果是 UI 对象则无需担心。*
 * 
 * 第三步：配置参数
 * 在 Inspector 面板中找到 "Gdui (Script)" 区域进行调整：
 * 
 * 1. Drag Speed (拖拽速度)
 *    - 默认为 1.0。
 *    - 值越大，手指动一点点，文本就跑得越快（灵敏度高）。
 *    - 值越小，拖拽手感越沉重。
 * 
 * 2. Clamp Position (开启边界限制)
 *    - 勾选此项后，文本将被限制在可移动范围内，无法无限拖飞。
 * 
 * 3. Max Y / Min Y (边界范围)
 *    - 【重要】这两个值是相对于“初始位置”的偏移量。
 *    - Max Y: 允许向上拖动的最大距离（正值）。
 *      例如设为 500，表示最多只能从初始位置向上跑 500 像素。
 *    - Min Y: 允许向下拖动的最大距离（负值）。
 *      例如设为 -500，表示最多只能从初始位置向下跑 500 像素。
 *    - 调试技巧：运行游戏，观察文本拖动到哪里合适，然后根据 Inspector 显示的 Y 轴坐标反推这两个值。
 * 
 * 第四步：测试
 * 1. 点击 Unity 上方的 Play 按钮。
 * 2. 用鼠标按住 UI 对象上下拖动，或者使用手机模拟器触摸测试。
 * 
 * ============================================================================== */

// [RequireComponent] 强制要求挂载此脚本的对象必须有 RectTransform 组件
// 这是 Unity UI 的基础组件，确保脚本只能作用于 UI 元素
[RequireComponent(typeof(RectTransform))]
public class gdui : MonoBehaviour
{
    [Header("基础设置")]
    // ----------------------------------------------------------------
    // 【Unity 操作】在 Inspector 面板直接输入数值
    // ----------------------------------------------------------------
    [Tooltip("文本移动的速度，1 表示手指动多少文本就动多少")]
    public float dragSpeed = 1.0f;

    [Header("边界限制（可选）")]
    // ----------------------------------------------------------------
    // 【Unity 操作】勾选此框以开启边界检测
    // ----------------------------------------------------------------
    [Tooltip("是否限制文本滑动的范围")]
    public bool clampPosition = true;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】设定允许滑动的上限（单位：像素）
    // ----------------------------------------------------------------
    [Tooltip("文本允许滑动的最大高度（相对于初始位置）")]
    public float maxY = 500f;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】设定允许滑动的下限（单位：像素）
    // ----------------------------------------------------------------
    [Tooltip("文本允许滑动的最小高度（相对于初始位置）")]
    public float minY = -500f;

    // 记录上一帧鼠标/手指的位置，用于计算移动距离
    private Vector2 lastPointerPos;
    // 标记当前是否处于按下并拖拽的状态
    private bool isDragging = false;
    // 记录游戏开始时 UI 的原始位置，作为边界计算的基准点
    private Vector2 initialPosition;

    // 缓存 RectTransform 组件引用，避免每帧都去获取，提高性能
    private RectTransform rectTransform;

    void Awake()
    {
        // 获取当前物体上的 RectTransform 组件
        rectTransform = GetComponent<RectTransform>();
        // 保存物体刚创建时的位置，确保 maxY 和 minY 是相对于这个起点的
        initialPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // 每一帧都检测输入状态
        HandleInput();
    }

    // 处理输入逻辑（兼容 PC 鼠标和手机触摸）
    void HandleInput()
    {
        // --- 1. 检测按下 (鼠标左键 或 第一根手指接触屏幕) ---
        if (Input.GetMouseButtonDown(0)) 
        {
            isDragging = true;
            // 记录按下时的屏幕坐标位置
            lastPointerPos = Input.mousePosition;
        }

        // --- 2. 检测抬起 (鼠标左键松开 或 手指离开屏幕) ---
        if (Input.GetMouseButtonUp(0)) 
        {
            isDragging = false;
        }

        // --- 3. 检测滑动并移动 UI ---
        if (isDragging)
        {
            // 获取当前帧的屏幕坐标位置
            Vector2 currentPointerPos = Input.mousePosition;

            // 计算这一帧鼠标/手指在 Y 轴上移动了多少距离
            // currentPointerPos.y: 当前位置
            // lastPointerPos.y: 上一帧位置
            float deltaY = currentPointerPos.y - lastPointerPos.y;

            // 应用移动速度倍率
            deltaY *= dragSpeed;

            // 准备更新 UI 位置
            Vector2 newPosition = rectTransform.anchoredPosition;
            // 将计算出的 Y 轴增量叠加到当前位置上
            newPosition.y += deltaY;

            // --- 边界限制逻辑 ---
            if (clampPosition)
            {
                // Mathf.Clamp 用于将数值限制在指定范围内
                // 最小值 = 初始位置 + minY (比如 0 + -500 = -500)
                // 最大值 = 初始位置 + maxY (比如 0 + 500 = 500)
                newPosition.y = Mathf.Clamp(newPosition.y, initialPosition.y + minY, initialPosition.y + maxY);
            }

            // 将计算好的新位置赋值给 UI 组件
            rectTransform.anchoredPosition = newPosition;

            // 【重要】更新上一帧位置，否则下一帧计算会出错
            lastPointerPos = currentPointerPos;
        }
    }
    
    // ---------------------------------------------------------------
    // 扩展说明：
    // 如果你想在手机上用更复杂的多点触控（例如防止两根手指操作时的冲突），
    // 可以使用 Input.touches 来替代 Input.GetMouseButtonX。
    // 上面的 HandleInput 使用统一的 Input 接口，对于大多数单指拖拽场景已经足够通用。
    // ---------------------------------------------------------------
}
