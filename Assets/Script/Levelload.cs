using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class Levelload : MonoBehaviour
{
    public Slider progressBar; // 拖入 Loading 场景的 Slider

    void Start()
    {
        // 读取 Level 类里的静态变量，看看要去哪
        if (string.IsNullOrEmpty(Level.nextSceneName))
        {
            Debug.LogError("致命错误：找不到目标场景名！无法继续。");
            return;
        }

        StartCoroutine(LoadWithMemoryBuffer());
    }

    IEnumerator LoadWithMemoryBuffer()
    {
        // --- 第一阶段：强制等待清理 (前3秒) ---
        // 用来解决手机闪退，给系统卸载旧资源的时间
        
        float cleanUpTime = 3f; 
        float timer = 0f;

        // 强制回收
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        while (timer < cleanUpTime)
        {
            timer += Time.deltaTime;
            
            // 进度条从 0% 走到 60%
            if (progressBar != null)
            {
                float progress = Mathf.Lerp(0f, 0.6f, timer / cleanUpTime);
                progressBar.value = progress;
            }
            yield return null;
        }

        // --- 第二阶段：开始真正加载目标场景 ---
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(Level.nextSceneName);
        operation.allowSceneActivation = false; // 暂时不自动跳转

        float fakeLoadTime = 2f; // 剩下的40%进度条跑多久
        float loadTimer = 0f;

        while (!operation.isDone)
        {
            loadTimer += Time.deltaTime;

            // 进度条从 60% 走到 100%
            if (progressBar != null)
            {
                float progress = Mathf.Lerp(0.6f, 1f, loadTimer / fakeLoadTime);
                progressBar.value = progress;
            }

            // 当后台加载到了 0.9 (实际上已经好了)
            if (operation.progress >= 0.9f)
            {
                // 等待假进度条走完
                if (loadTimer >= fakeLoadTime)
                {
                    operation.allowSceneActivation = true; // 允许跳转
                }
            }

            yield return null;
        }
    }
}
