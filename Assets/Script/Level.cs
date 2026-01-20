using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/* ==============================================================================
 * 脚本名称：Level (关卡 UI 管理与场景跳转控制器)
 * 
 * 【功能说明】
 * 此脚本用于管理主菜单或关卡选择界面的 UI 显示，以及处理场景之间的跳转逻辑。
 * 核心功能包括：
 * 1. UI 面板控制：切换信息面板的显示与隐藏。
 * 2. 异步加载流程：通过点击按钮，跳转到统一的“Loading”加载场景，并传递目标场景名称。
 * 3. 静态数据传递：使用静态变量记录下一个要进入的场景，供 Loading 场景脚本读取。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：场景设置 (Build Settings)
 * 1. 打开 File -> Build Settings。
 * 2. 确保你的场景已经添加到列表中：
 *    - 当前场景 (例如：MainMenu)
 *    - 中转场景 (名称必须填入下方的 loadingSceneName，通常叫 "Loading")
 *    - 目标场景 (名称必须填入 mainSceneName 和 shSceneName)
 * 
 * 第二步：UI 结构准备
 * 在 Canvas 下创建以下 UI 元素：
 * 1. Panel 1 (plane1)：你需要显示/隐藏的信息面板。
 * 2. Panel 2 (plane2)：通常用于 Loading 界面（但在本脚本逻辑中，跳转是直接切场景的，此面板可能用于其他自定义逻辑）。
 * 3. Slider (progressBar)：进度条组件。
 * 4. Buttons：4个按钮 (显示、隐藏、去Main、去Sh)。
 * 
 * 第三步：挂载脚本
 * 1. 在场景中创建一个空物体，命名为 "LevelManager"。
 * 2. 将此脚本拖拽上去。
 * 
 * 第四步：参数配置 (Inspector 面板)
 * 1. UI 面板区域：
 *    - 将场景中的 Panel 对象分别拖入 Plane1 和 Plane2 插槽。
 * 2. 进度条区域：
 *    - 将场景中的 Slider 对象拖入 Progress Bar 插槽。
 * 3. 功能按钮区域：
 *    - 将对应的按钮对象拖入 Btn Show, Btn Hide 等插槽。
 *    - *注意：脚本会自动在 Start 中绑定点击事件，无需在 Inspector 的 OnClick 中手动设置。*
 * 4. 场景配置区域：
 *    - Main Scene Name: 输入主场景的字符串名称（如 "main"）。
 *    - Sh Scene Name: 输入第二个场景的字符串名称（如 "sh"）。
 *    - Loading Scene Name: 输入加载过渡场景的名称（如 "Loading"）。
 * 
 * ============================================================================== */

public class Level : MonoBehaviour
{
    [Header("UI 面板")]
    // [Unity操作] 将场景中用于显示详细信息的 Panel 拖到这个插槽上
    public GameObject plane1; // 默认隐藏的面板
    
    // [Unity操作] 如果有专门的 Loading 进度条面板，拖入此处
    public GameObject plane2; // 加载进度条所在的 Panel

    [Header("进度条")]
    // [Unity操作] 将 Slider 组件拖入此处
    public Slider progressBar; // 进度条组件

    [Header("功能按钮")]
    // [Unity操作] 拖入负责打开 plane1 的按钮
    public Button btnShow;    // 按钮1：显示 plane1
    
    // [Unity操作] 拖入负责关闭 plane1 的按钮
    public Button btnHide;    // 按钮2：隐藏 plane1

    [Header("关卡按钮")]
    // [Unity操作] 拖入负责跳转到主场景的按钮
    public Button btnToMain;  // 关卡按钮：前往 main
    
    // [Unity操作] 拖入负责跳转到 sh 场景的按钮
    public Button btnToSh;    // 关卡按钮：前往 sh

    [Header("场景配置")]
    // [Unity操作] 请在此处准确输入 Build Settings 中的场景名称
    [Tooltip("目标场景1的名称，必须和 Build Settings 里的一致")]
    public string mainSceneName = "main";
    
    [Tooltip("目标场景2的名称，必须和 Build Settings 里的一致")]
    public string shSceneName = "sh";
    
    [Tooltip("加载过渡场景的名称，用于显示进度条")]
    public string loadingSceneName = "Loading"; 

    // --- 核心变量：静态变量 ---
    // 说明：此变量用于在不同场景间传递数据。
    // 当跳转到 Loading 场景后，Loading 场景的脚本会读取这个变量来决定最终加载哪个关卡。
    public static string nextSceneName = ""; 

    void Start()
    {
        // 初始化状态：默认隐藏所有面板
        if (plane1 != null) plane1.SetActive(false);
        if (plane2 != null) plane2.SetActive(false);
        if (btnShow != null) btnShow.gameObject.SetActive(true);

        // --- 自动绑定按钮事件 ---
        // 注意：这里通过代码自动绑定事件，所以你不需要在 Unity Inspector 的 Button -> OnClick 里手动配置。
        
        // 绑定“显示”按钮功能
        if (btnShow != null) btnShow.onClick.AddListener(ShowPlane1);
        
        // 绑定“隐藏”按钮功能
        if (btnHide != null) btnHide.onClick.AddListener(HidePlane1);
        
        // 绑定“跳转”按钮功能，使用 Lambda 表达式传递场景名称
        if (btnToMain != null) btnToMain.onClick.AddListener(() => CheckAndLoad(mainSceneName));
        if (btnToSh != null) btnToSh.onClick.AddListener(() => CheckAndLoad(shSceneName));
    }

    /// <summary>
    /// 显示信息面板 plane1
    /// </summary>
    void ShowPlane1()
    {
        if (plane1 != null) plane1.SetActive(true);
    }

    /// <summary>
    /// 隐藏信息面板 plane1
    /// </summary>
    void HidePlane1()
    {
        if (plane1 != null) plane1.SetActive(false);
    }

    /// <summary>
    /// 检查并执行加载流程
    /// </summary>
    /// <param name="targetSceneName">目标场景的名称字符串</param>
    void CheckAndLoad(string targetSceneName)
    {
        // 获取当前激活的场景名称
        string currentScene = SceneManager.GetActiveScene().name;

        // 如果当前已经在目标场景，则不执行跳转
        if (currentScene == targetSceneName) return;

        // [关键步骤] 将目标场景名存入静态变量，供 Loading 场景读取
        nextSceneName = targetSceneName;

        // 切换到 Loading 过渡场景
        // Loading 场景中应该有另一个脚本负责读取 nextSceneName 并显示进度条加载
        SceneManager.LoadScene(loadingSceneName);
    }
}
