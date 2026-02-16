Here is a comprehensive transcription and summary of the video featuring David Attias and Pat Walls from Starter Story.

### **Summary of Main Points**

**The Business Case**
*   **The Creator:** David Attias, a former quant trader from France with no prior mobile app development background.
*   **The Product:** **Stoppr**, a mobile app designed to help Gen-Z women (ages 13–25) quit processed sugar.
*   **The Inspiration:** David watched a podcast featuring the founders of **Quittr**, an app making $200k/month helping people break bad habits. He decided to "clone" their success.
*   **The Revenue:** In 5 months, Stoppr reached **$12,000 Monthly Recurring Revenue (MRR)**, 60,000 downloads, and ~900 paying customers.

**The "Cloning" Playbook (Step-by-Step)**
1.  **Find a Successful App:** Look for apps making significant revenue in a specific niche (David used Sensor Tower to verify Quittr was making ~$300k/mo).
2.  **Validate the Niche:** Use Google Trends and TikTok/Instagram to see if people are searching for the problem (e.g., "Stop Sugar"). David realized no one was serving this market specifically in France.
3.  **The "1-to-1" Copy:** Download the target app and take screenshots of every single screen.
4.  **Replicate with AI:**
    *   Import screenshots into **Figma** (using plugins).
    *   Use **Cursor** (AI code editor) to prompt the code generation based on the screenshots (e.g., "Make the same screen with the same visual elements...").
    *   *Key Strategy:* David copied Quittr's onboarding flow exactly because it was proven to work, only changing the colors (black to pink) and the subject (adult content/vaping to sugar).
5.  **Backend:** Use **Firebase** for easy integration of authentication (Google/Apple login) and database management.

**The Tech Stack & Costs**
*   **Coding:** Cursor ($200/mo for business plan) + Figma (Design).
*   **Revenue Management:** RevenueCat (Free tier/Commission).
*   **Analytics:** Mixpanel (~$100/mo).
*   **Marketing/Influencers:** Topyappers.com (to scrape influencer data) + Lindy.ai (to email them).
*   **Ads:** Meta Ads and TikTok Spark Ads (~$500/mo).
*   **Profit Margin:** Approximately **35%**.

**Key Advice**
*   **Don't reinvent the wheel:** The fastest way to make money is to find a proven app and localize it or niche it down.
*   **Don't copy the niche, copy the mechanism:** Do not steal their customers. Take their successful UI/UX and business model, but apply it to a different problem or a different geographic market (e.g., localize an American app for France).

***

### **Video Transcription**

**00:00 - 01:05: Introduction**
**David Attias:** I cloned a very successful app and in first month of revenue I made $5,000.
**Pat Walls (Narrator):** This is David Attias, a guy from France who up until six months ago had a regular job. That was until he saw an opportunity that most people would miss.
**David:** I was very amazed by the fact they were making $200,000 per month after 3 months.
**Pat:** He came across a mobile app making $200,000 a month, but when he used it, he thought to himself:
**David:** "...I think I can build something better."
**Pat:** So in a couple weeks, he vibe-coded a one-for-one clone, changed the colors, switched the niche, and just six months later, he's gone from zero to $10,000 a month. I brought David onto the channel to break down his entire playbook. In this video, we'll dive into the app he cloned, how he built his new app in just a month, the key change he decided to make to it, and lastly, the exact playbook he would use if he had to start over today.

**01:06 - 02:17: What is Stoppr?**
**Pat:** All right David, welcome to the channel. Tell me about who you are, what you built, and what's your story?
**David:** My name is David. I made an app that makes $12,000 per month. I built that app five months ago. To find the idea for my app, I cloned a very successful app and in the first month of revenue, I made $5,000.
**Pat:** That's awesome. It is something that I've been hearing about recently, taking proven English or American apps and localizing them to different countries. I'd like to know a little bit more about your app. What does it do?
**David:** That's a mobile app for women to stop their sugar cravings. My customers are around 13 years old to 25. So like, mostly Gen-Z women. And we have a monthly subscription, a weekly, and we have the yearly with a trial as well.
**Pat:** Okay, cool. Some of the numbers behind this... how many downloads, users, revenue?
**David:** So in five months, I got 60,000 downloads. Paying customers, it's around 900 people. And I've been working mainly with French influencers because I noticed that there was no one in France doing something like to help women by cutting completely processed sugar.

