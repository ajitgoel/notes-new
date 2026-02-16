https://github.com/eyaltoledano/claude-task-master/blob/main/docs/command-reference.md

```
## Parse PRD
# Parse a PRD file and generate tasks
task-master parse-prd <prd-file.txt>
# Allow task master to determine the number of tasks based on complexity
task-master parse-prd <prd-file.txt> --num-tasks=0

## Update Tasks
# Update tasks from a specific ID and provide context
task-master update --from=<id> --prompt="<prompt>"
# Update tasks using research role
task-master update --from=<id> --prompt="<prompt>" --research

## Update a Subtask
# Append additional information to a specific subtask
task-master update-subtask --id=<parentId.subtaskId> --prompt="<prompt>"

# Example: Add details about API rate limiting to subtask 2 of task 5
task-master update-subtask --id=5.2 --prompt="Add rate limiting of 100 requests per minute"

# Use research-backed updates
task-master update-subtask --id=<parentId.subtaskId> --prompt="<prompt>" --research

## Generate Task Files
# Generate individual task files from tasks.json
task-master generate

## Initialize a Project
# Initialize a new project with Task Master structure
task-master init

## Research Fresh Information
# Perform AI-powered research with fresh, up-to-date information
task-master research "What are the latest best practices for JWT authentication in Node.js?"

# Research with specific task context
task-master research "How to implement OAuth 2.0?" --id=15,16

# Research with file context for code-aware suggestions
task-master research "How can I optimize this API implementation?" --files=src/api.js,src/auth.js

# Research with custom context and project tree
task-master research "Best practices for error handling" --context="We're using Express.js" --tree

# Research with different detail levels
task-master research "React Query v5 migration guide" --detail=high

# Disable interactive follow-up questions (useful for scripting, is the default for MCP)
# Use a custom tasks file location
task-master research "How to implement this feature?" --file=custom-tasks.json

# Research within a specific tag context
task-master research "Database optimization strategies" --tag=feature-branch

# Save research conversation to .taskmaster/docs/research/ directory (for later reference)
task-master research "Database optimization techniques" --save-file

# Save key findings directly to a task or subtask (recommended for actionable insights)
task-master research "How to implement OAuth?" --save-to=15
task-master research "API optimization strategies" --save-to=15.2

# Combine context gathering with automatic saving of findings
task-master research "Best practices for this implementation" --id=15,16 --files=src/auth.js --save-to=15.3

```

# Available Models as of August 8, 2025

