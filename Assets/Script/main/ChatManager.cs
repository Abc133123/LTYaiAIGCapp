using UnityEngine;
using System.Collections.Generic;
using System;

/* ==============================================================================
 * 脚本名称：ChatManager (聊天业务逻辑管理器)
 * 
 * 【功能说明】
 * 本脚本是聊天系统的''中转服务器''，负责处理所有的业务逻辑和状态管理，但不直接处理
 * 网络请求（由 ChatMain 和 ChatLLMService 负责）。它的核心职责包括：
 * 
 * 1. 对话历史管理：
 *    - 维护用户的对话上下文（List<ChatMessage>）
 *    - 检测对话轮数，超过限制自动重置，防止Token溢出或上下文过长
 *    - 在新对话开始时注入 System Prompt（设定 AI 人设）
 * 
 * 2. 关键词交互系统：
 *    - 动作关键词：检测用户输入，触发指定的动画（如“挥手”），并通知 AI
 *    - 音效关键词：检测用户输入，播放指定的音效（如“谢谢”）
 *    - 音乐控制指令：识别“暂停”、“播放”等指令控制背景音乐
 * 
 * 3. 音频播放优先级管理：
 *    - 管理背景音乐 (BGM)、关键词音效、VITS 语音三者之间的互斥关系
 *    - 逻辑优先级：VITS 语音 > 关键词音效 > 背景音乐
 *    - 确保 VITS 播放时自动暂停 BGM，播放完自动恢复
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 注意：本脚本通常由 `ChatMain.cs` 自动添加和管理，你一般不需要手动挂载。
 * 但如果你需要单独测试或使用它，请按以下步骤操作：
 * 
 * 第一步：挂载脚本
 * 1. 选中 Hierarchy 中的管理物体（通常是 ChatMain 所在的物体）。
 * 2. Add Component -> 搜索 "ChatManager"。
 * 
 * 第二步：配置引用（通过代码初始化）
 * 此脚本不使用 Inspector 面板直接配置参数，而是通过 public `Init()` 方法进行初始化。
 * 你必须编写代码或在 Inspector 中调用 Init 方法，传入以下参数：
 * 
 * 1. KeywordActionPair[] actions：动作关键词配置数组。
 * 2. KeywordAudioPair[] audios：音效关键词配置数组。
 * 3. RandomMusicPlayer music：背景音乐播放器脚本引用。
 * 4. RandomAnimationPlayer anim：动画播放器脚本引用。
 * 5. int rounds：最大对话轮数（例如 7 轮）。
 * 6. bool pauseMusic：是否在播放音效时暂停音乐。
 * 7. AudioSource ttsSource：用于播放 VITS 语音的 AudioSource 组件。
 * 8. bool pauseMusicOnTTS：是否在播放 VITS 语音时暂停音乐。
 * 
 * 第三步：调用流程
 * 1. 游戏开始时，调用 `StartNewConversation()` 初始化上下文。
 * 2. 用户发送消息时，调用 `ProcessUserInput(inputText)`。
 *    - 返回 true：表示应该请求 LLM（ChatMain 会处理）。
 *    - 返回 false：表示不应该请求（目前逻辑总是返回 true）。
 * 3. 收到 AI 回复时，调用 `ProcessAIResponse(responseText)`。
 * 4. 如果需要播放语音，调用 `PlayVITSAudio(clip)`。
 * 
 * ============================================================================== */

// ==================== 聊天业务逻辑管理器 ====================
public class ChatManager : MonoBehaviour
{
    // ==================== 配置数据（由 ChatMain 传入） ====================
    
    private KeywordActionPair[] keywordActionList; // 动作关键词映射表
    private KeywordAudioPair[] keywordAudioList;   // 音效关键词映射表
    private RandomMusicPlayer randomMusicPlayer;   // 背景音乐控制
    private RandomAnimationPlayer randomAnimationPlayer; // 动作控制
    
    private int maxRounds;          // 最大对话轮数限制
    private bool pauseMusicOnKeyword; // 播放关键词音效时是否暂停 BGM
    
    // VITS TTS 配置
    private bool enableVITS;        // 是否启用语音合成
    private AudioSource ttsAudioSource; // 专门用于播放 AI 语音的音频源
    private bool pauseMusicOnTTS;   // 播放 AI 语音时是否暂停 BGM

    // ==================== 状态数据 ====================
    
    private List<ChatMessage> conversationHistory; // 对话历史记录（发送给 LLM 的内容）
    private AudioSource keywordAudioSource;        // 专门用于播放关键词音效的音频源
    private bool isPlayingKeywordAudio = false;    // 标记：是否正在播放关键词音效
    private bool isPlayingVITSAudio = false;       // 标记：是否正在播放 AI 语音

    // ==================== 事件定义 ====================
    
    /// <summary>
    /// 新消息事件：当有新消息（用户或AI）添加到历史时触发
    /// 参数：(sender, content) -> 发送者, 消息内容
    /// </summary>
    public event Action<string, string> OnNewMessage;
    
