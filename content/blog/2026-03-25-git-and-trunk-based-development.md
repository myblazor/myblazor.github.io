---
title: "Git From First Principles, and Why Trunk-Based Development Will Save Your Team"
date: 2026-03-25
author: myblazor-team
summary: A comprehensive deep dive into Git as a version control system — every command, every workflow, every configuration. Then, a persuasive case for trunk-based development aimed at teams reluctant to leave long-lived branches behind. Backed by a decade of DORA research.
tags:
  - git
  - version-control
  - trunk-based-development
  - devops
  - ci-cd
  - best-practices
  - dotnet
featured: true
---

## Part 1: Git From First Principles

### What Is Version Control, and Why Does It Exist?

Before version control systems existed, developers maintained multiple copies of their source code by hand — renaming folders to things like `project-v2-final-FINAL-fixed` and hoping they could remember which copy was which. When two developers needed to work on the same file, they would shout across the office or send emails with zipped attachments. This was expensive, error-prone, and utterly unsustainable.

Version control systems solve this by tracking every change to every file over time, allowing multiple people to work on the same codebase simultaneously, and providing the ability to revert to any previous state. Git is the dominant version control system today, with approximately 85% market share among software development teams.

### A Brief History: From Locks to Distributed Merging

Version control evolved through three generations, each expanding the ability to work in parallel.

**First generation (1970s–1980s):** Systems like SCCS and RCS used a lock-edit-unlock model. Only one person could edit a file at a time. Everyone else had to wait. This was safe but slow.

**Second generation (1990s–2000s):** Systems like CVS, Subversion (SVN), and Team Foundation Version Control (TFVC — the version control component of TFS/Azure DevOps) introduced a centralized server model with merge-based concurrent editing. Multiple people could edit the same file simultaneously, and the system would merge their changes. But you needed a network connection to the central server for most operations — committing, viewing history, branching.

**Third generation (2005–present):** Distributed systems like Git and Mercurial gave every developer a complete copy of the entire repository, including its full history. You can commit, branch, view history, and diff entirely offline. You synchronize with teammates by pushing and pulling changesets between repositories. Linus Torvalds created Git in 2005 specifically for Linux kernel development, where thousands of developers needed to work independently across time zones without a single point of failure.

### How Git Thinks: Snapshots, Not Diffs

Most version control systems store data as a list of file-based changes (deltas). Git is fundamentally different — it thinks of its data as a series of **snapshots** of the entire project at each point in time. When you commit, Git takes a snapshot of every file in your staging area and stores a reference to that snapshot. If a file has not changed, Git does not store it again; it stores a pointer to the previous identical file.

Every piece of data in Git is checksummed with SHA-1 (or SHA-256 in newer versions) before it is stored. This means Git knows if any file has been corrupted or tampered with. You cannot change the contents of any file or directory without Git knowing.

### The Three States

Every file in a Git working directory exists in one of three states:

**Modified** means you have changed the file in your working directory but have not staged it yet.

**Staged** means you have marked a modified file to be included in your next commit snapshot.

**Committed** means the data is safely stored in your local Git database.

This gives rise to the three main sections of a Git project:

1. **Working Directory** — the actual files on your disk
2. **Staging Area** (also called the "index") — a file that stores information about what will go into your next commit
3. **Git Directory** (the `.git` folder) — where Git stores the metadata and object database for your project

The basic Git workflow is: you modify files in your working directory, you stage the changes you want to include, and then you commit, which takes the staged snapshot and stores it permanently in the Git directory.

## Part 2: Every Command You Need

### Setup and Configuration

Before your first commit, configure your identity:

```bash
# Set your name and email (stored in commits)
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# Set default branch name to 'main'
git config --global init.defaultBranch main

# Set default editor (for commit messages)
git config --global core.editor "code --wait"  # VS Code
git config --global core.editor "vim"           # Vim
git config --global core.editor "notepad"       # Notepad on Windows

# Enable colored output
git config --global color.ui auto

# Set line ending behavior
git config --global core.autocrlf true   # Windows (converts LF to CRLF)
git config --global core.autocrlf input  # Mac/Linux (converts CRLF to LF on commit)

# View all configuration
git config --list --show-origin
```

Git configuration has three levels, each overriding the previous:

- **System** (`/etc/gitconfig`) — applies to every user on the machine
- **Global** (`~/.gitconfig`) — applies to your user account
- **Local** (`.git/config` in a repository) — applies to that specific repository

### Creating and Cloning Repositories

