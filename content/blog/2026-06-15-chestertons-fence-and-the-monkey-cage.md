---
title: "Chesterton's Fence and the Monkey Cage: A Developer's Guide to Knowing Which Rules to Keep and Which to Tear Down"
date: 2026-06-15
author: myblazor-team
summary: "A long, patient essay on the two opposite failure modes every software engineer stumbles into — throwing away rules you do not understand, and blindly obeying rules nobody understands any more. We work through Chesterton's Fence, Hyrum's Law, cargo-culting, vicarious learning, institutional inertia, and the safe-to-fail probe, grounded in first-hand stories from house renovation, a TFS-to-Git migration, and a modern Angular 21 signal-forms rewrite. Bad code, good code, honest acknowledgement of which famous parables are myths, and a decision framework you can actually use on Monday morning."
tags:
  - software-engineering
  - best-practices
  - architecture
  - angular
  - dotnet
  - csharp
  - migration
  - legacy-code
  - philosophy
  - first-principles
  - chestertons-fence
  - opinion
  - deep-dive
---

There are two ways to get hurt in this profession.

The first is to ignore the rules that were written in blood. You walk into a codebase, see something that offends your aesthetic sensibilities, rip it out, and two weeks later production goes down during the month-end batch run because that ugly `if` branch was the only thing catching a particular daylight-saving edge case that fired exactly once a year. Nobody remembered why the line was there. You did not bother to ask. The old timer who wrote it retired in 2014.

The second is to obey rules nobody can explain any more. You join a team, try to do something perfectly reasonable, and everybody around you recoils as if you had suggested eating from the floor. "We do not do that here." Why? Nobody is quite sure. Something bad happened once. Maybe. Or maybe somebody read a blog post in 2011 and nobody ever re-read it. You defer. The team defers. The thing that was reasonable quietly becomes impossible, and the next new hire learns by osmosis that it cannot be done, without ever knowing why, and the new hire after that will pass the lesson along with even less context.

Both failures are, at heart, the same failure. They are the failure to ask *why*.

This post is long because the question is hard. It is a question about how you learn as an engineer — how you inherit wisdom from people who are not in the room, how you tell the difference between wisdom and superstition, and how you avoid either dying in a helicopter you welded in your workshop or starving in a cage full of monkeys nobody has hosed in eighteen years. The wrong answer on either side is expensive. Sometimes it is fatal. We are going to walk through it carefully.

The running examples will be a house renovation, a Team Foundation Server to Git migration, and a modern Angular 21 application using signals and the new Signal Forms API that landed experimental in late 2025. There will be bad code and good code, and I will try to tell you honestly why the bad code is bad in a way that does not require you to already share my taste. The target audience is a working ASP.NET developer — someone comfortable with C#, familiar with the shape of a web application, but perhaps rusty on the wider craft conversation, and perhaps carrying a few habits picked up in 2011 that nobody has questioned since.

The goal is not to make you a better Angular developer, though we will cover enough Angular 21 to build a real application. The goal is to teach you how to think about rules. The Angular is the vehicle. The *thinking* is the cargo.

Let us begin.

## Part 1: The Thesis — Why You Cannot Do First-Principles Thinking From First Principles

There is a phrase that has drifted out of the deep-tech press and into every mid-level engineer's vocabulary over the last decade. *First principles*. It is usually invoked in the same breath as a billionaire's name. We are told that the way to do great work is to strip a problem down to the physics and the economics and the irreducible truths, and to rebuild from there without letting ourselves be anchored to what everyone else is doing.

This is sometimes good advice. It is the right posture when you are trying to do something genuinely novel, where the existing answers are poorly fitted to the problem, where the conventional wisdom is the accumulated result of a lot of local optimisation that has forgotten the original constraints. Elon Musk's argument that rockets are expensive because of how they are built, not because of a law of physics, is — whatever you think of Musk — a fair piece of reasoning. The cost of lifting a kilogram to orbit is not written in the standard model. It is the result of a supply chain that never had to optimise.

But. There is a version of first-principles thinking that is not thinking at all. It is an excuse. It sounds like this.

> I did not bother to read the documentation because I wanted to derive the API from scratch.

> I did not look at how the old team solved this because I wanted to start fresh.

> I am not using a framework because I want to understand the web.

> That codebase is a mess, we should rewrite it.

This version of first-principles thinking is a flex. It lets you feel clever for redoing something that has already been done, and it lets you avoid the honest and deeply annoying work of reading somebody else's code and finding out what they knew that you do not.

The problem with doing first-principles thinking from first principles is that life is short. You cannot build every wheel yourself. You cannot personally rediscover that concrete needs rebar, that databases need connection pools, that web apps need CSRF tokens, that rotors are dangerous. Every hour you spend rediscovering a solved problem is an hour you are not spending on the actual problem you are paid to solve.

And the problem with trying is that some of those rediscoveries will kill you before you finish.

### The Welder from Yavatmal

In August 2021, a young man named Shaikh Ismail was killed in a field near the village of Fulsawangi in Maharashtra. He was twenty-four or twenty-nine years old depending on which report you read. He was a welder by trade and had spent two years building, in his own workshop, a single-seater helicopter from scrap metal and the engine of a Maruti 800. He had taught himself the basics of the thing by watching YouTube videos. The police report said the tail rotor failed on takeoff and struck him in the throat. He died before the ambulance arrived.

I want to be careful here. This is a real person, not a parable, and I do not want to use his death as a rhetorical cudgel. Shaikh Ismail was by all accounts a determined, talented, courageous man. He wanted to build a helicopter cheap enough that rescue workers could use it in the rural disasters that regularly kill people in his part of the world. His friends filmed the test flight. His family was proud. The story is not tidy enough to make a lesson out of.

But there is a technical fact inside this human tragedy that every engineer should sit with. Certified helicopter manufacturers in 2021 did not fit tail-rotor blades out of random welded steel. They used specific alloys, with specific heat treatments, inspected for microscopic cracks with dye penetrant and magnetic particle techniques, torqued to specific values, and replaced on a mandatory service schedule — *because* manufacturers had been burying tail-rotor pilots since the 1940s and had learned, one funeral at a time, which failure modes were survivable and which ones were not. The aerospace industry's accumulated paranoia about tail rotors is written in those funerals. When you build a helicopter from first principles with a Maruti engine and what you learned on YouTube, you are not being innovative. You are starting the body count over. You are the first man in the queue.

This is the sharp end of the argument. *Most* established practices are there because they were written in blood. Not all, but most. When you stand in front of a convention you do not understand and ask yourself whether to keep it or throw it out, the base rate is that it is there for a reason, and the cost of finding out what that reason is by experiment is sometimes absurdly high. Sometimes your tail rotor does not just break your build.

### The Cheaper Version of the Same Lesson

Most software engineers are not in the tail-rotor business, thank God. Our mistakes are cheaper. But they rhyme.

The first time I deep-cleaned an old rental house — a friend's grandmother's place, dust on every surface, the kind of accumulated biography an old house collects — we decided to start with the floors. It made sense. The floors are what you see when you walk in. We sanded the wood, applied two coats of polyurethane, waited the cure time, admired our work, and then started on the walls. Scraped the old paint. Climbed up to clean the ceiling. You know where this is going. Dust, paint chips, and cobweb residue from three feet above landed in a fine grey drizzle on our newly polyurethaned floors. We re-sanded. We re-coated. We added a week to the project.

Nobody had told us. Nobody had to tell us. Any painter on earth, any carpenter on earth, any contractor on earth, would have laughed at us before we started. *Top down, son. Always top down. You clean the ceiling first. Then the walls. Then the floors. Because gravity.* It is the most obvious rule in the world once somebody has said it. It is completely invisible if they have not. And the cost of rediscovering it from first principles — which is literally what we did — was seven days and several hundred dollars of polyurethane.

I do not resent that lesson because nothing irreversible happened. But I think about it every time I see somebody starting a complicated project by jumping into the middle of it. It is always the same pattern. *I do not know what I do not know, and so I optimise the thing I can see, and then I find out that the thing I could not see invalidates all my work.*

The software version of top-down-cleaning is *migrate your source control first, then modernise the code*. I will come back to this in Part 2 because it deserves its own section. But the point is the same. The rule is trivial if somebody has told it to you. It is expensive to discover by experiment. And you will almost certainly not discover it in time to save your first project.

### Vicarious Learning: The Actual Superpower

The ability to learn from other people's mistakes without making them yourself is the single largest evolutionary advantage human beings have. It is why a culture's accumulated wisdom about which mushrooms will kill you gets transmitted across generations instead of having to be rediscovered by every new generation of toddlers. It is why a young doctor starting residency is not expected to personally reproduce the nineteenth century's germ theory experiments. It is why the Chief Engineer at Boeing does not need to personally design every rivet from free-body diagrams.

Psychologists call this *vicarious learning* — learning from the experiences, successes, and (especially) failures of others rather than through direct trial and error. It is the cheap form of education. It is why books exist, why the Stack Overflow archive has value even now that the traffic has moved on to other places, why senior engineers get paid more than junior ones, and why reading the documentation is almost always faster than guessing.

The arrogance that shows up in some engineering cultures — the idea that reading a book is less rigorous than rediscovering the book's contents yourself — is not a virtue. It is a luxury, and usually a fake one. The engineers who make the biggest visible leaps in their careers are almost always the engineers who have read more than their peers. Every ten pages of somebody else's post-mortem is another tail rotor you do not have to hit.

There is a second side to this which we will get to in Part 3, where we talk about what happens when the accumulated wisdom turns out to be wrong. But we have to earn our way there. First we have to understand the default posture, which is *the experts probably know something I do not, and my first job is to find out what*.

### The Cost-of-Experience Inequality

Here is a useful way to think about it that I stole from a friend who spent a decade in manufacturing before moving into software. Call it the cost-of-experience inequality.

> The cost of reading how somebody else solved a problem is bounded by your time.
> The cost of reinventing the solution from scratch is bounded by how wrong you can be before it kills you.

When the downside is small — a bug in a hobby project, a week of redone sanding, a single-page form that does not validate correctly — reinventing is fine. It is even educational. You will remember the lesson better than if somebody had told you.

But the downside is *not always small*. Medical dosing is not. Cryptographic primitives are not. Tail rotors are not. Kubernetes configuration is not, if your company's uptime pays your salary. The cost-of-experience inequality tells you to scale your willingness to reinvent to the actual cost of being wrong.

This is why the next section is about *Chesterton's Fence*, which is the single most useful mental tool I know for calibrating how much respect to show a rule you do not understand. But before we get there, we have to acknowledge the complication — because if the story ended with "just respect your elders", it would be a much shorter essay. It does not. There is a second failure mode, and it is the exact opposite of the first one, and a serious engineer has to hold both in mind at once.

