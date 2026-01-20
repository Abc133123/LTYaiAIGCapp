import os
os.environ["HF_ENDPOINT"] = "https://hf-mirror.com"

from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel
import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional, List
import uvicorn
import traceback

app = FastAPI(title="Qwen LoRA API Server")

# 1. 指定路径
base_model_path = "Qwen/Qwen1.5-0.5B-Chat"
lora_adapter_path = "./qwen_lora_result_v1"

# 消息模型
class Message(BaseModel):
    role: str
    content: str

# 请求模型
class ChatRequest(BaseModel):
    model: str
    messages: List[Message]
    max_new_tokens: Optional[int] = 128
    temperature: Optional[float] = 0.7
    top_p: Optional[float] = 0.9

# 响应模型
class ChatResponse(BaseModel):
    response: str

# 系统提示词
SYSTEM_PROMPTS = [
    "你现在要扮演是洛天依AI。你现在要扮演是洛天依AI说话简短、温柔。",
]

# 全局模型变量
model = None
tokenizer = None

@app.on_event("startup")
async def load_model():
    global model, tokenizer
    print("正在加载模型...")
    
    try:
        tokenizer = AutoTokenizer.from_pretrained(base_model_path, trust_remote_code=True)
        print(f"Tokenizer 加载成功: {base_model_path}")
        
        base_model = AutoModelForCausalLM.from_pretrained(
            base_model_path,
            device_map="auto",
            torch_dtype=torch.float16,
            trust_remote_code=True
        )
        print(f"Base model 加载成功")
        
        model = PeftModel.from_pretrained(base_model, lora_adapter_path)
        print(f"LoRA adapter 加载成功: {lora_adapter_path}")
        
        print("========== 模型加载完毕 ==========")
        print(f"设备: {model.device}")
        print(f"精度: {torch.float16}")
    except Exception as e:
        print(f"========== 模型加载失败 ==========")
        print(f"错误: {str(e)}")
        print(traceback.format_exc())

@app.post("/api/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    try:
        print(f"\n========== 收到请求 ==========")
        print(f"Model: {request.model}")
        print(f"Temperature: {request.temperature}")
        print(f"Max new tokens: {request.max_new_tokens}")
        print(f"Messages count: {len(request.messages)}")
        
        # 转换消息格式
        messages = [{"role": msg.role, "content": msg.content} for msg in request.messages]
        
        # 检查是否有 system 消息
        has_system = any(msg["role"] == "system" for msg in messages)
        if not has_system:
            messages.insert(0, {"role": "system", "content": SYSTEM_PROMPTS[0]})
            print(f"添加了默认 system prompt")
        
        # 打印消息内容（调试用）
        for i, msg in enumerate(messages):
            print(f"  [{i}] {msg['role']}: {msg['content'][:50]}...")
        
        # 应用 chat template
        text = tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True)
        print(f"Template 长度: {len(text)} 字符")
        
        # 编码输入
        model_inputs = tokenizer([text], return_tensors="pt").to(model.device)
        print(f"Input shape: {model_inputs.input_ids.shape}")
        
        # 生成
        print("开始生成...")
        generated_ids = model.generate(
            model_inputs.input_ids,
            max_new_tokens=request.max_new_tokens,
            pad_token_id=tokenizer.eos_token_id,
            temperature=request.temperature,
            top_p=request.top_p,
            do_sample=True if request.temperature > 0 else False
        )
        
        # 截取生成部分
        generated_ids = [
            output_ids[len(input_ids):] for input_ids, output_ids in zip(model_inputs.input_ids, generated_ids)
        ]
        
        # 解码
        response = tokenizer.batch_decode(generated_ids, skip_special_tokens=True)[0]
        print(f"生成响应: {response}")
        print("========== 请求完成 ==========\n")
        
        return ChatResponse(response=response)
    
    except Exception as e:
        print(f"\n========== 请求失败 ==========")
        print(f"错误类型: {type(e).__name__}")
        print(f"错误信息: {str(e)}")
        print("堆栈跟踪:")
        print(traceback.format_exc())
        raise HTTPException(status_code=500, detail=f"{str(e)}")

@app.get("/")
async def root():
    return {"message": "Qwen LoRA API Server is running"}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=3412)