```bash
# Initialize a new repository in the current directory
git init

# Initialize a new repository in a new directory
git init my-project

# Clone an existing repository
git clone https://github.com/user/repo.git

# Clone into a specific directory
git clone https://github.com/user/repo.git my-local-name

# Clone only the most recent commit (shallow clone, saves bandwidth)
git clone --depth 1 https://github.com/user/repo.git

# Clone a specific branch
git clone --branch develop https://github.com/user/repo.git
```

### Staging and Committing

```bash
# Check the status of your files
git status

# Short status (more compact output)
git status -s

# Stage a specific file
git add README.md

# Stage multiple specific files
git add file1.cs file2.cs file3.cs

# Stage all changes in a directory
git add src/

# Stage all changes in the entire repository
git add .

# Stage all tracked files that have been modified (ignores new untracked files)
git add -u

# Interactively stage parts of files (choose which hunks to stage)
git add -p

# Unstage a file (remove from staging area, keep changes in working directory)
git restore --staged README.md

# Discard changes in working directory (DANGEROUS — cannot be undone)
git restore README.md

# Commit staged changes with a message
git commit -m "Add user authentication module"

# Commit with a multi-line message
git commit -m "Add user authentication module" -m "Implements JWT-based auth with refresh tokens.
Closes #42."

# Stage all tracked modified files AND commit in one step
git commit -am "Fix null reference in OrderService"

# Amend the most recent commit (change message or add forgotten files)
git add forgotten-file.cs
git commit --amend -m "Add user authentication module (with tests)"

# Amend without changing the message
git commit --amend --no-edit

# Create an empty commit (useful for triggering CI)
git commit --allow-empty -m "Trigger CI rebuild"
```

### Viewing History

```bash
# View commit log
git log

# Compact one-line format
git log --oneline

# Show a graph of branches
git log --oneline --graph --all

# Show the last 5 commits
git log -5

# Show commits that changed a specific file
git log -- src/Program.cs

# Show commits by a specific author
git log --author="Alice"

# Show commits containing a search term in the message
git log --grep="authentication"

# Show commits between two dates
git log --after="2026-01-01" --before="2026-03-01"

# Show the diff introduced by each commit
git log -p

# Show stats (files changed, insertions, deletions)
git log --stat

# Show a pretty custom format
git log --pretty=format:"%h %ad | %s%d [%an]" --date=short

# Find which commit introduced a specific line of code
git log -S "connectionString" --oneline

# Show who last modified each line of a file (blame)
git blame src/Services/AuthService.cs

# Show blame for a specific range of lines
git blame -L 10,20 src/Services/AuthService.cs
```

### Branching

Branches in Git are incredibly lightweight — a branch is simply a pointer (a 41-byte file) to a specific commit. Creating a branch is nearly instantaneous regardless of repository size.

```bash
# List local branches
git branch

# List all branches (including remote-tracking branches)
git branch -a

# List branches with their last commit
git branch -v

# Create a new branch (does NOT switch to it)
git branch feature/user-profile

# Create a new branch AND switch to it
git checkout -b feature/user-profile
# Modern equivalent (Git 2.23+):
git switch -c feature/user-profile

# Switch to an existing branch
git checkout main
# Modern equivalent:
git switch main

# Rename a branch
git branch -m old-name new-name

# Rename the current branch
git branch -m new-name

# Delete a branch (only if fully merged)
git branch -d feature/user-profile

# Force delete a branch (even if not merged — DANGEROUS)
git branch -D feature/user-profile

# Delete a remote branch
git push origin --delete feature/user-profile
```

### Merging

```bash
# Merge a branch into the current branch
git merge feature/user-profile

# Merge with a merge commit even if fast-forward is possible
git merge --no-ff feature/user-profile

# Abort a merge in progress (if there are conflicts)
git merge --abort

# Continue a merge after resolving conflicts
git add .  # Stage the resolved files
git merge --continue
# Or equivalently:
git commit
```

**Fast-forward merge** happens when the target branch has no new commits since the feature branch was created. Git simply moves the pointer forward. No merge commit is created.

**Three-way merge** happens when both branches have diverged. Git creates a new "merge commit" with two parents.

**Merge conflicts** occur when the same lines in the same file were modified differently in both branches. Git marks these in the file:

```
<<<<<<< HEAD
    return user.GetFullName();
=======
    return $"{user.FirstName} {user.LastName}";
>>>>>>> feature/user-profile
```

You resolve the conflict by editing the file to the desired final state, removing the markers, staging the file, and completing the merge.

### Rebasing

Rebase is an alternative to merging. Instead of creating a merge commit, it replays your commits on top of the target branch, creating a linear history.