    /// <summary>
    /// 新对话事件：当开启一个新的对话会话时触发
    /// </summary>
    public event Action OnNewConversation;

    /// <summary>
    /// 初始化管理器
    /// 
    /// 【Unity 操作】通常在 ChatMain 的 Start() 方法中调用此方法进行配置
    /// </summary>
    public void Init(
        KeywordActionPair[] actions,   // 动作关键词列表
        KeywordAudioPair[] audios,     // 音效关键词列表
        RandomMusicPlayer music,       // BGM 播放器引用
        RandomAnimationPlayer anim,     // 动画播放器引用
        int rounds,                     // 最大对话轮数
        bool pauseMusic,                // 关键词音效暂停 BGM 开关
        bool enableTTS,                 // VITS 启用开关
        AudioSource ttsSource,          // VITS 音频源
        bool pauseMusicOnTTS)           // VITS 暂停 BGM 开关
    {
        keywordActionList = actions;
        keywordAudioList = audios;
        randomMusicPlayer = music;
        randomAnimationPlayer = anim;
        maxRounds = rounds;
        pauseMusicOnKeyword = pauseMusic;
        
        // VITS TTS配置
        enableVITS = enableTTS;
        ttsAudioSource = ttsSource;
        this.pauseMusicOnTTS = pauseMusicOnTTS;

        conversationHistory = new List<ChatMessage>();
        
        // 动态添加一个 AudioSource 组件专门用于播放关键词音效
        keywordAudioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// 开启一个新的对话会话
    /// 清空历史记录，并注入 System Prompt 设定 AI 人设
    /// </summary>
    public void StartNewConversation()
    {
        conversationHistory.Clear();
        
        // 注入系统提示词，设定 AI 为“洛天依”
        // 这里可以根据需要修改角色设定
        conversationHistory.Add(new ChatMessage { role = "system", content = "你现在要扮演是洛天依AI" });
        
        // 触发事件，通知 UI 清空聊天记录
        OnNewConversation?.Invoke();
        
        Debug.Log("[ChatManager] 新对话已开始");
    }

    /// <summary>
    /// 处理用户输入
    /// 
    /// 逻辑流程：
    /// 1. 检查是否包含音乐控制指令（暂停/播放）
    /// 2. 检查是否包含关键词音效 -> 播放音效
    /// 3. 检查是否包含动作关键词 -> 触发动作，并向 AI 隐式发送指令
    /// 4. 检查对话轮数 -> 超限则重置
    /// 5. 将用户输入加入历史记录
    /// </summary>
    /// <param name="input">用户输入的文本</param>
    /// <returns>是否需要继续调用 LLM（目前逻辑总是返回 true）</returns>
    public bool ProcessUserInput(string input)
    {
        // 1. 处理音乐控制指令（如“暂停音乐”）
        CheckMusicControl(input);
        
        // 2. 处理关键词音效（如“谢谢” -> 叮~）
        CheckKeywordAudio(input);

        // 3. 尝试触发动作（如“挥手” -> 播放挥手动画）
        // 如果触发了动作，逻辑会自动生成一条隐藏的 system 指令告诉 AI 你正在做什么
        if (TryTriggerAction(input))
        {
            return true; // 即使触发了动作，我们仍然可能需要 AI 对这个动作做出反应
        }

        // 4. 检查对话历史长度，如果超过最大轮数 (maxRounds * 2，因为每轮有user和assistant)
        // 就开启新对话，避免 Token 消耗过大或上下文混乱
        if (conversationHistory.Count >= 1 + (maxRounds * 2))
        {
            StartNewConversation();
            OnNewMessage?.Invoke("System", "（已开启新对话会话）");
        }

        // 5. 记录用户消息
        conversationHistory.Add(new ChatMessage { role = "user", content = input });
        
        // 触发事件，通知 UI 显示用户消息
        OnNewMessage?.Invoke("User", input);

        return true; // 返回 true，告诉 ChatMain 可以去请求 LLM 了
    }

    /// <summary>
    /// 处理 AI 的回复
    /// 将 AI 的消息加入历史记录，并触发 UI 显示
    /// </summary>
    public void ProcessAIResponse(string response)
    {
        // 记录 AI 消息
        conversationHistory.Add(new ChatMessage { role = "assistant", content = response });
        
        // 触发事件，通知 UI 显示 AI 消息
        OnNewMessage?.Invoke("AI", response);
    }

    /// <summary>
    /// 获取当前的对话历史记录
    /// ChatMain 会调用此方法并将历史发送给 ChatLLMService
    /// </summary>
    public List<ChatMessage> GetConversationHistory()
    {
        return conversationHistory;
    }

    // ==================== VITS 语音播放逻辑 ====================

    /// <summary>
    /// 播放 VITS 生成的语音
    /// 
    /// 优先级处理：
    /// - 暂停背景音乐（如果配置了）
    /// - 停止当前的关键词音效（避免声音冲突）
    /// - 播放 TTS 语音
    /// - 播放结束后恢复背景音乐
    /// </summary>
    /// <param name="clip">VITS 生成的 AudioClip</param>
    public void PlayVITSAudio(AudioClip clip)
    {
        if (clip == null || !enableVITS) return;

        // 1. 暂停背景音乐（如果配置了 pauseMusicOnTTS）
        if (pauseMusicOnTTS && randomMusicPlayer != null)
        {
            randomMusicPlayer.PauseMusic();
        }

        // 2. 停止当前正在播放的关键词音频（保证语音清晰）
        if (keywordAudioSource.isPlaying) keywordAudioSource.Stop();
        
        // 3. 播放 VITS 语音
        if (ttsAudioSource.isPlaying) ttsAudioSource.Stop();
        ttsAudioSource.clip = clip;
        ttsAudioSource.Play();
        isPlayingVITSAudio = true;

        Debug.Log($"[ChatManager] 播放VITS语音，时长: {clip.length}秒");

        // 4. 开始等待音频结束
        StopAllCoroutines(); // 停止之前的等待协程，防止逻辑错乱
        StartCoroutine(WaitForVITSAudioEnd(clip.length));
    }

    /// <summary>
    /// 等待 VITS 音频播放结束
    /// </summary>
    private System.Collections.IEnumerator WaitForVITSAudioEnd(float duration)
    {
        // 等待音频播放完毕
        yield return new WaitForSeconds(duration);
        
        isPlayingVITSAudio = false;
        
        // 恢复背景音乐（如果配置了）
        if (pauseMusicOnTTS && randomMusicPlayer != null)
        {
            randomMusicPlayer.ResumeMusic();
        }
        
        Debug.Log("[ChatManager] VITS语音播放结束");
    }

    // ==================== 关键词与动作逻辑 ====================

    /// <summary>
    /// 尝试触发动作（基于用户输入的关键词）
    /// </summary>
    private bool TryTriggerAction(string input)
    {
        if (keywordActionList == null) return false;

        foreach (var pair in keywordActionList)
        {
            // 检查输入是否包含关键词
            if (!string.IsNullOrEmpty(pair.keyword) && input.Contains(pair.keyword))
            {
                // 播放指定动画
                if (randomAnimationPlayer != null)
                {
                    randomAnimationPlayer.PlaySpecificAnimation(pair.actionClip);
                }

                // 【关键逻辑】
                // 即使触发了动作，我们也向 LLM 发送一条隐式的指令。
                // 这样 AI 就知道“我正在挥手”，可以做出相应的文本回复（比如“你看我挥手好看吗？”）。
                // 这条消息不会显示在 UI 上（因为是 user 或 system 类型，由 UI 层决定显示逻辑），
                // 但会作为上下文发送给 LLM。
                string modelPrompt = $"你现在正在{pair.promptDescription}";
                conversationHistory.Add(new ChatMessage { role = "user", content = modelPrompt });
                
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查音乐控制指令
    /// </summary>
    private void CheckMusicControl(string text)
    {
        if (randomMusicPlayer == null || isPlayingKeywordAudio || isPlayingVITSAudio) return;

        if (text.Contains("暂停")) 
            randomMusicPlayer.PauseMusic();
        else if (text.Contains("播放") || text.Contains("继续")) 
            randomMusicPlayer.ResumeMusic();
    }

    /// <summary>
    /// 检查并播放关键词音效
    /// </summary>
    private void CheckKeywordAudio(string text)
    {
        if (keywordAudioList == null) return;

        foreach (var pair in keywordAudioList)
        {
            if (text.Contains(pair.keyword))
            {
                PlayKeywordAudio(pair.audioClip);
                break; // 找到一个就播放并跳出
            }
        }
    }

    /// <summary>
    /// 播放关键词音效
    /// 
    /// 优先级处理：
    /// - 如果 VITS 正在播放，则忽略关键词音效（语音优先）
    /// - 否则暂停 BGM -> 播放音效 -> 恢复 BGM
    /// </summary>
    private void PlayKeywordAudio(AudioClip clip)
    {
        if (clip == null) return;

        // 优先级检查：如果 AI 正在说话，不打断它
        if (isPlayingVITSAudio)
        {
            Debug.Log("[ChatManager] VITS正在播放，忽略关键词音频");
            return;
        }

        // 暂停背景音乐
        if (pauseMusicOnKeyword && randomMusicPlayer != null)
        {
            randomMusicPlayer.PauseMusic();
        }

        // 播放音效
        if (keywordAudioSource.isPlaying) keywordAudioSource.Stop();
        
        keywordAudioSource.clip = clip;
        keywordAudioSource.Play();
        isPlayingKeywordAudio = true;
        
        // 启动恢复协程
        StopAllCoroutines();
        StartCoroutine(WaitForAudioEnd(clip.length));
    }

    /// <summary>
    /// 等待关键词音效结束
    /// </summary>
    private System.Collections.IEnumerator WaitForAudioEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isPlayingKeywordAudio = false;
        
        // 恢复背景音乐
        if (pauseMusicOnKeyword && randomMusicPlayer != null)
        {
            randomMusicPlayer.ResumeMusic();
        }
    }
}
