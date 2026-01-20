using UnityEngine;
using System.Collections;

/* ==============================================================================
 * 脚本名称：RandomMusicPlayer (随机背景音乐播放器)
 * 
 * 【功能说明】
 * 此脚本用于实现游戏背景音乐的随机循环播放。
 * 
 * 核心功能：
 * 1. 随机循环：从歌单中随机选取一首播放，播完自动切下一首。
 * 2. 防重复：算法确保不会连续播放同一首歌曲。
 * 3. 交互控制：支持暂停 和恢复。
 * 4. 精准续播：从暂停处恢复播放，并精确计算剩余时间，确保播完这首歌后才切下一首。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：创建音乐管理器
 * 1. 在 Hierarchy 面板右键 -> Create Empty。
 * 2. 将物体命名为 "BGMManager"。
 * 
 * 第二步：挂载脚本
 * 1. 选中 "BGMManager"。
 * 2. 在 Inspector 面板点击 "Add Component"，搜索 "RandomMusicPlayer" 并添加。
 *    *注意：脚本会自动添加 AudioSource 组件，无需手动添加。*
 * 
 * 第三步：导入音乐文件
 * 1. 将你的 MP3 或 WAV 音乐文件拖入 Unity 的 Project 窗口中。
 * 
 * 第四步：配置歌单
 * 1. 选中 "BGMManager"，在 Inspector 面板找到 "Music Clips" 数组。
 * 2. 设置 Size 为你想添加的歌曲数量。
 * 3. 将 Project 中的音乐文件分别拖入对应的 Element 槽位中。
 * 
 * 第五步：默认不管设置
 * 1. 展开 Inspector 中自动生成的 "Audio Source" 组件。
 * 2. **重要**：请确保 "Loop" (循环) 选项是 **未勾选** 的。
 *    - 原因：本脚本通过代码控制“播完一首切下一首”的逻辑。如果勾选了 Loop，
 *      单首歌曲会无限循环，脚本检测到“播放结束”的机制会失效，导致无法随机切换。
 * 
 * 第六步：外部调用（可选）
 * 如果需要在其他脚本或 UI 按钮中控制音乐：
 * 1. 暂停：`GetComponent<RandomMusicPlayer>().PauseMusic();`
 * 2. 恢复：`GetComponent<RandomMusicPlayer>().ResumeMusic();`
 * 
 * ============================================================================== */

public class RandomMusicPlayer : MonoBehaviour
{
    [Header("音乐文件")]
    // ----------------------------------------------------------------
    // 【Unity 操作】
    // 直接将 Project 窗口中的音频文件拖入下方的数组列表中
    // ----------------------------------------------------------------
    [Tooltip("在 Inspector 中拖入 MP3/WAV 文件，组成随机歌单")]
    public AudioClip[] musicClips;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;  // 记录上一首播放的索引，防止重复
    private bool isPaused = false;    // 标记当前是否处于暂停状态

    void Awake()
    {
        // 获取或添加 AudioSource 组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        // 启动时直接开始播放列表
        if (musicClips != null && musicClips.Length > 0)
        {
            PlayRandomMusic();
        }
        else
        {
            Debug.LogWarning("RandomMusicPlayer: 没有检测到音乐文件，请在 Inspector 中添加 Music Clips。");
        }
    }

    /// <summary>
    /// 随机播放一首音乐
    /// </summary>
    void PlayRandomMusic()
    {
        if (musicClips == null || musicClips.Length == 0) return;

        int randomIndex;

        // --- 随机算法 ---
        // 随机选取一个索引，如果随机到的索引和上一次一样，就重新随机
        // 这是为了避免“听完A -> 下一首还是A”的情况
        do
        {
            randomIndex = Random.Range(0, musicClips.Length);
        }
        while (randomIndex == lastPlayedIndex && musicClips.Length > 1);

        lastPlayedIndex = randomIndex;

        // 赋值并播放
        audioSource.clip = musicClips[randomIndex];
        audioSource.Play();
        isPaused = false;

        // 停止之前的倒计时协程，防止多重计时（比如从暂停恢复时）
        StopAllCoroutines();
        // 开启新的倒计时，等待这首歌播完后自动切下一首
        StartCoroutine(WaitForMusicToEnd());
    }

    // 等待当前歌曲播完
    IEnumerator WaitForMusicToEnd()
    {
        // 等待的时间等于歌曲的长度
        yield return new WaitForSeconds(audioSource.clip.length);
        PlayRandomMusic(); // 播完后递归调用，播放下一首
    }

    // ==================== 外部调用接口 (供 UI 按钮或其他脚本调用) ====================

    /// <summary>
    /// 暂停音乐
    /// 
    /// 【调用示例】
    /// 在按钮的 OnClick 事件中绑定此方法，或者在其他脚本中调用。
    /// 
    /// 关键逻辑：
    /// 1. 暂停 AudioSource。
    /// 2. 停止协程。这是为了防止后台还在计时，导致时间一到自动切歌。
    /// ----------------------------------------------------------------
    /// 【Unity 操作】可以将 UI 按钮的 OnClick 事件绑定到此方法
    /// ----------------------------------------------------------------
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            // 【关键】必须停止协程，否则暂停期间后台还在计时，时间一到会自动切歌
            StopAllCoroutines(); 
            isPaused = true;
            Debug.Log("音乐已暂停");
        }
    }

    /// <summary>
    /// 恢复/继续音乐
    /// 
    /// 【调用示例】
    /// 在按钮的 OnClick 事件中绑定此方法，或者在其他脚本中调用。
    /// 
    /// 关键逻辑：
    /// 1. 计算“当前歌曲已经播了多少秒”和“还剩多少秒”。
    /// 2. 使用剩余时间重新启动协程。
    /// 3. 确保播完这一首后才切下一首，而不是重头开始播。
    /// ----------------------------------------------------------------
    /// 【Unity 操作】可以将 UI 按钮的 OnClick 事件绑定到此方法
    /// ----------------------------------------------------------------
    public void ResumeMusic()
    {
        if (isPaused && audioSource.clip != null)
        {
            audioSource.UnPause();
            isPaused = false;

            // 计算当前歌曲的剩余时间 (总长度 - 当前播放到的位置)
            float remainingTime = audioSource.clip.length - audioSource.time;
            
            // 使用剩余时间重新启动倒计时，确保播完这一首后才切下一首
            StartCoroutine(WaitForMusicToEnd(remainingTime));
            Debug.Log("音乐继续播放");
        }
        else if (!audioSource.isPlaying)
        {
            // 如果既没暂停也没播放（比如之前被手动Stop了），就重新开始播
            PlayRandomMusic();
        }
    }

    // 重载协程方法：接收一个具体的时间参数，用于恢复播放时的精准计时
    IEnumerator WaitForMusicToEnd(float time)
    {
        yield return new WaitForSeconds(time);
        PlayRandomMusic();
    }
}