```bash
# Rebase current branch onto main
git rebase main

# Interactive rebase — edit, squash, reorder, or drop commits
git rebase -i main

# Interactive rebase of the last 3 commits
git rebase -i HEAD~3

# Abort a rebase in progress
git rebase --abort

# Continue after resolving a conflict during rebase
git add .
git rebase --continue

# Skip a problematic commit during rebase
git rebase --skip
```

In interactive rebase (`git rebase -i`), you get an editor showing commits with action keywords:

```
pick abc1234 Add user model
pick def5678 Add user service
pick ghi9012 Fix typo in user service

# Commands:
# p, pick = use commit
# r, reword = use commit, but edit the message
# e, edit = use commit, but stop for amending
# s, squash = use commit, but meld into previous commit
# f, fixup = like squash, but discard this commit's message
# d, drop = remove commit
```

**The golden rule of rebasing:** Never rebase commits that have been pushed to a shared branch that others are working from. Rebasing rewrites commit history — if someone else has based their work on the original commits, their history will diverge from yours, causing confusion and pain.

### Remote Repositories

```bash
# List remotes
git remote -v

# Add a remote
git remote add origin https://github.com/user/repo.git

# Add a second remote (e.g., a fork)
git remote add upstream https://github.com/original/repo.git

# Change a remote's URL
git remote set-url origin https://github.com/user/new-repo.git

# Remove a remote
git remote remove upstream

# Fetch changes from a remote (does NOT merge)
git fetch origin

# Fetch from all remotes
git fetch --all

# Pull (fetch + merge) from the remote
git pull origin main

# Pull with rebase instead of merge
git pull --rebase origin main

# Push to a remote
git push origin main

# Push and set upstream tracking
git push -u origin feature/user-profile

# Push all branches
git push --all origin

# Push tags
git push --tags

# Force push (DANGEROUS — overwrites remote history)
git push --force origin feature/user-profile

# Force push with safety (only overwrites if remote hasn't changed)
git push --force-with-lease origin feature/user-profile
```

### Stashing

Stash temporarily shelves changes so you can work on something else:

```bash
# Stash all modified tracked files
git stash

# Stash with a description
git stash push -m "WIP: halfway through refactoring auth"

# Stash including untracked files
git stash -u

# List all stashes
git stash list

# Apply the most recent stash (keeps it in stash list)
git stash apply

# Apply a specific stash
git stash apply stash@{2}

# Apply and remove from stash list
git stash pop

# Drop a specific stash
git stash drop stash@{0}

# Clear all stashes
git stash clear

# Create a branch from a stash
git stash branch new-branch-name stash@{0}
```

### Tagging

Tags are permanent bookmarks for specific commits, typically used for releases:

```bash
# List tags
git tag

# List tags matching a pattern
git tag -l "v1.*"

# Create a lightweight tag (just a pointer)
git tag v1.0.0

# Create an annotated tag (stores tagger info, date, message)
git tag -a v1.0.0 -m "Release version 1.0.0"

# Tag a specific commit
git tag -a v1.0.0 abc1234 -m "Release version 1.0.0"

# Push a specific tag
git push origin v1.0.0

# Push all tags
git push origin --tags

# Delete a local tag
git tag -d v1.0.0

# Delete a remote tag
git push origin --delete v1.0.0
```

### Undoing Things

```bash
# Undo the last commit, keep changes staged
git reset --soft HEAD~1

# Undo the last commit, keep changes in working directory (unstaged)
git reset --mixed HEAD~1  # --mixed is the default

# Undo the last commit, DISCARD all changes (DANGEROUS)
git reset --hard HEAD~1

# Reset a single file to the last committed version
git checkout HEAD -- src/Program.cs
# Modern equivalent:
git restore src/Program.cs

# Create a new commit that reverses a previous commit
# (safe for shared branches — doesn't rewrite history)
git revert abc1234

# Revert a merge commit (must specify which parent to keep)
git revert -m 1 abc1234

# Recover a "lost" commit (Git keeps everything for ~30 days)
git reflog
git checkout abc1234  # or git cherry-pick abc1234
```

### Cherry-Picking

Apply a specific commit from one branch to another:

```bash
# Apply a single commit to the current branch
git cherry-pick abc1234

# Apply multiple commits
git cherry-pick abc1234 def5678

# Cherry-pick without committing (just stage the changes)
git cherry-pick --no-commit abc1234

# Abort a cherry-pick
git cherry-pick --abort
```

### Advanced: Bisect, Clean, Archive