[](https://github.com/eyaltoledano/claude-task-master/blob/main/docs/models.md#available-models-as-of-august-8-2025)

## Main Models

[](https://github.com/eyaltoledano/claude-task-master/blob/main/docs/models.md#main-models)

|Provider|Model Name|SWE Score|Input Cost|Output Cost|
|---|---|---|---|---|
|anthropic|claude-sonnet-4-20250514|0.727|3|15|
|anthropic|claude-opus-4-20250514|0.725|15|75|
|anthropic|claude-3-7-sonnet-20250219|0.623|3|15|
|anthropic|claude-3-5-sonnet-20241022|0.49|3|15|
|claude-code|opus|0.725|0|0|
|claude-code|sonnet|0.727|0|0|
|mcp|mcp-sampling|—|0|0|
|gemini-cli|gemini-2.5-pro|0.72|0|0|
|gemini-cli|gemini-2.5-flash|0.71|0|0|
|openai|gpt-4o|0.332|2.5|10|
|openai|o1|0.489|15|60|
|openai|o3|0.5|2|8|
|openai|o3-mini|0.493|1.1|4.4|
|openai|o4-mini|0.45|1.1|4.4|
|openai|o1-mini|0.4|1.1|4.4|
|openai|o1-pro|—|150|600|
|openai|gpt-4-5-preview|0.38|75|150|
|openai|gpt-4-1-mini|—|0.4|1.6|
|openai|gpt-4-1-nano|—|0.1|0.4|
|openai|gpt-4o-mini|0.3|0.15|0.6|
|openai|gpt-5|0.749|5|20|
|google|gemini-2.5-pro-preview-05-06|0.638|—|—|
|google|gemini-2.5-pro-preview-03-25|0.638|—|—|
|google|gemini-2.5-flash-preview-04-17|0.604|—|—|
|google|gemini-2.0-flash|0.518|0.15|0.6|
|google|gemini-2.0-flash-lite|—|—|—|
|xai|grok-3|—|3|15|
|xai|grok-3-fast|—|5|25|
|xai|grok-4|—|3|15|
|groq|moonshotai/kimi-k2-instruct|0.66|1|3|
|groq|llama-3.3-70b-versatile|0.55|0.59|0.79|
|groq|llama-3.1-8b-instant|0.32|0.05|0.08|
|groq|llama-4-scout|0.45|0.11|0.34|
|groq|llama-4-maverick|0.52|0.5|0.77|
|groq|mixtral-8x7b-32768|0.35|0.24|0.24|
|groq|qwen-qwq-32b-preview|0.4|0.18|0.18|
|groq|deepseek-r1-distill-llama-70b|0.52|0.75|0.99|
|groq|gemma2-9b-it|0.3|0.2|0.2|
|groq|whisper-large-v3|—|0.11|0|
|perplexity|sonar-pro|—|3|15|
|perplexity|sonar-reasoning-pro|0.211|2|8|
|perplexity|sonar-reasoning|0.211|1|5|
|openrouter|google/gemini-2.5-flash-preview-05-20|—|0.15|0.6|
|openrouter|google/gemini-2.5-flash-preview-05-20:thinking|—|0.15|3.5|
|openrouter|google/gemini-2.5-pro-exp-03-25|—|0|0|
|openrouter|deepseek/deepseek-chat-v3-0324|—|0.27|1.1|
|openrouter|openai/gpt-4.1|—|2|8|
|openrouter|openai/gpt-4.1-mini|—|0.4|1.6|
|openrouter|openai/gpt-4.1-nano|—|0.1|0.4|
|openrouter|openai/o3|—|10|40|
|openrouter|openai/codex-mini|—|1.5|6|
|openrouter|openai/gpt-4o-mini|—|0.15|0.6|
|openrouter|openai/o4-mini|0.45|1.1|4.4|
|openrouter|openai/o4-mini-high|—|1.1|4.4|
|openrouter|openai/o1-pro|—|150|600|
|openrouter|meta-llama/llama-3.3-70b-instruct|—|120|600|
|openrouter|meta-llama/llama-4-maverick|—|0.18|0.6|
|openrouter|meta-llama/llama-4-scout|—|0.08|0.3|
|openrouter|qwen/qwen-max|—|1.6|6.4|
|openrouter|qwen/qwen-turbo|—|0.05|0.2|
|openrouter|qwen/qwen3-235b-a22b|—|0.14|2|
|openrouter|mistralai/mistral-small-3.1-24b-instruct|—|0.1|0.3|
|openrouter|mistralai/devstral-small|—|0.1|0.3|
|openrouter|mistralai/mistral-nemo|—|0.03|0.07|
|ollama|devstral:latest|—|0|0|
|ollama|qwen3:latest|—|0|0|
|ollama|qwen3:14b|—|0|0|
|ollama|qwen3:32b|—|0|0|
|ollama|mistral-small3.1:latest|—|0|0|
|ollama|llama3.3:latest|—|0|0|
|ollama|phi4:latest|—|0|0|
|azure|gpt-4o|0.332|2.5|10|
|azure|gpt-4o-mini|0.3|0.15|0.6|
|azure|gpt-4-1|—|2|10|
|bedrock|us.anthropic.claude-3-haiku-20240307-v1:0|0.4|0.25|1.25|
|bedrock|us.anthropic.claude-3-opus-20240229-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-3-5-sonnet-20240620-v1:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-5-sonnet-20241022-v2:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-7-sonnet-20250219-v1:0|0.623|3|15|
|bedrock|us.anthropic.claude-3-5-haiku-20241022-v1:0|0.4|0.8|4|
|bedrock|us.anthropic.claude-opus-4-20250514-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-sonnet-4-20250514-v1:0|0.727|3|15|

## Research Models

[](https://github.com/eyaltoledano/claude-task-master/blob/main/docs/models.md#research-models)

|Provider|Model Name|SWE Score|Input Cost|Output Cost|
|---|---|---|---|---|
|claude-code|opus|0.725|0|0|
|claude-code|sonnet|0.727|0|0|
|mcp|mcp-sampling|—|0|0|
|gemini-cli|gemini-2.5-pro|0.72|0|0|
|gemini-cli|gemini-2.5-flash|0.71|0|0|
|openai|gpt-4o-search-preview|0.33|2.5|10|
|openai|gpt-4o-mini-search-preview|0.3|0.15|0.6|
|xai|grok-3|—|3|15|
|xai|grok-3-fast|—|5|25|
|xai|grok-4|—|3|15|
|groq|llama-3.3-70b-versatile|0.55|0.59|0.79|
|groq|llama-4-scout|0.45|0.11|0.34|
|groq|llama-4-maverick|0.52|0.5|0.77|
|groq|qwen-qwq-32b-preview|0.4|0.18|0.18|
|groq|deepseek-r1-distill-llama-70b|0.52|0.75|0.99|
|perplexity|sonar-pro|—|3|15|
|perplexity|sonar|—|1|1|
|perplexity|deep-research|0.211|2|8|
|perplexity|sonar-reasoning-pro|0.211|2|8|
|perplexity|sonar-reasoning|0.211|1|5|
|bedrock|us.anthropic.claude-3-opus-20240229-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-3-5-sonnet-20240620-v1:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-5-sonnet-20241022-v2:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-7-sonnet-20250219-v1:0|0.623|3|15|
|bedrock|us.anthropic.claude-opus-4-20250514-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-sonnet-4-20250514-v1:0|0.727|3|15|
|bedrock|us.deepseek.r1-v1:0|—|1.35|5.4|

## Fallback Models

[](https://github.com/eyaltoledano/claude-task-master/blob/main/docs/models.md#fallback-models)

|Provider|Model Name|SWE Score|Input Cost|Output Cost|
|---|---|---|---|---|
|anthropic|claude-sonnet-4-20250514|0.727|3|15|
|anthropic|claude-opus-4-20250514|0.725|15|75|
|anthropic|claude-3-7-sonnet-20250219|0.623|3|15|
|anthropic|claude-3-5-sonnet-20241022|0.49|3|15|
|claude-code|opus|0.725|0|0|
|claude-code|sonnet|0.727|0|0|
|mcp|mcp-sampling|—|0|0|
|gemini-cli|gemini-2.5-pro|0.72|0|0|
|gemini-cli|gemini-2.5-flash|0.71|0|0|
|openai|gpt-4o|0.332|2.5|10|
|openai|o3|0.5|2|8|
|openai|o4-mini|0.45|1.1|4.4|
|openai|gpt-5|0.749|5|20|
|google|gemini-2.5-pro-preview-05-06|0.638|—|—|
|google|gemini-2.5-pro-preview-03-25|0.638|—|—|
|google|gemini-2.5-flash-preview-04-17|0.604|—|—|
|google|gemini-2.0-flash|0.518|0.15|0.6|
|google|gemini-2.0-flash-lite|—|—|—|
|xai|grok-3|—|3|15|
|xai|grok-3-fast|—|5|25|
|xai|grok-4|—|3|15|
|groq|moonshotai/kimi-k2-instruct|0.66|1|3|
|groq|llama-3.3-70b-versatile|0.55|0.59|0.79|
|groq|llama-3.1-8b-instant|0.32|0.05|0.08|
|groq|llama-4-scout|0.45|0.11|0.34|
|groq|llama-4-maverick|0.52|0.5|0.77|
|groq|mixtral-8x7b-32768|0.35|0.24|0.24|
|groq|qwen-qwq-32b-preview|0.4|0.18|0.18|
|groq|gemma2-9b-it|0.3|0.2|0.2|
|perplexity|sonar-reasoning-pro|0.211|2|8|
|perplexity|sonar-reasoning|0.211|1|5|
|openrouter|google/gemini-2.5-flash-preview-05-20|—|0.15|0.6|
|openrouter|google/gemini-2.5-flash-preview-05-20:thinking|—|0.15|3.5|
|openrouter|google/gemini-2.5-pro-exp-03-25|—|0|0|
|openrouter|openai/gpt-4.1|—|2|8|
|openrouter|openai/gpt-4.1-mini|—|0.4|1.6|
|openrouter|openai/gpt-4.1-nano|—|0.1|0.4|
|openrouter|openai/o3|—|10|40|
|openrouter|openai/codex-mini|—|1.5|6|
|openrouter|openai/gpt-4o-mini|—|0.15|0.6|
|openrouter|openai/o4-mini|0.45|1.1|4.4|
|openrouter|openai/o4-mini-high|—|1.1|4.4|
|openrouter|openai/o1-pro|—|150|600|
|openrouter|meta-llama/llama-3.3-70b-instruct|—|120|600|
|openrouter|meta-llama/llama-4-maverick|—|0.18|0.6|
|openrouter|meta-llama/llama-4-scout|—|0.08|0.3|
|openrouter|qwen/qwen-max|—|1.6|6.4|
|openrouter|qwen/qwen-turbo|—|0.05|0.2|
|openrouter|qwen/qwen3-235b-a22b|—|0.14|2|
|openrouter|mistralai/mistral-small-3.1-24b-instruct|—|0.1|0.3|
|openrouter|mistralai/mistral-nemo|—|0.03|0.07|
|ollama|devstral:latest|—|0|0|
|ollama|qwen3:latest|—|0|0|
|ollama|qwen3:14b|—|0|0|
|ollama|qwen3:32b|—|0|0|
|ollama|mistral-small3.1:latest|—|0|0|
|ollama|llama3.3:latest|—|0|0|
|ollama|phi4:latest|—|0|0|
|azure|gpt-4o|0.332|2.5|10|
|azure|gpt-4o-mini|0.3|0.15|0.6|
|azure|gpt-4-1|—|2|10|
|bedrock|us.anthropic.claude-3-haiku-20240307-v1:0|0.4|0.25|1.25|
|bedrock|us.anthropic.claude-3-opus-20240229-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-3-5-sonnet-20240620-v1:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-5-sonnet-20241022-v2:0|0.49|3|15|
|bedrock|us.anthropic.claude-3-7-sonnet-20250219-v1:0|0.623|3|15|
|bedrock|us.anthropic.claude-3-5-haiku-20241022-v1:0|0.4|0.8|4|
|bedrock|us.anthropic.claude-opus-4-20250514-v1:0|0.725|15|75|
|bedrock|us.anthropic.claude-sonnet-4-20250514-v1:0|0.727|3|15|