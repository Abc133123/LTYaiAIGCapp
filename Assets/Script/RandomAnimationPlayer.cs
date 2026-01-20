using UnityEngine;
using System.Collections;

/* ==============================================================================
 * 脚本名称：RandomAnimationPlayer (随机动画播放器)
 * 
 * 【功能说明】
 * 此脚本用于管理游戏对象的随机动画播放。它允许从一组动画片段中随机选择并循环播放，
 * 同时支持打断当前的随机循环，播放指定的特殊动作（你好），播放完毕后自动
 * 恢复随机循环。非常适合用于AIGC交流动作行为展示等场景。
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：准备动画片段
 * 1. 确保你有一组已经制作好的 AnimationClip（.anim 文件）。
 * 2. 注意：此脚本使用的是 Legacy (旧版) Animation 组件，而非 Animator。
 * 
 * 第二步：挂载脚本
 * 1. 在 Hierarchy 中选中需要播放随机动画的 3D 模型或空物体。
 * 2. 点击 Inspector -> Add Component，搜索并添加 "RandomAnimationPlayer"。
 * 
 * 第三步：配置动画列表
 * 1. 在 Inspector 面板找到 "Random Clips" 数组。
 * 2. 设置 Size 为你想要随机播放的动画数量。
 * 3. 将你的 .anim 文件从 Project 窗口拖拽到数组对应的 Element 槽位中。
 * 
 * 第四步：运行设置
 * 1. Play On Start: 如果勾选，游戏运行开始时会自动播放随机循环。
 *    如果不勾选，你需要通过按钮或其他脚本手动调用 "StartRandomLoop" 方法。
 * 
 * 第五步：如何在代码中触发特定动画
 * 此脚本提供了一个公共方法 `PlaySpecificAnimation(AnimationClip clip)`。
 * 你可以通过其他脚本（如攻击检测、对话触发）获取此脚本的引用，并调用该方法
 * 播放特定动作。播放完后，它会自动回到随机待机状态。
 * 
 * ============================================================================== */

public class RandomAnimationPlayer : MonoBehaviour
{
    [Header("动画设置")]
    // ----------------------------------------------------------------
    // 【Unity 操作】
    // 1. 点击右侧的小圆圈或在 Project 窗口选中多个 .anim 文件拖入此处
    // 2. 确保这些片段是非循环的（除非你想让某个随机动作无限循环卡住）
    // ----------------------------------------------------------------
    [Tooltip("随机动作列表：所有待机时可能随机播放的动画")]
    public AnimationClip[] randomClips; 

    // ----------------------------------------------------------------
    // 【Unity 操作】
    // 勾选此框，物体一旦生成立即开始随机播放动作
    // ----------------------------------------------------------------
    [Tooltip("如果勾选，物体启动时会自动开始随机循环")]
    public bool playOnStart = true;

    private Animation animComponent;
    private Coroutine randomLoopCoroutine;

    void Start()
    {
        // 获取或添加 Animation 组件 (Legacy 动画系统)
        animComponent = GetComponent<Animation>();
        if (animComponent == null)
        {
            animComponent = gameObject.AddComponent<Animation>();
        }

        // 将 Inspector 中配置的所有片段注册到 Animation 组件中
        foreach (var clip in randomClips)
        {
            if (clip != null && !animComponent.GetClip(clip.name))
            {
                animComponent.AddClip(clip, clip.name);
            }
        }

        if (playOnStart)
        {
            StartRandomLoop();
        }
    }

    /// <summary>
    /// 开始随机循环播放动画
    /// 【Unity 操作】可以通过 Button 的 OnClick 事件调用此方法来手动启动循环
    /// </summary>
    public void StartRandomLoop()
    {
        // 如果已经在运行，先停止旧的，防止逻辑冲突
        if (randomLoopCoroutine != null)
        {
            StopCoroutine(randomLoopCoroutine);
        }
        randomLoopCoroutine = StartCoroutine(RandomLoopRoutine());
    }

    /// <summary>
    /// 随机循环协程：播放一个 -> 等待结束 -> 播放下一个
    /// </summary>
    IEnumerator RandomLoopRoutine()
    {
        while (true)
        {
            if (randomClips != null && randomClips.Length > 0)
            {
                // 1. 播放一个随机片段
                PlayOneRandomClip();
                
                // 2. 等待当前动画播放完毕
                // 注意：这里必须确保动画不是 Loop 模式，否则协程结束了动画还在播，导致下一次随机无法触发
                if (animComponent.clip != null)
                {
                    yield return new WaitForSeconds(animComponent.clip.length);
                }
                else
                {
                    yield return null; 
                }
            }
            else
            {
                // 如果没有配置动画片段，跳出循环
                yield break;
            }
        }
    }

    /// <summary>
    /// 内部方法：随机并播放一个动画（单次播放）
    /// </summary>
    private void PlayOneRandomClip()
    {
        int index = Random.Range(0, randomClips.Length);
        AnimationClip clipToPlay = randomClips[index];
        if (clipToPlay != null)
        {
            // 【关键修复】强制设置为播放一次 (Once)
            // 这非常重要，因为如果动画源文件设置成了 Loop，而不在这里修改，
            // 它会一直循环这个动作，导致我们的“随机切换”逻辑失效。
            animComponent[clipToPlay.name].wrapMode = WrapMode.Once;
            animComponent.Play(clipToPlay.name);
        }
    }

    /// <summary>
    /// 【核心方法】播放指定动作（指定类）
    /// 使用场景：当需要播放特定动作（如打招呼、攻击）时调用。
    /// 逻辑：停止当前随机循环 -> 播放指定动画 -> 播放完后自动恢复随机循环
    /// 
    /// 调用示例：
    /// GameObject.Find("NPC").GetComponent<RandomAnimationPlayer>().PlaySpecificAnimation(waveClip);
    /// </summary>
    public void PlaySpecificAnimation(AnimationClip clip)
    {
        if (clip == null || animComponent == null) return;

        // 1. 停止当前的随机循环协程
        if (randomLoopCoroutine != null)
        {
            StopCoroutine(randomLoopCoroutine);
            randomLoopCoroutine = null;
        }

        // 2. 确保指定片段已添加到组件中
        if (animComponent.GetClip(clip.name) == null)
        {
            animComponent.AddClip(clip, clip.name);
        }

        // 3. 强制停止当前正在播放的任何动画
        animComponent.Stop();
        
        // 【关键修复】强制设置为播放一次，防止特定动作死循环
        animComponent[clip.name].wrapMode = WrapMode.Once;
        
        // 播放指定动画
        animComponent.Play(clip.name);
        
        Debug.Log($"[RandomAnimationPlayer] 播放指定动作: {clip.name}");

        // 4. 启动协程，等待动作结束后恢复随机循环
        StartCoroutine(WaitAndResumeLoop(clip.length));
    }

    /// <summary>
    /// 等待指定时长后恢复随机循环
    /// </summary>
    IEnumerator WaitAndResumeLoop(float duration)
    {
        yield return new WaitForSeconds(duration);
        Debug.Log("[RandomAnimationPlayer] 指定动作结束，恢复随机循环");
        StartRandomLoop();
    }
}