```bash
# Binary search for a bug-introducing commit
git bisect start
git bisect bad          # Current commit is broken
git bisect good v1.0.0  # This tag was known good
# Git checks out the middle commit. Test it, then:
git bisect good  # if this commit works
git bisect bad   # if this commit is broken
# Repeat until Git identifies the exact commit
git bisect reset  # Return to your original branch

# Remove untracked files
git clean -n    # Dry run (show what would be deleted)
git clean -f    # Actually delete untracked files
git clean -fd   # Delete untracked files and directories
git clean -fX   # Delete only ignored files (clean build artifacts)

# Create an archive of the repository
git archive --format=zip HEAD > project.zip
git archive --format=tar.gz --prefix=project/ HEAD > project.tar.gz
```

### The .gitignore File

`.gitignore` tells Git which files and directories to never track:

```gitignore
# Compiled output
bin/
obj/
publish/
*.dll
*.exe
*.pdb

# IDE files
.vs/
.vscode/
*.user
*.suo
.idea/

# OS files
.DS_Store
Thumbs.db

# Environment and secrets
.env
appsettings.Development.json

# NuGet packages
packages/

# Python
__pycache__/
*.pyc
.venv/

# Node
node_modules/

# Logs
*.log

# Negate a pattern (force include something that would otherwise be ignored)
!important.log
```

Patterns work as follows:

- `*.log` matches any file ending in `.log`
- `bin/` matches a directory named `bin` anywhere in the repo
- `/bin/` matches `bin` only at the repository root
- `**/logs` matches `logs` directories anywhere in the hierarchy
- `!` negates a pattern (force includes something)

### Git Aliases

Create shortcuts for frequently used commands:

```bash
git config --global alias.co checkout
git config --global alias.br branch
git config --global alias.ci commit
git config --global alias.st status
git config --global alias.unstage "restore --staged"
git config --global alias.last "log -1 HEAD"
git config --global alias.lg "log --oneline --graph --all --decorate"
git config --global alias.amend "commit --amend --no-edit"
```

Now `git lg` gives you a beautiful branch graph, `git co main` switches to main, and `git amend` amends the last commit without changing the message.

### Git Hooks

Git hooks are scripts that run automatically at certain points in the Git workflow. They live in `.git/hooks/` (local, not committed) or can be managed with tools like Husky or pre-commit.

Common hooks:

- `pre-commit` — runs before a commit is created (lint, format, run fast tests)
- `commit-msg` — validates the commit message format
- `pre-push` — runs before pushing (run full test suite)
- `post-merge` — runs after a merge (restore NuGet packages, run migrations)

Example `pre-commit` hook that runs dotnet format:

```bash
#!/bin/sh
# .git/hooks/pre-commit

dotnet format --verify-no-changes
if [ $? -ne 0 ]; then
    echo "Code formatting issues found. Run 'dotnet format' to fix."
    exit 1
fi
```

## Part 3: Branching Workflows

### Gitflow (The Heavyweight)

Gitflow, popularized by Vincent Driessen in 2010, uses multiple long-lived branches:

- `main` (or `master`) — always reflects production
- `develop` — integration branch for the next release
- `feature/*` — one branch per feature, branched from and merged back to `develop`
- `release/*` — preparation for a production release, branched from `develop`, merged to both `main` and `develop`
- `hotfix/*` — urgent production fixes, branched from `main`, merged to both `main` and `develop`

Gitflow was designed for projects with scheduled releases and multiple supported versions. It provides strict control but at the cost of significant complexity. Dave Farley, co-author of *Continuous Delivery*, has argued publicly that Gitflow contradicts CI/CD principles because it delays integration and introduces complexity that slows teams down.

### GitHub Flow (The Lightweight)

GitHub Flow is a simplified model:

1. `main` is always deployable
2. Create a branch from `main` for your work
3. Make commits on your branch
4. Open a pull request
5. Get code review
6. Merge to `main`
7. Deploy

This is simpler than Gitflow but still relies on feature branches that can become long-lived if the developer does not merge frequently.

### Trunk-Based Development (The Streamlined)

Trunk-based development is the simplest model. There is one branch: `main` (the trunk). All developers commit to the trunk at least once every 24 hours. There are no long-lived branches. For teams that need code review, short-lived feature branches (lasting hours or at most a day or two) are used, but they are merged to trunk quickly.

This is what we are going to argue for in Part 4.

## Part 4: The Case for Trunk-Based Development

This section is for the team that is hesitant. You have been using TFS (Team Foundation Server, now Azure DevOps) for years. Your workflow involves multiple long-lived branches — a `develop` branch, release branches, feature branches that live for weeks or months, and hotfix branches. You have code spanning multiple sprints. You know your current workflow. It works, mostly. Why change?

Because the evidence says you should.

