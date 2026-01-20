using UnityEngine;
using UnityEngine.UI;

public class openui : MonoBehaviour //第一次打开的提示面板脚本
{
    //静态变量，存在内存中，不会因为切换场景而销毁
    // 0 = 没看过， 1 = 看过了
    private static int memoryFlag = 0;

    [Header("UI 设置")]
    public GameObject targetPanel; // 你要控制显示/隐藏的那个 UI 面板
    public Button closeButton;     // 点击用来隐藏 UI 的按钮

    void Start()
    {
        // --- 1. 场景加载时读取内存变量 ---
        if (memoryFlag == 0)
        {
            // 第一次：确保 UI 显示
            if (targetPanel != null) targetPanel.SetActive(true);
        }
        // 【修改处】移除了 else 分支
        // 现在如果变量是 1，脚本不会强制修改面板状态
        // 面板是显示还是隐藏，完全取决于你在页面（Inspector/场景）里的默认设置

        // --- 2. 绑定按钮事件 ---
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    // 按钮点击触发的方法
    void OnCloseButtonClicked()
    {
        // --- 3. 按下按钮时的操作 ---
        
        // A. 隐藏 UI
        if (targetPanel != null) targetPanel.SetActive(false);

        // B. 内存变量调整为 1
        memoryFlag = 1;
    }
}
