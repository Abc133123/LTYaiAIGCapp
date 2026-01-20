using UnityEngine;

/* ==============================================================================
 * 关于功能说明 (版权提示)
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 第一步：挂载脚本
 * 1. 在 Unity Hierarchy（层级） 面板中右键 -> Create Empty 创建一个面板（也可以直接选中 Canvas）。
 * 2. 将此脚本拖拽到该物体上。
 * 
 * 第二步：绑定显示对象
 * 1. 确保场景中有一个你需要控制显示/隐藏的 UI 对象（例如：包含文字的 Panel）。
 * 2. 选中挂载了本脚本的物体。
 * 3. 在 Inspector 面板中找到 "Info Display" 选项。
 * 4. 将那个 UI 对象直接拖拽到 "Info Display" 这个框里。
 * 
 * 第三步：设置“显示”按钮
 * 1. 在 Hierarchy 右键 -> UI -> Button，创建一个按钮。
 * 2. 选中这个 Button，在 Inspector 面板找到 Button 组件下的 OnClick () 区域。
 * 3. 点击左下角的 "+" 号。
 * 4. 将挂载了本脚本的物体（第一步的那个物体）拖到 OnClick 的那个小空框里。
 * 5. 点击 OnClick 右侧的下拉菜单（No Function），选择：about -> ShowInfo。
 * 
 * 第四步：设置“隐藏”按钮
 * 1. 再创建一个 UI Button (或者复制上面的按钮)。
 * 2. 同样在 OnClick () 里点击 "+"。
 * 3. 拖入挂载脚本的物体。
 * 4. 在下拉菜单中选择：about -> HideInfo。
 * 5.改名text为确认
 * 
 * ============================================================================== */

public class about : MonoBehaviour
{
    // 在 Inspector 面板中拖入你的信息显示对象（比如一个 Panel 或 Text）
    // 【对应操作步骤：第二步】
    public GameObject infoDisplay;

    void Start()
    {
        // 游戏开始时，默认隐藏信息
        if (infoDisplay != null)
        {
            infoDisplay.SetActive(false);
        }
    }

    // 按钮一调用的方法：显示信息
    // 【对应操作步骤：第三步】
    public void ShowInfo()
    {
        if (infoDisplay != null)
        {
            infoDisplay.SetActive(true);
        }
    }

    // 按钮二调用的方法：隐藏信息
    // 【对应操作步骤：第四步】
    public void HideInfo()
    {
        if (infoDisplay != null)
        {
            infoDisplay.SetActive(false);
        }
    }
}