### The Evidence: DORA and Accelerate

The DevOps Research and Assessment (DORA) program, founded by Dr. Nicole Forsgren, Gene Kim, and Jez Humble and now part of Google Cloud, is the largest and longest-running academically rigorous research investigation into software delivery performance. Since 2014, their annual State of DevOps reports have surveyed tens of thousands of professionals across thousands of organizations.

Their findings, published in the book *Accelerate: The Science of Lean Software and DevOps* (2018), are unambiguous: trunk-based development is a statistically significant predictor of higher software delivery performance.

DORA measures performance using five key metrics: deployment frequency (how often you deploy to production), lead time for changes (how long from commit to production), change failure rate (what percentage of deployments cause failures), failed deployment recovery time (how quickly you fix failures), and reliability (how consistently your service meets performance goals).

Their research has consistently shown that speed and stability are not tradeoffs — elite performers do well across all five metrics, while low performers do poorly across all of them. This directly contradicts the intuition that moving faster means more breakage.

Elite performers who meet their reliability targets are 2.3 times more likely to practice trunk-based development than their peers. Elite performing teams deploy multiple times per day, have change lead times under 26 hours, maintain change failure rates below 1%, and recover from failures in less than 6 hours.

The research is clear: organizations that practice trunk-based development with continuous integration achieve higher delivery throughput AND higher stability than organizations using long-lived feature branches.

### Why Long-Lived Branches Are an Antipattern

Every day a branch lives, it accumulates divergence from the trunk. This divergence creates three escalating problems.

**Merge conflicts grow exponentially.** When two developers both modify the same module over the course of a sprint, the number of potential conflicts grows with each passing day. A branch that lives for two weeks will have significantly more conflicts than one that lives for two hours. These are not just textual conflicts that Git can flag — they are semantic conflicts where the code merges cleanly but the behavior is wrong. Your tests might pass individually on each branch but fail when the branches are combined. The longer you wait to integrate, the harder and riskier the integration becomes.

**Feedback is delayed.** When your code sits on a feature branch for three weeks, nobody else sees it. Nobody uses it. Nobody discovers that it conflicts with what they are building. Nobody discovers that it breaks a subtle assumption in another module. You do not learn about these problems until merge day, when it is hardest and most expensive to fix them. Thierry de Pauw, writing about trunk-based development benefits, makes this point forcefully: when you work on trunk, your work-in-progress gets used by your whole team before any actual user sees it, and they find bugs that they would never find if you were isolated on a feature branch.

**Integration becomes a terrifying event.** When you merge a branch that has been alive for weeks, the merge is large, risky, and stressful. This is what the DevOps Handbook calls "deployment pain" — the anxiety that comes with pushing large batches of changes. Teams that experience this pain naturally merge less often, which makes each merge even larger and more painful. It is a vicious cycle.

Martin Fowler, in his comprehensive article on branching patterns, observes that "feature branching is a poor man's modular architecture, instead of building systems with the ability to easily swap in and out features at runtime/deploytime they couple themselves to the source control providing this mechanism through manual merging." In other words, long-lived branches are often a symptom of poor architecture, not a solution to it.

### "But Our Features Span Multiple Sprints!"

This is the most common objection, and it reveals a fundamental misunderstanding. Trunk-based development does not mean you cannot work on large features. It means you do not use long-lived branches to isolate that work. Instead, you use two techniques: feature flags and branch by abstraction.

#### Feature Flags

A feature flag (also called a feature toggle) is a conditional in your code that controls whether a feature is visible to users. You merge your work-in-progress to trunk behind a flag. The code is in production, running through CI, being integrated with everyone else's work — but users do not see it until you flip the flag.

In a .NET application, this can be as simple as:

```csharp
// A simple feature flag using configuration
public class FeatureFlags
{
    public bool EnableNewCheckoutFlow { get; set; }
    public bool EnableAdvancedSearch { get; set; }
    public bool EnableBulkImport { get; set; }
}

// In Program.cs / Startup
builder.Services.Configure<FeatureFlags>(
    builder.Configuration.GetSection("Features"));

// In your service or controller
public class CheckoutService
{
    private readonly FeatureFlags _flags;

    public CheckoutService(IOptions<FeatureFlags> flags) =>
        _flags = flags.Value;

    public async Task<Order> ProcessCheckout(Cart cart)
    {
        if (_flags.EnableNewCheckoutFlow)
            return await ProcessNewCheckout(cart);
        else
            return await ProcessLegacyCheckout(cart);
    }
}
```

