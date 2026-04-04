---
title: "Git: The Complete Guide — Internals, Misconceptions, Branches, Commits, Tags, and Everything In Between"
date: 2026-04-22
author: observer-team
summary: A deep, exhaustive guide to Git covering its history, object model, and every major concept — with special focus on the most common and damaging misconceptions about branches, commits, tags, merging, and rebasing. Includes a full worked scenario demonstrating exactly how diverged branches conflict, how 3-way merge works under the hood, and how to reason about Git's DAG.
tags:
  - git
  - version-control
  - deep-dive
  - best-practices
  - software-engineering
  - devops
---

Git is everywhere. In 2022, surveys reported that nearly 95 percent of professional developers used Git as their primary version control system. The Linux kernel, the .NET runtime, the Chromium browser, every major open-source project you can name — all governed by Git. And yet, for all its ubiquity, Git is arguably the most misunderstood tool in mainstream software engineering. Not misunderstood in the sense of "I don't know the commands" — most developers know enough commands to get through their day. Misunderstood in the sense that the mental model most people carry around is subtly but fundamentally wrong, and those subtle wrongnesses cause real pain: lost work, botched releases, conflicts that seem inexplicable, fear of rebasing, confusion about tags versus branches, and the peculiar dread of the phrase "detached HEAD."

This article is about fixing that mental model from the ground up. We will start at the very beginning — the actual bytes on disk — and build upward through commits, branches, tags, merging, rebasing, workflows, and best practices. Along the way, we will work through a specific real-world scenario that illustrates exactly why branches conflict the way they do, why GitHub reports "Can't automatically merge" in certain cases, and what actually happens when Git performs a 3-way merge. Nothing will be hand-waved. No "just trust the tool." We are going to understand *why*.

The current stable release of Git as of this writing is version 2.53.0, released on February 2, 2026. Git is maintained by Junio Hamano, who took over from Linus Torvalds in July 2005 — less than four months after Git was born.

---

## Part 1: Where Git Came From — And Why It Matters

### 1.1 Life Before Git: Patches, Tarballs, and CVS

To understand why Git is designed the way it is, you have to understand what came before it and why it was inadequate.

In the earliest days of the Linux kernel (1991–2002), Linus Torvalds managed contributions the old-fashioned way: developers posted patches to a mailing list, trusted lieutenants reviewed and forwarded them, and Linus applied them manually to his own source tree. When a new kernel release was ready, Linus would publish the entire tree as a tarball. There was no version history per se — just a sequence of tarballs and a pile of emails. If you wanted to understand how a particular line of code had evolved, you compared tarballs with `diff`.

The dominant VCS of that era was CVS (Concurrent Versions System), which had been around since the mid-1980s. CVS was a client-server system. There was one canonical repository on one server. Developers checked out files, made changes, and committed them back to the central server. If the server was unavailable, you could not commit. If the network was slow, everything was slow. CVS tracked changes per file, not per snapshot of the entire tree, which led to subtle and painful inconsistencies when you wanted to understand the state of the project at a given point in time. And CVS branching was — charitably — fragile.

Subversion (SVN), which arrived around 2000, was explicitly designed as "CVS done right." It improved on many of CVS's rough edges — atomic commits, better handling of renames, proper directory versioning — but it retained the fundamental client-server model. Still one central repository. Still no commits without a network connection. Still a linear model at heart.

For most projects, this was fine. For the Linux kernel, it was not. The kernel had thousands of contributors spread across time zones, working asynchronously, submitting changes of wildly varying sizes and qualities. The notion of a single central server was both a practical bottleneck and a philosophical mismatch with how kernel development actually worked.

### 1.2 BitKeeper: The Controversial Middle Chapter

In 2002, Linus made a decision that shocked the open-source community: he started using BitKeeper for Linux kernel development. BitKeeper was a proprietary distributed version control system created by Larry McVoy's company BitMover. It was *not* free software. It was not open source. And yet Linus chose it.

His reasoning was pragmatic. BitKeeper was simply better than everything else available. It was distributed — every developer had a full local copy of the repository history, could commit locally without network access, and could work offline. It had fast branching and merging. It could handle the scale of the kernel project. No open-source alternative came close.

BitMover offered a free-of-charge license to the Linux kernel project, with significant restrictions: developers using BitKeeper couldn't work on competing version control projects. This was controversial, but Linus accepted the deal.

The arrangement lasted three years. In 2005, Andrew Tridgell — the creator of Samba and co-creator of rsync — created a tool called SourcePuller that could communicate with BitKeeper repositories. BitMover claimed this constituted reverse engineering of their protocols and violated the license terms. Larry McVoy revoked the free license. Overnight, the Linux kernel development team lost their primary collaboration tool.

Linus Torvalds had approximately zero good options. CVS was out of the question — he famously described it as an example of what not to do, coining the design principle "WWCVSND" (What Would CVS Not Do). Subversion was CVS in a new coat. Nothing else was close to adequate.

So he did what he did: he wrote his own.

### 1.3 Ten Days That Changed Software Development

On April 3, 2005, Linus cut the last non-Git Linux kernel release candidate. On April 6, he emailed the Linux Kernel Mailing List announcing he was working on a replacement. On April 7, he made the first commit to the new tool — a commit that used the tool itself to record its own creation. By April 18, Git was performing multi-branch merges. By April 29, it was benchmarked handling patches at 6.7 per second. By June 16, Git was managing the kernel 2.6.12 release.

In a famous GitHub interview in 2025 marking Git's 20th anniversary, Torvalds recalled: "It was about 10 days until I could use it for the kernel, yes." He also noted that even the first raw version "was superior to CVS."

Git 1.0 was released on December 21, 2005, by Junio Hamano, who had taken over maintainership from Torvalds in July of that year. Torvalds had maintained Git for less than four months.

Git 2.0, released on May 28, 2014, was the first backward-incompatible release. It changed `git push` default behavior so that only the current branch is pushed (instead of all matching branches), changed `git add -u` to operate on the entire repository regardless of current directory, and introduced bitmap indexes for faster fetch operations.

The current release series continues under Junio Hamano's stewardship. The most recent stable release, 2.53.0, arrived on February 2, 2026.

### 1.4 Design Goals That Shaped Everything

Understanding Git's peculiarities requires understanding what Torvalds was optimizing for when he designed it. His stated goals were:

**Speed.** Not "fast enough for most uses" speed. Extreme speed. The Linux kernel tree was enormous, with thousands of files and decades of history. Operations needed to be fast even on that scale.

**Data integrity.** Every object in Git is identified by a cryptographic hash of its contents. If a single byte of history is corrupted, Git detects it immediately. Accidental corruption and deliberate tampering are both caught.

**Distributed workflow.** Every clone of a repository is a complete copy of its entire history. There is no special "server" repo. Every developer has everything.

**Non-linear development.** Branching and merging must be cheap, fast, and correct. The kernel had thousands of parallel branches all the time.

These goals explain things that otherwise seem strange. Why does Git hash every object? Data integrity. Why does cloning copy the entire history? Distributed workflow. Why are branches just files containing a single hash? Cheap, fast branching.

---

## Part 2: The Git Object Model — What Is Actually Stored on Disk

This is the part most Git tutorials skip, and it is the single biggest reason people's mental models are wrong. If you understand the object model, everything else becomes obvious. If you do not, you are navigating by guesswork.

### 2.1 Git as a Content-Addressable Filesystem

At its core, Git is a content-addressable key-value store. You give it data; it gives you back a key (a hash) that you can use to retrieve that data later. The key is always a cryptographic hash — currently SHA-1 (160 bits, 40 hex characters) for repositories initialized without the `--object-format=sha256` flag, with SHA-256 support (256 bits, 64 hex characters) available as an opt-in since Git 2.29 and increasingly encouraged as SHA-1's weaknesses become more relevant.

The "content-addressable" part is crucial. The key is *derived from the content*, not assigned by the system. Two objects with identical content will always have the same hash. One object with different content will always have a different hash. This is why Git can detect corruption (the hash of the stored bytes won't match the stored name) and why it can deduplicate efficiently (identical files in different directories share one stored object).

Everything Git stores lives in `.git/objects/`. When you run `git init`, this directory is created empty. After your first `git add` and `git commit`, it contains a handful of files organized by the first two characters of their hash. A hash like `bd9dbf5aae1a3862dd1526723246b20206e5fc37` is stored at `.git/objects/bd/9dbf5aae1a3862dd1526723246b20206e5fc37`. This two-level directory structure is a performance optimization — searching a directory with 300,000 files is slower than searching 256 directories with ~1,000 files each.

You can inspect any object with:

```bash
git cat-file -t <hash>   # what type is this object?
git cat-file -p <hash>   # show me the contents
```

And you can compute the hash Git would assign to any content with:

```bash
echo 'hello world' | git hash-object --stdin
```

These are *plumbing* commands — low-level commands that expose Git's internals. The commands you use every day (`git add`, `git commit`, `git log`) are *porcelain* commands — higher-level interfaces built on top of plumbing.

### 2.2 The Four Object Types

Git has exactly four types of objects. Everything in a repository's history is stored as some combination of these four.

#### Blobs

A blob stores raw file content. Just the bytes of the file. No filename. No path. No permissions. Just bytes.

```bash
# Create a blob manually
echo 'console.log("hello");' | git hash-object -w --stdin
# Output: some 40-character hash
```

The hash is computed from a header (`blob <length>\0`) concatenated with the content, then SHA-1'd (or SHA-256'd). You can verify this:

```bash
printf 'blob 22\0console.log("hello");\n' | sha1sum
```

Because blobs contain no filename, the same file content in two different directories produces one blob object, not two. This is how Git achieves deduplication. A 10-megabyte configuration file that appears verbatim in 50 branches of a repository occupies 10 megabytes in the object store, not 500 megabytes.

This also explains why Git doesn't track empty directories. If there's no file, there's no blob. If there's no blob, there's nothing for the tree (next section) to reference. The conventional workaround is to place a `.gitkeep` file in an otherwise-empty directory.

#### Trees

A tree object represents a directory. It contains a list of entries, each specifying a mode (file permissions / entry type), an object type, a hash, and a name. A tree entry can point to a blob (a file) or another tree (a subdirectory).

```
100644 blob a8c34f2... README.md
100755 blob b7d9e81... run.sh
040000 tree 4fe2c19... src
```

The modes follow POSIX convention but Git only pays attention to the executable bit. `100644` is a regular file. `100755` is an executable file. `040000` is a subdirectory (tree). `160000` is a gitlink (submodule reference).

Because trees are content-addressed just like blobs, two trees with identical contents (same file names, same blob hashes, same permissions) produce one stored object. This allows Git to perform fast comparison: if two commits point to the same root tree hash, the entire working tree is identical. No need to examine individual files.

#### Commits

A commit object is the glue between a tree and the history. It contains:

- A pointer to a tree object (the state of the repository at this moment)
- Zero or more pointers to parent commits (zero for the first commit in a repository; one for most commits; two or more for merge commits)
- Author name, email, and timestamp (the person who wrote the change)
- Committer name, email, and timestamp (the person who made the commit — often the same as the author, but different in patch-based workflows)
- The commit message

```bash
git cat-file -p HEAD
# Output looks like:
tree 4b825dc642cb6eb9a060e54bf8d69288fbee4904
parent 7f3a1bc9d2e4f5a8c6b0d1e2f3a4b5c6d7e8f9a0
author Kushal <kushal@example.com> 1745280000 -0500
committer Kushal <kushal@example.com> 1745280000 -0500

Add navigation component
```

A key insight: **a commit does not store a diff**. It stores a complete snapshot — the full tree of the repository at that moment. When you run `git log -p` and see a diff, Git is not reading a stored diff. It is computing the diff on the fly by comparing the commit's tree to its parent's tree. This is counterintuitive for people coming from delta-based systems like CVS and SVN, but it is fundamental to how Git works and why it is fast.

Another key insight: **a commit is immutable**. Once created, it cannot be changed. Its hash is derived from its content (tree pointer, parent pointers, author, message). If you change the message, you get a different hash — effectively a new commit. This is why `git commit --amend` does not actually amend: it creates a new commit object and moves the branch pointer to it. The old commit still exists in the object store until garbage collection removes it.

#### Tags

A tag object (specifically, an *annotated* tag, as opposed to a *lightweight* tag which is just a ref) stores:

- A pointer to another object (usually a commit, but technically a tag can point to anything)
- The tagger's name, email, and timestamp
- A tag name
- A tag message (and optionally a GPG signature)

```bash
git cat-file -p v1.0.0
# Output looks like:
object 7f3a1bc9d2e4f5a8c6b0d1e2f3a4b5c6d7e8f9a0
type commit
tag v1.0.0
tagger Release Bot <releases@example.com> 1745280000 -0500

Release version 1.0.0

Stable release for production deployment.
-----BEGIN PGP SIGNATURE-----
...
-----END PGP SIGNATURE-----
```

The distinction between annotated tags and lightweight tags will be covered in detail in Part 5.

### 2.3 References: The Human Interface to Hashes

Hashes are great for computers. They are terrible for humans. Nobody wants to type `7f3a1bc9d2e4f5a8c6b0d1e2f3a4b5c6d7e8f9a0` every time they want to refer to a commit.

References (refs) are named pointers to hashes. They live in `.git/refs/`. A branch like `main` is stored as `.git/refs/heads/main`, and its contents are nothing more than a single hash — the hash of the commit that is the current tip of that branch.

```bash
cat .git/refs/heads/main
# Output:
7f3a1bc9d2e4f5a8c6b0d1e2f3a4b5c6d7e8f9a0
```

That's it. A branch is a 41-byte text file (40 hex characters plus a newline) containing a single commit hash. Nothing more. This is why branching in Git is essentially free — creating a branch is `echo <hash> > .git/refs/heads/new-branch`. No copying. No history-forking. No expensive operation.

There is also a special reference called `HEAD`. `HEAD` lives at `.git/HEAD` and normally contains a *symbolic ref* — a pointer to a branch name rather than directly to a commit hash.

```bash
cat .git/HEAD
# Output (normal state):
ref: refs/heads/main
```

When you are on the `main` branch, `HEAD` points to `refs/heads/main`, which points to a commit hash. When you make a commit, Git computes the new commit hash, writes it to `refs/heads/main`, and `HEAD` continues to point at `refs/heads/main`. You never need to update `HEAD` manually during normal branch-based work.

Remote tracking refs live at `.git/refs/remotes/`. `origin/main` is stored at `.git/refs/remotes/origin/main` and represents "the last known state of the `main` branch on the `origin` remote."

For repos with very large numbers of references, Git uses a packed-refs file (`.git/packed-refs`) for performance. Rather than one file per ref, many refs are stored in a single file. The on-disk format is simple: one line per ref, `<hash> <refname>`.

### 2.4 Visualizing the Object Graph

Let's build a mental picture of a simple repository to make all of this concrete.

You have a repository with two files, `README.md` and `src/main.cs`, and three commits: an initial commit, a commit adding README content, and a commit adding the C# file.

The object graph looks like this:

```
[Commit C] → tree-C
    ↑ parent       ├── blob: README.md (v2)
    │               └── tree: src/
[Commit B] → tree-B         └── blob: main.cs
    ↑ parent       ├── blob: README.md (v2)
    │
[Commit A] → tree-A
               └── blob: README.md (v1)

[main branch] → [Commit C hash]
[HEAD] → ref: refs/heads/main
```