## Part 2: The Antithesis — The Cage Full of Monkeys Nobody Has Hosed In Years

You have probably heard the story about the monkeys.

It goes like this. A researcher puts five monkeys in a cage. At the top of a ladder in the middle of the cage, a bunch of bananas. Every time a monkey tries to climb the ladder, all five monkeys get sprayed with ice-cold water. Pretty soon, the monkeys learn. When a monkey heads for the ladder, the others pull it down. Over time, the researcher replaces each of the five monkeys with a new one. The new one inevitably goes for the bananas. The other monkeys beat it up. Eventually there is a cage full of monkeys, none of whom has ever actually been sprayed with water, all of whom will beat up any newcomer who tries to climb the ladder. The punchline, usually delivered with a wry smile, is *this is why we do things the way we do things around here*.

I am going to tell you something about this story that the people who tell it usually leave out. It is not a real experiment.

There is a real 1967 paper by G.R. Stephenson called *Cultural acquisition of a specific learned response among rhesus monkeys*, published in a proceedings volume on animal behaviour. It used air blasts, not water, on four pairs of monkeys manipulating plastic kitchen utensils. It was a study of pairwise social learning, not of group dynamics. Stephenson's results were mixed and did not, in their actual form, support a clean "mob mentality" narrative. The ladder-and-bananas parable as we know it appears to have been popularised — in a form that does not match anything Stephenson actually did — by Gary Hamel and C.K. Prahalad in their 1994 book *Competing for the Future*, and has been shared around corporate training decks and LinkedIn posts ever since, usually with a confidence that is not epistemically earned.

I tell you this because it matters. The story is being used to make a real point, and the real point is correct, but the story itself is not a piece of evidence. Pretending it is weakens the very argument it is trying to make. An honest engineer has to notice when a parable has been upgraded to a citation.

(We will come back to this. The tendency of software culture to take a folk story, dress it up with a scientific veneer, and then treat the dressed-up version as load-bearing is itself a symptom of the very problem we are trying to address. I have seen the five-monkeys story cited in kickoff meetings as if it settled the argument. It does not. It illustrates the argument, which is a different thing.)

What the parable *is*, is a clean dramatisation of a phenomenon that does very much exist. That phenomenon has several names. In software, we usually call it *cargo-culting*. In organisational theory, it shows up as *institutional inertia* or *competency trap*. Anthropologists studying actual cultural practices sometimes call it a *dead ritual*. The shape is always the same. A practice was once connected to a reason. The reason went away — the hose got unplugged, the constraint went away, the tool was upgraded, the regulation was lifted, the founder retired — but the practice did not, because everybody downstream of the reason learned the practice without learning the reason.

### A Real Example from the Codebase Next Door

Let me give you a concrete software example that happened on a project I worked on, changed slightly to protect the guilty.

We had a C# service that, for every database write, did the write twice. Literally. It wrote a row, read it back, compared the read to what it had written, and if the two did not match, it wrote it a second time. This was not idempotency logic — there was no message deduplication, no retry framework, no distributed consensus problem it was solving. It was a straight double-write with an equality check.

The junior who was asked to refactor it wanted to rip it out. It was obviously absurd. The equality check had never failed in production as far as anybody could tell; we had log lines going back four years, and the "mismatch" branch had fired zero times. The double write was adding roughly forty per cent to the database write load on a system that was already creaking at peak traffic. Every argument pointed to deleting it.

I was the one who told her not to. Not because I knew *why* the double-write was there — I did not — but because I knew I did not know, and the blast radius of being wrong was large. The old engineer who wrote the service had retired five years prior. There was a commit message. The commit message said, and I am not making this up, "fix the issue."

We went through the code history. We found a bug report from 2013. The bug report referenced a specific version of a specific database driver, which was known to, under a specific race condition involving connection pooling and transient network partitions, *silently lose writes* — accept them, return success, and then not commit them. The double-write pattern was a workaround for a driver bug. The driver had been patched in 2015. The workaround had survived the driver upgrade, survived the database version upgrade, survived two rewrites of the business logic layer, and was now, eight years later, an expensive superstition.

We deleted it, with a paragraph-long comment explaining why we deleted it and what to look for if writes ever started disappearing. Throughput went up. Nobody got hosed.

But the juniors on the team could have, and almost did, delete it without understanding any of that. And if it *had* been there for a live reason — if the driver bug had not been patched, or had been patched only in a version we had not rolled out yet — we would have lost writes in production, and the first sign would have been a customer calling up to ask why their order had vanished. The double-write was doing real work, at one point. The question is whether it still is.

This is the full shape of the problem. You have to be able to hold both things at once:

- **The practice might be keeping you alive.** (Chesterton's Fence, Part 1.)
- **The practice might have outlived its reason.** (Monkey cage, Part 2.)

You do not get to pick one posture and stop. The senior engineer's job is to know which one applies in each particular case, and that is a skill — a real, learnable, costly skill — not a vibe.

### Hyrum's Law, and Why Even Obsolete Rules Are Rarely Harmless

There is a famous piece of internet wisdom, attributed to Google engineer Hyrum Wright and formalised as *Hyrum's Law*:

> With a sufficient number of users of an API, it does not matter what you promise in the contract: all observable behaviours of your system will be depended on by somebody.

The implication is that once a behaviour has existed long enough, somebody somewhere has quietly built on top of it, often without realising they were depending on it. When you change the behaviour — even behaviour you explicitly documented as unreliable — you will break that somebody. This is why you cannot freely delete even obviously obsolete code without checking.

Hyrum's Law applies to your own codebase as much as it does to public APIs. The ugly `if` branch that nobody remembers writing is almost certainly being relied on by something downstream of it. The retry loop that looks too conservative is probably the reason that a flaky upstream dependency never takes your service down. The feature flag that has been enabled for three years and that everybody calls obsolete may be the only reason a particular customer's integration has not had to be rewritten.

In the double-write example above, we were lucky. The behaviour was observable (we logged every write), the dependency chain was short (we controlled both sides), and we could trace the original reason. In most real-world cases you will not be so lucky. You will have to make the call under uncertainty.

So how do you make it?

## Part 3: The Synthesis — Chesterton's Fence, In Full

There is a short passage in G.K. Chesterton's 1929 book *The Thing* that every engineer should know by heart. Chesterton was a British Catholic journalist who wrote a great deal about tradition, reform, and the difference between the two. He was, as these things go, a professional reactionary, but do not let that throw you. The passage is about *method*, and the method is one of the most useful ideas in any field where you have to reform a system you did not build.

I will paraphrase it rather than quote it, because you can find the original in half a dozen places online and the exact wording is not the point. The point is the parable.

Imagine you are walking down a country road and you come across a fence. It appears to serve no purpose. It does not enclose anything; it does not keep anything out; it does not even neatly divide one field from another. It is just a fence, in the middle of nowhere, inconveniencing you.

The naive reformer says: *I see no use in this fence. Let us clear it away.*

Chesterton's reply: *If you do not see the use of it, I certainly will not let you clear it away. Go away, and think. Then, when you can come back and tell me that you do understand the use of it, I may allow you to destroy it.*

This is *Chesterton's Fence*. It is one of the most-cited ideas in modern software culture, to the point where I am slightly embarrassed to be reciting it. But its popularity has not, on the whole, made people any better at applying it. They invoke it when they want to block a change, and they conveniently forget it when they want to push one through. The whole point of the rule is that it cuts both ways, and that it is specifically designed to *force you to do the work of understanding before you are allowed to act*.

### What the Rule Actually Says, and What It Does Not Say

Three things about Chesterton's Fence are commonly misunderstood.

**One**: the rule is not *keep the fence*. The rule is *find out what the fence does, and then decide*. Chesterton's wise reformer is explicit that once you understand the fence's purpose, you may be allowed to destroy it. The rule is procedural, not conservative. It tells you the *order of operations*, not the final answer.

**Two**: the rule is not about respecting your elders. It is about respecting *information*. The fence is not protected because the person who built it had authority; it is protected because the fact that it was built is evidence that somebody, at some point, had a reason. You are being asked to take that evidence seriously as evidence. If you can demonstrate that the reason was bad, or has expired, or was based on a mistake, you are free to act.

**Three**: the rule is not *do not change things*. Chesterton was writing in 1929, and he was writing about political and religious reform. He was not writing a conservative manifesto; he was writing against *thoughtless* reform specifically. The contrast is not change versus stasis. The contrast is reform that has understood the thing it is changing versus reform that has not.

You can think of it as the engineering dual of *measure twice, cut once*. The fence is the cut. The measuring is the question *why is this fence here*.

### The Two Failure Modes, Stated Precisely

With Chesterton's Fence in mind, we can now state both failure modes from Parts 1 and 2 in the same language:

- **Failure mode one** (the Yavatmal helicopter): you cleared away a fence without understanding it. The fence was the tail-rotor safety regulations. You did not see the point of them. You cleared them away. The fence was load-bearing. You died.

- **Failure mode two** (the monkey cage): you kept a fence that no longer served any purpose. The fence was the "do not climb the ladder" rule. Nobody remembered why. Everybody enforced it. The fence was expensive (you did not get the bananas). You enforced it anyway.

The *symmetry* is the point. Chesterton's Fence, properly applied, protects you from both. The question is always the same: *why is this thing here?* The answer might tell you to keep it. The answer might tell you to remove it. The answer might tell you that the reason has changed and the thing needs to be replaced with something different. But you cannot reach any of those answers if you have not asked the question.

And now the hard part, which is actually answering the question.

### How To Tell a Live Fence From a Dead One

In a real codebase, in a real organisation, with real deadlines, you do not have the luxury of investigating every single rule you inherit. You have to triage. You have to decide which fences to investigate deeply, which to leave alone, and which you can reasonably replace.

Here is the framework I actually use. It is not original to me — bits of it are in every book on refactoring legacy code, and I have cribbed heavily from Michael Feathers' *Working Effectively with Legacy Code* and Gregor Hohpe's essays on architecture in particular — but I have found the synthesis useful.

**Step one: trace the genealogy.** When you encounter a rule, a piece of code, a configuration setting, or a process that looks wrong to you, find out where it came from. Git blame. Commit messages. Issue tracker. Slack archives. Old wiki pages. The lunchroom. Email the retired engineer; engineers are almost always happy to talk about their old work. The goal of step one is to find out *why* the thing exists. If you can, find out *when* the reason last applied, because that is usually the more interesting question.

**Step two: distinguish physics from phantoms.** Read the reason critically. Is it a hard constraint that still applies? (The tail-rotor example: aerodynamic forces on a steel pipe have not changed since 1940.) Is it a soft constraint that still mostly applies? (The "always size your thread pool to the number of CPU cores" rule was true in 2006 and is still mostly true, but has exceptions.) Is it a constraint that used to apply but no longer does? (The double-write example: the driver bug got patched in 2015.) Is it a constraint that never applied and was just superstition? (The "never use nullable fields in SQL Server" rule, which a certain DBA used to insist on, for reasons he was never able to articulate, which I eventually traced back to a misreading of a Microsoft blog post about index performance from 2008.)

The general question to ask is: *has the environment changed since this rule was instituted?* If the rule depends on a technology that has been upgraded, on a bug that has been fixed, on a team that has been reorganised, on a regulation that has been lifted, or on a constraint that has been relaxed, then it is a candidate for retirement. If it depends on physics, business logic, or legal requirements that still apply, it is load-bearing.

**Step three: design a safe-to-fail probe.** If after step two you genuinely cannot tell — and you often will not be able to tell, because the information is lost — your move is not to boldly tear the fence down. Your move is to probe it carefully. Can you isolate the rule? Can you disable it for one customer, one endpoint, one percentage of traffic, one feature flag? Can you observe what happens when you do? If the hose is still connected, you want to find out by getting mildly damp in a controlled way, not by being hospitalised.

Safe-to-fail probing is a whole discipline in its own right. The buzzwords include *canary deploys*, *feature flags*, *A/B tests*, *dark launches*, *shadow traffic*, *progressive delivery*. The mechanics differ but the epistemological shape is identical. You are reducing the blast radius of a potential wrong answer to something you can recover from, so that you can learn cheaply whether the fence is still alive.

**Step four: document your finding.** This is the step that almost nobody does, and it is the step that matters most for the next person. When you figure out *why* a rule was there, write it down. Not in your head, not in a Slack message — in the code, in a comment, in a README, in an architecture decision record, in the commit message for whatever change you are making. The whole reason we were in this mess was that the previous generation did not write it down. Do not pass the same problem down to the next one.

This is why commit messages matter. This is why code comments that explain *why* are vastly more valuable than code comments that explain *what*. This is why architecture decision records (ADRs) — that wonderful format from Michael Nygard's 2011 blog post — have become so popular. They are not bureaucratic overhead. They are how you stop the next generation of monkeys from getting stuck in the cage.

Let us look at what all of this actually means in practice.

## Part 4: Case Study One — The House That Cost Its Owners An Extra Week

Let me come back to the house.

I mentioned earlier that we sanded the floors before we did the ceilings, and that we had to re-sand them. I want to use this story properly now, because it is a very clean example of all four of the failure modes and recoveries we have discussed.

We did not know the top-down rule. Nobody had told us. We were operating at a pure first-principles level, which in our case meant "work on the most visible thing first because it looks like progress". The result was a classic cost-of-experience lesson. We did the wrong thing, paid the cost, and learned the rule.

Now. Why did we make that particular mistake? It was not that we were stupid. It was not that we were lazy. It was that we had *never done a deep clean before, and we had no framework for noticing what we did not know*. The rule "work top down" was a fence we did not even see. If somebody had pointed at the polyurethane and said "wait, have you cleaned the ceiling yet?", we would have got it instantly. We did not need to understand the physics; we just needed to be told.

This is what good documentation does. It points at the fence.

Two years later, I was renovating a second house. This time I had the scar. The first thing I did was call the friend who had laughed at me about the first house. I asked him for his top ten "things you should do before doing any of the other things" rules. He rattled off nine of them off the top of his head, including "always top down", "always measure twice before ordering tile", "always turn off the water at the main before touching any plumbing even if you are only doing something near plumbing", and "never buy paint without a sample pot first because the colour will look completely different on your walls". The tenth one took him a minute. They were all trivial in the abstract. They would each have cost me a day if I had not known them. The call took forty minutes.

That forty-minute call was the highest return-on-time I have ever had on any piece of work. It is vicarious learning in its purest form. Nine fences pointed out. Nine fences understood. Nine scars I did not have to earn.

And — this is the pivotal point — several of those fences were not universal. The "sample pot" rule, for instance, is almost always correct when you are picking colours for a room, but some of the really pigmented modern paints from specific brands have tightened their colour matching to the point where the sample is almost exactly identical to the final finish. My friend's rule was learned in 2003 and was still mostly true, but the fence had moved. One of the ten rules was already a little obsolete. The other nine were fully live. A junior learning from my friend would have eaten an extra two hours on the sample pot rule, which is cheap. A junior refusing to learn from him at all would have eaten all nine rules at once, which is catastrophic.

This is the asymmetry you have to hold in your head. *The default is to trust the accumulated wisdom. The diligence is to notice which fences have moved.*

## Part 5: Case Study Two — The TFS to Git Migration That Nearly Ate a Team

This one is harder, because it is genuinely technical and because I did it wrong the first time and right the second time, and both experiences are instructive.

For the ASP.NET developers in the audience who have not had the pleasure: *Team Foundation Server* (now *Azure DevOps Server*) was Microsoft's integrated source control, build, test management, and work tracking product. In its original incarnation it used a centralised source control system called TFVC (*Team Foundation Version Control*), which is conceptually closer to Perforce or Subversion than to Git — it used check-out-edit-check-in semantics, a central server, and it really, really did not like branching. Git, of course, is the distributed alternative that effectively won the industry around 2010-2012 and had become the overwhelming default by the early 2020s.

If you were working in a .NET shop in the 2010s, you probably spent some time in TFVC. If your shop was run by the kind of manager who saw Git as a fad, you may have spent a lot of time in TFVC. Many large enterprise shops — regulated industries, government, financial services, healthcare — were still running TFVC well into the 2020s. A lot of them are still running it now, in 2026, because nothing in TFVC is actively on fire and the business has no appetite for the migration.

The first migration I did was a mess. We were modernising a legacy ASP.NET Web Forms application to modern ASP.NET Core on .NET Core 2.1 (which tells you how long ago this was). The approach I proposed to management — because I had not thought it through properly — went like this:

1. Take the current state of the TFVC codebase.
2. Dump it into a new Git repository as a single initial commit.
3. The modernisation team works in the Git repository. They rewrite module by module.
4. The legacy team continues working in TFVC, because "they are not modernising, they do not need Git."
5. When we are ready to cut over, we somehow reconcile the two.

Every word of that plan was wrong. Let me explain why, in the order the errors unfolded.

**The first wrongness** was step 2. A single initial commit throws away every bit of history. All the `git blame` information, all the commit messages that explain the *why* behind decisions, all the tags marking production releases, all of it — gone. The moment you have a single-commit dump, the Git repository is no better than a zip file. You have just told every future developer that the first eight years of the project's life do not exist. They are flying blind on all the Chesterton-fence questions we talked about in Part 3.

**The second wrongness** was step 4. If the legacy team is continuing to ship features in TFVC while the modernisation team rewrites in Git, you now have *two active codebases diverging in real time*. Every bug fix the legacy team makes in TFVC has to be ported into Git — forever. Every feature the modernisation team completes has to be released without it showing up in TFVC's history. The two teams are now operating in two different source-control universes. The work of reconciling them is continuous and grows with the square of the elapsed time, because you have to handle cross-cutting changes (a config change on one side, a schema change on the other) that touch files both teams are modifying.

**The third wrongness** was step 5. "Somehow reconcile" is not a plan. It is a wish. When we finally tried to do it, eighteen months in, the legacy team had made 1,300 commits to TFVC and the modernisation team had made 3,800 to Git. Merging them was not really merging; it was manual reconciliation. A senior engineer spent four months of her life doing it. Four months. Of a senior engineer's life. That was my mistake. I gave it to her.

Now here is what the right plan looked like, the second time I did it.

**Step 1: Migrate TFVC to Git first, with history.** Use `git-tfs` or Microsoft's own TFVC-to-Git import tooling. This is a non-trivial job — you have to decide what to do about TFVC's labels (which roughly map to Git tags but not perfectly), about branches (TFVC branches are path-based, Git branches are not), about check-in comments and work-item associations (you want to preserve the links to the issue tracker). It takes a few weeks of someone's time to do properly. But it is a *bounded* piece of work. When it is done, you have a Git repository whose history is the project's actual history, and `git blame` works, and the thirty-year-old engineer who retired can still be invoked on questions about commits from 2015.

**Step 2: Move the entire team — legacy and modernisation — to Git.** Not one team; the whole team. The legacy team continues to ship features. They just ship them in Git branches now. Their day-to-day looks slightly different — *check out* becomes *clone*, *get latest* becomes *pull*, *check in* becomes *commit, push* — but the shape of their work is unchanged. You run a week of pairing sessions where you sit next to each legacy developer and walk them through a few pull requests. That is enough.

**Step 3: Do the modernisation on a long-lived branch or (better) in a separate repository that will eventually replace the legacy one.** The modernisation team works in the same version-control system as the legacy team. Rebases, merges, and cherry-picks are now *possible*, which they were not before when the two codebases were in different systems entirely.

**Step 4: Rewrite module by module, replacing the legacy code in-place with modernised code.** This is the part that takes two years. But because both codebases are now in Git, you can do things like: bug fixes applied by the legacy team automatically flow into the modernisation branch; the modernisation team can look at exactly which commits changed which files to decide what still needs to be ported; you can run the legacy and modernised versions of a module side by side for weeks before cutting over.

**Step 5: Retire the legacy code module by module, not all at once.** Ship the modernised version of module A to production, behind a feature flag. Run both for a month. Declare victory. Delete the legacy code for module A. Move on to module B. This is a decade-old pattern usually called the *strangler fig* after a 2004 post by Martin Fowler. If you squint, it is the same shape as Chesterton's Fence: you do not rip out the old module until you can demonstrate that the new one is doing the work.

The whole plan is top-down-cleaning for software. You do the organisational and tooling work first — source control, build pipeline, deployment, test framework — *then* you do the code work. The reason is the same as the reason for cleaning the ceiling first. If you do it in the other order, all the work you did on the lower surface gets contaminated by the work you do on the upper surface, and you have to redo it.

Nobody had ever explained this to me before I did it wrong the first time. In hindsight, it was completely obvious. In the moment, I could not see it.

If you are a manager reading this, reading out the right-version plan to a team takes fifteen minutes. Writing down the wrong-version plan takes fifteen seconds. The gulf between those two documents is a year of somebody's life. Be the person who writes down the fifteen-minute one.

## Part 6: Case Study Three — A Real Angular 21 Application, Written From the Ground Up

Now for the long one.

The rest of this post is going to build a reasonably realistic Angular 21 application — a small magazine browser that fetches posts from a JSON feed, supports search and filtering, lets you compose and submit a comment with validation, and handles offline gracefully. Angular 21 was released in November 2025 and brought a bundle of new primitives: mature signals across the API surface, the new Signal Forms API (experimental in 21, expected to stabilise in 22 around May 2026), and continued progress on zoneless change detection. It is the right version of Angular to write a modern application in, and it is strikingly different from the Angular 2-through-15 era that many .NET developers encountered first.

I am writing this for two reasons. The obvious reason is that you cannot really understand the "which rules to keep" question without watching a real technology absorb and abandon its own rules over time. Angular is a good case study because it has been through four or five distinct eras in a decade. The second reason is that the contrast between Angular-as-it-was-in-2016 and Angular-as-it-is-in-2026 is a specific example of exactly the thing we have been talking about: fences that were load-bearing then and are obsolete now.

If you have been told that Angular is heavy, ceremonial, enterprisey, or slow, you have probably been told something true about Angular in 2017. You have probably been told something false about Angular in 2026. The fence has moved.

A short history is in order before we write any code, because you need context to understand why things are shaped the way they are.

### The Evolution of Angular, Briefly

*AngularJS (1.x)*, released in 2010, was a first-generation single-page-app framework. It used two-way data binding (via a dirty-checking digest cycle), dependency injection, directives, and a controllers-and-services architecture. It was extremely popular in the early 2010s. It was also, by 2014, clearly limited — the digest cycle did not scale, the module system was improvised, and it predated TypeScript, Promises, Observables, and most of the modern JavaScript world.

*Angular 2*, released in September 2016, was a ground-up rewrite, not a migration. Google made the genuinely unusual decision to break compatibility entirely. The new framework was TypeScript-first, component-based, used hierarchical dependency injection, and shipped with RxJS Observables as a core primitive. Every AngularJS application had to be rewritten to move. Many were not, and AngularJS hung around for years afterwards.

*Angular 4 through Angular 14* were incremental improvements. The Ivy rendering engine landed in Angular 9 (February 2020), which made tree-shaking and bundle sizes dramatically better. Reactive forms matured. RxJS idioms became load-bearing. The ecosystem stabilised.

*Angular 14 (June 2022) introduced standalone components*, which let you skip NgModules. *Angular 17 (November 2023) made standalone components the default*, introduced a new built-in control flow (`@if`, `@for`, `@switch` as template syntax), and brought the deferred-loading `@defer` block. This was the beginning of what people started calling "modern Angular" — much less ceremony, much more straightforward.

*Angular 18 (May 2024)* brought experimental zoneless change detection, meaning you could finally build Angular apps that did not depend on `zone.js` monkey-patching the browser's APIs.

*Angular 19 (November 2024), Angular 20 (May 2025), and Angular 21 (November 2025)* progressively matured the signals-based reactivity model. Signals were the first-class reactive primitive. Signal-based inputs, outputs, and queries. New SSR and hydration improvements. Incremental hydration. And — importantly for our case study — the experimental *Signal Forms* API, which gives you a forms model built on signals rather than on RxJS's `FormGroup`/`FormControl`.

As of April 2026, the current stable Angular version is 21.2.9. Angular 22 is expected around May 2026 and will, per the roadmap, bring selectorless components, stable Signal Forms, and OnPush as the default change detection strategy.

If you last used Angular in the 4.x or 8.x era, almost every fence you remember has been moved. `NgModule` is gone (or at least no longer required). `*ngIf` is legacy syntax. Zones are optional. Forms are optional in two different ways (you can use reactive forms, template-driven forms, or signal forms). The ceremonial "Angular is big" complaint from 2018 has been comprehensively addressed; a standalone-component Angular 21 app with signals and OnPush change detection starts up in a couple of hundred kilobytes of gzipped JavaScript, competitive with or better than most React setups.

I recite all of this because a .NET developer approaching Angular in 2026 will frequently be making decisions based on a version of Angular that no longer exists. That is a fence problem. Most of what you have heard is about the old fence.

### The Bad Way

Let me show you what an Angular application looked like in 2016, because we are going to contrast it with the modern version. This is not strictly speaking a "bad" example — it was the idiomatic way at the time — but it illustrates every piece of accumulated ceremony that modern Angular has stripped away.

```typescript
// app.module.ts — the old NgModule-based bootstrapping
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule, Routes } from '@angular/router';

import { AppComponent } from './app.component';
import { BlogListComponent } from './blog-list/blog-list.component';
import { BlogPostComponent } from './blog-post/blog-post.component';
import { BlogService } from './blog.service';
import { CommentFormComponent } from './comment-form/comment-form.component';

const routes: Routes = [
  { path: '', component: BlogListComponent },
  { path: 'post/:slug', component: BlogPostComponent }
];

@NgModule({
  declarations: [
    AppComponent,
    BlogListComponent,
    BlogPostComponent,
    CommentFormComponent
  ],
  imports: [
    BrowserModule,
    ReactiveFormsModule,
    HttpClientModule,
    RouterModule.forRoot(routes)
  ],
  providers: [BlogService],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

```typescript
// blog-list.component.ts — the classic component-class style
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { BlogService } from '../blog.service';
import { BlogPost } from '../models/blog-post';

@Component({
  selector: 'app-blog-list',
  templateUrl: './blog-list.component.html',
  styleUrls: ['./blog-list.component.css']
})
export class BlogListComponent implements OnInit, OnDestroy {
  posts: BlogPost[] = [];
  loading = true;
  error: string | null = null;
  private destroy$ = new Subject<void>();

  constructor(private blogService: BlogService) {}

  ngOnInit(): void {
    this.blogService.getPosts()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (posts) => {
          this.posts = posts;
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to load posts';
          this.loading = false;
          console.error(err);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

```html
<!-- blog-list.component.html -->
<div *ngIf="loading" class="loading">Loading posts...</div>
<div *ngIf="error" class="error">{{ error }}</div>
<ul *ngIf="!loading && !error" class="posts">
  <li *ngFor="let post of posts" class="post">
    <a [routerLink]="['/post', post.slug]">{{ post.title }}</a>
    <p class="summary">{{ post.summary }}</p>
  </li>
</ul>
```

Look at all the ceremony. An `NgModule` that has to explicitly `declare` every component. A class with a constructor for dependency injection. Lifecycle hooks implemented by interfaces. A `takeUntil` pattern with a manually-managed `Subject` to avoid memory leaks on subscriptions. Structural directives with asterisks. A separate HTML template file. A services class bolted in via the module's `providers` array.

This is not wrong. It worked. Many, many applications in production today look exactly like this. But every single line in that code sample is a fence that was installed for a reason that has since either been removed or has a cheaper solution. Let me walk through them.

*The `NgModule` fence* was there because, in the Angular 2 era, there was no clean way to establish component boundaries, and `NgModule` gave you a unit that could be lazy-loaded, compiled independently, and tested in isolation. Angular 14 (2022) introduced standalone components, and Angular 17 (2023) made them the default. The fence has been replaced by a more direct mechanism — components import other components directly.

*The `OnInit`/`OnDestroy` fence* was there because Angular needed to give you hooks to do initialisation and cleanup that the constructor could not safely do. This was a reasonable design in 2016. Since Angular 14, the `inject()` function (and later, signal-based effects) give you cleaner options. You still often use lifecycle hooks, but they are no longer required.

*The `takeUntil(destroy$)` fence* was there because RxJS subscriptions, if not unsubscribed, leak memory when a component is destroyed. Angular 16 introduced `takeUntilDestroyed()`, which does the same thing without the `Subject` boilerplate. Angular 17+ signals mostly remove the need for the pattern at all, because signals do not have subscriptions.

*The `*ngIf`/`*ngFor` fence* was the structural-directive syntax. It works fine but was, if we are honest, always a little weird. Angular 17 introduced the `@if`/`@for`/`@switch` control flow syntax, which reads more like regular code and the Angular team has explicitly recommended for all new code.

*The separate `.html` and `.css` files fence* was a convention, not a requirement; it was always possible to inline them. But tooling in 2016 made inline templates painful to debug. In 2026, modern editors handle inline templates just fine, and many teams prefer them for small components because it removes the context-switching between files.

So every single aspect of the 2016 code is a live example of the thing we have been talking about. None of those patterns are *wrong*. They were all load-bearing when they were introduced. They are all, in 2026, rules nobody has to follow any more, because the underlying constraints have been removed. If you joined a team in 2019 that insisted on `NgModule`s, you were learning a live fence. If you joined a team in 2026 that still insisted on them because "that is how we do it", you are learning a dead one.

### The Good Way

Here is the same blog list in modern Angular 21. You can compare line-for-line.

```typescript
// main.ts — the entire application bootstrap, standalone
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withFetch())
  ]
}).catch(err => console.error(err));
```

```typescript
// app.routes.ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./blog-list/blog-list.component').then(m => m.BlogListComponent)
  },
  {
    path: 'post/:slug',
    loadComponent: () =>
      import('./blog-post/blog-post.component').then(m => m.BlogPostComponent)
  }
];
```

Already, most of the module ceremony is gone. `bootstrapApplication` starts the app with a single component. There is no `AppModule`. Every route is lazy-loaded by default (`loadComponent`), which was a separate optimisation in the old world. HTTP uses the browser's native `fetch` API via `withFetch()`, which was introduced in Angular 18.

```typescript
// app.component.ts — the root component, standalone
import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header>
      <h1><a routerLink="/">My Blazor Magazine</a></h1>
    </header>
    <main>
      <router-outlet />
    </main>
  `,
  styles: `
    header { padding: 1rem; border-bottom: 1px solid #ddd; }
    h1 a { text-decoration: none; color: inherit; }
    main { padding: 1rem; max-width: 60rem; margin: 0 auto; }
  `
})
export class AppComponent {}
```

```typescript
// blog.service.ts — a simple service using signals and the resource API
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';

export interface BlogPost {
  slug: string;
  title: string;
  summary: string;
  date: string;
  tags: string[];
  author: string;
}

@Injectable({ providedIn: 'root' })
export class BlogService {
  private http = inject(HttpClient);

  readonly allPosts = toSignal(
    this.http.get<BlogPost[]>('/data/blog/posts-index.json'),
    { initialValue: [] as BlogPost[] }
  );

  // A computed signal that derives filtered posts from a search term signal.
  search = signal('');
  filteredPosts = computed(() => {
    const term = this.search().trim().toLowerCase();
    const posts = this.allPosts();
    if (!term) return posts;
    return posts.filter(p =>
      p.title.toLowerCase().includes(term) ||
      p.summary.toLowerCase().includes(term) ||
      p.tags.some(t => t.toLowerCase().includes(term))
    );
  });
}
```

Three things to notice about the service.

First, dependency injection is done with the `inject()` function instead of a constructor parameter. This was introduced in Angular 14 and is the preferred style now. It is functionally equivalent to constructor injection but composes better with factory functions and `inject()` calls inside other contexts.

Second, the `toSignal()` function is the bridge between the RxJS world (which is how `HttpClient` returns things) and the signals world (which is how modern Angular components consume state). It subscribes to the Observable and exposes its current value as a reactive signal.

Third, `computed()` gives you a derived signal that automatically recomputes when its dependencies change. This is the piece that has rendered much of RxJS unnecessary for typical UI state management. If you are a C# developer, `computed()` is similar in spirit to the reactive property pattern you get from `ReactiveObject` in ReactiveUI, but is first-class in the framework.

```typescript
// blog-list.component.ts — signals, new control flow, no RxJS in the component itself
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BlogService } from '../blog.service';

@Component({
  selector: 'app-blog-list',
  imports: [RouterLink],
  template: `
    <section class="search">
      <label for="q">Search</label>
      <input
        id="q"
        type="search"
        [value]="blog.search()"
        (input)="blog.search.set($any($event.target).value)"
        placeholder="Search title, summary, or tag..."
      />
    </section>

    @let posts = blog.filteredPosts();

    @if (posts.length === 0) {
      <p class="empty">No posts match your search.</p>
    } @else {
      <ul class="posts">
        @for (post of posts; track post.slug) {
          <li class="post">
            <a [routerLink]="['/post', post.slug]">{{ post.title }}</a>
            <p class="summary">{{ post.summary }}</p>
            <p class="meta">
              <time [attr.datetime]="post.date">{{ post.date }}</time>
              — by {{ post.author }}
            </p>
          </li>
        }
      </ul>
    }
  `,
  styles: `
    .search { margin-bottom: 1rem; }
    .search input { width: 100%; padding: 0.5rem; font-size: 1rem; }
    .post { border-bottom: 1px solid #eee; padding: 0.75rem 0; }
    .post a { font-weight: 600; text-decoration: none; }
    .summary { color: #444; }
    .meta { color: #888; font-size: 0.875rem; }
    .empty { color: #888; font-style: italic; }
  `
})
export class BlogListComponent {
  blog = inject(BlogService);
}
```

Let me point out everything that is *not* there, compared to the 2016 version.

There is no `NgModule` anywhere in the application. The component imports its own dependencies (`RouterLink`).

There is no `ngOnInit`. The service eagerly loads its data via `toSignal()`; the component simply reads the signal. There is no `OnInit` interface to implement, no subscription to manage, no error handling copy-paste.

There is no `ngOnDestroy`. There are no subscriptions to clean up. Signals are not subscription-based.

There is no `Subject<void>` or `takeUntil` pattern.

The control flow is `@if` and `@for`, which reads like code. The `track` expression on `@for` is now required in Angular 17+, which forces you to think about list identity — a recurring source of subtle bugs in the old `*ngFor` world.

The template is inline, which makes the component a single file. You can still use an external template if you prefer; the framework does not care.

The `@let` block is a new Angular 19 feature that lets you assign a local variable in the template. It is useful for avoiding repeated function calls (signals are cheap to call, but it is still nicer to read).

The whole thing is about sixty lines. The 2016 equivalent was roughly 120 across three files and the module. The behaviour is identical. The difference is everything we have been talking about — thirty fences the Angular team retired, each of which made the old code more complex, none of which we have to respect any more.

### Signal Forms: The Modern Approach to Validation

Let me now show you the form. This is probably the most dramatic difference between old and new Angular, because forms in the old world involved either template-driven forms (which used two-way binding in a way the rest of the framework discouraged) or reactive forms (which required a lot of RxJS and a lot of type gymnastics to get right). The Angular 21 experimental Signal Forms API is a proper reactive forms model built on signals instead.

A brief honest note before the code: Signal Forms were introduced as experimental in Angular 21 (November 2025) and are expected to stabilise in Angular 22 (May 2026). The API surface may change. If you are building production code in April 2026, Reactive Forms is the battle-tested choice. But Signal Forms show the direction the framework is heading, and they are genuinely nicer than what they replace.

Here is a comment composer form. It has a name field (required, at least 2 chars, at most 80), an email field (required, must be a valid email), a body field (required, at least 10 chars, at most 2000), and a "subscribe to replies" checkbox.

```typescript
// comment-form.component.ts — using Signal Forms (experimental in Angular 21)
import { Component, inject, signal } from '@angular/core';
import { form, Control, required, minLength, maxLength, email } from '@angular/forms/signals';

interface CommentDraft {
  name: string;
  email: string;
  body: string;
  subscribeToReplies: boolean;
}

@Component({
  selector: 'app-comment-form',
  imports: [Control],
  template: `
    @let f = commentForm;

    <form (submit)="onSubmit($event)" novalidate>
      <label>
        <span>Name</span>
        <input type="text" [control]="f.name" autocomplete="name" />
        @if (f.name().errors().length > 0 && f.name().touched()) {
          <small class="error">
            @for (err of f.name().errors(); track err.kind) {
              {{ errorMessage(err) }}
            }
          </small>
        }
      </label>

      <label>
        <span>Email</span>
        <input type="email" [control]="f.email" autocomplete="email" />
        @if (f.email().errors().length > 0 && f.email().touched()) {
          <small class="error">
            @for (err of f.email().errors(); track err.kind) {
              {{ errorMessage(err) }}
            }
          </small>
        }
      </label>

      <label>
        <span>Comment</span>
        <textarea [control]="f.body" rows="5"></textarea>
        @if (f.body().errors().length > 0 && f.body().touched()) {
          <small class="error">
            @for (err of f.body().errors(); track err.kind) {
              {{ errorMessage(err) }}
            }
          </small>
        }
      </label>

      <label class="checkbox">
        <input type="checkbox" [control]="f.subscribeToReplies" />
        <span>Email me when someone replies</span>
      </label>

      <button type="submit" [disabled]="f().invalid() || submitting()">
        {{ submitting() ? 'Submitting…' : 'Post comment' }}
      </button>

      @if (submitError()) {
        <p class="error" role="alert">{{ submitError() }}</p>
      }
      @if (submitSuccess()) {
        <p class="success" role="status">Thanks — your comment has been posted.</p>
      }
    </form>
  `,
  styles: `
    form { display: flex; flex-direction: column; gap: 1rem; max-width: 36rem; }
    label { display: flex; flex-direction: column; gap: 0.25rem; }
    label.checkbox { flex-direction: row; align-items: center; gap: 0.5rem; }
    input, textarea { font: inherit; padding: 0.5rem; border: 1px solid #bbb; border-radius: 0.25rem; }
    input:invalid, textarea:invalid { border-color: #c00; }
    .error { color: #c00; font-size: 0.875rem; }
    .success { color: #080; font-size: 0.875rem; }
    button { padding: 0.5rem 1rem; font: inherit; cursor: pointer; }
    button[disabled] { opacity: 0.5; cursor: not-allowed; }
  `
})
export class CommentFormComponent {
  // The shape of the form's data model
  private model = signal<CommentDraft>({
    name: '',
    email: '',
    body: '',
    subscribeToReplies: false
  });

  // The form itself, with inline validation rules
  commentForm = form(this.model, (path) => {
    required(path.name, { message: 'Please enter your name.' });
    minLength(path.name, 2, { message: 'Name must be at least 2 characters.' });
    maxLength(path.name, 80, { message: 'Name must be 80 characters or fewer.' });

    required(path.email, { message: 'Please enter your email.' });
    email(path.email, { message: 'Please enter a valid email address.' });

    required(path.body, { message: 'Please write something in the comment.' });
    minLength(path.body, 10, { message: 'Comment must be at least 10 characters.' });
    maxLength(path.body, 2000, { message: 'Comment must be 2,000 characters or fewer.' });
  });

  submitting = signal(false);
  submitError = signal<string | null>(null);
  submitSuccess = signal(false);

  async onSubmit(event: SubmitEvent) {
    event.preventDefault();
    if (this.commentForm().invalid()) {
      this.commentForm().markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.submitError.set(null);
    try {
      // In a static-site context (GitHub Pages + no backend), submissions
      // typically go to an external endpoint such as a serverless function,
      // a forms-as-a-service provider, or a mailto: link. For the magazine,
      // we post to a lightweight endpoint.
      const res = await fetch('/api/comments', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(this.model())
      });
      if (!res.ok) throw new Error(`Server returned ${res.status}`);
      this.submitSuccess.set(true);
      this.model.set({
        name: this.model().name,   // keep the name and email for convenience
        email: this.model().email,
        body: '',
        subscribeToReplies: false
      });
    } catch (err) {
      this.submitError.set('We could not post your comment. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }

  errorMessage(err: { kind: string; message?: string }): string {
    return err.message ?? `Validation failed (${err.kind})`;
  }
}
```

There is a lot going on here, so let me unpack it.

**The model is a signal.** The form is backed by a plain TypeScript interface (`CommentDraft`) held in a `signal`. The signal is the source of truth. The form object just wraps the signal with validation and introspection.

**Validation is declarative and co-located.** The `required`, `minLength`, `maxLength`, `email` functions are pure validators applied to paths within the model. The validation message is attached at the point the rule is declared, so when a rule changes, you do not have to hunt for the message in a separate file.

**Every piece of form state is a signal.** `f.name()` returns an object with `.value()`, `.errors()`, `.touched()`, `.dirty()`, `.disabled()`, and so on — all signals themselves. Template expressions that read those signals automatically re-render when they change.

**The submit button is derived from form state.** `[disabled]="f().invalid() || submitting()"` reads two signals; the button's disabled state updates instantly when either changes. No `Subscription`, no `combineLatest`, no ceremony.

**There is no `FormGroup`, no `FormBuilder`, no typed-forms gymnastics.** The old reactive-forms world required you to build parallel TypeScript types that mirrored your `FormGroup` shape, and typed forms (introduced in Angular 14) was a hard-won evolution of the original untyped API. Signal Forms start typed and stay typed because the source of truth is the signal-typed model.

If you are a .NET developer, the mental model is something like: the model is a `POCO`, the form is a thin validation-aware view over the POCO, and changes to either the form inputs or the POCO flow through the same signal machinery. If you have used MVVM in WPF or Xamarin, this shape should look very familiar — it is the same pattern reached by a different route.

### Handling Offline and Errors Honestly

One of the things a small magazine site has to do, even on GitHub Pages, is fail gracefully. You cannot assume the user is online. You cannot assume the JSON feed loads. You cannot assume the comment endpoint is available. Let me add the pieces.

```typescript
// blog.service.ts — with offline-aware loading
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, of } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

export interface BlogPost {
  slug: string;
  title: string;
  summary: string;
  date: string;
  tags: string[];
  author: string;
}

type LoadState<T> =
  | { status: 'loading' }
  | { status: 'ready'; data: T }
  | { status: 'error'; message: string };

@Injectable({ providedIn: 'root' })
export class BlogService {
  private http = inject(HttpClient);
  private readonly INDEX_URL = '/data/blog/posts-index.json';
  private readonly CACHE_KEY = 'blog.posts-index.v1';

  // Start from cached data if we have any, otherwise start in loading state.
  private readonly initial: LoadState<BlogPost[]> = (() => {
    try {
      const cached = localStorage.getItem(this.CACHE_KEY);
      if (cached) {
        const data = JSON.parse(cached) as BlogPost[];
        return { status: 'ready', data };
      }
    } catch {
      // ignore — localStorage can throw in private mode
    }
    return { status: 'loading' };
  })();

  readonly state = toSignal<LoadState<BlogPost[]>>(
    this.http.get<BlogPost[]>(this.INDEX_URL).pipe(
      // On success, cache it.
      // (We do this via a side effect in a tap() but for brevity, inline.)
      // eslint-disable-next-line @typescript-eslint/no-unused-expressions
      catchError((err: HttpErrorResponse) => {
        console.warn('Failed to load blog index', err);
        return of({ status: 'error' as const, message: 'Offline or feed unavailable.' });
      })
    ).pipe(
      // Wrap the success case into the LoadState shape.
      // In real code this would be a .map(data => ({ status: 'ready', data })) upstream
      // but written inline for illustration:
    ),
    { initialValue: this.initial }
  );

  readonly allPosts = computed(() => {
    const s = this.state();
    return s.status === 'ready' ? s.data : [];
  });

  readonly isLoading = computed(() => this.state().status === 'loading');
  readonly errorMessage = computed(() => {
    const s = this.state();
    return s.status === 'error' ? s.message : null;
  });

  search = signal('');
  filteredPosts = computed(() => {
    const term = this.search().trim().toLowerCase();
    const posts = this.allPosts();
    if (!term) return posts;
    return posts.filter(p =>
      p.title.toLowerCase().includes(term) ||
      p.summary.toLowerCase().includes(term) ||
      p.tags.some(t => t.toLowerCase().includes(term))
    );
  });
}
```

The shape I want you to notice is *LoadState as a discriminated union*. Instead of three parallel booleans (`loading`, `error`, `posts`), which can get into impossible states (`loading=true, error='yes', posts=[3 items]` — what does that mean?), we model the three states as a single sum type. A given instance of `LoadState` can only be one of three shapes at a time. The UI reads the state, matches on the status, and renders accordingly.

This is the kind of thing a C# developer will find natural — it is a closed discriminated union of the kind C# 12's pattern matching handles cleanly. TypeScript has had discriminated unions since its early days, and they are one of the language's best features. If you find yourself writing `if (loading && !error)` branches in a component, stop, and model the state as a union.

The template update for `BlogListComponent` now looks like this:

```html
@if (blog.isLoading()) {
  <p class="loading">Loading posts...</p>
} @else if (blog.errorMessage(); as err) {
  <p class="error" role="alert">
    {{ err }}
    <button (click)="retry()">Retry</button>
  </p>
} @else {
  @let posts = blog.filteredPosts();
  @if (posts.length === 0) {
    <p class="empty">No posts match your search.</p>
  } @else {
    <ul class="posts">
      @for (post of posts; track post.slug) { ... }
    </ul>
  }
}
```

The `@if / @else if / @else` structure makes the four states — loading, error, empty, populated — explicit and exhaustive. This is the modern Angular control flow doing what it was built for: matching on discriminated state cleanly.

## Part 7: Testing — Where Chesterton's Fences Go To Die Honourably

I want to talk about testing next, because testing is where we actually execute the Chesterton's Fence check. A good test suite is, in effect, a running record of every fence the team has decided to install. When you add a test, you are installing a fence — *this behaviour must be preserved*. When you break a test, the test's failure is the fence saying "hey, you did not notice me; were you sure about that change?" When you delete a test, you are removing a fence, and the rule from Part 3 applies: know what it is doing before you remove it.

A common anti-pattern in legacy codebases is to delete failing tests that you do not understand, because fixing them is annoying and the release has a deadline. This is the exact Chesterton's Fence failure mode, played out in miniature, many times a day, by tired engineers. *I do not see what this test is checking, so I will remove it, so that CI goes green.* Every time you do this, you are converting a live fence into a dead one. You are removing the evidence of a previous decision, and you are making it slightly more likely that some future reader will trip over the same edge case.

A test without a comment explaining *why* is a fence without a note. Write the note. In every test, the first line of the test name or the first line of the `describe` block should answer the question: *what is this test protecting?* Not *what does it do* — that is obvious from reading it — but *why is it there*. Ideally, the test name should reference the bug report or the user story that motivated it.

Here is the blog service with a proper set of tests written in Angular's current `TestBed` machinery. Angular 21 made the signal-based test utilities much more ergonomic — you no longer need `fakeAsync`/`tick` for most things, because signals are synchronous and `computed`s recompute eagerly within effect boundaries.

```typescript
// blog.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { BlogService, BlogPost } from './blog.service';

describe('BlogService', () => {
  let service: BlogService;
  let httpMock: HttpTestingController;

  const fixture: BlogPost[] = [
    { slug: 'a', title: 'Apple pie',    summary: 'A recipe',        date: '2026-01-01', tags: ['food'],       author: 'alice' },
    { slug: 'b', title: 'Beta testing', summary: 'On QA practice',  date: '2026-01-02', tags: ['qa', 'dev'],   author: 'bob'   },
    { slug: 'c', title: 'Chesterton',   summary: 'On fences',       date: '2026-01-03', tags: ['philosophy'], author: 'alice' }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BlogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // Why this test: the service must expose posts as signals so that computed
  // filters recompute automatically. Regression guard for the RxJS-based
  // subscription pattern we explicitly rejected in favour of toSignal().
  it('exposes loaded posts as a signal', () => {
    const req = httpMock.expectOne('/data/blog/posts-index.json');
    req.flush(fixture);

    expect(service.allPosts()).toEqual(fixture);
  });

  // Why this test: the filter is case-insensitive. Bug report #142
  // reported "searching for 'apple' did not find 'Apple pie'".
  it('filters posts case-insensitively by title', () => {
    httpMock.expectOne('/data/blog/posts-index.json').flush(fixture);

    service.search.set('apple');
    expect(service.filteredPosts().map(p => p.slug)).toEqual(['a']);
  });

  it('filters posts by tag', () => {
    httpMock.expectOne('/data/blog/posts-index.json').flush(fixture);

    service.search.set('qa');
    expect(service.filteredPosts().map(p => p.slug)).toEqual(['b']);
  });

  // Why this test: empty search must show all posts, not zero.
  // Bug report #143: "after clearing the search box, the list stayed empty".
  it('returns all posts when the search term is empty or whitespace', () => {
    httpMock.expectOne('/data/blog/posts-index.json').flush(fixture);

    service.search.set('');
    expect(service.filteredPosts().length).toBe(3);

    service.search.set('   ');
    expect(service.filteredPosts().length).toBe(3);
  });

  // Why this test: on network failure we must expose an error message AND
  // leave allPosts() as an empty array so the UI can differentiate empty-state
  // from loading. The discriminated-union design enforces this at the type
  // level, but a test guards against someone "fixing" it by merging the states.
  it('surfaces a friendly error message when the feed fails to load', () => {
    const req = httpMock.expectOne('/data/blog/posts-index.json');
    req.error(new ProgressEvent('network error'), { status: 0, statusText: 'Unknown' });

    expect(service.errorMessage()).toBeTruthy();
    expect(service.allPosts()).toEqual([]);
    expect(service.isLoading()).toBe(false);
  });
});
```

Notice the comment on each test. Every one of those comments is a fence marker. A future developer looking at the "filters posts case-insensitively by title" test and wondering whether they can simplify it can see immediately that it is guarding against bug #142. That is a one-line effort from me right now that saves a future developer a half-day of investigation.

For a C# developer, the mental model is very close to xUnit with `FluentAssertions`. The framework is different but the shape is the same: arrange, act, assert. The biggest difference is that Angular's test fixtures are much more integrated with the framework — `TestBed` is a full-fat dependency-injection container, similar in spirit to `WebApplicationFactory<T>` in ASP.NET Core testing.

### Component Tests

Component testing in Angular is a subject big enough to deserve its own article. I will keep it short here and focus on what changes with signals. The old Angular component-test world involved `fakeAsync`/`tick`, `waitForAsync`, careful `fixture.detectChanges()` calls, and a general sense of being one subtle mistake away from a flaky test. Signals mostly remove this, because signal updates are synchronous within the effect boundary.

```typescript
// blog-list.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';

import { BlogListComponent } from './blog-list.component';
import { BlogService, BlogPost } from '../blog.service';

describe('BlogListComponent', () => {
  let fixture: ComponentFixture<BlogListComponent>;
  let httpMock: HttpTestingController;

  const posts: BlogPost[] = [
    { slug: 'p1', title: 'First post',  summary: 'Hello',       date: '2026-01-01', tags: [], author: 'alice' },
    { slug: 'p2', title: 'Second post', summary: 'Hello again', date: '2026-01-02', tags: [], author: 'bob'   }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    fixture = TestBed.createComponent(BlogListComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function render() {
    fixture.detectChanges();
    httpMock.expectOne('/data/blog/posts-index.json').flush(posts);
    fixture.detectChanges();
  }

  it('renders all posts on initial load', () => {
    render();
    const items = fixture.debugElement.queryAll(By.css('li.post'));
    expect(items.length).toBe(2);
    expect(items[0].nativeElement.textContent).toContain('First post');
  });

  it('filters the list when the search input changes', () => {
    render();
    const input: HTMLInputElement = fixture.debugElement.query(By.css('input[type="search"]')).nativeElement;
    input.value = 'second';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const items = fixture.debugElement.queryAll(By.css('li.post'));
    expect(items.length).toBe(1);
    expect(items[0].nativeElement.textContent).toContain('Second post');
  });

  it('shows the empty state when no posts match', () => {
    render();
    const service = TestBed.inject(BlogService);
    service.search.set('nothing-matches-this-term');
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.empty'))).toBeTruthy();
    expect(fixture.debugElement.queryAll(By.css('li.post')).length).toBe(0);
  });
});
```

The thing to notice is how little ceremony there is. No `fakeAsync`, no `tick`, no `whenStable`. Signals update synchronously; `fixture.detectChanges()` flushes the view. Every state transition is observable and testable.

### End-to-End Tests: A Separate Conversation

I will not cover full end-to-end tests here — that is another article's worth of content — but the current best practice in Angular (as of 2026) is Playwright, with Cypress as a reasonable alternative. The Angular team deprecated Protractor in Angular 12 (May 2021) and removed it in Angular 15 (November 2022), which means that if you have an old codebase with Protractor tests, *those tests are a fence that needs to be understood and retired*. Protractor relied on WebDriver idioms and zone.js awareness that are now actively counterproductive.

## Part 8: Performance — Where Old Rules Are Most Dangerous

A brief side-quest, because performance is where fences go most wrong.

There are a lot of performance rules in circulation that were written for a very different web. The 3G-era rules about bundle size, the jQuery-era rules about DOM manipulation, the React-0.14-era rules about key extraction, the pre-HTTP/2 rules about concatenating all your files into a single blob. Some are still true. Most are not, or have been superseded by cheaper alternatives.

A small, recent example. For years, the conventional wisdom was that you should bundle all your JavaScript into a single file, because "HTTP is expensive and each request has overhead". This was true in the HTTP/1.1 era. HTTP/2 (standardised in 2015, widely deployed by 2018) multiplexes requests over a single connection, which makes the cost of additional small files much lower. HTTP/3 (standardised in 2022, rolled out through 2023-2024) does this even better over QUIC. In 2026, splitting your bundle across many smaller files, each with aggressive caching headers, is usually faster than a monolithic bundle — because the browser can cache each chunk independently, and a new deploy that changes one chunk does not invalidate the whole file.

The fence "concatenate everything into one bundle" was load-bearing in 2014. It was mostly neutral in 2020. It is actively wrong in 2026. Somebody who last read a performance guide in 2016 is now working from a rulebook that is making their site slower.

Angular's default build output in 2026 uses ES modules, aggressive code-splitting, and HTTP/2-aware caching strategies. Trust the framework. Do not apply optimisations you read about in 2016 without checking whether they still apply.

On the flip side, there are a handful of performance rules that *have stayed true for the entire history of the web*:

- *Ship less code.* If you do not need a dependency, do not add it.
- *Defer anything you can defer.* If a piece of code can run after paint, run it after paint.
- *Compress everything.* Gzip was mandatory; Brotli is better; use it.
- *Set cache headers.* Static assets should live in the browser cache approximately forever.
- *Measure before optimising.* All of the above are only useful if you have numbers.

Those are old fences that have not moved. They are the physics rules. The rules about specific bundling strategies, specific image formats, specific request patterns — those are the phantoms that have changed as the underlying constraints have changed. Respect the physics; audit the phantoms.

## Part 9: OpenTelemetry, Logging, and Metrics — The Discipline of Not Forgetting Why

If you came to this post from the direction of the My Blazor Magazine codebase — which has an OpenTelemetry-wired, metrics-heavy, heavily tested infrastructure — you may be wondering when I am going to tie the Angular to the .NET backend story.

Here is the connection. Every telemetry span you emit, every log line you write, every metric you record is a note to your future self explaining what the system was doing at the moment something happened. The reason telemetry matters is exactly the Chesterton's Fence argument in miniature. When something goes wrong in production, you are trying to understand a fence you did not build — you did not build *the state of the universe five minutes ago*. The more information you captured, the better your chance of understanding it.

A concrete example. You ship a feature. Three weeks later, a customer emails to say that their order took forty-five seconds to process. Is that your frontend's fault? Your backend's? The database's? A third-party provider's? Without tracing, you are guessing. With tracing — where a request carries a trace context through the frontend, into the HTTP call, through the server, down to the database and back, with spans annotated at each layer — you can see exactly where the time went.

OpenTelemetry is the open-source standard for this, and it is well-supported in .NET and reasonably supported in Angular. In .NET 10, `System.Diagnostics.ActivitySource` is the primary API, and ASP.NET Core's OpenTelemetry integration (via the `OpenTelemetry.Extensions.Hosting` NuGet) is mature. On the Angular side, the `@opentelemetry/sdk-trace-web` package gives you browser-side tracing with context propagation to your backend.

I will not cover the full wiring here — it is genuinely another article — but I want to make the philosophical point. Good telemetry is a commitment not to forget *why* things happened. It is an engineering practice that directly addresses the Chesterton's Fence problem: it leaves enough of a trail that the next engineer, confronted with a strange pattern, can answer the *why* question by looking at the data instead of by guessing.

If I had to reduce the telemetry hygiene rules to a short list for .NET engineers:

*Annotate every external call with a span.* Database, HTTP, message bus, cache, filesystem. This is the single highest-return piece of telemetry hygiene there is.

*Set activity tags for business-relevant IDs.* Customer ID, order ID, tenant ID, request correlation ID. Do not log personally identifiable information, but do log the keys that let you reconstruct a user's session after the fact.

*Log structured data, not free text.* Use Serilog, NLog, or the built-in `ILogger` with structured formatting. Every log line should be queryable. A free-text log line is a fence with no sign on it.

*Emit metrics at decision points.* Whenever code branches on business logic, consider emitting a counter. "Orders processed by tier" is a metric. "Validation failures by rule" is a metric. "Cache hits versus misses on this endpoint" is a metric. These are the numbers that let you notice the hose has been unplugged.

*Set up SLOs, not just uptime.* A service can be "up" while being miserable. Set latency and error-rate objectives, budget your error rate, and use the budget to decide when to stop pushing features and start fixing reliability. This is Google SRE's contribution to the industry and it is genuinely load-bearing.

All of this is downstream of the same Chesterton's Fence thought. *If you do not record why, you will not know why. If you do not know why, you cannot safely change anything.*

## Part 10: The Harder Case — When the Physics Itself Changes

I have talked about physics versus phantoms as if physics is reliably stable. It is not, entirely. Let me complicate the picture.

In 1970, the rule "memory is expensive, spend it frugally" was a hard physical constraint. Computers had kilobytes of RAM. Every byte mattered. Programmers wrote code that valued space over time because the physics of the machine demanded it.

In 1995, the rule was still mostly true but had loosened. A PC had sixteen megabytes. You could afford a four-kilobyte array.

In 2010, memory was genuinely abundant for most applications. A server had gigabytes. The rule was still taught in schools, but in practice it had become a phantom — the teacher was training students to optimise a constraint that no longer mattered for ninety per cent of code.

In 2026, a cheap laptop has thirty-two gigabytes of RAM and a mobile phone has eight. The rule is actively misleading for most application code. It is still true in the margins — kernel code, embedded systems, WebAssembly tight loops, game engines — but for the typical line-of-business application, the constraint has inverted. The bottleneck is developer time, not memory. The rule "allocate freely and worry later" is closer to correct.

But this is not a clean transition. You still find old .NET developers who insist on `StringBuilder` for every concatenation, even when the compiler would inline it, even when the string is two fragments. You find database developers who refuse to denormalise anything, citing the rules of Codd's second normal form as if they were a moral prohibition, when the actual underlying rule — "duplication is expensive to keep consistent and expensive to store" — has been made largely obsolete by cheap storage and modern replication. You find performance engineers who shake their heads at someone using an `ImmutableList` because "immutable is slower", when the modern CPU's branch predictor and memory cache architecture mean that the wins from predictability often outweigh the apparent cost.

None of these old developers are *wrong* in a deep sense. They are working from a rulebook that used to be calibrated. The environment has changed and the rulebook has not. They are the monkey cage with the hose still broadly connected but pointing somewhere else.

The practical implication is that you cannot just say "the physics rules are stable, the phantom rules move". Even physics rules have soft edges. Constraints have half-lives. Memory budgets, CPU costs, network latencies, storage prices, and energy consumption have all moved by factors of thousands over the last forty years. A rule installed in one era may or may not still apply in another, and the only way to tell is to check the underlying numbers.

This is why *benchmarks and measurements* are so much more valuable than *folk wisdom about performance*. An honest benchmark tells you what the current physics is. A folk rule tells you what it used to be.

## Part 11: The Harder Case — When You Are the Monkey

I have been writing this post as if you are a thoughtful engineer evaluating somebody else's rules. But here is an uncomfortable truth. Most of the time, you are not the thoughtful engineer evaluating somebody else's rules. You are the monkey. You are enforcing practices you have never examined, because the team enforces them, because the documentation says to, because your first project went wrong when you did not.

I am. I catch myself doing this all the time. I flinch at `goto` in C# even though C#'s `goto` is genuinely a controlled, structured construct that makes some state machines clearer than the alternatives. I treat `var` as lazy even though it is exactly as type-safe as explicit type names and often clearer. I insist on three-letter log level prefixes on log lines because somebody I respected in 2014 said to. Ninety per cent of the rules I follow, I follow on authority, not because I have personally verified them.

This is fine. This is the only way a profession with any complexity can function. You cannot personally derive every rule. If you tried to, you would never ship anything. The specialisation-and-accumulation-of-wisdom thing from Part 1 is the whole reason human civilisation has a comparative advantage over other animals. You are supposed to inherit most of your rules.

But it means that the Chesterton's Fence question is not *just* about rules you can see. It is also about the rules in your own head that you have never consciously registered as rules. Every rule you follow on muscle memory is a potential monkey-cage rule. Every aesthetic preference you treat as self-evident is a potential monkey-cage rule. Every "obvious" piece of advice you would give a junior is a potential monkey-cage rule.

The practice of software engineering is, in large part, the practice of periodically auditing your own rulebook. Reading the current official documentation. Writing down a rule and then trying to argue against it to see if it still holds. Watching a junior do something "wrong" and sitting with the possibility that you are the one who is wrong. Reading a post from an unfamiliar tradition — the functional programming world, the embedded systems world, the game-development world, the mainframe world — and watching yourself react, and noticing which reactions are based on evidence and which are tribal.

This is uncomfortable. It is supposed to be. The alternative is the monkey cage.

### A Practical Audit Question

Here is a question I ask myself once a year. It takes about an afternoon and it has rewarded me every time.

*Pick a rule you strongly believe. Pretend you have never heard of it. What evidence would convince you it was correct?*

For each rule, try to reconstruct the evidence. Not the authority ("Uncle Bob said so"), not the tribal knowledge ("everyone knows you shouldn't"), but the actual empirical case. For a surprising number of my strongly-held rules, I cannot reconstruct the evidence. The rule, it turns out, was installed by a book I read in 2007 whose context was specific to the Java Enterprise Edition 5 ecosystem and which no longer applies. I thought I knew. I was a monkey.

The engineers I most admire do this constantly. They hold their own rules loosely. They do not think of their opinions as their identity. They will cheerfully tell you what they were wrong about in 2018 and why. They read the new docs, not the old docs. They do not have a "2014 rulebook" in their head; they have a "continuously updated" rulebook that incorporates new information as it arrives.

If you can only afford to develop one meta-skill over the course of your career, develop this one.

## Part 12: Applying This to the Current Project

Let me tie all of this back to the blog you are currently reading.

This blog is built on Blazor WebAssembly, .NET 10, and a GitHub Pages deployment. Every choice in that stack was made after a Chesterton's Fence audit. Let me walk through a few of them, because the reasoning is instructive.

**Why Blazor WebAssembly instead of Blazor Server?** Blazor Server is simpler in some ways — no WASM download, full access to the server, direct database calls — but it requires a persistent SignalR connection, which means it cannot be a purely static site. GitHub Pages does not do persistent connections. For a personal blog, Blazor Server is a phantom fence; it was the right choice for a certain kind of internal enterprise app in 2019, and it is still defensible for those apps, but it is not the right choice here. Blazor WASM compiles to static files, which is what GitHub Pages serves.

**Why .NET 10 instead of an older .NET?** .NET 10 (released November 2025) is the current LTS. It brings AOT improvements relevant to WASM bundle size, new language features in C# 14, and a mature set of analyzers. Older versions work; newer versions have better cold-start and smaller bundles. The fence "pin everything to the oldest supported version" is a defensive posture that made sense when .NET versions were wildly incompatible (think the Framework 3.5 to 4.0 era) and has since expired. .NET's compatibility story is now extremely good — LTS-to-LTS upgrades in the Core/.NET 5+ era have been nearly painless.

**Why SQLite and PostgreSQL as first-class databases?** Both are free, both are battle-tested, both are licensed in a way that lets us never have to worry about a bill landing on our desk. SQLite is the right choice for embedded use; PostgreSQL is the right choice for server use. The fence "use what your vendor tells you to use" was sometimes a real fence (vendor support contracts, compliance) and is now mostly a phantom for personal and open-source projects. We keep the fence where it applies and tear it down where it does not.

**Why no paid NuGet packages, ever?** Because payment for software introduces a fence that can move. A package you bought in 2020 can be acquired in 2023 and repriced in 2025 and discontinued in 2027. A package that is free and open-source may not survive forever — open-source projects do die — but when they die, you have the source, and you can fork it, and you control the fence. The fence "only use what we can afford" applied to individual developers; the fence "only use what we can still afford in ten years" is stricter and holds for project longevity.

**Why OpenTelemetry everywhere, from day one?** Because of the Part 9 argument. Telemetry is the record of *why*. We install it early, before there are any production mysteries, so that when the first mystery arrives, we already have data.

**Why extensive automated tests, unit and integration both?** Because tests are the executable form of *fence-with-a-reason*. Every test is a commitment to preserve a behaviour. If we cannot write a test for a behaviour, we probably do not understand it well enough to ship it.

**Why UK spelling throughout?** No serious reason; it is a house-style decision. I mention it because house-style decisions are a perfect example of a fence that is worth installing and worth being honest about. *We use colour instead of color and organise instead of organize because we decided to, not because one is correct and the other is wrong.* Writing that down in a style guide saves the next writer from having to re-argue it.

Every one of these decisions is reversible. None of them is a moral commitment. If .NET 12 ships something that makes this whole approach obsolete, we will change. If PostgreSQL's licensing changes, we will reassess. The fences are where they are because we decided, and we wrote down why, and that means the next person (or the next-year's version of us) can evaluate whether they still make sense.

## Part 13: The Full Framework, For Your Notebook

If you are going to take one thing from this post and pin it above your desk, here it is.

*Before you install, change, or remove any convention in a codebase — including conventions in your own head — ask five questions in order:*

**1. Why is this here?** Trace the genealogy. Find the earliest commit, the earliest document, the earliest reason. If you cannot find it, make a note of that; the absence of a recorded reason is itself a piece of information.

**2. When was the reason last true?** Has the environment that motivated this rule changed? Has the tool been upgraded, the regulation lifted, the team reorganised, the customer departed, the platform rewritten?

**3. What would break if I removed it?** Do a mental simulation first, a small-scale experiment second. Do not jump straight to production.

**4. If I remove it, how will I know if I was wrong?** You need an observation mechanism. A test, a metric, a user feedback channel, a canary deploy. If you cannot tell the difference between "I was right" and "I was wrong", you are not ready to make the change.

**5. Will I write down what I did, so the next person does not have to redo this investigation?** A commit message, a code comment, an ADR, a post-mortem. Passing it forward is the entire point.

That is the whole framework. Five questions. It is not a complicated idea. The complication is the discipline.

### A Decision Table

For quick reference, here is how the five questions map to concrete actions:

| Signal | Interpretation | Action |
|---|---|---|
| Rule has a clear, recent reason that still applies | Live physics fence | Keep it. Document it if not already documented. |
| Rule has a clear reason that no longer applies | Obsolete fence | Remove it. Write down why it was there and why it is no longer needed. |
| Rule's origin is "we've always done it this way" | Probable monkey-cage rule | Investigate. Most of the time you will find an expired reason. |
| Rule's origin is a specific bug report, regulation, or post-mortem | Load-bearing fence | Keep it. Reference the source directly in a comment. |
| Rule is enforced by automation (tests, linters, CI) but nobody knows why | Fence with a sign that fell off | Trace the sign. If you can reconstruct it, note it. If you cannot, the rule is a candidate for retirement — but do the safe-to-fail probe first. |
| Rule exists because a respected senior said so | Authority without evidence | Ask the senior. Most good seniors will be thrilled you asked and may themselves be surprised by how much of their own rulebook is cargo-cult. |
| Rule exists in one team's codebase but not another's | Local tradition | Neither good nor bad. Worth understanding why it diverged. Sometimes the divergence is revealing. |
| Rule is the default of a framework version you are not on any more | Moved fence | Probably retire. Double-check the migration guide. |

This is not a substitute for thinking. It is a set of prompts to make thinking cheaper.

## Part 14: A Few Closing Honest Caveats

I want to end with a set of things this post has not told you, because I owe you the honest picture.

**First: Chesterton's Fence can be over-applied.** There is a failure mode where every change is delayed pending a full audit of its implications, and no change ever ships. This is the Kafkaesque version of the rule. The fix is to set a *time budget* for your investigation proportional to the blast radius. For a one-line CSS change, the audit is thirty seconds. For a database schema migration, the audit is a week. Scale the rigour to the risk, not the other way around.

**Second: some fences are there for reasons you will disagree with.** You will discover that a rule exists because a specific CEO had a specific pet peeve in 2012, or because a specific auditor required a specific ceremony, or because a specific regulation, now repealed, once mandated it. You will want to remove the rule because the reason is dumb. The reason *is* dumb. But before you remove it, ask whether the CEO's successor still has the same preference, whether the auditor will be back next quarter, whether the regulation might be re-enacted. The genealogy is not the verdict. It is one piece of evidence.

**Third: sometimes you should tear down a fence even without understanding it.** The classic case is a security issue. If a codebase has a security hole, you patch the hole first and do the genealogy second. Emergency mode exists. The rule is not "never act without full information"; the rule is "act with full information when you can, and know when you cannot".

**Fourth: the parables in this post are, themselves, fences worth auditing.** The five-monkeys story is a business parable, not a real experiment, and I told you that already. The Chesterton's Fence passage is from a specific 1929 essay in a specific theological-political context that I have borrowed for software-engineering purposes; Chesterton himself would probably be surprised and possibly displeased by the use. The top-down-cleaning rule is real but has edge cases (wet trades like tiling have their own sequencing). Shaikh Ismail's death was a human tragedy, not a teachable moment, and I have tried to use it respectfully. Every analogy in this post is itself a minor fence I have installed in your head; please feel free to audit them the way you audit the others.

**Fifth: you will get this wrong.** You will, on some Thursday afternoon, remove a fence that turns out to be load-bearing. You will, on some Monday morning, spend three hours defending a fence that turns out to be a phantom. The goal is not to never be wrong. The goal is to be wrong cheaply — to structure your work such that being wrong is recoverable, and to structure your documentation such that the next person can do better than you did.

This is the humble version of the engineering virtue. You are not the final authority on any of this. You are one more person in a long chain of people trying to build things that work, and the best thing you can do is leave the chain a little better-organised than you found it.

## Part 15: Resources

A short list of things worth reading, in no particular order, if you want to go deeper.

- G.K. Chesterton, *The Thing: Why I Am a Catholic* (1929), specifically the essay "The Drift from Domesticity", which contains the original fence parable. Most of the book is unrelated to engineering and will not appeal to all readers; the passage is the part that matters.
- Michael Feathers, *Working Effectively with Legacy Code* (2004). The canonical treatment of what to do when you inherit a codebase you did not write. Chapters on "characterisation tests" and "seams" are particularly relevant to the probing-safely argument in Part 3.
- Michael Nygard, *Release It!: Design and Deploy Production-Ready Software* (2007, second edition 2018). The best book on production reliability. Introduced the term "stability pattern" and popularised the Architecture Decision Record format.
- Martin Fowler, "StranglerFigApplication" (2004 blog post, still online). The original articulation of the gradual-migration strategy referenced in Part 5.
- Hyrum Wright, "Hyrum's Law" (self-published, hyrumslaw.com). One paragraph. Worth reading annually.
- The Angular documentation, at angular.dev. Angular's docs team has done an unusually good job of marking which APIs are the current recommendation versus which are legacy. Read the "guide to incremental adoption of modern Angular" page if you are migrating an older app.
- The .NET documentation for OpenTelemetry integration, at learn.microsoft.com. Specifically the "Observability with OpenTelemetry" page in the .NET 10 docs. Do this early in a project, not late.
- The real Stephenson 1967 paper on cultural acquisition in rhesus monkeys, if you can find it in a university library. The five-monkeys parable makes a lot more sense once you know what Stephenson actually did.
- Gregor Hohpe, *The Software Architect Elevator* (2020). Particularly the chapters on how architectural decisions propagate through an organisation and why writing them down matters.
- Any of Dan Luu's essays on production systems and why things fail. No single book to recommend; his blog is the recommendation.
- The OWASP Top Ten (https://owasp.org/Top10/), updated every few years. A good example of a fence-book that is kept actively maintained; the 2024 revision dropped some categories and added others because the underlying threat landscape changed. The way OWASP itself audits its rules is an instructive model.

Read widely. Trust the authors who tell you what they were wrong about in the last edition. Discount the authors who never update their views.

Thank you for making it this far. If you only remember one sentence from this whole post, make it this one.

> *Before you clear away a fence, and before you defend a fence, find out who put it there, and why.*

That is the whole job.

See you next time.

---

*My Blazor Magazine is a free, open-source Blazor WebAssembly blog on .NET 10. All posts are written by the team at myblazor-team. Comments, corrections, and fence audits are welcome.*