```json
// appsettings.json (production — flag off)
{
  "Features": {
    "EnableNewCheckoutFlow": false,
    "EnableAdvancedSearch": false,
    "EnableBulkImport": true
  }
}
```

```json
// appsettings.Development.json (local dev — flag on)
{
  "Features": {
    "EnableNewCheckoutFlow": true,
    "EnableAdvancedSearch": true,
    "EnableBulkImport": true
  }
}
```

Martin Fowler categorizes feature flags into several types: release toggles (to hide incomplete features), experiment toggles (for A/B testing), ops toggles (to disable features under load), and permission toggles (to enable features for specific users). Release toggles — the type most relevant to trunk-based development — should be short-lived. Once a feature is complete and released, remove the flag. Pete Hodgson, writing on martinfowler.com, warns that feature flags have a carrying cost and should be treated as inventory — teams should proactively work to keep the number of active flags as low as possible. Knight Capital Group's famous $460 million loss is a cautionary tale about what happens when old feature flags are not cleaned up.

#### Branch by Abstraction

Branch by abstraction, a technique named by Paul Hammant and documented extensively by Martin Fowler, is for large-scale infrastructure changes — replacing a database, swapping an ORM, rewriting a major subsystem. The idea is to create an abstraction layer (an interface) between the code that uses a component and the component itself, then gradually swap out the implementation behind that abstraction.

Here is a concrete .NET example. Suppose you are migrating from Dapper to Entity Framework:

```csharp
// Step 1: Create the abstraction
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Order>> GetRecentAsync(int count);
    Task CreateAsync(Order order);
    Task UpdateAsync(Order order);
}

// Step 2: Wrap the existing Dapper implementation
public class DapperOrderRepository : IOrderRepository
{
    private readonly IDbConnection _db;

    public DapperOrderRepository(IDbConnection db) => _db = db;

    public async Task<Order?> GetByIdAsync(Guid id) =>
        await _db.QueryFirstOrDefaultAsync<Order>(
            "SELECT * FROM Orders WHERE Id = @Id", new { Id = id });

    // ... other methods using Dapper
}

// Step 3: Build the new EF implementation alongside it
public class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public EfOrderRepository(AppDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(Guid id) =>
        await _context.Orders.FindAsync(id);

    // ... other methods using EF
}

// Step 4: Use a feature flag to switch between them
builder.Services.AddScoped<IOrderRepository>(sp =>
{
    var flags = sp.GetRequiredService<IOptions<FeatureFlags>>().Value;
    return flags.UseEntityFramework
        ? sp.GetRequiredService<EfOrderRepository>()
        : sp.GetRequiredService<DapperOrderRepository>();
});
```

Every step is a small, mergeable commit to trunk. At no point is the codebase broken. You can release at any time. The old and new implementations coexist. Once the migration is complete and verified, you remove the old implementation, the flag, and optionally the abstraction layer.

Jez Humble describes how his team at ThoughtWorks used this technique to replace both an ORM (iBatis to Hibernate) and a web framework (Velocity/JsTemplate to Ruby on Rails) for the Go continuous delivery tool — all while continuing to release the application regularly.

### "But What About Production Hotfixes?"

This is actually easier with trunk-based development, not harder.

In a Gitflow model, a hotfix requires: creating a branch from `main`, making the fix, merging back to `main`, tagging a release, and then merging back to `develop` (and possibly to every active release branch and feature branch). Miss a branch and you have a fix that is in production but not in development.

In trunk-based development: you make the fix on trunk (or a very short-lived branch that is merged to trunk within hours), and it deploys through your normal pipeline. There is only one branch, so there is no question of whether the fix is everywhere — it is.

If you need to patch an older release, you use release branches — but these are not long-lived development branches. They are cut from trunk at release time and receive only cherry-picked critical fixes. They are maintenance branches, not development branches.

```bash
# Cut a release branch when ready to release
git checkout -b release/1.0 main
git tag v1.0.0

# Later, if a hotfix is needed:
# First, fix it on trunk
git checkout main
git commit -am "Fix critical payment processing bug (#789)"

# Then cherry-pick to the release branch
git checkout release/1.0
git cherry-pick abc1234
git tag v1.0.1
```

### "We're Not Google. We Can't Do This."

This is a common reflexive objection, and it is backwards. Google has 35,000 developers working in a single monorepo trunk. If trunk-based development scales to that, it certainly scales to your team.

But more importantly, trunk-based development actually scales down better than Gitflow. A small team benefits enormously from the simplicity. You do not need to maintain multiple long-lived branches, you do not need complex merge strategies, and you do not need to understand a complicated branching model. There is one branch. Everyone commits to it. Done.