Notice that `tree-B` and `tree-C` both reference the same blob for `README.md` (it didn't change between those commits), so that blob is stored only once. This deduplication happens automatically, at the blob level, and is one of the reasons Git repositories are compact even with long histories.

Now let's say you create a branch `feature/login`. The operation is:

```bash
git checkout -b feature/login
```

Internally this:
1. Creates `.git/refs/heads/feature/login` containing the same hash as `.git/refs/heads/main`
2. Updates `.git/HEAD` to `ref: refs/heads/feature/login`

That's it. No files were copied. No history was duplicated. The two branches share the same commit history up to this point.

---

## Part 3: Commits — What They Really Are and Common Misconceptions

### 3.1 Misconception: "Commits Store Diffs"

This is the single most common misconception about Git. Developers who have used SVN or CVS for years carry this mental model into Git, and it silently causes confusion in dozens of situations.

**In CVS and SVN**, the primary unit of storage is a *delta* — what changed from one version to the next. To reconstruct the state of the repository at a given point, the system starts from some base state and applies a chain of deltas forward (or backward). This makes individual commits cheap to store but makes arbitrary-point-in-time reconstruction potentially expensive.

**In Git**, the primary unit of storage is a *snapshot* — the complete state of every tracked file at the moment of the commit. Every commit points to a tree that represents the full working directory. To reconstruct the working directory at any commit, Git just reads that commit's tree — no chains of deltas to reconstruct.

In practice, Git does use delta compression under the hood in *pack files* (which are optimized storage bundles created during `git gc` or `git push`), but this is an implementation detail of the storage layer, invisible to the user. Logically, every commit is a complete snapshot.

**Why does this matter?** Because if commits were diffs, a branch would need to carry its entire diff chain for Git to know the state at any point. But since commits are snapshots, a branch is just a pointer to a single commit object, and that commit contains everything you need.

### 3.2 Misconception: "git commit --amend Edits a Commit"

When you run `git commit --amend`, Git does *not* edit the existing commit. It:

1. Reads the parent(s) of the current commit
2. Stages any new changes you've added
3. Opens the editor with the current commit message
4. Creates a *brand new commit object* with the updated tree and message
5. Moves the branch pointer to the new commit

The old commit still exists in the object store. You can find it with `git reflog`. It will be garbage collected after the reflog expiry period (typically 90 days by default).

This is not an academic distinction. It has a practical consequence: **if you have already pushed a commit and then amend it, the amended commit has a different hash**. Anyone else who has pulled your branch now has a different history than you. They have commit `A`. You have commit `A'`. When they try to merge, Git sees two diverged histories with a common ancestor — confusion and extra merge commits ensue. This is why you should never amend commits that you have already pushed to a shared branch.

### 3.3 Misconception: "Commits Are Ordered by Time"

Git commits are ordered by the *parent-child relationship*, not by timestamp. A commit's timestamp is just metadata stored in the commit object. It is not enforced to be monotonically increasing, and it is trivially forgeable (you can set `GIT_COMMITTER_DATE` and `GIT_AUTHOR_DATE` to any value when creating a commit).

This matters in a few situations:

- When you rebase, you are creating new commit objects. The new commits will have the current timestamp (unless you use `--committer-date-is-author-date`), so rebased commits appear "newer" than they were.
- When you cherry-pick a commit, the new commit object will have a new committer timestamp (your current time) but may preserve the author timestamp.
- `git log` by default sorts by commit date, not topological order. Use `git log --topo-order` if you want topological sorting.

### 3.4 The Commit Graph (DAG)

The commit history in Git forms a Directed Acyclic Graph (DAG). Each commit points to its parent(s) — this is the "directed" part. The graph has no cycles — you cannot follow parent pointers and end up back where you started — this is the "acyclic" part.

For simple linear history:

```
A ← B ← C ← D   (main)
```

Each commit points to exactly one parent. This is the most common shape.

For a branched history:

```
A ← B ← C ← D ← E   (main)
              ↑
              └── F ← G   (feature)
```

Commit `D` is the common ancestor of `E` (on main) and `G` (on feature). This common ancestor is the foundation for 3-way merge, which we'll cover in detail in Part 6.

For a merge commit:

```
A ← B ← C ← D ← E ← M   (main, after merge)
              ↑         ↑
              └── F ← G─┘
```

Merge commit `M` has two parents: `E` (the tip of main at the time of merge) and `G` (the tip of the feature branch). This is how the history of the feature branch becomes part of main's history.

### 3.5 Practical Git: Staging, the Index, and What git add Really Does

There is a layer between your working directory and your commits that many developers underuse: the *index*, also called the *staging area* or *cache*. It lives at `.git/index`.

When you run `git add README.md`:
1. Git reads the content of `README.md`
2. Computes its SHA-1 hash
3. Stores the content as a blob in `.git/objects/`
4. Adds an entry to the index: mode, blob hash, filename

When you run `git commit`:
1. Git reads the index
2. Creates tree objects representing the directory structure
3. Creates a commit object pointing to the root tree and to the parent commit(s)
4. Moves the current branch pointer to the new commit hash

The index is a snapshot of what will go into the next commit. This is why you can stage part of your changes and commit them, leaving other changes unstaged. It is also why `git diff` (without `--staged`) shows unstaged changes, while `git diff --staged` shows staged changes.

Understanding the index helps demystify several confusing behaviors:

- `git add -p` lets you stage individual hunks of a file, so a single file's changes can be split across multiple commits
- `git reset HEAD <file>` unstages a file but leaves the working tree unchanged
- `git checkout -- <file>` discards working tree changes but only for files that are not staged
- `git stash` saves *both* the index state and working tree changes, not just working tree changes

---

## Part 4: Branches — What They Are and What They Are Not

### 4.1 The Core Truth: A Branch Is a Mutable Pointer

Here is the sentence you need to engrave in your mind:

**A branch is a named, mutable pointer to a commit.**

Nothing more. Not a copy of files. Not a timeline. Not a separate workspace. Not a stream of changes. A pointer to a commit.

When you look at `.git/refs/heads/main`, you see a single commit hash. That hash is the "tip" of the branch — the most recent commit in the branch's linear ancestry. Everything "behind" that commit (reachable by following parent pointers) is considered to be part of the branch's history.

This is why branching in Git is so cheap compared to other VCS. In Subversion, creating a branch copies the entire directory structure server-side (even with optimizations, it's a heavier operation). In Git, creating a branch is writing 41 bytes to a file.

### 4.2 What "Checking Out a Branch" Actually Does

When you run `git checkout feature/login` (or `git switch feature/login` in modern Git):

1. Git reads the hash stored in `.git/refs/heads/feature/login`
2. Git updates the working directory to match the tree of that commit
3. Git updates the index to match the tree of that commit
4. Git updates `.git/HEAD` to `ref: refs/heads/feature/login`

Steps 1–3 are the substantive work. Step 4 is just updating the special pointer. After this operation, any new commits you make will be recorded with `feature/login` as the branch, and `feature/login`'s pointer will advance to each new commit.

### 4.3 What Happens When You Make a Commit on a Branch

Say you're on `feature/login`, which currently points to commit `D`. You make a change and run `git commit`.

1. Git creates a new commit object `E` with parent `D`
2. Git writes the hash of `E` to `.git/refs/heads/feature/login`
3. `HEAD` still points to `ref: refs/heads/feature/login`
4. `feature/login` now points to `E`
5. `main` is completely untouched

The only thing that changed, from the refs perspective, is the contents of one 41-byte file. The main branch doesn't know or care. It still points to whatever commit it pointed to before.

### 4.4 Detached HEAD: When HEAD Points to a Commit Directly

Normally, `HEAD` contains a symbolic ref: `ref: refs/heads/some-branch`. When you make a commit, Git advances `some-branch`'s pointer, and `HEAD` continues to point at `some-branch`.

But there is another state: *detached HEAD*. In this state, `HEAD` contains a commit hash directly, rather than a branch name. You enter detached HEAD state when you:

- Check out a commit by hash: `git checkout 7f3a1bc`
- Check out a tag: `git checkout v1.0.0` (tags point to commits, not branches)
- Check out a remote tracking branch directly: `git checkout origin/main`
- Have Git put you there during an interactive rebase

```bash
git checkout 7f3a1bc
# Warning: You are in 'detached HEAD' state.
# You can look around, make experimental changes and commit them,
# and you can discard any commits you make in this state without
# impacting any branches by switching back to a branch.
```

When you are in detached HEAD state and make a commit, the commit is created normally — it has a parent, it has a tree, it has a hash — but *no branch pointer is updated*. The new commit is only reachable via `HEAD` itself. If you switch to another branch without first capturing that commit in a branch or tag, the commit becomes *dangling* — still in the object store, but unreachable by name. Git's garbage collector will eventually remove it (after the reflog expiry, typically 30–90 days).

The fix is simple: before you switch away from a detached HEAD, create a branch from your current position:

```bash
git checkout -b my-experimental-work
# or, in modern Git:
git switch -c my-experimental-work
```

This creates a new branch pointing at the current commit and attaches HEAD to it. You are no longer detached.

**Useful applications of detached HEAD:**
- Inspecting the code as it was at a specific release: `git checkout v2.3.1`
- Running tests against a specific historical commit
- Using `git bisect` to find which commit introduced a bug (bisect temporarily puts you in detached HEAD state at each step)

### 4.5 Remote Tracking Branches and the Difference Between `origin/main` and `main`

When you clone a repository, Git creates two kinds of refs:

- Local branches: `.git/refs/heads/main` — this is your local branch, which you can commit to and which moves forward as you make commits
- Remote tracking branches: `.git/refs/remotes/origin/main` — this is a read-only snapshot of where `main` was on the remote the last time you communicated with it

`git fetch` updates remote tracking branches to reflect the remote's current state, but it does *not* update your local branches. `git pull` is essentially `git fetch` followed by `git merge origin/main` (or `git rebase origin/main` if you've configured `pull.rebase = true`).

When you run `git push`, you are uploading your local commits to the remote and asking the remote to update its ref. The remote will accept the push if it is a fast-forward (the remote's current commit is an ancestor of the commit you're pushing). If it's not a fast-forward — because someone else has pushed commits to the remote since you last fetched — the remote will reject the push. You need to fetch, integrate the new remote commits into your local branch, and then push again.

The common mistake is running `git push --force` without understanding what it does: it overwrites the remote's ref with your local ref, even if it would lose commits. Anyone who has pulled those commits now has a history that has been abandoned by the remote. Use `git push --force-with-lease` instead — it checks that the remote is still at the commit you expect, and fails if someone else has pushed in the meantime.

### 4.6 Branch Naming Conventions

Git imposes very few restrictions on branch names. The main technical rules are:
- Cannot begin or end with `/`
- Cannot contain consecutive `..`
- Cannot contain spaces
- Cannot contain certain control characters

Beyond the technical rules, teams use conventions. Common ones:

- `main` or `master` — the primary integration branch
- `develop` or `dev` — sometimes used as a secondary integration branch in GitFlow
- `feature/<name>` — feature branches
- `bugfix/<name>` or `fix/<name>` — bugfix branches
- `release/<version>` — release preparation branches
- `hotfix/<version>` — emergency fixes to production
- `<initials>/<name>` — personal branches (e.g., `kd/refactor-auth`)

The Wikipedia article on Git notes that `git init` creates a branch named `master` by default, but GitHub, GitLab, and other platforms default to `main`. Git itself will start using `main` as the default from the planned 3.0 release, expected by the end of 2026.

You can configure the default branch name for new repositories:

```bash
git config --global init.defaultBranch main
```

---

## Part 5: Tags — What They Are and How They Differ from Branches

### 5.1 Lightweight Tags vs. Annotated Tags

Git has two fundamentally different kinds of tags, and the distinction matters more than most developers realize.

**Lightweight tags** are exactly like branches: a named pointer to a commit hash, stored as a file in `.git/refs/tags/`. The only difference between a lightweight tag and a branch is that lightweight tags do not move when you make commits. They are static pointers.

```bash
git tag v1.0.0             # create a lightweight tag at HEAD
cat .git/refs/tags/v1.0.0  # contains the commit hash
```

**Annotated tags** are full Git objects stored in the object database. They have their own hash. They contain the tagger's identity, a timestamp, a message, and a pointer to another object (usually a commit). They can be signed with GPG.

```bash
git tag -a v1.0.0 -m "Version 1.0.0 — stable release"
git cat-file -t v1.0.0  # "tag" — this is a tag object, not just a ref
git cat-file -p v1.0.0  # shows the full tag object with metadata
```

### 5.2 When to Use Each Type

Use annotated tags for anything that matters — release points, milestone markers, anything that might need a GPG signature for release verification. Annotated tags preserve who created the tag, when, and why. `git describe` works better with annotated tags. `git push --follow-tags` only pushes annotated tags.

Use lightweight tags for local, temporary, or personal markers — "I want to come back to this commit, here's a bookmark." Lightweight tags are fine for internal use but should not be shared or used for official releases.

A pragmatic rule: if you're tagging something that will go into a `CHANGELOG`, use an annotated tag. If you're just marking something for yourself locally, a lightweight tag is fine.

### 5.3 The Critical Misconception: Tags Are Not Immutable in Git

Tags in Git are *not* enforced to be immutable. You can delete a tag. You can move a lightweight tag to a different commit. You can even recreate an annotated tag with a different hash.

What you *cannot* do (without `--force`) is create a tag that already exists. But `git tag -f v1.0.0 <new-hash>` will move the tag to a different commit.

This becomes catastrophic in a shared repository because tags are cached. If you push tag `v1.0.0` pointing to commit `A`, and then move it to point to commit `B` and force-push, everyone who has already fetched `v1.0.0` still has it pointing to `A`. You now have two different objects both called `v1.0.0`, with no reliable way to know which is "authoritative" without out-of-band communication.

**Best practice:** never move or delete pushed tags. Treat them as immutable once published. If you tagged the wrong commit, either add a new tag (`v1.0.0-correct`) and communicate the change, or create a new annotated tag with a note explaining the correction.

Tags are not automatically pushed by `git push`. You must push them explicitly:

```bash
git push origin v1.0.0       # push a specific tag
git push origin --tags        # push all tags
git push --follow-tags        # push only annotated tags reachable from pushed commits
```

The `--follow-tags` option is generally the best choice: it pushes annotated tags that are reachable from the commits you're pushing, without pushing every tag in your local repository.

### 5.4 Tags vs. Branches: The Key Difference

People sometimes ask: "If a tag is just a pointer to a commit, how is it different from a branch?"

The answer is behavioral, not structural:

| Property | Branch | Lightweight Tag | Annotated Tag |
|----------|--------|-----------------|---------------|
| Stored as | File in `.git/refs/heads/` | File in `.git/refs/tags/` | Full Git object + pointer file |
| Moves on commit | Yes (HEAD follows along) | No (static) | No (static) |
| Contains metadata | No | No | Yes (tagger, date, message, optional signature) |
| Can be pushed automatically | Yes | Not by default | Not by default |
| `git describe` uses | Current branch context | Yes, if reachable | Yes, preferentially |

The intent is different too. A branch represents *ongoing work* — a moving frontier. A tag represents *a named historical moment* — a snapshot that will remain meaningful in the future. "Version 2.1.4 is the commit my users are running right now" — that is what tags are for.

---

## Part 6: The Scenario — Branches, Conflicts, and 3-Way Merge Explained

Now let's work through the specific scenario you described, because it illustrates exactly the kind of confusion that arises when the mental model is wrong. I'll use the real repository at `https://github.com/kusl/learningbydoing` as the running example.

### 6.1 Setting Up the Initial State

You start on `main`. The README contains this text:

```
This is the base commit.

This is common for both branches.
In the next line, I will write the branch name.
main
In the line above, I will replace with the name of the current branch
in each of my two branches.
Because each of those two branches are directly from main,
I won't be able to merge one into the other directly without a conflict
or so I think.
Lets find out.
```

Let's call the commit hash of this state `C-base`. The state of the DAG is:

```
[C-base] ← main ← HEAD
```

### 6.2 Creating branch-1

You run `git checkout -b branch-1` (or `git switch -c branch-1`). At this point:

```
[C-base] ← main
              ↑
              └── branch-1 ← HEAD
```

Both `main` and `branch-1` point to exactly the same commit, `C-base`. No data was copied. No new objects were created.

You edit the README, changing `main` to `branch-1` on line 5, and commit. Git creates a new commit object `C-b1` with:
- Tree reflecting the modified README
- Parent: `C-base`

The DAG is now:

```
[C-base] ← [C-b1] ← branch-1 ← HEAD
    ↑
    └── main
```

### 6.3 Creating branch-2

You switch back to `main` (`git checkout main`) and create `branch-2` from there:

```bash
git checkout main
git checkout -b branch-2
```

Now:

```
[C-base] ← [C-b1] ← branch-1
    ↑
    └── branch-2 ← HEAD
```

`branch-2` starts from `C-base`, the *same starting point* as `branch-1`. They are siblings — both children of `C-base`.

You edit the README, changing `main` to `branch-2` on line 5, and commit. Git creates `C-b2`:

```
[C-base] ← [C-b1] ← branch-1
    ↑
    └── [C-b2] ← branch-2 ← HEAD
```

### 6.4 Why Merging branch-1 into main Works

Now switch back to `main` and merge `branch-1`:

```bash
git checkout main
git merge branch-1
```

`main` currently points to `C-base`. `branch-1` points to `C-b1`, which has `C-base` as its parent. In other words, `C-base` is a direct ancestor of `C-b1` — the entire history of `C-b1` builds directly on what `main` already has.

This is a **fast-forward merge**. Git doesn't need to create a merge commit. It just advances `main`'s pointer to `C-b1`:

```
[C-base] ← [C-b1] ← branch-1
                 ↑
                 └── main ← HEAD
```

The README on `main` now says `branch-1` on line 5. The merge "succeeded" trivially because no actual merging (reconciling divergent histories) was necessary.

### 6.5 Why Merging branch-2 into main Fails (or Would Need a Merge Commit)

Now try to merge `branch-2` into `main`:

```bash
git merge branch-2
```

`main` is now at `C-b1`. `branch-2` is at `C-b2`. Their common ancestor (called the *merge base*) is `C-base`.

Git performs a **3-way merge**:
- It looks at the merge base (`C-base`): README has `main` on line 5
- It looks at the current branch (`C-b1`, now `main`): README has `branch-1` on line 5
- It looks at the branch being merged (`C-b2`): README has `branch-2` on line 5

For line 5:
- `C-base` had: `main`
- `C-b1` (current) changed it to: `branch-1`
- `C-b2` (incoming) changed it to: `branch-2`
- **Both sides changed the same line in incompatible ways**

Git cannot determine which version should "win." This is a **conflict**. The merge stops, leaves conflict markers in the README, and asks you to resolve:

```
<<<<<<< HEAD
branch-1
=======
branch-2
>>>>>>> branch-2
```

You must manually edit the file, remove the conflict markers, and choose (or combine) the content. Then you stage the resolved file and run `git commit` to complete the merge.

### 6.6 Why GitHub Says "Can't Automatically Merge"

When you create a pull request on GitHub to merge `branch-1` into `branch-2` (or vice versa), GitHub runs a simulated 3-way merge to check whether the merge can be completed automatically. If it detects a conflict, it shows "Can't automatically merge. Don't worry, you can still create the pull request."

This is exactly the situation above. The common ancestor of `branch-1` and `branch-2` is `C-base`. Both branches modified the same line (`main` → `branch-1` and `main` → `branch-2`). Git's merge algorithm can't automatically choose between them, so it flags the conflict.

The pull request can still be created — GitHub is just telling you upfront that merging it will require manual conflict resolution. You can fetch the branches locally, merge them, resolve the conflict, push the result, and then GitHub will show the PR as mergeable.

### 6.7 The Suspicion About Merging branch-1 First, Then branch-2

You raised a very perceptive question: "If I merge branch-1 into main first, I won't be able to merge branch-2 into main anymore?"

This is partially correct, but the explanation is subtle. Let's trace through it carefully.

**State before any merge:**
- `main` → `C-base` (README: `main` on line 5)
- `branch-1` → `C-b1` (README: `branch-1` on line 5)
- `branch-2` → `C-b2` (README: `branch-2` on line 5)

**After fast-forward merging branch-1 into main:**
- `main` → `C-b1` (README: `branch-1` on line 5) — fast-forward, no merge commit
- `branch-1` → `C-b1` (same)
- `branch-2` → `C-b2` (README: `branch-2` on line 5)

**Now try to merge branch-2 into main:**

Merge base: `C-base`
- `C-base` line 5: `main`
- `main` (= `C-b1`) line 5: `branch-1`
- `branch-2` line 5: `branch-2`

**Conflict.** Same situation as before. The merge can still be done — it just requires manual resolution. The merge commit would result in whatever you choose for line 5. If you choose `branch-2`, main's README will say `branch-2`. If you merge both, you can write something else.

So the answer to your question is: **you can still merge branch-2 into main after merging branch-1, but it will require conflict resolution.** The first merge doesn't "poison" the second merge — it just means the second merge has to reconcile more divergence.

### 6.8 The Two-File Scenario: Silent Merge and Why Manual Intervention Is Still Needed

Now for the more interesting case you raised. Let's say you have two files, `file-a.txt` and `file-b.txt`:

- On `main`: both files are at their baseline state.
- On `branch-1`: `file-a.txt` is modified, `file-b.txt` is unchanged.
- On `branch-2`: `file-b.txt` is modified in a way that is *incompatible with the file-a change from branch-1*, but `file-a.txt` is unchanged.

This is a critically important scenario. Let me work through it precisely.

**3-way merge: branch-2 into branch-1**

Merge base: The common ancestor commit.
- `file-a.txt`: base is unchanged; `branch-1` changed it; `branch-2` did not change it → **no conflict**, Git takes `branch-1`'s version
- `file-b.txt`: base is unchanged; `branch-1` did not change it; `branch-2` changed it → **no conflict**, Git takes `branch-2`'s version

Result: Git merges cleanly. No conflict markers. The merge commit has *both* changes: `file-a.txt` from `branch-1` and `file-b.txt` from `branch-2`.

And here is the critical insight you identified: **the result may be semantically wrong even though Git reported no conflicts.**

Consider a concrete example. Suppose `file-a.txt` contains a function signature:

```
// file-a.txt (baseline)
public int ProcessOrder(Order order)

// file-a.txt (branch-1)
public int ProcessOrder(Order order, bool priority)
```

And `file-b.txt` contains the usage of that function:

```
// file-b.txt (baseline)
int result = ProcessOrder(userOrder);

// file-b.txt (branch-2)
int result = ProcessOrder(userOrder, completionDate);
```

After the merge, `file-a.txt` has the new signature and `file-b.txt` has an updated call site. But there's a problem: `branch-2`'s call uses `completionDate` as the second argument, but `branch-1` changed the parameter to `bool priority`. The call site in `file-b.txt` is now passing a `DateTime` where a `bool` is expected. This is a **semantic conflict** — the code will not compile, or worse, will compile but do the wrong thing at runtime.

Git cannot detect this. Git's merge algorithm operates at the textual level. It has no understanding of semantics, types, or program logic. If the textual changes to `file-a.txt` and `file-b.txt` do not overlap (they modify different lines), Git will merge them silently and declare success.

**This is why human oversight is always required, even when Git reports no conflicts.**

The standard professional safeguard is a comprehensive automated test suite that runs after every merge. If the merged code has a semantic conflict, the tests will catch it — provided the tests are good enough. This is one of the strongest arguments for TDD (test-driven development) and high test coverage: it's not just about catching bugs before production, it's about catching semantic merge conflicts that Git silently introduces.

**You are absolutely right** that the result is "not correct" and "needs manual intervention anyway" in the sense that someone needs to verify the merged code actually works as intended. Git's "no conflict" message means only that the textual merge was unambiguous. It does not mean the merged code is correct.

---

## Part 7: Merging in Depth — Fast-Forward, 3-Way, and Merge Strategies

### 7.1 Fast-Forward Merges

A fast-forward merge is possible when the branch you're merging from is a direct linear descendant of the branch you're merging into. In other words, the branch you're merging into is an ancestor of the tip of the branch being merged.

```
[A] ← [B] ← [C] ← main
              ↑
              └── [D] ← [E] ← feature
```

If you merge `feature` into `main`, Git can fast-forward `main`'s pointer to `E`. No new commit is created.

To prevent fast-forward merges and always create a merge commit, use `--no-ff`:

```bash
git merge --no-ff feature
```

Some teams prefer this for all branch merges to preserve the "shape" of the history — you can see in the graph exactly when a feature branch was integrated. Others prefer fast-forward merges for cleaner linear history, accepting that you lose the visual indication of branch topology.

### 7.2 The Default Strategy: `ort` (Ostensibly Recursive's Twin)

Since Git 2.34, the default merge strategy is `ort`. Before that, from Git v0.99.9k through v2.33.0, the default was `recursive`. In Git v2.49.0, `recursive` became a synonym for `ort`. As of v2.50.0, `recursive` literally redirects to `ort`.

The `ort` strategy:
- Resolves two-branch merges using a 3-way merge algorithm
- When there are multiple possible merge bases (criss-cross merges), it creates a "virtual merge base" by merging the merge bases together, then uses that as the reference
- Detects and handles renames
- Is generally faster than the old `recursive` implementation, especially in large repositories

The 3-way merge algorithm works as follows:

Given a merge base `B`, a current branch tip `C` (ours), and an incoming branch tip `I` (theirs):

For each hunk of text in each file:
- If the hunk is the same in `C` and `B` (we didn't change it), take `I`'s version
- If the hunk is the same in `I` and `B` (they didn't change it), take `C`'s version
- If both `C` and `I` changed the hunk the same way (both made the same edit), take either version (they're identical)
- If `C` and `I` changed the hunk in different ways → **conflict**

This is why 3-way merge is so powerful: if only one side changed something, Git takes that change automatically. Only when *both sides changed the same thing differently* does Git need human input.

### 7.3 The `resolve` Strategy

The `resolve` strategy (`git merge -s resolve`) is an older, simpler 3-way merge strategy. It tries to carefully detect criss-cross merge ambiguities and will refuse to proceed if it finds them. It does not handle renames.

Use case: if you encounter a pathological case where `ort` produces a result you don't like, you can try `resolve` to see if it behaves differently. In practice this is rare.

### 7.4 The `octopus` Strategy

`octopus` (`git merge -s octopus branch1 branch2 branch3`) is used when merging more than two branches at once. It applies to cases like merging multiple feature branches into a release branch simultaneously. However, if there are any conflicts that require manual resolution, `octopus` refuses the merge entirely — it's an all-or-nothing proposition. Use it only when you're confident all branches have non-conflicting changes.

### 7.5 The `ours` Strategy and the `ours` Option

Be careful not to confuse these two:

**`git merge -s ours`** (the strategy): merges commit appears in history, but the result is always the current branch's tree. The other branch's changes are completely discarded. This is useful when you want to record that you've "merged" a branch (for history purposes, such as preventing future merges from re-introducing its changes) without actually taking any of its changes.

**`git merge -X ours`** (the option): uses the `ort` strategy but resolves conflicts by preferring the current branch's version. Changes from the other branch that do not conflict are still merged in. This is very different from the `-s ours` strategy.

### 7.6 Squash Merges

`git merge --squash feature` takes all commits from the `feature` branch, bundles their changes together, and stages them — but does not create a merge commit. You then run `git commit` to create a single new commit containing all the changes.

The advantage: a cleaner history on your main branch. Instead of 23 "WIP" commits from a feature branch, you get one tidy commit.

The disadvantage: you lose the individual commit history of the feature. `git blame` and `git log` on files will show the single squash commit, not the individual commits that built up the feature. Also, after a squash merge, the feature branch is technically not "merged" in Git's sense — its commits are not ancestors of the target branch. If you later try to merge the same branch again, Git will try to merge all those commits again (not understanding they've been squashed in). You should delete the feature branch after a squash merge to avoid this.

### 7.7 Criss-Cross Merges and Why They Are Tricky

A criss-cross merge (also called a diamond merge) happens when two branches have merged in a circular pattern:

```
[A] ← [B] ← [C]   (branch-1)
 ↑     ↑     ↑
 │   merge   │
 ↓     ↓     ↓
[D] ← [E] ← [F]   (branch-2)
```

Where `C` merged in `E` (branch-2's content), and `F` merged in `B` (branch-1's content). Now if you try to merge `C` and `F`, there are *two* possible merge bases: `B` and `E`. The `ort` strategy handles this by creating a virtual merge base — it merges `B` and `E` together first, then uses that result as the merge base. This is more complex but produces fewer spurious conflicts than earlier strategies.

---

## Part 8: Rebasing — Rewriting History Safely

### 8.1 What Rebase Actually Does

Rebasing takes a series of commits and *replays* them on top of a different base commit, creating brand new commit objects with new hashes.

Suppose you have:

```
[A] ← [B] ← [C] ← [D]   (main)
              ↑
              └── [E] ← [F]   (feature)
```

You're on `feature`. Running `git rebase main` does this:

1. Git finds the common ancestor of `feature` and `main` (commit `C`)
2. Git temporarily saves the commits on `feature` that are not in `main` (commits `E` and `F`)
3. Git moves `feature`'s base to point at the tip of `main` (commit `D`)
4. Git replays `E` as a new commit `E'` on top of `D`, resolving any conflicts
5. Git replays `F` as a new commit `F'` on top of `E'`, resolving any conflicts

Result:

```
[A] ← [B] ← [C] ← [D]   (main)
                    ↑
                    └── [E'] ← [F']   (feature)
```

The new commits `E'` and `F'` have different hashes than `E` and `F`. They contain the same *changes* (same diffs), but their parent pointers are different, so their hashes are different. The old commits `E` and `F` still exist in the object store but are no longer reachable via any branch (they may appear in the reflog for a while).

From the outside, it looks like you wrote your feature on top of the latest `main`, even if in reality you branched off an older commit. The history is linear and clean.

### 8.2 The Golden Rule of Rebasing

**Never rebase commits that you have already pushed to a shared branch and that others have based their work on.**

This is not a suggestion. This is a rule that, when violated, causes genuine chaos.

When you rebase and force-push a branch that others have pulled:

1. You have commits `E` and `F` on `feature`
2. Your colleague pulls `feature` and starts work based on `F`, creating `G`
3. You rebase `feature`, creating `E'` and `F'`, and force-push
4. Your colleague tries to push `G`, but `G`'s parent is `F`, which no longer exists on the remote
5. If your colleague pulls, Git sees two diverged histories and tries to merge them, producing duplicate commits (`E`, `E'`, `F`, `F'`, `G`, and a merge commit)
6. The repository history is a mess

The safe domain for rebasing is your *private, unpublished branches*. You can rebase aggressively on branches that only you have pulled. Once a branch is shared, use merge.

### 8.3 Interactive Rebase: Rewriting History With Surgical Precision

`git rebase -i` (interactive rebase) is one of the most powerful features in Git. It lets you rewrite the history of a series of commits before they go public.

```bash
git rebase -i HEAD~5  # interactively rewrite the last 5 commits
```

This opens an editor with a list of the commits to be processed, oldest first:

```
pick a1b2c3d Implement user authentication
pick e4f5g6h Fix typo in error message
pick i7j8k9l WIP: auth token validation
pick m1n2o3p Fix another typo
pick q5r6s7t Finish token validation
```

For each commit you can use one of these commands:

- `pick` (p): use the commit as-is
- `reword` (r): use the commit, but edit the message
- `edit` (e): stop and allow amending this commit (adding more files, splitting it)
- `squash` (s): combine this commit into the previous one, keeping both messages
- `fixup` (f): combine this commit into the previous one, discarding this message
- `drop` (d): delete the commit entirely
- `exec` (x): run a shell command after this commit
- `break` (b): stop here and allow manual work before continuing

A common use case: clean up work-in-progress commits before opening a pull request.

```
pick a1b2c3d Implement user authentication
squash e4f5g6h Fix typo in error message
squash i7j8k9l WIP: auth token validation
squash m1n2o3p Fix another typo
fixup q5r6s7t Finish token validation
```

Result: all five commits become one clean commit, with a combined message (minus the fixup's message).

Another common use: split a commit that was too large.

```bash
# In the rebase script, mark the commit with 'edit'
edit a1b2c3d Large commit that should be two commits
# Git stops here. Reset the staging area:
git reset HEAD^
# Now selectively stage and commit each logical unit
git add src/auth/
git commit -m "Add authentication module"
git add tests/auth/
git commit -m "Add authentication tests"
# Continue the rebase
git rebase --continue
```

### 8.4 Rebase vs. Merge: When to Use Each

This is the subject of endless debate in the Git community, and the honest answer is that both have appropriate uses.

**Use merge when:**
- Integrating a completed feature into a long-lived shared branch (like `main` or `develop`)
- Preserving the full historical record of when features were integrated is important (audit trails, compliance)
- Multiple people are collaborating on the same feature branch
- The branch's history should be preserved as a narrative of how the feature was developed

**Use rebase when:**
- Updating a private feature branch with the latest changes from `main` (to keep it current without creating spurious merge commits)
- Cleaning up a messy local history before sharing with the team
- Working on a feature branch where a clean, linear history will aid code review
- Following a workflow (like "rebase onto main before PR merge") that results in a clean, readable main branch history

**The hybrid approach** that many experienced teams use: rebase aggressively locally (keep your feature branch up to date with `git rebase origin/main`, clean up commits with `git rebase -i`), then merge into main. You get the benefits of both: clean history on feature branches, explicit merge commits recording integration points.

### 8.5 `git rebase --onto`: Moving Branches to Different Bases

The `--onto` flag is one of the most powerful and least-known Git features.

Suppose you have:

```
[A] ← [B] ← [C] ← [D]   (main)
              ↑
              └── [E] ← [F] ← [G]   (feature-a)
                         ↑
                         └── [H] ← [I]   (feature-b)
```

`feature-b` was branched off `feature-a` at commit `F`. But `feature-a` has been redesigned and its commits have been rewritten. You want to move `feature-b` to branch off `main` instead, without including `feature-a`'s commits.

```bash
git rebase --onto main feature-a feature-b
```

This says: "Take the commits on `feature-b` that are not in `feature-a` (commits `H` and `I`) and replay them on top of `main`."

```
[A] ← [B] ← [C] ← [D]   (main)
              ↑            ↑
              │            └── [H'] ← [I']   (feature-b)
              └── [E] ← [F] ← [G]   (feature-a)
```

This is invaluable in scenarios where a feature branch was mistakenly based on another feature branch, or when feature-a was abandoned but feature-b's work should continue.

---

## Part 9: Cherry-Pick — Applying Individual Commits

### 9.1 What Cherry-Pick Does

`git cherry-pick <commit-hash>` applies the changes introduced by a specific commit onto the current branch, creating a new commit.

Crucially, cherry-pick does *not* move the original commit. It reads the diff between the original commit and its parent, and applies that diff to the current HEAD. A new commit is created with a new hash. The original commit is untouched and remains on its original branch.

```bash
git checkout main
git cherry-pick 7f3a1bc  # apply the changes from commit 7f3a1bc to main
```

The new commit on `main` will have:
- The same author (from the original commit)
- A new committer identity (you, now)
- A new parent (the current HEAD of main)
- A new hash

### 9.2 When to Use Cherry-Pick

**Hotfixes to multiple release branches**: You fix a bug on `main`. You need the same fix on `release/2.0` and `release/1.9`, which are no longer direct ancestors of `main`. Rather than re-implementing the fix, cherry-pick the commit to each release branch.

```bash
git checkout release/2.0
git cherry-pick <bug-fix-hash>
git checkout release/1.9
git cherry-pick <bug-fix-hash>
```

**Rescuing work from a dead branch**: Suppose a feature branch was abandoned, but one commit in it contains a useful utility function. Cherry-pick that commit to your current branch to get just that code.

### 9.3 Cherry-Pick Pitfalls

**Conflicts**: Cherry-picking can conflict if the current branch's context is too different from where the original commit was made. The conflict resolution process is the same as for merge conflicts.

**Duplicate commits in history**: If you cherry-pick a commit from `feature` to `main`, and then later merge `feature` into `main`, you will have the same logical change twice — once from the cherry-pick, once from the merge. Git doesn't recognize that they are "the same change" because they have different hashes. This can cause phantom conflicts or confusing history.

**No automatic tracking**: Git doesn't record that a commit was cherry-picked. If you cherry-pick commit `A` from `feature` to `main`, there's no automatic notation in either history. Some teams use `git notes` or include the original hash in the commit message (`(cherry picked from commit 7f3a1bc)`) to track this.

---

## Part 10: git bisect — Finding Bugs in History

### 10.1 The Problem Bisect Solves

Imagine you're debugging a production regression. You know it doesn't exist in `v3.0.0` (released two months ago) but it definitely exists in `v3.1.2` (current). The release contains 847 commits. Which one introduced the bug?

You *could* check out the midpoint of the commit range, test, then narrow the range, then test again. That's binary search — O(log n). For 847 commits, you'd need to test about 10 commits to find the bad one.

`git bisect` automates this binary search.

### 10.2 Using git bisect

```bash
git bisect start
git bisect bad                    # current HEAD (v3.1.2) is bad
git bisect good v3.0.0            # v3.0.0 was good
```

Git checks out the midpoint commit, puts you in detached HEAD state at that commit, and asks you to test.

```bash
# Run your tests or manually check the behavior
# If the bug is present:
git bisect bad
# If the bug is not present:
git bisect good
```

Git narrows the range and checks out the next midpoint. Repeat until Git identifies the exact first bad commit:

```
7f3a1bc9 is the first bad commit
commit 7f3a1bc9d2e4f5a8c6b0d1e2f3a4b5c6d7e8f9a0
Author: Kushal <kushal@example.com>
Date:   Mon Mar 23 14:30:00 2026 -0500

    Refactor order processing pipeline
```

Now you know exactly which commit introduced the bug.

```bash
git bisect reset  # return to the original HEAD
```

### 10.3 Automating Bisect

If you have a test or script that can determine automatically whether a given commit is "good" or "bad", you can fully automate bisect:

```bash
git bisect start
git bisect bad HEAD
git bisect good v3.0.0
git bisect run ./run-test.sh
```

Git will run `./run-test.sh` at each midpoint. A zero exit code means "good"; non-zero means "bad." This can reduce a multi-day debugging exercise to a few minutes of automated testing.

---

## Part 11: The Reflog — Your Safety Net

### 11.1 What the Reflog Is

The reflog is a log of where `HEAD` (and each branch) has pointed over time. It lives in `.git/logs/`. Every time you make a commit, check out a branch, rebase, reset, or perform any operation that moves a ref, an entry is added to the reflog.

```bash
git reflog
# Output:
7f3a1bc HEAD@{0}: commit: Add navigation component
4a2b9e3 HEAD@{1}: checkout: moving from feature to main
2c7d1f8 HEAD@{2}: commit: Fix responsive table breakpoints
```

The reflog is your safety net when things go wrong.

### 11.2 Recovering Lost Commits with Reflog

Scenario: you ran `git reset --hard HEAD~3` to undo three commits, then realized you wanted to keep them.

Without the reflog, those commits would be gone (well, still in the object store, but unreachable). With the reflog, you can find them:

```bash
git reflog
# Find the hash from before the reset
git checkout -b recovery-branch 7f3a1bc
# or
git reset --hard 7f3a1bc  # if you're on the same branch and just want to undo the reset
```

### 11.3 Recovering a Deleted Branch

Scenario: you ran `git branch -D feature/login` thinking you had merged it, but you hadn't.

```bash
git reflog
# Find the last commit hash for feature/login
git checkout -b feature/login <hash>
```

The reflog retains entries for 90 days by default (configurable with `gc.reflogExpire`). After that, the entries expire and the commits become candidates for garbage collection.

### 11.4 `git stash` and the Reflog

`git stash` saves both your staged and unstaged changes as a special kind of commit, stored under `refs/stash`. The stash is itself logged in the reflog.

```bash
git stash push -m "WIP: half-finished authentication"
# Do some other work
git stash pop   # restores the most recent stash
```

`git stash list` shows all saved stashes. `git stash apply stash@{2}` applies a specific stash without removing it from the stash list.

---

## Part 12: Workflows — From Solo Projects to Enterprise Teams

### 12.1 Centralized Workflow

The simplest Git workflow: everyone commits to `main`. Suitable for very small teams or solo projects where branching overhead is not worth the benefit.

```bash
git pull origin main
# Make changes
git add .
git commit -m "Add feature X"
git push origin main
```

Problems: no code review, no parallel development isolation, merge conflicts directly in `main`.

### 12.2 Feature Branch Workflow

The baseline for most professional development:
1. `main` is always deployable
2. New work happens in feature branches
3. Feature branches are merged to `main` via pull requests
4. Pull requests include code review

```bash
git checkout -b feature/oauth-login
# ... develop ...
git push origin feature/oauth-login
# Open PR on GitHub, get review, merge
```

This is the baseline most teams should default to unless they have a specific reason for more complexity.

### 12.3 GitFlow

GitFlow (by Vincent Driessen, 2010) adds additional long-lived branches:

- `main` — production releases only
- `develop` — integration branch for features
- `feature/*` — individual feature branches from `develop`
- `release/*` — release preparation branches from `develop`, merged into both `main` and `develop`
- `hotfix/*` — emergency fixes from `main`, merged into both `main` and `develop`

GitFlow adds formality and traceability but also complexity. It is best suited for software with formal versioned releases (desktop apps, mobile apps, libraries). It is overkill for projects with continuous deployment, where `main` can be deployed at any time.

### 12.4 GitHub Flow

A simpler alternative to GitFlow for continuous deployment:
- `main` is always deployable
- Any change is a feature branch off `main`
- Feature branches are deployed and tested before merging
- Merged to `main` via pull request

This is the workflow GitHub themselves use and recommend for CI/CD environments.

### 12.5 Trunk-Based Development

The most streamlined approach: everyone works on very short-lived branches (or even directly on `main`), integrating frequently. Features are hidden behind feature flags if not ready for users. CI/CD pipelines automatically deploy any passing commit to production.

This approach minimizes merge conflicts (because nobody diverges from trunk for long) but requires excellent CI/CD infrastructure and discipline around feature flags.

---

## Part 13: git Configuration — Defaults That Matter

### 13.1 Essential Global Configuration

```bash
# Identity (required — embedded in every commit you make)
git config --global user.name "Kushal"
git config --global user.email "kushal@example.com"

# Default editor for commit messages, rebase scripts, etc.
git config --global core.editor "code --wait"  # VS Code
git config --global core.editor "nvim"         # Neovim

# Default branch name for new repositories
git config --global init.defaultBranch main

# Always rebase instead of merge on pull
git config --global pull.rebase true

# Better diff algorithm (histogram is generally superior to patience)
git config --global diff.algorithm histogram

# Push only the current branch, not all matching branches
git config --global push.default current

# Automatically set up tracking when pushing a new branch
git config --global push.autoSetupRemote true

# Prune stale remote tracking refs when fetching
git config --global fetch.prune true

# Colorize output
git config --global color.ui auto

# Use rerere (reuse recorded resolution) — saves resolved conflict resolutions
# and re-applies them automatically if the same conflict appears again
git config --global rerere.enabled true
```

### 13.2 Useful Aliases

```bash
git config --global alias.lg "log --oneline --graph --decorate --all"
git config --global alias.st "status -sb"
git config --global alias.last "log -1 HEAD --stat"
git config --global alias.unstage "reset HEAD --"
git config --global alias.undo "reset --soft HEAD~1"
git config --global alias.fixup "commit --amend --no-edit"
```

The `lg` alias gives you a beautiful, compact graph view of the entire repository history.

### 13.3 Repository-Level Configuration

Repository-level configuration in `.git/config` overrides global settings. You can use this to specify per-repo settings without affecting your global configuration:

```ini
[user]
    email = work@company.com   # different email for work repos

[core]
    autocrlf = true   # on Windows, for repos shared with Linux

[push]
    default = current
```

### 13.4 `.gitattributes` — Per-File Settings

The `.gitattributes` file controls how Git treats specific files:

```gitattributes
# Normalize line endings for all text files
* text=auto

# Always use LF for scripts (critical for scripts that run on Linux)
*.sh text eol=lf
*.bash text eol=lf

# Binary files — tell Git not to try text diff
*.png binary
*.jpg binary
*.pdf binary
*.zip binary
*.exe binary

# Force specific diff driver for certain file types
*.cs diff=csharp
*.md diff=markdown

# Exclude from archives and exports
.gitignore export-ignore
.gitattributes export-ignore
```

### 13.5 `.gitignore` — Excluding Files from Tracking

`.gitignore` tells Git which files to ignore. Patterns match relative to the location of the `.gitignore` file.

```gitignore
# Build output
bin/
obj/
out/
dist/

# IDE files
.vs/
.idea/
*.user
*.suo

# Secrets and environment
.env
.env.local
*.pem
*.key
appsettings.Development.json

# OS files
.DS_Store
Thumbs.db

# Test coverage
coverage/
*.coverage

# NuGet
*.nupkg
```

Key rules:
- Lines starting with `#` are comments
- A pattern ending with `/` matches directories only
- A pattern starting with `!` negates the pattern (un-ignores something previously ignored)
- A `**` matches zero or more directories: `**/logs/` matches `logs/`, `a/logs/`, `a/b/logs/`, etc.

Important: `.gitignore` only works for files that are not already tracked. If you've already committed a file and then add it to `.gitignore`, Git will continue to track it. To stop tracking it:

```bash
git rm --cached <file>   # remove from index (untrack) but keep the file on disk
git commit -m "Stop tracking <file>"
```

---

## Part 14: Working with Remotes — Collaboration Mechanics

### 14.1 Cloning and What It Creates

When you run `git clone https://github.com/user/repo.git`, Git:

1. Creates a new directory
2. Initializes a Git repository (`.git/`)
3. Adds a remote named `origin` pointing to the URL
4. Fetches all objects from the remote
5. Creates remote tracking branches for all remote branches (e.g., `origin/main`, `origin/develop`)
6. Creates a local branch (usually `main`) that tracks `origin/main`
7. Checks out the local branch

The `--depth` option creates a *shallow clone* that only downloads the most recent N commits, not the entire history. This is useful for CI/CD pipelines where full history is unnecessary:

```bash
git clone --depth 1 https://github.com/user/repo.git
```

Shallow clones are faster and smaller but cannot be rebased arbitrarily (you don't have the full ancestor history). You can later unshallow: `git fetch --unshallow`.

### 14.2 Fetch vs. Pull

`git fetch`:
- Downloads objects and refs from the remote
- Updates remote tracking branches (`origin/main`, etc.)
- Does NOT modify your local branches
- Does NOT modify your working directory
- Is always safe

`git pull`:
- Runs `git fetch`
- Then runs `git merge` (or `git rebase` if configured) to integrate the fetched changes into your current branch

Rule of thumb: prefer `git fetch` followed by a deliberate `git merge` or `git rebase`. It keeps the two operations separate and lets you see what's coming in before integrating it.

### 14.3 Push and Force Push

`git push origin feature/login`:
- Uploads local commits to the remote
- Asks the remote to advance `feature/login` to your new tip
- Remote accepts only if it's a fast-forward (your new tip is a descendant of the remote's current tip)

If the push is rejected (non-fast-forward), you need to integrate the remote's new commits first:

```bash
git fetch origin
git rebase origin/feature/login  # or git merge origin/feature/login
git push origin feature/login
```

`git push --force` unconditionally overwrites the remote ref. Dangerous on shared branches.

`git push --force-with-lease` is the safe version: it refuses the push if the remote ref has been updated since your last fetch. If someone else pushed in the meantime, the force-with-lease will fail and ask you to fetch first.

### 14.4 Pull Requests and Code Review

Pull requests (PRs) / merge requests (MRs) are not a Git feature — they are a GitHub/GitLab/Bitbucket feature. Git itself knows nothing about them.

A PR is a request to merge one Git branch into another, with a review and discussion interface around that merge. The code review happens in the PR interface; the merge is ultimately a `git merge` (or squash merge, or rebase merge, depending on the platform's settings) performed by the platform.

**PR best practices for .NET developers:**
- Keep PRs small and focused — ideally under 400 lines changed
- Write a meaningful PR title and description (include the "why", not just the "what")
- Reference the issue or ticket being addressed
- Include screenshots for UI changes
- Ensure CI passes before requesting review
- Respond promptly to review comments
- Avoid pushing force to a PR branch unless you've communicated with reviewers (it invalidates their review comments)

---

## Part 15: Common Pitfalls and How to Avoid Them

### 15.1 Committing Sensitive Data

If you accidentally commit secrets (API keys, passwords, certificates), removing them from history requires rewriting history:

```bash
# Modern approach: git filter-repo (requires separate installation)
git filter-repo --path config/secrets.json --invert-paths

# Or for a specific string, replace it across all commits:
git filter-repo --replace-text replacements.txt
```

After rewriting history, all collaborators need to re-clone. GitHub and other platforms have built-in secret scanning and will alert you if known patterns of secrets are pushed. Regardless, treat any committed secret as compromised — rotate it immediately.

Tools like `git-secrets` (by AWS) or `pre-commit` hooks can prevent this from happening in the first place:

```bash
# Example pre-commit hook
#!/bin/sh
if git diff --cached | grep -q "PRIVATE KEY\|API_KEY=\|password="; then
  echo "Potential secret detected in staged files"
  exit 1
fi
```

### 15.2 Pushing to the Wrong Branch

Protect your important branches on the remote:

- GitHub: **Branch protection rules** → require PR reviews, require status checks, prevent force pushes, prevent direct pushes
- GitLab: **Protected branches** with similar options
- Azure DevOps: **Branch policies**

In local `.gitconfig`, you can add a safeguard against accidentally pushing to `main`:

```bash
# This will require explicit confirmation before pushing to main
git config branch.main.pushRemote DO_NOT_PUSH
```

Or use a pre-push hook:

```bash
#!/bin/sh
BRANCH=$(git rev-parse --abbrev-ref HEAD)
if [ "$BRANCH" = "main" ]; then
  echo "Direct push to main is not allowed. Use a feature branch."
  exit 1
fi
```

### 15.3 Merge Conflicts in Long-Running Branches

The longer a branch diverges from `main`, the more painful merging becomes. The solution is frequent rebasing (or merging) of your feature branch against `main`:

```bash
# Every morning, or after every significant push to main:
git fetch origin
git rebase origin/main   # or git merge origin/main
```

This keeps your branch close to the current state of `main`, ensuring that conflicts are small and manageable rather than large and terrifying.

### 15.4 "Dirty" Working Directory During Operations

Git operations like rebase, cherry-pick, and checkout can fail or produce unexpected results if your working directory has uncommitted changes. Get in the habit of checking `git status` before any significant operation, and stashing or committing changes first:

```bash
git stash push -m "WIP before rebase"
git rebase origin/main
git stash pop
```

### 15.5 Confusing Author and Committer

Git tracks two identities for every commit: *author* (who wrote the change) and *committer* (who made the commit). In most workflows these are the same. But when you:
- Apply a patch from an email: you are the committer, the patch author is the author
- Use `git am` or cherry-pick from someone else's branch: the original author is preserved, you are the committer
- Rebase: author dates are preserved, committer date is updated to now (unless you use `--committer-date-is-author-date`)

If you need to fix an incorrect email or name in past commits, use `git filter-repo`:

```bash
git filter-repo --email-callback 'return email.replace(b"wrong@example.com", b"correct@example.com")'
```

### 15.6 Binary Files in Git

Git is designed for text. Binary files — images, compiled artifacts, databases, zip files — can be committed to Git, but they don't benefit from Git's diff and merge capabilities. Every version of a binary file is stored in full in the object store (unless pack-file delta compression happens to help, which it may for some binary formats). A 10-megabyte binary file committed 100 times is 1 gigabyte in the object store.

**Strategies:**
- **Git LFS (Large File Storage)**: replaces large files with small pointer files in the Git repository, and stores the actual content on a separate storage server. Requires server support (GitHub, GitLab, Bitbucket all support it).
- **External artifact storage**: store build artifacts in dedicated registries (NuGet, NPM, Docker) rather than in Git
- **Keep binaries out of the repository**: generated files, compiled outputs — add them to `.gitignore`

### 15.7 Fixup Commits and Maintaining a Clean History

If you notice a mistake in a recent commit that hasn't been shared yet, instead of making a new "fix typo" commit:

```bash
# Stage the fix
git add -p  # or git add <specific-file>
# Create a fixup commit targeting the commit to fix
git commit --fixup <hash-of-commit-to-fix>
# Later, auto-squash during interactive rebase:
git rebase -i --autosquash <base>
```

The `--autosquash` flag automatically moves `fixup!` and `squash!` prefixed commits to the right position in the rebase script and marks them for squash/fixup.

---

## Part 16: Advanced Topics

### 16.1 Git Hooks — Automating Checks

Git hooks are scripts that run automatically at specific points in the Git workflow. They live in `.git/hooks/`. Common hooks:

**Client-side hooks:**

- `pre-commit`: runs before a commit is created; can abort with non-zero exit code. Use for: lint, format checks, running tests, secret scanning.

```bash
#!/bin/sh
# Run dotnet format and fail if formatting is wrong
if ! dotnet format --verify-no-changes; then
  echo "Code formatting issues detected. Run 'dotnet format' to fix."
  exit 1
fi
```

- `commit-msg`: validates the commit message format:

```bash
#!/bin/sh
COMMIT_MSG=$(cat "$1")
if ! echo "$COMMIT_MSG" | grep -qE "^(feat|fix|docs|style|refactor|test|chore)(\(.+\))?: .{1,72}"; then
  echo "Commit message must follow Conventional Commits format."
  echo "Example: feat(auth): add OAuth2 support"
  exit 1
fi
```

- `prepare-commit-msg`: pre-populates the commit message editor (e.g., with the branch name)
- `pre-push`: runs before pushing; can abort to prevent bad pushes

**Server-side hooks** (run on the remote):
- `pre-receive`: runs before any refs are updated; can reject specific pushes
- `update`: runs once per ref being pushed
- `post-receive`: runs after all refs are updated; useful for triggering CI

Note: `.git/hooks` is not tracked by Git (it's inside `.git/`), so hooks don't automatically share with collaborators. Solutions:
- Store hooks in a tracked directory (e.g., `.githooks/`) and configure: `git config core.hooksPath .githooks/`
- Use a tool like `pre-commit` (https://pre-commit.com) which manages hooks as a configuration file

### 16.2 Submodules — Repositories Within Repositories

`git submodule` allows you to embed one Git repository inside another, at a specific commit. Common use cases: vendoring third-party libraries, shared components across multiple repositories.

```bash
# Add a submodule
git submodule add https://github.com/some/library.git lib/library

# Clone a repository with submodules
git clone --recurse-submodules https://github.com/user/repo.git

# Update all submodules to their recorded commits
git submodule update --init --recursive

# Update all submodules to their latest commits on the tracked branch
git submodule update --remote
```

Submodules are tricky. The parent repository doesn't track the submodule's content, only the specific commit. If you forget to commit the updated submodule reference, collaborators will have a different version than you intend. If the submodule's history is rewritten, the recorded commit may no longer exist.

**Alternative**: Git subtree merges (`git subtree`) — pull in another repository's history directly into a subdirectory, as part of the main repository's history. More complex to set up, but less surprising to work with.

### 16.3 Worktrees — Multiple Working Directories

`git worktree` lets you check out multiple branches simultaneously in different directories, all sharing the same object store:

```bash
# Add a worktree for the release branch
git worktree add ../release-2.0 release/2.0

# List active worktrees
git worktree list

# Remove a worktree when done
git worktree remove ../release-2.0
```

This is invaluable when you need to:
- Work on a hotfix while keeping your feature branch intact
- Run a long build/test cycle on one branch while developing on another
- Compare behavior between branches without stashing and switching

### 16.4 Sparse Checkout — Working With Large Repositories

In very large monorepos, you may only care about a subset of the files. Sparse checkout lets you check out only a specific set of directories:

```bash
git sparse-checkout init --cone
git sparse-checkout set src/ObserverMagazine.Web src/ObserverMagazine.Tests
# Only the specified directories are in the working tree
```

The `--cone` mode (recommended) uses a pattern format that is efficient for directory-based filtering.

### 16.5 `git blame` — Understanding Code Provenance

`git blame <file>` shows the last commit that modified each line of a file:

```bash
git blame src/ObserverMagazine.Web/Pages/BlogPost.razor
```

Each line shows: commit hash, author, date, and line content. Useful for understanding why a line was written a certain way, or who to ask about a confusing piece of code.

`-L <start>,<end>` limits the output to specific lines. `--ignore-rev <hash>` ignores a specific commit (useful for large formatting commits that would otherwise dominate `blame` output). `--ignore-revs-file .git-blame-ignore-revs` lets you specify a file of commits to ignore.

### 16.6 `git log` — Mining History

`git log` is far more powerful than most developers use:

```bash
# One line per commit, with graph and branch labels
git log --oneline --graph --decorate --all

# Show commits that changed a specific file
git log -- src/Services/BlogService.cs

# Show commits by a specific author
git log --author="Kushal"

# Show commits in a date range
git log --since="2026-01-01" --until="2026-04-01"

# Show commits that changed a specific function (language-aware)
git log -L :GetPostsAsync:src/ObserverMagazine.Web/Services/BlogService.cs

# Show commits that added or removed a specific string
git log -S "ProcessOrder" -- *.cs

# Show commits where the patch contains a specific string (regex)
git log -G "void.*Process.*Order" -- *.cs

# Show the range of commits between two branches
git log main..feature/login  # commits on feature/login not on main
git log main...feature/login # commits on either, not on both (symmetric diff)
```

The `-S` and `-G` options (called the "pickaxe") are particularly powerful for finding when specific code was introduced or removed. `-S` finds commits where the count of a string changed (it was added or removed). `-G` finds commits where a line matching the regex appears in the diff.

### 16.7 `git bisect` with Custom Scripts (Revisited)

For .NET projects, a bisect run script might look like:

```bash
#!/bin/bash
# bisect-test.sh

# Restore and build
dotnet restore --no-cache
dotnet build --no-restore
if [ $? -ne 0 ]; then
  # Build failed - mark as "skip" not "bad"
  # Exit code 125 tells git bisect to skip this commit
  exit 125
fi

# Run specific test that catches the regression
dotnet test tests/ObserverMagazine.Integration.Tests \
  --filter "FullyQualifiedName~ProcessOrderTests" \
  --no-build

exit $?
```

```bash
git bisect start
git bisect bad HEAD
git bisect good v2.0.0
git bisect run ./bisect-test.sh
```

Exit code 125 is special: it tells bisect to *skip* that commit (mark it as neither good nor bad), which is useful for commits that don't build (can't determine if they're the culprit).

---

## Part 17: SHA-1, SHA-256, and the Hash Transition

### 17.1 Why SHA-1 Has Been a Concern

SHA-1 is a 160-bit hash function. For years it was considered collision-resistant enough for Git's purposes. In 2017, the SHAttered attack demonstrated the first practical SHA-1 collision — two distinct PDF files with the same SHA-1 hash. While the specific attack did not apply to Git's object format (Git uses a variant of SHA-1 that's resistant to the SHAttered attack), it raised legitimate concerns about the long-term security of the hash function.

Linus Torvalds himself noted that SHA-1 was chosen primarily for speed and integrity checking against accidental corruption, not as a cryptographic security guarantee. The actual security model relies on signing (GPG-signed commits and tags) at a higher level, not solely on the hash function.

### 17.2 The SHA-256 Transition

Git 2.29 (October 2020) introduced experimental support for SHA-256 repositories, where all object hashes are 256-bit SHA-256 hashes. This provides a much larger safety margin against collision attacks.

```bash
git init --object-format=sha256 my-repo
```

SHA-256 repositories are not interoperable with SHA-1 repositories without explicit conversion. As of 2026, most hosting platforms support SHA-256 repositories, and the Git project is working toward eventually making SHA-256 the default.

For most developers, SHA-1 repositories remain fine for years to come. The practical risk of a SHA-1 collision in a typical software project is negligible. But for security-sensitive projects or organizations with long time horizons, migrating to SHA-256 is prudent.

---

## Part 18: Git in CI/CD — Practical Patterns

### 18.1 GitHub Actions and Git

GitHub Actions workflows trigger on Git events. Understanding the Git context in workflows is important:

```yaml
name: CI

on:
  push:
    branches: [main, 'feature/**']
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # fetch full history (needed for git describe, git log, etc.)
          # Default is fetch-depth: 1 (shallow clone)

      - name: Get version from tag
        run: |
          VERSION=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
          echo "VERSION=$VERSION" >> $GITHUB_ENV

      - name: Build
        run: dotnet build --configuration Release

      - name: Test
        run: dotnet test --configuration Release --no-build
```

The `fetch-depth: 0` is crucial for operations that need the full history: `git describe`, `git log`, `git bisect`, and anything that computes version numbers from tags.

### 18.2 Generating Version Numbers from Git

A common pattern is deriving semantic version numbers from Git tags:

```bash
# Most recent annotated tag (e.g., v2.1.0), or describe it with additional info
git describe --tags --abbrev=7
# Output examples:
# v2.1.0          (if HEAD is exactly at the tag)
# v2.1.0-14-g7f3a1bc  (14 commits since v2.1.0, HEAD is g7f3a1bc)
```

In .NET projects, `Directory.Build.props` can incorporate the Git version:

```xml
<Project>
  <PropertyGroup>
    <Version>$(GitVersion)</Version>
  </PropertyGroup>
  <Target Name="GetGitVersion" BeforeTargets="Build">
    <Exec Command="git describe --tags --abbrev=0" ConsoleToMsBuild="true"
          IgnoreExitCode="true">
      <Output TaskParameter="ConsoleOutput" PropertyName="GitVersion" />
    </Exec>
    <!-- Strip leading 'v' if present -->
    <PropertyGroup>
      <GitVersion>$([System.String]::Copy('$(GitVersion)').TrimStart('v'))</GitVersion>
    </PropertyGroup>
  </Target>
</Project>
```

Tools like `GitVersion` (the NuGet package) provide more sophisticated version computation from Git history, including support for semantic versioning, release channels, and hotfix version bumping.

### 18.3 Commit Message Conventions

**Conventional Commits** (https://www.conventionalcommits.org) is a widely adopted specification for commit messages:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`

A `feat` commit triggers a minor version bump in semantic versioning. A `fix` triggers a patch version bump. A commit with `BREAKING CHANGE:` in the footer triggers a major version bump.

```
feat(blog): add TTS audio player for blog posts

Adds a text-to-speech audio player component that reads blog post
content aloud. Uses browser Web Speech API with a KittenTTS-generated
MP3 as fallback.

Closes #42
```

Conventional Commits enables automated changelog generation and automated version bumping in CI/CD pipelines.

---

## Part 19: Practical Git for .NET and C# Developers

### 19.1 `.gitignore` for .NET Projects

A comprehensive `.gitignore` for .NET development:

```gitignore
## .NET
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
.vs/
*.vspscc
_Upgrade_Report_Files/

## Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww]in32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

## NuGet
*.nupkg
*.snupkg
**/[Pp]ackages/*
!**/[Pp]ackages/build/
*.nuget.props
*.nuget.targets
project.lock.json

## ASP.NET
wwwroot/lib/    # only if using LibMan or CDN-managed assets
appsettings.*.json   # if you have secret-containing override files
!appsettings.Development.json  # keep dev settings if non-secret

## Blazor WASM
dist/

## Test results
TestResults/
coverage/
*.coverage
*.coveragexml

## ReSharper
_ReSharperCaches/
*.DotSettings.user

## Rider
.idea/
*.sln.iml
```

### 19.2 Git Integration in Visual Studio and Rider

Both Visual Studio and JetBrains Rider have excellent Git integration:

**Visual Studio:**
- `View > Git Repository` for a full visual graph
- `View > Git Changes` for staging, committing, and resolving conflicts
- Pull requests directly from the IDE (with GitHub / Azure DevOps integration)
- Branch management via the bottom status bar

**JetBrains Rider:**
- `Git > Log` for the full commit history graph
- `Git > Branches` for branch management
- Inline diff and blame via the gutter
- `Git > Show History for Selection` to see history for specific lines
- Conflict resolver with three-way diff UI

For command-line enthusiasts, the cross-platform `lazygit` (a terminal UI for Git) provides a visual experience in the terminal.

### 19.3 Practical Workflow for Observer Magazine Development

For a project like Observer Magazine (Blazor WASM, GitHub Pages deployment), a sensible workflow might be:

```bash
# Start a new article or feature
git checkout main
git pull origin main
git checkout -b content/2026-04-22-git-guide

# Work, test, iterate
dotnet format
dotnet restore
dotnet run --project tools/ObserverMagazine.ContentProcessor -- ...
dotnet test

# Commit incrementally with meaningful messages
git add content/blog/2026-04-22-git-guide.md
git commit -m "docs(blog): add comprehensive Git guide article"

# Before opening PR, make sure you're up to date
git fetch origin
git rebase origin/main  # or git merge origin/main

# Push and open PR
git push origin content/2026-04-22-git-guide
# GitHub Actions (pr-check.yml) will run tests automatically

# After PR is merged, clean up
git checkout main
git pull origin main
git branch -d content/2026-04-22-git-guide
```

This workflow keeps `main` always deployable, uses short-lived branches, and integrates CI/CD checks before merging.

---

## Part 20: Misconceptions — A Summary and Debunking

Let's gather all the misconceptions we've touched on and address them systematically.

**Misconception 1: "A branch is a copy of the code."**
Reality: A branch is a 41-byte file containing a single commit hash. No code is copied. Creating a branch costs essentially nothing.

**Misconception 2: "Commits store diffs."**
Reality: Commits store complete snapshots of the working tree. Diffs are computed on-the-fly by comparing snapshots.

**Misconception 3: "git commit --amend edits the commit."**
Reality: It creates a new commit object and moves the branch pointer. The old commit still exists.

**Misconception 4: "Commits are ordered by time."**
Reality: Commits are ordered by parent-child relationships. Timestamps are metadata that can be set arbitrarily.

**Misconception 5: "If Git doesn't report a conflict, the merge is correct."**
Reality: Git operates at the textual level. Semantic conflicts (type mismatches, API contract violations, logic errors) are invisible to Git and require human verification and automated tests.

**Misconception 6: "Tags are immutable."**
Reality: Lightweight tags can be moved with `git tag -f`. Annotated tags can be deleted and recreated. Only the convention of treating tags as immutable makes them so. Push protection and team discipline are the actual enforcement mechanism.

**Misconception 7: "Rebasing is dangerous and should be avoided."**
Reality: Rebasing *unpublished* branches is safe and produces cleaner history. Rebasing *shared, published* branches is dangerous. Understanding the distinction is the key.

**Misconception 8: "Detached HEAD is an error state."**
Reality: Detached HEAD is a deliberate and useful state for inspecting history, running bisect, or experimenting. It only becomes a problem if you make commits and then leave without capturing them in a branch.

**Misconception 9: "git pull is always safe."**
Reality: `git pull` does a `git merge` by default, which can create merge commits in your local history unexpectedly. Using `git pull --rebase` (or configuring `pull.rebase = true`) keeps history linear. Or use `git fetch` + deliberate merge/rebase for full control.

**Misconception 10: "The remote repository is the authoritative source of truth."**
Reality: In a distributed VCS like Git, every clone is equally authoritative. What the remote has is what the team has agreed to treat as the canonical state by convention, not by technical enforcement. `git push --force` can change the remote's history to match yours, which is why protection rules matter.

**Misconception 11: "Two branches from the same base can always be merged without conflicts if they touch different parts of the codebase."**
Reality: If two branches each touch *textually different* parts of each file, they merge without textual conflicts. But semantic conflicts (incompatible changes to related code across different files) are invisible to Git. Always run tests after any merge.

**Misconception 12: "git revert undoes a commit."**
Reality: `git revert <hash>` creates a *new commit* that introduces the inverse of the specified commit's changes. The original commit remains in history. This is the safe way to undo changes on a shared branch. If you want to truly remove commits from history (on an unshared branch), use `git reset`.

---

## Conclusion

Git is a profoundly well-designed system. The object model — blobs, trees, commits, tags, all identified by content hashes, all immutable once written, all connected in a DAG — is elegant, efficient, and deeply consistent. Once you understand the model, seemingly magical operations (cheap branching, bisect, reflog recovery) become obvious. And seemingly inexplicable behaviors (conflicts when merging siblings, silent semantic conflicts, the chaos of force-pushing a rebased branch) become predictable.

The scenario from `github.com/kusl/learningbydoing` illustrates the most important practical lesson: **merging is about 3-way text comparison, not about logic**. Two branches that both modified line 5 of the same file cannot be merged automatically, regardless of which platform you use or what strategy you apply. That's working as designed. And two branches that modified *different* files can be merged automatically, but the result may still be semantically wrong — which is why CI/CD and comprehensive testing are not optional in serious software development.

Master the mental model — objects, refs, HEAD, the DAG — and everything else follows. The commands are just syntax on top of the model.

---

## Resources

- **Pro Git (free online)**: https://git-scm.com/book/en/v2 — the canonical reference, authored by Scott Chacon and Ben Straub
- **Git Reference Manual**: https://git-scm.com/docs — official documentation for every Git command
- **Git Internals (Pro Git Chapter 10)**: https://git-scm.com/book/en/v2/Git-Internals-Plumbing-and-Porcelain
- **Conventional Commits Specification**: https://www.conventionalcommits.org
- **Git Flight Rules**: https://github.com/k88hudson/git-flight-rules — a guide for what to do when things go wrong
- **Oh Shit, Git!**: https://ohshitgit.com — plain-English recovery procedures for common mistakes
- **learngitbranching.js.org**: https://learngitbranching.js.org — interactive visual Git tutorial
- **Atlassian Git Tutorials**: https://www.atlassian.com/git/tutorials — comprehensive tutorials on all topics
- **GitHub Git Guides**: https://github.com/git-guides — quick-start guides from GitHub
- **Git source code**: https://github.com/git/git — the source code itself, which is highly readable
- **gitoxide**: https://github.com/Byron/gitoxide — a modern Rust implementation of Git, useful for understanding the protocol
- **libgit2**: https://libgit2.org — the C library for embedding Git operations in applications (used by GitHub Desktop, VS, Rider)
