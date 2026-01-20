using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/* ==============================================================================
 * 脚本名称：ChatUIController (聊天界面显示控制器)
 * 
 * 【功能说明】
 * 本脚本专门负责处理聊天界面的 UI 显示逻辑。它将纯文本数据转换为可视化的
 * 聊天记录，并处理布局和滚动相关的细节。
 * 
 * 核心功能：
 * 1. 消息显示：
 *    - 区分发送者（用户/AI/系统），并显示不同的颜色。
 *    - 支持富文本格式。
 * 2. 布局自适应：
 *    - 动态计算文本高度，自动调整 ScrollRect 的 Content 大小。
 *    - 配置 Text 组件的 RectTransform，使其从左上角向下扩展。
 * 3. 自动滚动：
 *    - 当有新消息到来时，自动将滚动视图滚动到底部，确保用户看到最新内容。
 * 4. 文本格式化：
 *    - 简单的换行处理（防止单行过长）。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 注意：本脚本通常由 ChatMain.cs 自动添加并初始化，你通常不需要手动挂载。
 * 如果需要单独测试或手动配置，请按以下步骤操作：
 * 
 * 第一步：UI 结构准备
 * 1. 在 Canvas 下创建一个 ScrollRect。
 *    - 右键 -> UI -> Scroll View。
 *    - 删除 Scroll View 内部的 Scrollbar（如果只需要垂直滚动）。
 * 2. 配置 ScrollRect：
 *    - Content 属性：指向 Scroll View 内部的 "Content" 物体。
 *    - Horizontal：取消勾选（关闭水平滚动）。
 *    - Vertical：勾选（开启垂直滚动）。
 * 3. 配置 Content 物体：
 *    - 选中 Content 物体。
 *    - 移除默认的 Image 组件（可选，只是为了透明）。
 *    - 添加一个 Text 组件（如果还没有）。
 *    - **关键设置**（脚本会自动设置这些，但理解它们有助于调试）：
 *      - Rect Transform：
 *        - Anchor Presets: 按住 Shift + Alt 点击左上角（拉伸填满，轴心在左上）。
 *      - Text 组件：
 *        - Alignment: 左上角。
 *        - Horizontal Overflow: Wrap。
 *        - Vertical Overflow: Overflow。
 *        - Rich Text: 勾选（必须勾选，否则颜色代码会显示为文本）。
 * 
 * 第二步：初始化脚本
 * 本脚本不依赖 Inspector 面板的公共变量，而是通过代码初始化。
 * 你需要在其他脚本（如 ChatMain）中调用 Init 方法：
 * 
 * uiController.Init(mainChatTextComponent, scrollRectComponent);
 * 
 * ============================================================================== */

// ==================== UI 显示控制器 ====================
public class ChatUIController : MonoBehaviour
{
    // 私有变量：存储主要的文本组件引用
    private Text mainChatText;
    
    // 私有变量：存储滚动视图组件引用
    private ScrollRect scrollRect;

    /// <summary>
    /// 初始化控制器
    /// 
    /// 【Unity 操作】通常在 ChatMain 的 Start() 方法中调用此方法进行配置
    /// </summary>
    /// <param name="textRef">Scroll Rect Content 上的 Text 组件</param>
    /// <param name="scrollRef">Scroll Rect 组件本身</param>
    public void Init(Text textRef, ScrollRect scrollRef)
    {
        mainChatText = textRef;
        scrollRect = scrollRef;

        // 强制设置 Text 的 RectTransform 属性，确保布局从左上角开始，高度随内容自动增加
        RectTransform textRect = mainChatText.GetComponent<RectTransform>();
        
        // 设置锚点：左上角 (0,1)
        // 浮点数后添加 'f'
        textRect.anchorMin = new Vector2(0f, 1f);
        
        // 设置锚点：右上角 (1,1)
        textRect.anchorMax = new Vector2(1f, 1f);
        
        // 设置轴心：顶部中间 (0.5, 1)
        // 这样当我们增加 height 时，它会向下增长，而不是向上下两端延伸
        textRect.pivot = new Vector2(0.5f, 1f);
        
        // 初始高度设为 0
        textRect.sizeDelta = new Vector2(0f, 0f);

        // 设置文本溢出模式
        mainChatText.horizontalOverflow = HorizontalWrapMode.Wrap; // 水平自动换行
        mainChatText.verticalOverflow = VerticalWrapMode.Overflow;  // 垂直允许溢出（高度由外部控制）
        mainChatText.alignment = TextAnchor.UpperLeft;             // 左上角对齐
        
        // 开启富文本支持（必须开启，否则颜色标签 <color> 会直接显示出来）
        mainChatText.supportRichText = true; 
    }