Netflix, Microsoft (for many products), Google, Facebook (Meta), Amazon, Etsy, and Flickr all practice trunk-based development at scale. Etsy famously deploys to production more than 50 times per day.

Thierry de Pauw documents that trunk-based development has been successfully adopted by highly regulated industries including healthcare, gambling, and finance. The objection that "this cannot work for regulated industries" or "this cannot work for large systems" has been empirically disproven.

### "Our Developers Are Not Ready for This."

In a long-lived-branch workflow, developer mistakes are hidden on isolated branches until merge day, when they become everyone's problem simultaneously. In trunk-based development, mistakes are caught immediately because CI runs on every commit and the whole team sees the changes within hours.

The trunk-based model is actually more forgiving, not less. If you break something, you find out in minutes (because CI caught it or a teammate noticed), not in weeks (because the branch finally merged). The blast radius of any single commit is small because commits are small.

The real question is not whether your developers are ready but whether you trust them. Thierry de Pauw makes a profound point: pull requests in a corporate setting essentially indicate that the team owns the codebase but is not allowed to contribute. This creates a low-trust environment. Trunk-based development, where everyone commits to trunk, creates a high-trust environment. It reduces fear and blame. It is the team that owns quality, not individuals.

### Practical Steps to Adopt Trunk-Based Development

If you are currently using long-lived branches and want to migrate, do not try to change everything at once. Here is a gradual adoption path:

**Week 1–2: Shorten branch lifetimes.** Adopt a team rule: no branch lives longer than two days. If your work takes longer than that, break it into smaller pieces. Use feature flags to hide incomplete work.

**Week 3–4: Improve CI.** Your CI pipeline must be fast and reliable. If it takes 30 minutes to run, developers will avoid committing frequently. Aim for a pipeline that completes in under 10 minutes. Run unit tests on every commit. Run integration tests on every merge to trunk.

**Week 5–6: Add feature flags infrastructure.** Start simple — configuration-based flags in `appsettings.json`. You do not need a commercial feature flag service. As your needs grow, consider tools like Microsoft.FeatureManagement (free, open source).

```csharp
// Using Microsoft.FeatureManagement (MIT licensed, free)
// Install: dotnet add package Microsoft.FeatureManagement.AspNetCore

builder.Services.AddFeatureManagement();

// In appsettings.json:
{
  "FeatureManagement": {
    "NewDashboard": false,
    "BetaSearch": true
  }
}

// In a controller or Razor page:
public class DashboardController : Controller
{
    private readonly IFeatureManager _features;

    public DashboardController(IFeatureManager features) =>
        _features = features;

    public async Task<IActionResult> Index()
    {
        if (await _features.IsEnabledAsync("NewDashboard"))
            return View("DashboardV2");
        else
            return View("Dashboard");
    }
}
```

**Week 7–8: Delete long-lived branches.** Merge or close every branch that is more than a few days old. Going forward, all new work happens on trunk (or very short-lived branches from trunk).

**Ongoing: Build the muscle.** Trunk-based development is a skill. It gets easier with practice. Developers learn to make smaller, more focused commits. They learn to think about how to decompose large features into small, independently deployable pieces. This is not just a version control technique — it is a design discipline that makes your software more modular and your team more effective.

### Configuration for a Trunk-Based .NET Repository

Here is how to configure a repository to enforce trunk-based practices:

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

      - name: Format check
        run: dotnet format --verify-no-changes
```

On GitHub, configure branch protection rules for `main`:

- Require pull request reviews before merging (1 reviewer is enough — keep it lightweight)
- Require status checks to pass (CI must be green)
- Require branches to be up to date before merging
- Automatically delete head branches after merge

These rules ensure quality without creating bottlenecks.

## Part 5: Git Configuration Reference

### Useful Global Configuration

```bash
# Rebase by default when pulling (avoids unnecessary merge commits)
git config --global pull.rebase true

# Auto-stash before rebase (saves uncommitted work automatically)
git config --global rebase.autoStash true

# Always push the current branch
git config --global push.default current

# Show more context in diffs
git config --global diff.context 5

# Use histogram diff algorithm (better results for many code changes)
git config --global diff.algorithm histogram

# Remember conflict resolutions (if you resolve the same conflict twice, Git remembers)
git config --global rerere.enabled true

# Prune remote-tracking branches on fetch
git config --global fetch.prune true

# Sign commits with GPG (optional but recommended for open source)
git config --global commit.gpgsign true
git config --global user.signingkey YOUR_GPG_KEY_ID

