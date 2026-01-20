using System; 
using System.Text; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* ==============================================================================
 * 脚本名称：ChatMain (聊天系统主控制器)
 * 
 * 【功能说明】
 * 本脚本是整个 AI 对话系统的核心控制器，负责协调各个功能模块：
 * 
 * 1. UI 交互管理：
 *    - 处理用户输入框和发送按钮
 *    - 显示对话历史内容，自动滚动到底部
 *    - 提供清空对话的功能
 * 
 * 2. LLM 对话集成：
 *    - 支持阿里云百炼（DashScope）API
 *    - 支持本地 Python LLM 服务器
 *    - 动态切换服务提供商（从 LLMset 读取配置）
 *    - 管理对话上下文，支持多轮对话
 * 
 * 3. VITS 语音合成：
 *    - 调用 VITS TTS API 将 AI 回复转为语音
 *    - 支持两种 TTS 模式：
 *      • Sentences3：只朗读前 3 句话
 *      • All：完整朗读全部内容
 *    - 动态配置服务器地址（从 LLMset 读取 IP）
 * 
 * 4. 关键词系统：
 *    - 关键词触发动作：检测 AI 回复中的关键词，播放对应动画
 *    - 关键词触发音效：检测关键词，播放预设音频
 *    - 支持暂停背景音乐（优先播放关键词音效）
 * 
 * 5. 音频系统集成：
 *    - 与 RandomMusicPlayer 集成，控制背景音乐播放
 *    - 与 RandomAnimationPlayer 集成，触发特定动作
 *    - TTS 播放时可选择暂停背景音乐
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：创建主控制器对象
 * 1. 在 Hierarchy 面板右键 -> Create Empty
 * 2. 命名为 "ChatManager" 或 "ChatMain"
 * 
 * 第二步：UI 准备
 * 在 Canvas 下创建以下 UI 元素：
 * 1. InputField：用于用户输入文本
 *    - 建议设置为多行模式，高度适中
 * 2. Button：发送按钮
 * 3. Panel（带 ScrollRect）：对话显示区域
 *    - 在 Panel 下创建 Text 组件，用于显示对话内容
 *    - 设置 ScrollRect 的 Content 为该 Panel
 * 4. ScrollRect 组件：
 *    - 确保 Vertical（垂直滚动）已勾选
 *    - Content 绑定到显示对话的 Panel
 * 
 * 第三步：挂载脚本并配置 UI 引用
 * 1. 选中 "ChatManager" 对象
 * 2. 将 ChatMain 脚本拖入
 * 3. 在 Inspector 面板配置：
 *    - Input Field：拖入输入框对象
 *    - Send Button：拖入发送按钮
 *    - Scroll Rect：拖入 ScrollRect 组件所在对象
 *    - Main Chat Text：拖入显示对话的 Text 组件
 * 
 * 第四步：配置关联脚本
 * 1. Random Music Player：
 *    - 确保场景中有 RandomMusicPlayer 脚本运行
 *    - 拖入对应的对象引用
 * 2. Random Animation Player：
 *    - 确保需要执行动作的角色上有 RandomAnimationPlayer 脚本
 *    - 拖入对应的对象引用
 * 
 * 第五步：配置关键词系统
 * 1. Keyword Audio List（关键词音效）：
 *    - 设置 Size 为需要的数量
 *    - 每个 KeywordActionPair：
 *      • Keyword：输入关键词（如"你好"、"谢谢"）
 *      • Audio Clip：拖入对应的音效文件
 * 2. Keyword Action List（关键词动作）：
 *    - 设置 Size 为需要的数量
 *    - 每个 KeywordActionPair：
 *      • Keyword：输入关键词
 *      • Action Clip：拖入对应的动画片段（AnimationClip）
 *      • Prompt Description：输入动作描述（可选，用于发送给模型）
 * 
 * 第六步：配置 LLM 服务
 * 1. LLM Service Type：
 *    - DashScope：使用阿里云百炼 API
 *    - LocalLLM：使用本地 Python 服务器
 * 2. DashScope API Key：
 *    - 输入你的阿里云 API Key（格式：sk-xxxxxxxx）
 * 3. DashScope Model Name：
 *    - 输入模型名称（如：qwen-turbo, qwen-plus, qwen-max）
 * 4. LocalLLM Url：
 *    - 输入本地服务器地址（如：http://192.168.1.6:3412/api/chat）
 *    - 注意：实际使用的 IP 会从 LLMset 动态读取
 * 5. LocalLLM Model Name：
 *    - 输入本地模型名称（如：Qwen1.5-0.5B-Chat）
 * 
 * 第七步：配置 VITS TTS
 * 1. Enable VITS：勾选启用语音合成
 * 2. VITS TTS URL：
 *    - 输入 VITS 服务器地址（如：http://192.168.1.6:3712/）
 *    - 注意：实际使用的 IP 会从 LLMset 动态读取
 * 3. TTS Mode：
 *    - Sentences3：只朗读 AI 回复的前 3 句话（推荐，节省时间）
 *    - All：完整朗读全部内容
 * 4. Pause Music On TTS：
 *    - 勾选后，播放语音时自动暂停背景音乐
 * 
 * 第八步：其他配置
 * 1. Pause Background Music On Keyword：
 *    - 勾选后，触发关键词音效时暂停背景音乐
 * 
 * 第九步：运行测试
 * 1. 点击 Play 按钮
 * 2. 在输入框输入消息，点击发送
 * 3. 观察 AI 回复、语音播放、动作触发是否正常
 * 
 * ----------------------------------------------------------------
 * 【依赖说明】
 * ----------------------------------------------------------------
 * 
 * 本脚本依赖以下脚本/组件，请确保已正确设置：
 * 1. ChatLLMService：LLM 网络请求服务（本脚本会自动添加）
 * 2. ChatManager：聊天逻辑管理器（本脚本会自动添加）
 * 3. ChatUIController：UI 控制器（本脚本会自动添加）
 * 4. LLMset：LLM 配置设置脚本（需单独创建并配置）
 * 5. RandomMusicPlayer：背景音乐播放器
 * 6. RandomAnimationPlayer：动画播放器
 * 
 * ============================================================================== */