**02:18 - 02:59: David’s Background & The Spark**
**Pat:** How do you even get into building mobile apps? I understand you don't come from a developer background.
**David:** I worked 8 years as a quant trader. So basically I was helping traders to figure out which stocks to buy. And then I discovered some famous indie hackers like Pieter Levels and Tibo from Hypefury... I got really inspired by their story because they were able to generate a lot of cash quickly. Five months ago, I was watching a podcast on YouTube with three teenagers, the founders of Quittr. And after month three, I heard they were making like $200,000 per month. I was envious, I was jealous, and that's what got me started to start Stoppr.

**03:00 - 03:56: Sponsor Segment (Rocket)**
*(Pat Walls promotes Rocket, a tool for generating full-stack apps from prompts and Figma designs).*

**03:57 - 05:12: Finding & Validating the Idea**
**Pat:** You took a proven app and you sort of made it 1% different and now you have a really successful app. How did you decide to clone that specific idea?
**David:** The way that I found this idea is from YouTube. I saw those three guys on YouTube... making $200,000 per month after three months. And then when I had downloaded the Quittr app, I was very pleased by their onboarding. And I did one exact copy of their onboarding because it was so good.
**Pat:** How did you know that *this* niche, quitting sugar, was an app that was worth building?
**David:** Basically what I did, I went on Google Trends. I was looking for keywords such as "stop sugar." And because for the past five years, the trend of those keywords is basically going higher and higher. And then I check for the same keywords on TikTok and Instagram. And I saw that they were a ton of women influencers in France... they were talking about quitting sugar. So I validated by keyword research and by content research on TikTok and Instagram.

**05:13 - 05:46: Building the App with AI**
**Pat:** Validated this idea. What's the next step? Building it.
**David:** So my first step was to download Quittr, to take some screenshot of each screen. What I wanted to see is basically how we go from screen one to screen two to screen three. So I was prompting with Cursor: "Please make the same screen with the same visual elements with the same color, with the same image." In two weeks and a half, I did one full copy of Quittr. And then to submit the first version of the app and to get the approval from Apple, that took me one week.

**05:47 - 07:03: The Playbook for 2025**
**Pat:** If you were to do this again, to start over from scratch, what would be your playbook to clone a successful app from the App Store in 2025?
**David:**
1.  **Find a very successful app.** Check if it's for a specific niche and if that app markets for specific regions. To confirm the revenue, go to Sensor Tower to have a sense of what the app is making as a monthly recurring revenue.
2.  **Research on TikTok and Google Trends** if it's a niche worth building.
3.  **Build the design.** Use a Figma plugin to import all those JPEG screenshots in Figma. You are going to see some discrepancies... but for 80% of all the screen, that's near perfect. There are some websites like *screensdesign.com* and *mobbin.com*... you can download all the screenshots from a given app with one click.
4.  **Backend.** The easiest is Firebase. Because with Firebase, that manages the connection with Google... email password, and with the Apple sign-in as well.
5.  **Connect.** Right now with Figma MCP, you can connect your Figma to Cursor. Before it took me like one month to do the MVP, right now with Figma MCP that would probably take me two weeks.

**07:04 - 08:07: Tech Stack, Costs & Margins**
**Pat:** Can you break down the full stack that Stoppr is built on?
**David:**
*   To vibe code, I use **Cursor**.
*   To track my revenue within the app, I am using **RevenueCat**.
*   To have some deeper analytics... I am using **Mixpanel**.
*   I am using a tool called **Topyappers.com**, which is basically a SaaS that scrapes TikTok and Instagram... to find the exact profiles that I need of influencers that worked with other apps before.
*   I will add the email that I find from Topyappers... and feed that into **Lindy.ai** (for outreach).
*   Since last week, ads on Meta and Spark Ads on TikTok.
**Pat:** What are the costs to run an app like this?
**David:** The ultra plan of Cursor cost $200/month. On Mixpanel, I paid around $100. And for the TikTok Spark Ads... around $500/month. After five months, my profit margin for Stoppr is 35%.

**08:08 - 10:04: Final Advice & Conclusion**
**Pat:** If you could stand on young David's shoulders before you started your app... what would be your advice?
**David:** Fastest way to make some money right now is to do one-to-one copy of a very successful app. And when I say one-to-one copy, I mean you copy each screen, you copy each word. I changed black by pink. I replaced adult content with sugar. And then done.
**Pat:** I agree. I think it's totally okay to copy an app... The only thing that I think that you shouldn't do is don't copy the niche. You don't want to go and take their customers. Take everything they did and put it to a different niche. Thank you David for coming on sharing everything.