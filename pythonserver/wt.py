# -*- coding: utf-8 -*-
import os
os.environ["HF_ENDPOINT"] = "https://hf-mirror.com"  # 使用镜像加速下载

import torch
from datasets import load_dataset
from transformers import (
    AutoModelForCausalLM,
    AutoTokenizer,
    TrainingArguments,
)
from peft import LoraConfig
from trl import SFTTrainer

# ---------------- 配置区 ----------------
# 基础模型
model_name = "Qwen/Qwen1.5-0.5B-Chat"
# 你的数据文件
data_file = "./my_data.jsonl"
# 输出目录
output_dir = "./qwen_lora_result_v1"  # 改成 v2，避免覆盖之前的

# ---------------- 1. 加载模型和分词器 ----------------
print("正在加载模型...")

tokenizer = AutoTokenizer.from_pretrained(model_name, trust_remote_code=True)

model = AutoModelForCausalLM.from_pretrained(
    model_name,
    torch_dtype=torch.float16,
    device_map="auto",
    trust_remote_code=True
)

# ---------------- 2. 配置 LoRA参数（扩大目标模块）----------------
peft_config = LoraConfig(
    r=16,
    lora_alpha=32,
    lora_dropout=0.05,
    bias="none",
    task_type="CAUSAL_LM",
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"]
)

# ---------------- 3. 数据格式化函数（修正）----------------
def formatting_prompts_func(examples):
    output_texts = []
    for messages in examples['messages']:
        text = tokenizer.apply_chat_template(
            messages,
            tokenize=False,
            add_generation_prompt=True  # ← 改成 True！
        )
        output_texts.append(text)
    return output_texts

# ---------------- 4. 加载数据 ----------------
dataset = load_dataset("json", data_files=data_file, split="train")

# ---------------- 5. 配置训练参数 ----------------
training_args = TrainingArguments(
    output_dir=output_dir,
    per_device_train_batch_size=2,
    gradient_accumulation_steps=4,
    learning_rate=2e-4,
    logging_steps=10,
    num_train_epochs=4,
    fp16=True,
    save_strategy="epoch",
)

# ---------------- 6. 初始化训练器 ----------------
trainer = SFTTrainer(
    model=model,
    train_dataset=dataset,
    formatting_func=formatting_prompts_func,
    peft_config=peft_config,
    max_seq_length=512,
    tokenizer=tokenizer,
    args=training_args,
)

# ---------------- 7. 开始训练并保存 ----------------
print("开始训练...")
trainer.train()

print("正在保存模型...")
trainer.model.save_pretrained(output_dir)
tokenizer.save_pretrained(output_dir)

print(f"训练完成！LoRA 适配器已保存至: {output_dir}")