// ==================== 数据模型定义 ====================

/// <summary>
/// 聊天消息数据模型
/// 用于构建对话上下文，发送给 LLM 服务
/// </summary>
[System.Serializable]
public class ChatMessage
{
    public string role;    // 角色："user"（用户）或 "assistant"（AI）
    public string content; // 消息内容
}

/// <summary>
/// 关键词-动作映射对
/// 定义当 AI 回复中出现特定关键词时，应该播放的动作
/// </summary>
[System.Serializable]
public class KeywordActionPair
{
    [Tooltip("需要检测的关键词")]
    public string keyword;
    
    [Tooltip("触发的动作动画片段")]
    public AnimationClip actionClip;
    
    [Tooltip("发送给模型的动作描述（用于上下文理解）")]
    public string promptDescription; 
}

/// <summary>
/// 关键词-音效映射对
/// 定义当 AI 回复中出现特定关键词时，应该播放的音效
/// </summary>
[System.Serializable]
public class KeywordAudioPair
{
    [Tooltip("需要检测的关键词")]
    public string keyword;
    
    [Tooltip("触发的音效文件")]
    public AudioClip audioClip;
}

// ==================== 主控制器 ====================
public class ChatMain : MonoBehaviour
{
    [Header("=== UI 引用 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入场景中的 InputField 组件
    // 建议设置为多行输入模式，方便用户输入较长文本
    // ----------------------------------------------------------------
    public InputField inputField;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入发送按钮（UI Button）
    // 点击此按钮将发送输入框中的内容
    // ----------------------------------------------------------------
    public Button sendButton;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入对话显示区域的 ScrollRect 组件
    // ScrollRect 确保对话内容超出显示范围时可以滚动查看
    // ----------------------------------------------------------------
    public ScrollRect scrollRect;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入用于显示所有对话内容的 Text 组件
    // 建议：
    // - 将 Text 放在一个 ScrollRect 的 Content 中
    // - 设置字体大小、行高、对齐方式
    // - 设置为支持富文本（Rich Text），可使用颜色等格式
    // ----------------------------------------------------------------
    [Tooltip("拖入场景中那个用于显示所有对话的 Text 组件")]
    public Text mainChatText;

    [Header("=== 关联脚本组件 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入场景中的 RandomMusicPlayer 脚本所在对象
    // 用于：
    // - 在触发关键词音效时暂停背景音乐
    // - 播放完毕后恢复背景音乐
    // ----------------------------------------------------------------
    public RandomMusicPlayer randomMusicPlayer;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】拖入需要执行动作的角色对象上的 RandomAnimationPlayer 脚本
    // 用于：
    // - 触发关键词动作时播放特定动画
    // - 暂停随机待机动作，播放指定动作，然后恢复
    // ----------------------------------------------------------------
    public RandomAnimationPlayer randomAnimationPlayer;

    [Header("=== 关键词音频设置 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】
    // 1. 设置 Size 为需要的关键词数量
    // 2. 对每个 KeywordAudioPair：
    //    - Keyword：输入要检测的关键词（如"你好"、"谢谢"、"抱歉"等）
    //    - Audio Clip：拖入对应的音效文件
    // 
    // 工作原理：
    // - 当 AI 回复中包含某个关键词时，自动播放对应的音效
    // - 如果勾选了 Pause Background Music On Keyword，会暂停背景音乐
    // ----------------------------------------------------------------
    public KeywordAudioPair[] keywordAudioList;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】勾选此项以在播放关键词音效时暂停背景音乐
    // 这样可以确保音效不会被背景音乐覆盖，提升体验
    // ----------------------------------------------------------------
    public bool pauseBackgroundMusicOnKeyword = true;

    [Header("=== 动作关键词设置 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】
    // 1. 设置 Size 为需要的关键词数量
    // 2. 对每个 KeywordActionPair：
    //    - Keyword：输入要检测的关键词（如"挥手"、"鞠躬"、"跳跃"等）
    //    - Action Clip：拖入对应的动画片段（AnimationClip）
    //    - Prompt Description：输入动作描述（可选）
    //      此描述可以发送给模型，帮助模型理解什么时候会触发动作
    // 
    // 工作原理：
    // - 当 AI 回复中包含某个关键词时，自动播放对应的动画
    // - 动画使用 RandomAnimationPlayer 的 PlaySpecificAnimation 方法
    // - 播放完毕后自动恢复随机待机循环
    // ----------------------------------------------------------------
    public KeywordActionPair[] keywordActionList;

    [Header("=== LLM 配置 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】选择 LLM 服务类型
    // - DashScope：使用阿里云百炼 API（需要网络连接和 API Key）
    // - LocalLLM：使用本地 Python 服务器（需要本地运行 Python 脚本）
    // 
    // 注意：实际选择的服务商还会从 LLMset 动态读取
    // ----------------------------------------------------------------
    [Tooltip("LLM服务类型：DashScope=阿里云百炼，LocalLLM=本地Python服务器")]
    public ChatLLMService.LLMServiceType llmServiceType = ChatLLMService.LLMServiceType.DashScope;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】输入你的阿里云 DashScope API Key
    // 获取方式：
    // 1. 登录阿里云控制台：https://dashscope.console.aliyun.com/
    // 2. 开通百炼服务
    // 3. 在 API-KEY 管理页面创建并复制 API Key
    // 格式示例：sk-xxxxxxxxxxxxxxxxxxxxxxxx
    // ----------------------------------------------------------------
    [Tooltip("阿里云DashScope API Key")]
    public string dashScopeApiKey = "sk-xxxxxxxxxxxxxxxxxxxxxxxx";
    
    // ----------------------------------------------------------------
    // 【Unity 操作】输入 DashScope 模型名称
    // 常用模型：
    // - qwen-turbo：快速响应，适合简单对话
    // - qwen-plus：平衡性能和质量
    // - qwen-max：最高质量，但响应较慢
    // - qwen-vl-plus：支持多模态（图片+文字）
    // ----------------------------------------------------------------
    [Tooltip("DashScope模型名称")]
    public string dashScopeModelName = "qwen-turbo";
    
    // ----------------------------------------------------------------
    // 【Unity 操作】输入本地 LLM 服务器的地址
    // 默认地址：http://192.168.1.6:3412/api/chat
    // 
    // 注意：
    // 1. IP 地址会从 LLMset 动态读取，这里只是默认值
    // 2. 端口号需与你的 Python 服务器配置一致（如 3412）
    // 3. 确保 Python 服务器已启动并监听该端口
    // ----------------------------------------------------------------
    [Tooltip("本地LLM服务器地址")]
    public string localLLMUrl = "http://192.168.1.6:3412/api/chat";
    
    // ----------------------------------------------------------------
    // 【Unity 操作】输入本地 LLM 模型的名称
    // 
    // 常见模型示例：
    // - Qwen1.5-0.5B-Chat
    // - Qwen1.5-1.8B-Chat
    // - Qwen1.5-7B-Chat
    // 
    // 需要与你的 Python 服务器加载的模型名称一致
    // ----------------------------------------------------------------
    [Tooltip("本地LLM模型名称")]
    public string localLLMModelName = "Qwen1.5-0.5B-Chat";

    [Header("=== VITS TTS 配置 ===")]
    
    // ----------------------------------------------------------------
    // 【Unity 操作】勾选此项以启用 VITS 语音合成
    // 如果禁用，AI 回复将只以文字形式显示，不会语音播放
    // ----------------------------------------------------------------
    [Tooltip("启用VITS语音合成")]
    public bool enableVITS = true;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】输入 VITS TTS 服务器的地址
    // 默认地址：http://192.168.1.6:3712/
    // 
    // 注意：
    // 1. IP 地址会从 LLMset 动态读取，这里只是默认值
    // 2. 确保 VITS 服务器已启动并监听该端口
    // 3. VITS 服务器需要返回 WAV 格式的音频数据
    // ----------------------------------------------------------------
    [Tooltip("VITS服务器地址")]
    public string vitsTTSUrl = "http://192.168.1.6:3712/";
    
    // ----------------------------------------------------------------
    // 【Unity 操作】选择 TTS 播放模式
    // - Sentences3：只朗读 AI 回复的前 3 句话
    //   • 节省时间，适合快速对话
    //   • 自动在每句话后添加句号
    // - All：完整朗读 AI 回复的全部内容
    //   • 适合需要完整传达信息的场景
    //   • 可能播放时间较长
    // 
    // 实际模式还会从 LLMset 的 IsVitsAllMode 动态读取
    // ----------------------------------------------------------------
    [Tooltip("TTS模式：Sentences3=只念3句话，All=全部")]
    public TTSMode ttsMode = TTSMode.Sentences3;
    
    // ----------------------------------------------------------------
    // 【Unity 操作】勾选此项以在播放 VITS 语音时暂停背景音乐
    // 这样可以确保语音清晰可听，不会被背景音乐干扰
    // ----------------------------------------------------------------
    [Tooltip("播放VITS音频时暂停背景音乐")]
    public bool pauseMusicOnTTS = true;

    [Header("=== 其他配置 ===")]
    
    // 内部常量：LLM 请求超时时间（秒）
    private const int REQUEST_TIMEOUT = 30;
    
    // 内部常量：最大对话轮数
    // 超过此轮数后，会自动清空历史，开始新的对话
    private const int MAX_CONVERSATION_ROUNDS = 7;

    // ==================== 内部模块引用 ====================
    private ChatLLMService llmService;        // LLM 网络服务
    private ChatManager chatManager;           // 聊天逻辑管理器
    private ChatUIController uiController;     // UI 控制器
    private AudioSource ttsAudioSource;        // TTS 音频播放器

    // ==================== TTS模式枚举 ====================
    /// <summary>
    /// TTS 播放模式枚举
    /// </summary>
    public enum TTSMode
    {
        Sentences3,    // 只念前 3 句话
        All            // 全部朗读
    }

    /// <summary>
    /// 系统初始化
    /// </summary>
    void Start()
    {
        Debug.Log("========== [ChatMain] 系统初始化 ==========");

        // 1. 初始化各个功能模块（动态添加组件）
        llmService = gameObject.AddComponent<ChatLLMService>();
        chatManager = gameObject.AddComponent<ChatManager>();
        uiController = gameObject.AddComponent<ChatUIController>();
        ttsAudioSource = gameObject.AddComponent<AudioSource>();

        // 2. 配置 UI 模块
        if (mainChatText != null && scrollRect != null)
        {
            uiController.Init(mainChatText, scrollRect);
        }
        else
        {
            Debug.LogError("[ChatMain] UI 引用未设置！");
        }

        // 3. 配置聊天逻辑模块
        chatManager.Init(
            keywordActionList,           // 关键词-动作映射
            keywordAudioList,            // 关键词-音效映射
            randomMusicPlayer,           // 背景音乐播放器
            randomAnimationPlayer,       // 动画播放器
            MAX_CONVERSATION_ROUNDS,     // 最大对话轮数
            pauseBackgroundMusicOnKeyword, // 关键词音效时暂停背景音乐
            enableVITS,                  // 是否启用 VITS
            ttsAudioSource,              // TTS 音频源
            pauseMusicOnTTS              // TTS 播放时暂停背景音乐
        );

        // 4. 初始化对话上下文
        chatManager.StartNewConversation();

        // 5. 绑定 UI 事件
        sendButton.onClick.AddListener(OnSendClick);                    // 发送按钮点击
        inputField.onEndEdit.AddListener(s => OnSendClick());           // 输入框回车发送
        
        // 6. 订阅消息事件
        chatManager.OnNewMessage += OnNewMessage;                       // 新消息显示
        chatManager.OnNewConversation += () => uiController.ClearChat(); // 新对话清空
        
        Debug.Log($"[ChatMain] LLM服务: {llmServiceType}");
        Debug.Log($"[ChatMain] VITS TTS: {(enableVITS ? "启用" : "禁用")}");
        Debug.Log($"[ChatMain] 本地服务器IP: {LLMset.LocalServerIP}");
    }

    /// <summary>
    /// 发送按钮点击事件处理
    /// 获取输入框内容，清空输入框，并发送至聊天管理器
    /// </summary>
    void OnSendClick()
    {
        if (!this.isActiveAndEnabled) return;

        // 获取用户输入
        string userText = inputField.text;
        if (string.IsNullOrWhiteSpace(userText)) return;

        // 清空输入框
        inputField.text = "";

        // 交给 ChatManager 处理用户输入
        // 返回值表示是否需要调用 LLM（某些关键词可能不需要）
        bool shouldCallLLM = chatManager.ProcessUserInput(userText);

        if (shouldCallLLM)
        {
            StartCoroutine(SendLLMRequestRoutine());
        }
    }

    /// <summary>
    /// 新消息事件处理
    /// 当聊天管理器产生新消息时调用
    /// </summary>
    /// <param name="sender">发送者标识："User"、"AI"、"System" 等</param>
    /// <param name="text">消息内容</param>
    private void OnNewMessage(string sender, string text)
    {
        // 在 UI 上显示消息
        uiController.AppendMessage(sender, text);

        // 如果是 AI 回复且启用了 VITS，调用 TTS
        if (sender == "AI" && enableVITS)
        {
            StartCoroutine(PlayVITSRoutine(text));
        }
    }

    /// <summary>
    /// 发送 LLM 请求协程
    /// 从 ChatManager 获取对话历史，发送到 LLM 服务
    /// 支持动态切换服务商（从 LLMset 读取）
    /// </summary>
    private IEnumerator SendLLMRequestRoutine()
    {
        // 获取对话历史
        List<ChatMessage> history = chatManager.GetConversationHistory();

        // 获取当前选中的文本（从 LLMset 动态读取）
        string selectedProvider = LLMset.SelectedProvider;
        
        // 获取动态配置的服务器IP（从 LLMset 动态读取）
        string serverIP = LLMset.LocalServerIP;
        
        // 判断服务类型
        ChatLLMService.LLMServiceType serviceType;
        
        // 根据 LLMset 中的提供商选择动态决定服务类型
        if (selectedProvider.Contains("本地") || selectedProvider.ToLower().Contains("local"))
        {
            serviceType = ChatLLMService.LLMServiceType.LocalLLM;
        }
        else
        {
            serviceType = ChatLLMService.LLMServiceType.DashScope;
        }

        // 准备 API 参数
        string apiKey = serviceType == ChatLLMService.LLMServiceType.DashScope ? dashScopeApiKey : "";
        string model = serviceType == ChatLLMService.LLMServiceType.DashScope ? dashScopeModelName : localLLMModelName;
        
        // 动态构建本地LLM URL（使用从 LLMset 读取的 IP）
        string dynamicLocalLLMUrl = $"http://{serverIP}:3412/api/chat";

        Debug.Log($"[ChatMain] 识别服务商: [{selectedProvider}] -> 类型: {serviceType}");
        Debug.Log($"[ChatMain] 本地LLM地址: {dynamicLocalLLMUrl}");

        // 发送请求
        yield return llmService.SendRequestToLLM(
            serviceType,
            apiKey, 
            model, 
            dynamicLocalLLMUrl,  // 使用动态URL
            history, 
            REQUEST_TIMEOUT,
            onSuccess: (aiResponse) => {
                // 成功回调：交给 ChatManager 处理 AI 回复
                chatManager.ProcessAIResponse(aiResponse);
            },
            onError: (error) => {
                // 失败回调：在 UI 上显示错误信息
                uiController.AppendMessage("System", "❌ 连接失败: " + error);
            }
        );
    }

    /// <summary>
    /// VITS 语音播放协程
    /// 将 AI 回复的文本转换为语音并播放
    /// 支持动态 TTS 模式（从 LLMset 的 IsVitsAllMode 读取）
    /// </summary>
    /// <param name="text">AI 回复的文本内容</param>
    private IEnumerator PlayVITSRoutine(string text)
    {
        // === 动态从 LLMset 读取当前用户选择的模式 ===
        // 如果 IsVitsAllMode 为 true，使用 All 模式（全部朗读）
        // 否则使用默认的 Sentences3 模式（只读3句）
        ttsMode = LLMset.IsVitsAllMode ? TTSMode.All : TTSMode.Sentences3;
        
        // 根据 TTS 模式截取文本
        string textToSpeak = PrepareTextForTTS(text);
        
        if (string.IsNullOrWhiteSpace(textToSpeak))
        {
            Debug.LogWarning("[ChatMain] 没有文本需要朗读");
            yield break;
        }

        Debug.Log($"[ChatMain] VITS TTS ({(ttsMode == TTSMode.All ? "全部" : "前3句")}): {textToSpeak}");

        // 使用动态 IP 构建 VITS URL（从 LLMset 读取）
        string serverIP = LLMset.LocalServerIP;
        string dynamicVITSUrl = $"http://{serverIP}:3712/";

        // 调用 VITS API
        yield return llmService.CallVITSAPI(
            textToSpeak,
            dynamicVITSUrl,
            onSuccess: (clip) => {
                // 成功回调：交给 ChatManager 播放音频
                chatManager.PlayVITSAudio(clip);
            },
            onError: (error) => {
                Debug.LogError("[ChatMain] VITS错误: " + error);
            }
        );
    }

    /// <summary>
    /// 根据 TTS 模式准备要朗读的文本
    /// 
    /// Sentences3 模式：
    /// - 将文本按句号、问号、感叹号分割
    /// - 只取前 3 句
    /// - 自动补充句号
    /// 
    /// All 模式：
    /// - 直接返回原文本
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>处理后的文本</returns>
    private string PrepareTextForTTS(string text)
    {
        // All 模式：直接返回原文本
        if (ttsMode == TTSMode.All)
        {
            return text;
        }

        // Sentences3 模式：只念前 3 句
        // 按中文和英文标点符号分割句子
        string[] sentences = text.Split(new[] { '。', '！', '？', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        // 取最多 3 句
        int maxSentences = Mathf.Min(sentences.Length, 3);
        StringBuilder sb = new StringBuilder();
        
        // 重组句子，并补充句号
        for (int i = 0; i < maxSentences; i++)
        {
            sb.Append(sentences[i].Trim());
            if (i < maxSentences - 1) sb.Append("。");
        }
        
        return sb.ToString();
    }
}