    /// <summary>
    /// 清空聊天记录
    /// </summary>
    public void ClearChat()
    {
        if (mainChatText != null) mainChatText.text = "";
    }

    /// <summary>
    /// 追加一条新消息到聊天框
    /// </summary>
    /// <param name="sender">发送者标识 ("User", "AI", "System")</param>
    /// <param name="text">消息内容</param>
    public void AppendMessage(string sender, string text)
    {
        if (mainChatText == null) return;

        // 1. 格式化正文内容
        string formattedBody = FormatText(text);
        
        // 2. 获取发送者的前缀（带颜色）
        string prefix = GetSenderPrefix(sender);

        // 3. 拼接并显示
        mainChatText.text += prefix + formattedBody + "\n\n";

        // 4. 更新滚动区域的高度，并滚动到底部
        if (scrollRect != null && scrollRect.content != null)
        {
            UpdateContentHeight();
            StartCoroutine(ScrollToBottom());
        }
    }

    /// <summary>
    /// 根据发送者类型返回带颜色的前缀字符串
    /// </summary>
    private string GetSenderPrefix(string sender)
    {
        switch (sender)
        {
            case "User": 
                // 蓝色表示用户
                return "<color=#3299ff>【我】：</color> ";
            case "AI": 
                // 亮蓝色表示 AI（洛天依）
                // 注意：确保 Text 组件开启了 Rich Text
                return "<color=#66CCFF>【洛天依】：</color> "; 
            case "System": 
                // 黄色表示系统消息
                return "<color=#ffcc00>【系统】：</color> ";
            default: 
                return "";
        }
    }

    /// <summary>
    /// 格式化文本内容
    /// 简单的换行处理，防止长文本不换行撑破布局
    /// </summary>
    private string FormatText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string result = "";
        for (int i = 0; i < text.Length; i++)
        {
            result += text[i];
            // 每 24 个字符强制换行一次（根据实际 UI 宽度可调整）
            if ((i + 1) % 24 == 0 && i != text.Length - 1) result += "\n";
        }
        return result;
    }

    /// <summary>
    /// 更新 Content 的高度以适配文本内容
    /// 
    /// 【原理】
    /// Text 组件有一个 preferredHeight 属性，表示渲染该文本所需的总高度。
    /// 我们获取这个高度，并赋值给 ScrollRect 的 Content 的 RectTransform height (sizeDelta.y)。
    /// 这样滚动条就知道可以滚多远。
    /// </summary>
    private void UpdateContentHeight()
    {
        float requiredHeight = mainChatText.preferredHeight;
        RectTransform contentRect = scrollRect.content;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    /// <summary>
    /// 将滚动视图滚动到最底部
    /// 使用协程是为了确保在 Unity 渲染完新文本布局后再执行滚动
    /// </summary>
    private IEnumerator ScrollToBottom()
    {
        // 等待这一帧结束，确保 Layout 系统已经计算出新的高度
        yield return new WaitForEndOfFrame();
        
        if (scrollRect != null)
        {
            // verticalNormalizedPosition = 0 表示滚动到底部
            // verticalNormalizedPosition = 1 表示滚动到顶部
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