# Better diff for C# files
git config --global diff.csharp.xfuncname "^[ \t]*(((static|public|internal|private|protected|new|virtual|sealed|override|unsafe|async|partial)[ \t]+)*[][<>@.~_[:alnum:]]+[ \t]+[<>@._[:alnum:]]+[ \t]*\\(.*\\))[ \t]*[{;]?"
```

### Commit Message Convention

A good commit message convention improves readability and enables automated changelogs. The Conventional Commits specification is widely adopted:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Types include `feat` (new feature), `fix` (bug fix), `docs` (documentation), `style` (formatting), `refactor`, `test`, `chore` (build system, CI), and `perf` (performance improvement).

Examples:

```
feat(auth): add JWT refresh token rotation

Implements automatic refresh token rotation on each use.
Old refresh tokens are invalidated immediately.

Closes #142
```

```
fix(checkout): prevent double-charge on retry

The payment service was not checking for idempotency keys
when a user retried a failed payment.
```

```
chore(ci): add dotnet format check to PR pipeline
```

## Part 6: Summary and Further Reading

Git is a powerful tool, but like any tool, how you use it matters more than which features it has. The branching model you choose profoundly affects your team's velocity, quality, and happiness.

The evidence from a decade of DORA research is clear: trunk-based development with continuous integration leads to higher performance on every metric that matters — speed, stability, and recovery. Long-lived branches create integration risk, delay feedback, and slow you down. Feature flags and branch by abstraction give you every capability that long-lived branches provide, without the cost.

You do not need to be Google to benefit. You just need to trust your team, invest in CI, and commit to small, frequent changes. The hardest part is the cultural shift. The technology is the easy part — you already have everything you need in Git.

### Sources and Further Reading

- Forsgren, Nicole, Jez Humble, and Gene Kim. *Accelerate: The Science of Lean Software and DevOps.* IT Revolution Press, 2018. The foundational research text.
- DORA Research Program. [dora.dev/research](https://dora.dev/research). Ongoing annual State of DevOps reports.
- DORA Metrics Guide. [dora.dev/guides/dora-metrics-four-keys](https://dora.dev/guides/dora-metrics-four-keys/). Authoritative definitions of the five key metrics.
- Humble, Jez, and David Farley. *Continuous Delivery: Reliable Software Releases through Build, Test, and Deployment Automation.* Addison-Wesley, 2010.
- Kim, Gene, Jez Humble, Patrick Debois, and John Willis. *The DevOps Handbook.* IT Revolution Press, 2016.
- Hammant, Paul. [trunkbaseddevelopment.com](https://trunkbaseddevelopment.com/). The definitive reference site for trunk-based development practices and techniques.
- Fowler, Martin. "Branch by Abstraction." [martinfowler.com/bliki/BranchByAbstraction.html](https://martinfowler.com/bliki/BranchByAbstraction.html).
- Fowler, Martin. "Patterns for Managing Source Code Branches." [martinfowler.com/articles/branching-patterns.html](https://martinfowler.com/articles/branching-patterns.html). Comprehensive taxonomy of branching strategies.
- Fowler, Martin. "Continuous Integration." [martinfowler.com/articles/continuousIntegration.html](https://martinfowler.com/articles/continuousIntegration.html). Updated 2024 article on CI principles.
- Hodgson, Pete. "Feature Toggles (aka Feature Flags)." [martinfowler.com/articles/feature-toggles.html](https://martinfowler.com/articles/feature-toggles.html). Comprehensive guide to feature flag categories and management.
- de Pauw, Thierry. "On the Benefits of Trunk-Based Development." [thinkinglabs.io](https://thinkinglabs.io/articles/2025/07/21/on-the-benefits-of-trunk-based-development.html). July 2025. A practitioner's summary of TBD benefits.
- Atlassian. "Trunk-Based Development." [atlassian.com/continuous-delivery/continuous-integration/trunk-based-development](https://www.atlassian.com/continuous-delivery/continuous-integration/trunk-based-development).
- AWS Prescriptive Guidance. "Advantages and Disadvantages of the Trunk Strategy." [docs.aws.amazon.com](https://docs.aws.amazon.com/prescriptive-guidance/latest/choosing-git-branch-approach/advantages-and-disadvantages-of-the-trunk-strategy.html).
- LaunchDarkly. "Elite Performance with Trunk-Based Development." [launchdarkly.com](https://launchdarkly.com/blog/elite-performance-with-trunk-based-development/). Analysis of DORA data showing elite performers are 2.3x more likely to use TBD.
- Toptal. "Trunk-Based Development vs. Git Flow." [toptal.com](https://www.toptal.com/software/trunk-based-development-git-flow). Updated February 2026. Practical comparison with pros and cons.
