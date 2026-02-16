I’ve been using Cursor for months now, and while it’s great at understanding individual files, I always felt frustrated when working with larger codebases.

Cursor would give me solid suggestions for the code I was looking at, but it struggled to understand how everything connected across my project.

# The Problem I Was Facing

Working on a React/Node.js project with about 200 files, I’d constantly run into situations where:

- I wanted to refactor a function but couldn’t easily see what would break
- I needed to understand data flow across multiple components
- I was looking for similar patterns that already existed in the codebase
- Onboarding new team members was painful because they couldn’t grasp the overall architecture

Cursor helped with individual files, but I was missing the bigger picture.

That’s when I decided to use **Deep Graph MCP** to enhance Cursor’s repository knowledge and give it the architectural context it was missing.

# What Deep Graph MCP Actually Does

Instead of Cursor only seeing your current file, Deep Graph MCP gives it a map of your entire codebase as an interconnected graph. Now I can ask questions like:

- “What functions call this authentication method?”
- “Show me the complete data flow from this API endpoint”
- “Find similar error handling patterns in this codebase”

And get accurate, contextual answers.

# How I Set It Up

The setup took me about 5 minutes:

# Step 1: Open Cursor Settings

- `Cursor > Settings`
- Go to “Tools & Integrations” → “MCP Tools”
- Click on “Add Custo MCP”

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*hRC5eotiLlxee4CYda-BFg.png)

# Step 2: Add This Configuration

For public repos like facebook/react (if you want test first):

{  
   "mcpServers": {  
      "Deep Graph MCP": {  
         "command": "npx",  
         "args": [  
            "-y",   
            "mcp-code-graph@latest",   
            "facebook/react"  
         ]  
      }  
   }  
}

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*Wz5ax7IwM4tB8myIerqe5A.png)

For my private project (needed a CodeGPT account):

{  
   "mcpServers": {  
      "Deep Graph MCP": {  
         "command": "npx",  
         "args": [  
            "-y",   
            "mcp-code-graph@latest",   
            "YOUR_CODEGPT_API_KEY",  
            "CODEGPT_API_KEY", // Required  
            "CODEGPT_ORG_ID", // Optional  
            "CODEGPT_GRAPH_ID" // Optional  
         ]  
      }  
   }  
}

Create a CodeGPT account for free in this link:

[

## Sign up for CodeGPT and empower your business with a team of AI co-pilots.

### Explore our AI Assistants and Copilot Generator Platform, tailored for businesses, personal use, and coding. We offer…

app.codegpt.co



](https://app.codegpt.co/en/signup?source=post_page-----ed981e89d64c---------------------------------------)

Get your API KEY and ORG ID in the section “API Connections”:

![](https://miro.medium.com/v2/resize:fit:1208/1*3mRbfoIcbh2bGCdekT2yVA.png)

And Upload your repo in Code Graph to get your CODEGPT_GRAPH_ID

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*YoGhaH0CQm7vJNt0kwt_6w.png)

With everything set up, you’ll be able to see the MCP and the tools already available.

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*4rjHqVntcSFNS3Cm-ZRaYA.png)

# Use the MCP

To use the MCP you just have to open the cursor chat and it will automatically detect the tools and show you a message to execute them.

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*lrvMWZsjTMfMEhj3ZFbZmw.png)

Full response directly from the React repository

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*Jq6T-tKfqSwtzsbsttKd5A.png)

# Real Examples from My Workflow

Here are actual scenarios where this made a difference:

**Refactoring a shared utility function:** Before: Manually grep through files to find usages Now: “What files use the `formatDate` function and how?" - instant comprehensive list

**Understanding authentication flow:** Before: Opening 8 different files to trace the login process Now: “Walk me through the complete user authentication flow” — clear explanation with relevant code snippets

**Finding code patterns:** Before: Hoping I’d remember where I implemented similar logic Now: “Show me other places where we handle async API calls with error boundaries” — found 3 similar patterns I’d forgotten about

# What Actually Changed

The biggest change isn’t the individual features — it’s that Cursor now feels like it actually understands my project. Instead of context-switching between files in my head, I can ask architectural questions and get intelligent answers.

It’s particularly useful when:

- Planning refactors (understanding impact)
- Code reviews (seeing broader implications)
- Feature development (finding existing patterns)
- Debugging (tracing through call chains)

# Small Issues I Hit

**Cursor sometimes doesn’t recognize the MCP** for searches. In this case, you have two options:

1. **Force MCP usage in your prompt**: If your MCP is named “Deep Graph MCP”, add to your prompt: “Use Deep Graph MCP to perform the following task…”

Press enter or click to view image in full size

![](https://miro.medium.com/v2/resize:fit:1400/1*t5hRWROhdSddTqsjAUJf3Q.png)

1. **Change the MCP name to your repository name**: In the JSON configuration, change “Deep Graph MCP” to your repository name. This way you can ask Cursor more naturally, like: “In Project A, create the Login component”

# Worth the Setup Time?

Absolutely. It took 5 minutes to set up and has already saved me hours of manually tracing through code. If you work with codebases larger than a few dozen files, this is a no-brainer.

The combination feels natural, Cursor’s AI capabilities with actual architectural understanding of your project.

# Try It Yourself

1. Pick a repository (start with a public one to test)
2. Add the MCP configuration to Cursor
3. Ask it some architectural questions about your code
4. See how it changes your workflow

I’m planning to set this up for our other projects too. It’s one of those tools that makes you wonder how you worked without it.

_Get the tool here:_ [_Deep Graph MCP_](https://github.com/JudiniLabs/mcp-code-graph)