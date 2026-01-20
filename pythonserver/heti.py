# -*- coding: utf-8 -*-
import os
os.environ["HF_ENDPOINT"] = "https://hf-mirror.com"

from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel
import torch

# 配置路径
base_model_path = "Qwen/Qwen1.5-0.5B-Chat"
lora_adapter_path = "./qwen_lora_result_v1"      # 你的微调输出文件夹
output_merged_path = "./merged_qwen_full"      # 合并后的模型保存位置

print("正在加载底座模型...")
base_model = AutoModelForCausalLM.from_pretrained(
    base_model_path,
    torch_dtype=torch.float16,
    device_map="auto",
    trust_remote_code=True
)

print("正在加载 LoRA 适配器...")
model = PeftModel.from_pretrained(base_model, lora_adapter_path)

print("正在合并权重（这可能需要几分钟）...")
model = model.merge_and_unload()  # 关键：合并

print("正在加载分词器...")
tokenizer = AutoTokenizer.from_pretrained(base_model_path, trust_remote_code=True)

print(f"正在保存合并后的模型到: {output_merged_path}")
model.save_pretrained(output_merged_path)
tokenizer.save_pretrained(output_merged_path)

print("合并完成！你现在得到了一个完整的微调模型。")
