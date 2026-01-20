using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections; 

/* ==============================================================================
 * 脚本名称：ChatLLMService(LLM 网络服务)

 *【功能说明】 (LLM对话服务 + WAV音频转换工具)
 * 
 * 
 * 本模块实现了两个核心功能：
 * 
 * 1. LLM对话服务（ChatLLMService）：
 *    - 支持连接阿里云百炼（DashScope）API进行AI对话
 *    - 支持连接本地Python LLM服务器
 *    - 发送对话消息并接收AI回复
 *    - 支持自定义模型、超时时间等参数
 * 
 * 2. VITS语音合成集成：
 *    - 调用VITS TTS API将文本转换为语音
 *    - 支持本地VITS服务器
 *    - 将返回的WAV格式音频转换为Unity可用的AudioClip
 * 
 * 3. WAV音频助手（WavHelper）：
 *    - 解析WAV格式音频文件
 *    - 支持8位和16位PCM编码
 *    - 转换为Unity AudioClip对象供播放使用
 * 
 * ----------------------------------------------------------------
 * 【Unity 操作步骤指南】
 * ----------------------------------------------------------------
 * 
 * 【LLM对话服务使用】
 * 
 * 第一步：挂载脚本
 * 1. 在 Hierarchy 中创建一个空物体，命名为 "LLMServiceManager"。
 * 2. 将此脚本拖拽到该物体上。
 * 
 * 第二步：配置阿里云百炼（DashScope）
 * 1. 登录阿里云控制台（https://dashscope.console.aliyun.com/）
 * 2. 开通百炼服务并获取 API Key（格式：sk-xxxxxxxxxxxxxx）
 * 3. 选择合适的模型（如：qwen0.5b 等）
 * 
 * 第三步：配置本地LLM服务器（可选）
 * 1. 确保本地Python服务器已启动，如 llmapi.py
 * 2. 确认服务器地址（默认：http://localhost:9880/ 我的是3712）
 * 
 * 第四步：调用（在ChatMain.cs脚本中）
 * 
public class ChatLLMService : MonoBehaviour
{
    private const string dashScopeUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

    /// <summary>
    /// LLM服务类型枚举
    /// </summary>
    public enum LLMServiceType
    {
        DashScope,    // 阿里云百炼
        LocalLLM      // 本地Python服务器
    }

    /// <summary>
    /// 发送请求到 LLM（支持两种服务类型）
    /// </summary>
    public IEnumerator SendRequestToLLM(
        LLMServiceType serviceType,
        string apiKey, 
        string model, 
        string localLLMUrl,
        List<ChatMessage> messages, 
        int timeout, 
        Action<string> onSuccess, 
        Action<string> onError)
    {
        string url = serviceType == LLMServiceType.DashScope ? dashScopeUrl : localLLMUrl;
        string json = BuildJsonPayload(model, messages, serviceType);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeout;
            request.SetRequestHeader("Content-Type", "application/json");
            
            // 仅DashScope需要Authorization头
            if (serviceType == LLMServiceType.DashScope)
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                string aiContent = ParseResponse(responseJson, serviceType);
                
                if (!string.IsNullOrEmpty(aiContent))
                {
                    onSuccess?.Invoke(aiContent);
                }
                else
                {
                    onError?.Invoke("API 返回空数据");
                }
            }
        }
    }

    /// <summary>
    /// 调用VITS TTS API将文本转为语音
    /// </summary>
    /// <param name="text">要转换的文本</param>
    /// <param name="ttsUrl">VITS服务器地址，默认为本地</param>
    /// <param name="onSuccess">成功回调，返回AudioClip</param>
    /// <param name="onError">失败回调，返回错误信息</param>
    /// <param name="timeout">超时时间（秒）</param>
    public IEnumerator CallVITSAPI(
        string text, 
        string ttsUrl = "http://192.168.1.6:3712/", 
        Action<AudioClip> onSuccess = null, 
        Action<string> onError = null, 
        int timeout = 30)
    {
        // 构建VITS API请求的JSON体
        string json = "{\"text\":\"" + EscapeJson(text) + "\",\"text_language\":\"zh\"}";

        using (UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeout;
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke("VITS连接失败: " + request.error);
            }
            else
            {
                // 获取返回的音频数据（wav格式）
                byte[] audioData = request.downloadHandler.data;
                if (audioData != null && audioData.Length > 0)
                {
                    AudioClip clip = WavHelper.ToAudioClip(audioData);
                    if (clip != null)
                    {
                        onSuccess?.Invoke(clip);
                    }
                    else
                    {
                        onError?.Invoke("音频数据转换失败");
                    }
                }
                else
                {
                    onError?.Invoke("VITS返回空数据");
                }
            }
        }
    }

    private string BuildJsonPayload(string model, List<ChatMessage> messages, LLMServiceType serviceType)
    {
        // 本地LLM使用简化的JSON格式（参考73.py）
        if (serviceType == LLMServiceType.LocalLLM)
        {
            string json = "{";
            json += "\"model\":\"" + model + "\",";
            json += "\"messages\":[";
            for (int i = 0; i < messages.Count; i++)
            {
                json += "{";
                json += "\"role\":\"" + messages[i].role + "\",";
                json += "\"content\":\"" + EscapeJson(messages[i].content) + "\"";
                json += "}";
                if (i < messages.Count - 1) json += ",";
            }
            json += "],";
            json += "\"max_new_tokens\":128,";
            json += "\"temperature\":0.7,";
            json += "\"top_p\":0.9";
            json += "}";
            return json;
        }

        // DashScope使用标准格式
        string dashScopeJson = "{";
        dashScopeJson += "\"model\":\"" + model + "\",";
        dashScopeJson += "\"messages\":[";
        for (int i = 0; i < messages.Count; i++)
        {
            dashScopeJson += "{";
            dashScopeJson += "\"role\":\"" + messages[i].role + "\",";
            dashScopeJson += "\"content\":\"" + EscapeJson(messages[i].content) + "\"";
            dashScopeJson += "}";
            if (i < messages.Count - 1) dashScopeJson += ",";
        }
        dashScopeJson += "]}";
        return dashScopeJson;
    }

    private string ParseResponse(string json, LLMServiceType serviceType)
    {
        if (serviceType == LLMServiceType.LocalLLM)
        {
            // 本地LLM返回格式：{"response": "xxx"} 或直接返回文本
            string responseKey = "\"response\":\"";
            int responseIndex = json.IndexOf(responseKey);
            if (responseIndex != -1)
            {
                // 修复：重命名局部变量以避免与外层作用域冲突 (CS0136)
                int localStartIndex = responseIndex + responseKey.Length;
                int localEndIndex = json.IndexOf("\"", localStartIndex);
                if (localEndIndex > localStartIndex)
                {
                    return json.Substring(localStartIndex, localEndIndex - localStartIndex).Replace("\\n", "\n").Replace("\\\"", "\"");
                }
            }
            // 尝试直接解析为纯文本（如果API直接返回）
            return json.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        // DashScope返回格式
        string contentKey = "\"content\":\"";
        int contentIndex = json.IndexOf(contentKey);
        if (contentIndex == -1) return null;

        int startIndex = contentIndex + contentKey.Length;
        int endIndex = json.IndexOf("\"", startIndex);
        
        if (endIndex > startIndex)
        {
            return json.Substring(startIndex, endIndex - startIndex).Replace("\\n", "\n").Replace("\\\"", "\"");
        }
        return null;
    }

    private string EscapeJson(string str)
    {
        if (str == null) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}

/// <summary>
/// WAV音频助手类 - 用于将VITS返回的wav数据转换为Unity AudioClip
/// </summary>
public static class WavHelper
{
    public static AudioClip ToAudioClip(byte[] wavData)
    {
        try
        {
            // 解析WAV头部
            int pos = 12; // 跳过RIFF和WAVE标识
            
            // 查找fmt chunk
            while (pos < wavData.Length - 8)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = BitConverter.ToInt32(wavData, pos + 4);
                
                if (chunkId == "fmt ")
                {
                    short audioFormat = BitConverter.ToInt16(wavData, pos + 8);
                    short channels = BitConverter.ToInt16(wavData, pos + 10);
                    int sampleRate = BitConverter.ToInt32(wavData, pos + 12);
                    short bitsPerSample = BitConverter.ToInt16(wavData, pos + 22);
                    
                    if (audioFormat != 1)
                    {
                        Debug.LogWarning("不支持的音频格式: " + audioFormat);
                        return null;
                    }
                    
                    // 跳到data chunk
                    pos = 12;
                    while (pos < wavData.Length - 8)
                    {
                        chunkId = System.Text.Encoding.ASCII.GetString(wavData, pos, 4);
                        chunkSize = BitConverter.ToInt32(wavData, pos + 4);
                        
                        if (chunkId == "data")
                        {
                            int dataSize = chunkSize;
                            float[] samples = new float[dataSize / (bitsPerSample / 8)];
                            
                            if (bitsPerSample == 16)
                            {
                                for (int i = 0; i < samples.Length; i++)
                                {
                                    short sample = BitConverter.ToInt16(wavData, pos + 8 + i * 2);
                                    samples[i] = sample / 32768f;
                                }
                            }
                            else if (bitsPerSample == 8)
                            {
                                for (int i = 0; i < samples.Length; i++)
                                {
                                    byte sample = wavData[pos + 8 + i];
                                    samples[i] = (sample - 128) / 128f;
                                }
                            }
                            
                            AudioClip clip = AudioClip.Create("VITS_Voice", samples.Length, channels, sampleRate, false);
                            clip.SetData(samples, 0);
                            return clip;
                        }
                        
                        pos += 8 + chunkSize;
                    }
                    
                    return null;
                }
                
                pos += 8 + chunkSize;
            }
            
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("WAV解析错误: " + e.Message);
            return null;
        }
    }
}
