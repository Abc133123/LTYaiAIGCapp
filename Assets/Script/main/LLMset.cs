using UnityEngine;
using UnityEngine.UI;

/* ==============================================================================
 * 脚本名称：LLMset (LLM 设置与配置管理器)
 * 
 * 【功能说明】
 * 本脚本负责管理游戏运行时的配置设置，并提供了一个 UI 面板供玩家修改这些设置。
 * 所有的配置都会使用 Unity 的 PlayerPrefs 进行本地持久化存储，下次启动游戏时会自动读取。
 * 
 * 核心功能：
 * 1. 服务提供商切换：
 *    - 允许玩家在“阿里云”和“本地服务器”之间切换。
 * 2. 本地服务器 IP 地址配置：
 *    - 允许输入本地 LLM 和 VITS 服务的 IP 地址。
 *    - 智能格式化（支持只输入最后一段数字，自动补全 192.168.1.x）。
 * 3. VITS 语音模式配置：
 *    - 提供 Toggle 开关，切换“仅朗读前3句”或“全文朗读”模式。
 * 4. 全局静态访问：
 *    - 通过静态属性（SelectedProvider, LocalServerIP, IsVitsAllMode）
 *      向其他脚本（如 ChatMain）提供最新的配置数据。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：创建设置 UI 面板
 * 1. 在 Canvas 下创建一个 Panel，命名为 "SettingsPanel"。
 * 2. 在 Panel 内部创建以下 UI 控件：
 *    - Dropdown (Provider)：用于选择服务商（Options：阿里云, 本地）。
 *    - InputField (IP Address)：用于输入 IP 地址。
 *    - Toggle (VITS Mode)：用于切换朗读模式（开启=全文，关闭=前3句）。
 *    - Button (Confirm)：确认按钮。
 *    - Button (Cancel)：取消按钮。
 * 
 * 第二步：挂载脚本
 * 1. 在 Hierarchy 中创建一个空物体，命名为 "LLMSettings"（或挂在任何常驻物体上）。
 * 2. 将此脚本拖拽上去。
 * 
 * 第三步：绑定 UI 引用
 * 在 Inspector 面板中，将刚才创建的 UI 控件拖入对应的插槽：
 * 1. Settings Panel：拖入 "SettingsPanel"。
 * 2. Provider Dropdown：拖入 Dropdown 组件。
 * 3. Confirm Button：拖入确认按钮。
 * 4. Cancel Button：拖入取消按钮。
 * 5. Ip Input Field：拖入 IP 输入框。
 * 6. Vits Mode Toggle：拖入 VITS 模式开关。
 * 
 * 第四步：调用设置面板
 * 通常你会有一个主界面的“设置”按钮。在该按钮的 OnClick 事件中：
 * 1. 拖入挂载了 LLMset 脚本的物体。
 * 2. 选择函数列表中的：LLMset -> ShowPanel。
 * 
 * 第五步：在其他脚本中读取设置
 * 其他脚本（如 ChatMain）无需引用 LLMset 对象，直接通过类名访问静态属性：
 * 
 * 

public class LLMset : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject settingsPanel;

    [Header("控件")]
    public Dropdown providerDropdown;
    public Button confirmButton;
    public Button cancelButton;
    public InputField ipInputField;

    // 新增：VITS模式控制开关
    [Header("VITS 设置")]
    public Toggle vitsModeToggle;

    // PlayerPrefs 存储键名
    private const string PROVIDER_KEY = "LLM_SelectedProvider";
    private const string IP_ADDRESS_KEY = "LLM_IPAddress";
    private const string VITS_ALL_MODE_KEY = "LLM_VitsAllMode"; // 新增键名

    // 默认值
    private string defaultProvider = "阿里云";
    private string defaultIP = "192.168.1.6";
    private bool defaultVitsAllMode = false; // 默认为false，即默认前3句

    // 静态属性（封装，方便扩展）
    public static string SelectedProvider { get; private set; }
    public static string LocalServerIP { get; private set; }
    public static bool IsVitsAllMode { get; private set; } // 新增静态属性，供外部访问

    void Awake()
    {
        // 读取保存的设置，如果没有就用默认值
        SelectedProvider = PlayerPrefs.GetString(PROVIDER_KEY, defaultProvider);
        LocalServerIP = PlayerPrefs.GetString(IP_ADDRESS_KEY, defaultIP);
        
        // 读取VITS模式设置 (0=false, 1=true)
        IsVitsAllMode = PlayerPrefs.GetInt(VITS_ALL_MODE_KEY, defaultVitsAllMode ? 1 : 0) == 1;
        
        Debug.Log("[LLMset] 已读取保存的设置: " + SelectedProvider);
        Debug.Log("[LLMset] 已读取服务器IP: " + LocalServerIP);
        Debug.Log("[LLMset] VITS模式: " + (IsVitsAllMode ? "全部朗读" : "仅前3句"));
    }

    void Start()
    {
        // 默认隐藏面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 同步 UI：Provider
        if (providerDropdown != null)
        {
            int savedIndex = -1;
            for (int i = 0; i < providerDropdown.options.Count; i++)
            {
                if (providerDropdown.options[i].text == SelectedProvider)
                {
                    savedIndex = i;
                    break;
                }
            }

            if (savedIndex >= 0)
            {
                providerDropdown.value = savedIndex;
            }
            else
            {
                providerDropdown.value = 0;
            }
            providerDropdown.RefreshShownValue();
        }

        // 同步 IP 输入框
        if (ipInputField != null)
        {
            ipInputField.text = LocalServerIP;
        }

        // 同步 VITS Toggle 开关
        if (vitsModeToggle != null)
        {
            vitsModeToggle.isOn = IsVitsAllMode;
        }

        // 绑定按钮事件
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
    }

    public void ShowPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    void OnConfirm()
    {
        if (providerDropdown != null)
        {
            string newText = providerDropdown.options[providerDropdown.value].text;
            SelectedProvider = newText;
            PlayerPrefs.SetString(PROVIDER_KEY, newText);
            PlayerPrefs.Save();
            Debug.Log("[LLMset] 设置已更新并保存: " + SelectedProvider);
        }

        // 处理IP地址设置
        if (ipInputField != null)
        {
            string inputIP = ipInputField.text.Trim();
            string formattedIP = FormatIPAddress(inputIP);
            LocalServerIP = formattedIP;
            PlayerPrefs.SetString(IP_ADDRESS_KEY, formattedIP);
            PlayerPrefs.Save();
            Debug.Log("[LLMset] 服务器IP已更新: " + LocalServerIP);
        }

        // 新增：处理 VITS 模式设置
        if (vitsModeToggle != null)
        {
            IsVitsAllMode = vitsModeToggle.isOn;
            PlayerPrefs.SetInt(VITS_ALL_MODE_KEY, IsVitsAllMode ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("[LLMset] VITS模式已更新: " + (IsVitsAllMode ? "全部朗读" : "仅前3句"));
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void OnCancel()
    {
        // 取消时恢复显示当前保存的IP
        if (ipInputField != null)
        {
            ipInputField.text = LocalServerIP;
        }
        // 恢复显示当前保存的VITS模式
        if (vitsModeToggle != null)
        {
            vitsModeToggle.isOn = IsVitsAllMode;
        }
        
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Debug.Log("[LLMset] 取消设置");
    }

    // 保留原有的 FormatIPAddress 方法
    private string FormatIPAddress(string input)
    {
        if (string.IsNullOrEmpty(input)) return defaultIP;
        string[] parts = input.Split('.');
        if (parts.Length == 4)
        {
            if (parts[0] == "192" && parts[1] == "168")
            {
                return input;
            }
        }
        if (parts.Length == 1)
        {
            return $"192.168.1.{parts[0]}";
        }
        else if (parts.Length == 2)
        {
            return $"192.168.{parts[0]}.{parts[1]}";
        }
        return input;
    }
}
