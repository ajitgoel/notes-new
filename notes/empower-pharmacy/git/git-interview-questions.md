# Git Interview Questions & Answers

This document contains a comprehensive list of Git interview questions, categorized by difficulty, with detailed answers.

---

## 🟢 Basic Level

### 1. What is Git?
**Answer:**
Git is a distributed version control system (DVCS). It allows developers to track changes in their code over time, collaborate with others, and maintain multiple versions of a project. Unlike centralized systems (like SVN), every developer has a full copy of the repository and its history on their local machine.

### 2. What is the difference between `git add` and `git commit`?
**Answer:**
- **`git add`**: Adds changes in the working directory to the **staging area** (index). It tells Git which changes you want to include in the next snapshot.
- **`git commit`**: Takes the staged changes and records them in the repository's history as a new snapshot with a unique SHA-1 hash.

### 3. What is a "Repository" in Git?
**Answer:**
A repository (or "repo") is a directory that contains all the project files and the history of changes made to those files. It is stored in a hidden `.git` folder within the project.

### 4. How do you initialize a new Git repository?
**Answer:**
Use the command:
```bash
git init
```
This creates a `.git` subdirectory in your current directory.

### 5. What is the purpose of `.gitignore`?
**Answer:**
The `.gitignore` file specifies intentionally untracked files that Git should ignore. This usually includes build artifacts (`/bin`, `/dist`), dependencies (`node_modules/`), environment variables (`.env`), and OS-specific files (`.DS_Store`).

---

## 🟡 Intermediate Level

### 6. What is the difference between `git pull` and `git fetch`?
**Answer:**
- **`git fetch`**: Downloads the latest changes from the remote repository but **does not merge** them into your local branch. It only updates your remote-tracking branches.
- **`git pull`**: A combination of `git fetch` followed by `git merge`. It downloads changes and immediately tries to integrate them into your current branch.

### 7. What is `git stash` and when should you use it?
**Answer:**
`git stash` temporarily shelves (or "stashes") changes you've made to your working directory so you can work on something else, and then come back and re-apply them later.
**Use case:** You are in the middle of a feature but need to switch to a different branch to fix a bug immediately. You stash your work, switch branches, and later return and `git stash pop`.

### 8. What is the difference between `git merge` and `git rebase`?
**Answer:**
- ==**`git merge`**: Combines the changes from one branch into another, creating a new "merge commit." It preserves the historical chronology of when changes happened.==
- **`git rebase`**: Moves or combines a sequence of commits to a new base commit. It "rewrites" history by moving the entire branch to begin from the tip of another branch, resulting in a cleaner, linear history.

### 9. What is a "Merge Conflict"?
**Answer:**
A merge conflict occurs when Git is unable to automatically resolve differences in code between two commits being merged. This usually happens when the same line in the same file is modified differently in both branches.

### 10. How do you see the history of commits in Git?
**Answer:**
Use the command:
```bash
git log
```
For a more concise view:
```bash
git log --oneline --graph --decorate
```

---

## 🔴 Advanced Level

### 11. What is `git reflog`?
**Answer:**
`git reflog` (Reference Logs) records every time the tip of a branch is updated in your local repository. It allows you to find commits that are no longer referenced by any branch or tag, making it a "safety net" for recovering lost work or undoing an accidental hard reset.

### 12. Explain the difference between `git reset --soft`, `--mixed`, and `--hard`.
**Answer:**
- **`--soft`**: Moves HEAD to a specific commit. Changes in the staging area and working directory are **preserved**.
- **`--mixed` (default)**: Moves HEAD and resets the staging area. Changes are **preserved in the working directory** but are unstaged.
- **`--hard`**: Moves HEAD, resets the staging area, and resets the working directory. **All uncommitted changes are lost.**

### 13. What is `git cherry-pick`?
**Answer:**
==`git cherry-pick` allows you to apply the changes introduced by an existing commit from one branch onto another branch. It creates a new commit with the same changes but a different hash.==

### 14. How do you fix a commit message that has already been pushed?
**Answer:**
1. Fix it locally: `git commit --amend -m "New message"`
2. Force push (carefully!): `git push --force` (or `git push --force-with-lease`)
*Note: Avoid force-pushing to shared branches as it disrupts other developers' history.*

### 15. What is a "Detached HEAD"?
**Answer:**
A "Detached HEAD" state occurs when you check out a specific commit, tag, or remote branch instead of a local branch. In this state, any new commits you make won't belong to any branch and can be easily lost unless you create a new branch from them.

---

## 💡 Practical Scenarios

### Scenario: You accidentally committed a large sensitive file (like an API key). How do you remove it from history?
**Answer:**
You can use `git filter-branch` or the more modern tool **BFG Repo-Cleaner**. Alternatively, use `git rebase -i` to delete the commit if it's recent, or `git rm --cached` followed by an amend if it was the last commit.

### Scenario: You want to combine multiple commits into one.
**Answer:**
Use interactive rebase:
```bash
git rebase -i HEAD~N
```
(where N is the number of commits). Then change `pick` to `squash` (or `s`) for the commits you want to combine.
