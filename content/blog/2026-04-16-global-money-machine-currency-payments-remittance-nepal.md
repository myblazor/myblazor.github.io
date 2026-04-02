---
title: "The Global Money Machine: Currency, Digital Payments, Remittance, and Nepal's Place in a Changing World"
date: 2026-04-16
author: observer-team
summary: An exhaustive exploration of how money moves around the world — from SWIFT messages and card networks to Brazil's Pix, India's UPI, China's digital yuan, Europe's Wero, and CBDCs — with a deep focus on remittance, foreign exchange, and what all of it means for Nepal.
tags:
  - finance
  - payments
  - remittance
  - nepal
  - digital-currency
  - cbdc
  - deep-dive
  - policy
---

Picture this: a twenty-four-year-old Nepali construction worker in Doha finishes a twelve-hour shift. He opens an app on his cracked-screen smartphone, punches in his mother's phone number in Dhading, and sends fifteen thousand Nepali rupees home. The money arrives before he has finished his dal bhat. On the other side of the planet, a German tourist in Kathmandu taps her phone against a card reader at a Thamel coffee shop, and her payment travels from her account in Frankfurt through at least four intermediaries — a card network, an acquiring bank, a correspondent bank, a local processor — before the café owner's Nepali bank account is credited three days later. Both transactions move "money." But the infrastructure, the cost, the speed, and the political implications behind each one are so wildly different that calling them both "payments" is like calling both a bicycle and an Airbus A380 "vehicles."

This article is about how money actually moves — not just between bank accounts in New York, but between a street vendor in São Paulo and her supplier, between a migrant worker in Seoul and his family in Sunsari, between a central bank and every citizen who uses its currency. We will cover the ancient plumbing of correspondent banking and SWIFT, the card empires of Visa and Mastercard, the real-time payment revolutions of India's UPI and Brazil's Pix, the super-app ecosystems of Alipay and WeChat Pay, the emerging sovereignty movements of Europe's Wero and the digital euro, the most advanced CBDC experiment on Earth in China's e-CNY, the role of cryptocurrency and stablecoins, the mechanics and politics of foreign exchange, and the deeply human story of remittance — what it really means, who it really serves, and whether it is a lifeline or a trap.

And then we will talk about Nepal. Because Nepal sits at the intersection of almost every trend in this article: a remittance-dependent economy where workers abroad send home more than the country earns from tourism, exports, and foreign aid combined. A country where digital wallets like eSewa and Khalti are spreading fast, where the Nepali rupee is pegged to the Indian rupee, where foreign exchange reserves rise and fall with how many young people board planes to the Gulf, and where the question of what comes next — a central bank digital currency? UPI integration? A shift from Gulf labor to skilled migration to the West? — is not academic. It is existential.

Let us begin.

## Part 1: What Is Money, Really? — A Five-Minute History That Explains Everything That Follows

Before we can understand digital currencies or SWIFT messages, we need to understand what money actually is. Not the textbook definition — "a medium of exchange, a unit of account, a store of value" — but the practical reality.

Money is a shared fiction. It works because everyone agrees it works. A hundred-rupee note is a piece of polymer with Sagarmatha printed on it. It has no intrinsic value. You cannot eat it. But the shopkeeper in Bhaktapur accepts it because she knows the vegetable wholesaler in Kalimati will accept it from her, and the wholesaler knows Nepal Rastra Bank stands behind it.

This shared fiction has taken many forms throughout history. Cowrie shells in South Asia and Africa. Gold coins in Rome and the Ottoman Empire. Tally sticks in medieval England. Salt in Ethiopia (the Amharic word for salary, "demoz," shares a root with the word for salt). Enormous stone discs called Rai on the island of Yap in Micronesia — some too heavy to move, so "ownership" was simply agreed upon by the community, an eerily prescient model of blockchain's distributed ledger.

The critical innovation that created the modern financial system was not a new form of money itself, but the idea that you could write a promise to pay money later. Bills of exchange — essentially IOUs — emerged in medieval Italy and the Islamic world roughly simultaneously. A merchant in Venice could write a note promising to pay a sum in Florence, hand it to a trader heading south, and that trader could present it to a banker in Florence for payment. The banker would be repaid by the Venetian merchant's local agent. No gold had to travel the dangerous roads between cities. Only a piece of paper did.

This is, in essence, still how international money transfer works today. When you send money from New York to Kathmandu, physical dollars do not fly across the ocean. Messages fly. Banks settle their obligations to each other through accounts they hold with one another — just like those medieval Venetian and Florentine bankers. The technology has changed. The fundamental architecture has not.

### The Bretton Woods System and the Dollar's Dominance

In 1944, forty-four Allied nations met at Bretton Woods, New Hampshire, and agreed to peg their currencies to the US dollar, which was itself pegged to gold at $35 per ounce. This created a stable system for international trade. If you knew the exchange rate between your currency and the dollar, and between the dollar and any other currency, you could trade with anyone.

In 1971, President Nixon ended the dollar's convertibility to gold — the so-called "Nixon shock." Currencies began floating against each other, their values determined by market forces. The volume of foreign exchange transactions exploded. Banks needed a faster, more reliable way to communicate payment instructions across borders. The old Telex system — manual, slow, error-prone — was no longer sufficient.

This is where SWIFT enters the picture.

## Part 2: The Plumbing — SWIFT, Correspondent Banking, and How International Transfers Actually Work

### What SWIFT Is (and What It Is Not)

SWIFT — the Society for Worldwide Interbank Financial Telecommunication — is perhaps the most misunderstood institution in global finance. People say "I'll send you a SWIFT transfer," implying that SWIFT moves their money. It does not. SWIFT is a messaging network. It carries instructions between banks. The actual money moves through a system of correspondent banking accounts — a system that predates SWIFT by centuries.

SWIFT was founded in Brussels in 1973 by 239 banks from 15 countries. It went live in 1977, replacing the Telex system. Today, over 11,000 financial institutions in more than 200 countries use SWIFT. In 2024, member institutions sent an average of 53.3 million messages per day — up from 2.4 million daily messages in 1995.

Here is how a SWIFT payment actually works. Imagine you are a software developer in Virginia, and you want to send $500 to your friend's bank account in Kathmandu.

**Step 1: You initiate the transfer.** You log into your bank's online portal, enter the recipient's account number, the recipient bank's SWIFT/BIC code (an 8-or-11-character alphanumeric code identifying the bank), and the amount.

**Step 2: Your bank sends a SWIFT message.** Your bank generates an MT103 message — the standard SWIFT message type for a single customer credit transfer. This message contains your details, the recipient's details, the amount, the currency, and any intermediary bank routing information. The message travels through SWIFT's secure network.

**Step 3: Correspondent banking takes over.** Your bank in Virginia probably does not have a direct relationship with Nepal Investment Bank in Kathmandu. It needs an intermediary. Your bank might have a "nostro" account (from the Latin "ours") at a major correspondent bank — say, Citibank in New York. Citibank, in turn, has a relationship with Standard Chartered in Nepal, which has a relationship with Nepal Investment Bank. The payment "hops" through these relationships.

At each hop, the correspondent bank debits one account and credits another. No physical money moves. Ledger entries are adjusted. The SWIFT message is the instruction that tells each bank what to debit and credit.

**Step 4: Settlement.** The payment settles — meaning it becomes final and irrevocable — through the local payment systems of each country involved. In the US, this might be Fedwire. In Nepal, it would be the Nepal Rastra Bank's RTGS (Real-Time Gross Settlement) system.

**Step 5: The recipient's account is credited.** After 1 to 5 business days (sometimes longer), the recipient in Kathmandu sees the funds in their account. Fees have been deducted along the way — your bank's fee, the correspondent bank's fee, possibly a currency conversion fee.

### Why International Transfers Are Slow and Expensive

The multi-hop correspondent banking model has three fundamental problems:

**Speed.** Each hop takes time. Banks operate in different time zones, observe different holidays, and process payments in batches. A payment initiated on a Friday afternoon in New York might not arrive in Kathmandu until the following Wednesday.

**Cost.** Each intermediary takes a fee. A $500 transfer might cost $25–$45 in fees — 5 to 9 percent. For low-value remittances, this is punishing. The global average cost of sending $200 was 4.26 percent in Q1 2025, down from 7.36 percent in 2020, but still well above the UN Sustainable Development Goal target of less than 3 percent.

**Opacity.** Once you initiate a SWIFT transfer, you often cannot see where your money is or when it will arrive. SWIFT's "gpi" (Global Payments Innovation) initiative has improved tracking — you can now follow a payment in real time, like a FedEx package — but adoption is not yet universal.

### The ISO 20022 Revolution

The SWIFT network is undergoing its most significant technical transformation since its founding. Starting November 2025, banks must use ISO 20022 message formats for cross-border payment instructions. ISO 20022 replaces the old MT (Message Type) format with XML-based messages that can carry far richer data — not just "send $500 to account X," but structured information about the purpose of the payment, the parties involved, tax identifiers, and more. This richer data should improve compliance, reduce manual intervention, and eventually speed up processing.

## Part 3: The Card Empires — Visa, Mastercard, UnionPay, and How a Plastic Rectangle Conquered the World

When you tap your credit card at a store, a complex dance occurs in less than two seconds. Understanding this dance is essential to understanding why new payment systems are emerging to challenge it.

### The Four-Party Model

The traditional card payment model involves four parties: the **cardholder** (you), the **merchant** (the store), the **issuing bank** (the bank that gave you the card), and the **acquiring bank** (the bank that processes payments for the merchant). Visa and Mastercard sit in the middle as the **network** — they do not issue cards or lend money. They operate the rails.

When you tap your card, the payment terminal sends a request through the acquiring bank to the card network (Visa or Mastercard), which routes it to your issuing bank. Your issuing bank checks your account balance or credit limit, approves or declines the transaction, and sends an authorization response back through the same chain. All of this happens in roughly 1–2 seconds.

Settlement — the actual transfer of funds — happens later, typically the next business day. The merchant receives the transaction amount minus an "interchange fee" (set by the card network, paid by the acquiring bank to the issuing bank) and a "merchant discount rate" (the total cut taken from the merchant's revenue). These fees typically range from 1.5 to 3.5 percent of the transaction amount.

### The Scale of Card Networks

The numbers are staggering:

**Visa** processed approximately 257.5 billion transactions in fiscal year 2025, with total payment volume of $14.5 trillion. It has 4.48 billion cards in circulation worldwide, accepted at roughly 150 million merchant locations. Visa's global revenue for FY 2024 was $35.9 billion.

**Mastercard** processed approximately 197 billion transactions in 2024, with gross dollar volume of $9.2 trillion and net revenue of $28.2 billion for the year.

**UnionPay**, often overlooked in Western discourse, is actually the world's largest card network by number of cards in circulation. Founded in China in 2002, it recorded 228 billion transactions globally in 2023 and has surpassed both Visa and Mastercard in total payment value. Its dominance comes from being the only interbank card network in China, linking all ATMs in the country. The majority of UnionPay transactions are debit transactions.

**American Express** operates a slightly different model — it is both the network and the issuer — with 83.6 million proprietary cards and an additional 62.9 million cards issued by third-party institutions.

### Why Card Networks Are Being Challenged

For all their convenience, card networks have three vulnerabilities that new systems are exploiting:

**Cost.** A 2–3 percent cut of every transaction adds up. For a small grocery store in Brazil with thin margins, paying 3 percent to Visa is the difference between profit and loss. This is why Brazil's Pix, which charges merchants roughly 0.33 percent, has been so disruptive.

**Speed.** Card settlement takes 1–2 business days. Merchants do not receive their money instantly. Real-time payment systems deliver funds in seconds.

**Sovereignty.** Visa and Mastercard are American companies. Every euro, real, or rupee that flows through their networks generates revenue for shareholders in the United States. It also gives the US government leverage — as demonstrated when Visa and Mastercard suspended operations in Russia in 2022. This sovereignty concern is the primary driver behind Europe's Wero, India's RuPay, and China's UnionPay.

## Part 4: The Real-Time Payment Revolutions — How India, Brazil, and China Rewired Money

### India's UPI: The Largest Digital Payment System on Earth

To understand the scale of what India has built, consider this single statistic: in December 2025, India's Unified Payments Interface (UPI) processed 21.63 billion transactions worth ₹27.97 trillion (approximately $336 billion) — in a single month. In the full calendar year 2025, UPI recorded over 228 billion transactions worth nearly ₹300 trillion (approximately $3.6 trillion). That is roughly 625 million transactions per day.

UPI was launched on April 11, 2016, by the National Payments Corporation of India (NPCI). It is an account-to-account payment system — meaning money moves directly from one bank account to another, without any intermediary like a card network. You identify yourself with a "UPI ID" (like yourname@bankhandle), and you authorize payments with a PIN on your smartphone.

The ecosystem is dominated by two apps: PhonePe (approximately 48 percent market share) and Google Pay (approximately 37 percent). Paytm holds about 7–8 percent. Together, the top two control over 85 percent of all UPI transactions, which has led NPCI to propose a 30 percent volume cap per app to prevent monopolistic concentration (though enforcement has been repeatedly delayed).

**What makes UPI remarkable:**

**Zero cost for consumers.** There are no fees for person-to-person UPI transfers. For person-to-merchant transactions above ₹2,000, a small merchant discount rate (1.1 percent) applies, but for smaller transactions — the overwhelming majority — there is effectively no fee. This zero-cost structure is politically popular but creates an ongoing debate about sustainability. The payments industry has lobbied to introduce MDR (merchant discount rate) on UPI, arguing that without revenue, payment apps cannot sustain their operations.

**Interoperability.** Unlike closed-loop systems (where you can only pay within one app's ecosystem), UPI works across all participating banks. You can use PhonePe to pay someone on Google Pay because both apps connect to the same underlying bank infrastructure.

**International expansion.** UPI is now accepted in at least 12 countries, including Nepal, Bhutan, Sri Lanka, Mauritius, Singapore, UAE, France, and Qatar. In Nepal specifically, Indian tourists can use UPI-linked apps to pay at merchants that accept it. NPCI has been actively pursuing partnerships to extend UPI's cross-border reach, framing it as a technology stack that other nations can adopt.

**The average ticket size is shrinking.** In H1 2025, the average UPI transaction was just ₹1,348 — down from ₹1,478 in H1 2024. This means UPI is increasingly used for everyday micro-purchases: a cup of chai, a bus ticket, a kg of onions. This is financial inclusion in action — people who never had credit cards and rarely used debit cards are now transacting digitally for the first time.

### Brazil's Pix: How a Central Bank Built the Future of Money in Two Years

If UPI is the largest real-time payment system, Brazil's Pix might be the most transformative. Announced by the Central Bank of Brazil in February 2019 and launched on November 16, 2020, Pix has become the dominant payment method in Brazil in barely five years.

The statistics are extraordinary. Pix processed 63.4 billion transactions worth $4.6 trillion in 2024 — a 53 percent year-over-year growth in both volume and value. By May 2025, Pix had accumulated over 175 million users (160 million individuals and 15 million businesses), covering 93 percent of Brazil's adult population. In June 2025, Pix hit a single-day record of 276.7 million transactions — a daily volume that exceeds the entire monthly transaction count of most European instant payment systems.

**How Pix works:** Like UPI, Pix is an account-to-account system built on top of the existing banking infrastructure. Users register a "Pix key" — which can be their CPF (tax ID), email, phone number, or a random key — linked to their bank account. To pay, you either scan a QR code, enter the recipient's key, or (since February 2025) tap your phone via NFC using "Pix por Aproximação" (Contactless Pix). Money moves instantly, 24/7, 365 days a year, including holidays and weekends. For individuals, Pix is completely free.

**Why Pix succeeded so spectacularly:**

**It solved real pain.** Before Pix, Brazil had a complex landscape of payment methods: boletos (payment slips that took 1–3 days to clear), TED and DOC bank transfers (expensive, with limited hours), credit cards (high merchant fees of 2–5 percent), and cash (expensive to handle, insecure). Pix replaced all of them with a single, instant, free alternative.

**The central bank mandated participation.** Any financial institution with more than 500,000 active accounts was required to offer Pix. This was not optional. This ensured universal availability from day one.

**QR code standardization.** The Central Bank of Brazil created a standardized QR code format, so every merchant — from a major retailer to a beach coconut vendor — could accept Pix with the same consistent experience.

**New features keep expanding use cases.** Pix Agendado (scheduled payments, launched October 2024) lets you schedule transfers for future dates. Pix Automático (automatic recurring payments, launched June 2025) enables subscriptions and utility bill payments — critical for the 60 million Brazilians who do not have credit cards. Pix now accounts for 42 percent of Brazilian e-commerce and 34 percent of point-of-sale value.

In July 2025, Nobel Prize-winning economist Paul Krugman praised Pix and suggested that Brazil may have invented the "future of money" — a system that is "actually achieving what cryptocurrency boosters claimed, falsely, to be able to deliver."

Pix is also expanding internationally. As of 2025, it is accepted in Argentina, Chile, Portugal, Spain, and the United States, driven by merchant demand for alternative payment acceptance.

**The political dimension is fascinating.** In July 2025, the Office of the United States Trade Representative launched an investigation into what it described as unfair trading practices by Brazil in the electronic payment services sector — an investigation widely understood to target Pix specifically, under pressure from American credit card companies. Brazilian President Lula da Silva accused President Trump of being "bothered by Pix" because it "will put an end to credit cards."

### China: Alipay, WeChat Pay, and the Super-App Model

China's digital payment revolution took a different path. Rather than being led by the central bank (like Brazil) or a banking consortium (like India), China's revolution was led by technology companies — specifically Alibaba's Alipay (launched 2004) and Tencent's WeChat Pay (launched 2013).

These are not just payment apps. They are super-apps — platforms that combine messaging, social media, shopping, food delivery, ride-hailing, bill payment, insurance, investments, and payments into a single interface. In China, it is entirely common to go weeks without touching cash or a bank card. You scan a QR code for everything: your morning jianbing from a street vendor, your taxi ride, your electricity bill, your hospital co-pay.

**Alipay** (through its parent Ant Group) connects 1.8 billion users to 100 million merchants across 14 markets via the Alipay+ platform. **WeChat Pay** has integrated payments so deeply into social interactions that sending money is as natural as sending a message. The "red envelope" feature — a digital version of the traditional cash gift — went viral during Chinese New Year and drove hundreds of millions of users to activate WeChat Pay.

Together, Alipay and WeChat Pay process the vast majority of China's retail digital payments. Their dominance created a curious problem for the Chinese government: two private companies effectively controlled the country's payment infrastructure. This is one reason China accelerated its CBDC development.

## Part 5: Central Bank Digital Currencies — The e-CNY and the Digital Euro

### China's Digital Yuan (e-CNY): The World's Largest CBDC Experiment

China's digital yuan, officially the e-CNY, is the most advanced central bank digital currency in the world by any measure. The People's Bank of China (PBOC) began research in 2014, started pilot programs in 2020, and by the end of November 2025 had recorded 3.48 billion cumulative transactions worth 16.7 trillion yuan (approximately $2.37 trillion). That transaction value grew over 800 percent from 2023.

On January 1, 2026, a new management framework took effect that represents a fundamental shift in the e-CNY's nature. The digital yuan transitioned from "digital cash" — non-interest-bearing, like physical banknotes — to "digital deposit money." Under the new framework, commercial banks can pay interest on e-CNY wallet balances, making it the world's first interest-bearing CBDC. Wallet balances are now treated under existing deposit insurance rules, and banks must hold reserves against them, just like traditional deposits.

**Why this matters:** The e-CNY is designed to compete directly with Alipay and WeChat Pay. By offering interest on balances, the PBOC hopes to incentivize users to keep money in e-CNY wallets rather than converting it back to bank deposits after each transaction. The interest-bearing feature makes e-CNY more attractive as a store of value, not just a payment tool.

**The international dimension is equally important.** The PBOC has established an E-CNY Operations and Management Center in Beijing for domestic infrastructure and an International Operations Center in Shanghai (launched September 2025) for cross-border use cases. Project mBridge — a multi-CBDC platform connecting central banks from China, Thailand, the UAE, Hong Kong, and Saudi Arabia — has seen its transaction volume surge to $55.49 billion, with e-CNY making up over 95 percent of total settlement volume. This positions the digital yuan as a potential alternative settlement currency for countries seeking to reduce reliance on the US dollar.

### The Digital Euro: Europe's Answer

The European Central Bank is developing a digital euro — a CBDC for the 21 countries of the eurozone (Bulgaria will join in 2026). The timeline is clear:

- The preparation phase ran from November 2023 to October 2025.
- The ECB expects EU co-legislators to adopt the digital euro regulation in the course of 2026.
- If legislation passes, a 12-month pilot will begin in the second half of 2027.
- Full issuance could happen during 2029.

Technical standards will be announced in the summer of 2026, and the ECB's ECON committee is scheduled to vote on the proposals on May 5, 2026. The European Parliament voted in February 2026 to back the digital euro project.

The estimated cost for EU banks to implement the digital euro is €4–6 billion over four years. The ECB estimates a total build cost of approximately €1.3 billion, with annual running costs of €320 million.

The digital euro would carry a holding limit of €3,000–4,000 per person. Acceptance by merchants would be mandatory by law. Basic digital euro services would be free for individuals. Offline payments — working without an internet connection — are a key design feature, intended to provide cash-like privacy and resilience.

The political motivation is sovereignty. Non-European companies currently process nearly two-thirds of eurozone card transactions. Thirteen EU member states depend entirely on international card schemes. The digital euro, combined with Wero, represents Europe's attempt to reclaim control of its payment infrastructure.

## Part 6: Wero — Europe's Pan-European Payment System

While the digital euro is years away, Europe's more immediate challenge to Visa and Mastercard is already live. Wero, launched on July 2, 2024, by the European Payments Initiative (EPI), is a pan-European mobile payment system built on SEPA Instant Credit Transfers.

Wero enables real-time account-to-account payments using a phone number, QR code, or URL. It is intended to replace fragmented national systems: Giropay in Germany, Paylib in France, Payconiq in Belgium and Luxembourg, and iDEAL in the Netherlands.

**Current status as of early 2026:**

- Live for peer-to-peer payments in Germany, France, Belgium, and Luxembourg.
- The Netherlands is migrating from iDEAL to Wero (co-branding phase began January 2026; full phase-out of iDEAL planned by end of 2027).
- E-commerce payments launched in Germany in November 2025 and are rolling out in France and Belgium.
- NFC-enabled point-of-sale payments (tap-to-pay) are scheduled for 2026–2027.
- Wero has exceeded 50 million registered users as of February 2026, with €7.5 billion in transfers in its first year.

Major brands are signing on. In France, Air France, E.Leclerc, Orange, and Veepee accept Wero. The French government's tax authority (DGFIP) plans to integrate Wero for public services. In Germany, Deutsche Bank, Postbank, Sparkassen, VR Banks, ING, Revolut, and N26 have all joined.

The long-term roadmap includes BNPL (Buy Now, Pay Later), subscription management, digital identity, and loyalty program integration. If the ECB launches the digital euro, Wero is positioned to serve as its primary distribution channel — users could hold and spend digital euros alongside bank account funds in the same app.

The big question is whether Wero can succeed where previous European payment initiatives have struggled. Its rollout remains concentrated in Western Europe — Spain, Italy, Poland, and the Nordics are absent from the current roadmap. But the momentum is real: with iDEAL's entire Dutch merchant base forced to migrate by 2027, Wero will soon have a captive national market, and the EPI-EuroPA partnership extends its potential reach to 15 countries and over 382 million people.

## Part 7: Cryptocurrency and Stablecoins — The Parallel Universe

No article on money transfer would be complete without addressing cryptocurrency, but it is important to be precise about what crypto does and does not do in the real world of payments.

**Bitcoin** was designed as "peer-to-peer electronic cash" according to its 2008 white paper. In practice, it has become primarily a speculative asset and a store of value (or at least an attempted store of value — its volatility makes it poorly suited for everyday transactions). You would not want to pay for groceries with an asset that might be worth 10 percent less by the time you finish cooking dinner.

**Stablecoins** — cryptocurrencies pegged to a fiat currency, typically the US dollar — have found much more traction in payments. USDT (Tether) and USDC (Circle) processed over $4 trillion in transactions from January to July 2025, making up over 40 percent of all crypto payments. Stablecoins are used heavily for cross-border remittances, particularly in corridors where traditional banking is slow, expensive, or restricted.

For developing countries, stablecoins offer a paradox. They can be faster and cheaper than SWIFT for sending money across borders. But they also represent de facto dollarization — when citizens hold USDT instead of their local currency, they are effectively shifting their savings into US dollars, which can undermine the local currency and the central bank's monetary policy.

**Ripple's XRP** and the RippleNet On-Demand Liquidity (ODL) system achieved settlement in as fast as 10 seconds for 93 percent of global transfers in 2025. This positions it as a potential SWIFT alternative, though regulatory challenges (particularly the SEC lawsuit in the US) have hampered adoption.

**A consortium of 11 European banks** is building a euro-backed stablecoin, reflecting a desire to harness blockchain's efficiency without ceding monetary sovereignty to US dollar-denominated tokens.

The honest assessment: cryptocurrency has not replaced traditional payment systems for everyday use. But stablecoins are carving out a genuine role in cross-border remittances and trade settlement, particularly in regions underserved by traditional banking.

## Part 8: Foreign Exchange — The Invisible Force That Shapes Everything

Every international payment involves a currency conversion, and the foreign exchange (forex) market is the largest financial market in the world. Daily forex trading volume exceeds $7.5 trillion — dwarfing the stock market, the bond market, and everything else.

### How Exchange Rates Work

In a **floating exchange rate** regime (used by the US dollar, euro, Japanese yen, British pound, and most major currencies), the value of a currency is determined by supply and demand in the market. When more people want to buy dollars (perhaps because the US economy is strong or US interest rates are high), the dollar appreciates. When fewer people want dollars, it depreciates.

In a **fixed (pegged) exchange rate** regime, a country's central bank commits to maintaining its currency at a specific rate against another currency or basket of currencies. The central bank must buy or sell its own currency to maintain the peg, which requires holding large foreign exchange reserves.

In a **managed float** (also called a "dirty float"), the central bank allows the market to determine the rate in general but intervenes occasionally to prevent excessive volatility.

### Nepal's Currency Regime

Nepal operates a **fixed peg to the Indian rupee** at a rate of 1 INR = 1.6 NPR, established in 1993. This peg is a deliberate policy choice with significant implications:

**Why Nepal pegs to the Indian rupee:** India is Nepal's largest trading partner, accounting for roughly two-thirds of Nepal's total trade. The open border between the two countries means that goods, services, and people flow freely. A stable exchange rate with India reduces transaction costs and uncertainty for cross-border trade.

**The consequences of the peg:** Nepal's monetary policy is, in practice, constrained by India's monetary policy. If the Reserve Bank of India raises interest rates and the INR strengthens, the NPR strengthens too — even if Nepal's domestic economy would benefit from a weaker currency. Nepal essentially imports India's monetary conditions.

**The dollar question:** While the NPR is officially pegged to the INR, its value against the US dollar fluctuates with the INR/USD exchange rate. When the INR weakens against the dollar, the NPR weakens too. This matters enormously because many of Nepal's imports (particularly petroleum products) are priced in dollars, and remittances from Gulf countries and the US arrive in dollars. The NPR depreciated 2.3 percent against the US dollar between mid-July and mid-October 2025, with the buying rate reaching Rs 140.22 per dollar by mid-October.

### Unique Challenges by Country Type

Different countries face different forex challenges depending on their economic structure:

**Oil exporters** (Saudi Arabia, UAE, Kuwait) tend to peg to the dollar because oil is priced in dollars. Their reserves are massive, making the peg easy to maintain — until oil prices crash.

**Manufacturing exporters** (China, South Korea, Vietnam) need competitive exchange rates to keep their exports affordable. A currency that is "too strong" can price their goods out of global markets.

**Remittance-dependent economies** (Nepal, Philippines, Bangladesh, El Salvador) need their currency to be stable enough to maintain purchasing power for remittance-receiving families, but not so strong that it discourages remittances (a stronger home currency means each dollar sent home buys fewer local goods — oh wait, it is the opposite: a weaker home currency means each dollar buys more, making remittances more valuable in local terms).

**Nepal falls squarely into the remittance-dependent category.** Its forex reserves, trade deficit, and current account balance are all driven primarily by remittance flows. When remittances rise, reserves rise, the current account improves, and there is more foreign currency available for imports. When remittances decline — as during COVID-19 or the current West Asia conflict — the entire economy feels the strain.

## Part 9: Remittance — What It Actually Means

### The Textbook Definition

Remittance is money sent by a person in a foreign country to someone (typically a family member) in their home country. The World Bank defines "personal remittances" as the sum of personal transfers and compensation of employees.

### The Real-World Meaning

But the textbook definition misses the human reality. Let us break down what remittance actually means in practice:

**For the sender**, remittance is sacrifice. It is the twenty-four-year-old Nepali man working 60-hour weeks in a construction site in Qatar in 45-degree heat, living in a dormitory shared with eleven other men, eating dal bhat from a communal kitchen, and sending 60 to 70 percent of his salary home. It is the nurse from Kerala in a hospital in Riyadh, the domestic worker from the Philippines in Hong Kong, the taxi driver from Bangladesh in Dubai. Remittance is the financial expression of love, obligation, and separation.

**For the receiver**, remittance is a lifeline. It pays for school fees, medical bills, daily groceries, loan repayments, house construction, and — yes — the smartphone and mobile data plan that makes the next transfer possible. In Nepal, remittances have helped reduce extreme poverty from nearly 70 percent to approximately 25 percent over the last 15 years, according to the World Bank.

**For the economy**, remittance is a macroeconomic pillar. In Nepal, remittances constitute approximately 26.6 percent of GDP as of 2023. By some measures, including compensation of employees, the figure was 66.12 percent of GDP in 2024 according to Trading Economics, though this broader measure includes short-term worker compensation and is calculated differently. Remittance accounts for nearly 67 percent of Nepal's foreign currency inflows and finances approximately 84 percent of the trade deficit.

**For the government**, remittance is a double-edged sword. It provides foreign exchange, supports the balance of payments, and reduces poverty — all without the government having to do anything. This creates a "comfortable position" (as Nepali economists have noted) where the government is not compelled to develop productive sectors like manufacturing, agriculture, and tourism, because foreign exchange flows in regardless.

### The Paradox of Rising Remittance and Declining Per-Worker Earnings

Here is a question that cuts to the heart of Nepal's remittance economy: is it true that while total remittance inflows have risen dramatically, the remittance per worker has declined?

The answer is nuanced but the underlying trend is real. Let us look at the numbers:

Total remittance inflows to Nepal have grown from NPR 875 billion in FY 2019/20 to NPR 1,445.3 billion in FY 2023/24. In the first eleven months of FY 2024/25, inflows reached NPR 1,532.93 billion (an increase of 15.5 percent year-on-year). In US dollar terms, inflows were $11.25 billion for the same period.

Meanwhile, the number of workers leaving for foreign employment has also surged. In FY 2024/25, 452,324 workers received first-time approval for foreign employment, and 308,067 received renewal approvals. In FY 2023/24, 839,266 Nepalis left for foreign employment. The year before, the total was 741,297.

The critical insight from NRB spokesperson Guru Prasad Paudel explains the growth in remittance through three factors: rising outmigration, the appreciation of the US dollar against the Nepali rupee, and the shift of Nepalis toward higher-wage Western destinations. The third factor is key — a growing number of workers are heading to countries like South Korea, Japan, Australia, the UK, and the US, where wages are substantially higher than in Gulf countries.

But the broader economic question remains: is Nepal simply exporting more and more of its young people to achieve higher aggregate remittance numbers? If total remittance is rising primarily because more people are leaving, rather than because each worker is earning more, then the strategy is one of diminishing returns. And there are social costs that do not appear on any balance sheet.

### The Social Cost of Remittance

In some Nepali villages, up to 90 percent of young men have left. The social consequences are profound:

**Family fragmentation.** Children grow up without fathers. Spouses are separated for years. Elderly parents are cared for by remittance money rather than by their children.

**Gender role shifts.** With men gone, women take on greater household and community responsibilities. This has accelerated women's empowerment and contributed to a 30 percent decline in fertility over the last decade. But it is empowerment born of necessity, not opportunity.

**Agricultural decline.** Research demonstrates that migration negatively affects agricultural yield. Remittance-receiving households have not improved agricultural productivity despite higher incomes — the money goes to consumption and house construction, not to investing in farms.

**Brain drain.** Nepal loses trained nurses, engineers, and teachers to foreign labor markets. Health facilities lose staff. The "demographic dividend" window — where a large working-age population can drive economic growth — is being squandered as that working-age population leaves.

**HIV, divorce, and social disruption.** Men traveling in groups to new places increases sexual promiscuity. HIV rates among migrants are significantly higher than the national average. Divorces are increasing.

## Part 10: Nepal's Digital Payment Landscape

Despite the challenges, Nepal's domestic digital payment ecosystem has grown remarkably. Here is the current state:

### Mobile Wallets

**eSewa** (launched 2009) is Nepal's oldest and most widely used digital wallet, with over 8 million users. It was Nepal's first licensed Payment Service Provider (PSP). Services include mobile recharge, utility bill payments, online shopping, ticket booking, and QR code payments, with integration across 50+ banks and 150,000+ merchants.

**Khalti** (launched 2017) merged with IME Pay in July 2025 to form Khalti by IME Limited, now Nepal's largest digital wallet by combined user base and capital strength. The merger combines Khalti's modern interface and cashback appeal with IME Group's remittance muscle — the IME Group is one of Nepal's largest remittance companies.

**ConnectIPS**, developed by Nepal Clearing House Limited (NCHL), is a different animal — it is an interbank payment platform directly linked to users' bank accounts. It functions as a real-time bank-to-bank transfer system supporting P2P, B2C, C2G, and e-commerce payments, with integration across 60+ banks and financial institutions. Think of it as Nepal's closest equivalent to a national real-time payment system.

**Fonepay** plays a vital role as the interoperable network backbone for QR-based commerce, dominating person-to-merchant QR transactions and enabling payments between different wallets and banks.

### The UPI Integration

India's UPI is now accepted in Nepal for Indian tourists and visitors. Indian travelers can use UPI-linked apps at Nepali merchants who accept it. This is a significant development — it brings one of the world's most advanced payment systems to Nepal's doorstep. But the integration is one-directional: Nepali users cannot use Nepali wallets to pay in India via UPI (yet).

### Challenges Remaining

**Rural access.** While digital wallets are spreading fast in Kathmandu Valley and other urban centers, rural areas lag behind. Financial inclusion stands at about 50 percent in rural areas versus 60 percent in urban areas.

**Digital literacy.** Limited digital and financial literacy leads to distrust in financial institutions and lower retention of remittances in banks. About 10.4 percent of Nepalese adults still use informal (hundi) channels for remittances, though this figure has declined sharply as digitization progresses.

**Interoperability.** While Fonepay provides a QR-based interoperability layer, full interoperability between all wallets and banks — the kind that makes UPI work seamlessly in India — is still a work in progress.

**PayPal restrictions.** Nepal is not supported by PayPal, which creates significant friction for freelancers and small businesses trying to participate in the global digital economy. Payoneer is the most practical alternative for receiving international payments.

## Part 11: The Remittance Corridor — How Money Actually Gets from Doha to Dhading

Let us trace the actual journey of a remittance payment from a Nepali worker in Qatar to his family in Nepal.

### Traditional SWIFT/Bank Transfer

The worker goes to a local bank or exchange house in Doha and initiates a transfer. The money moves through the SWIFT network to a correspondent bank (possibly in the US or Singapore), then to the recipient's bank in Nepal. This takes 2–5 business days and costs $15–40 in fees plus an exchange rate markup.

### Money Transfer Operators (MTOs)

Western Union, MoneyGram, and IME (Nepal's own international remittance company) operate a network of sending and receiving agents. The worker visits a Western Union agent in Doha, pays cash, and the recipient collects cash at an agent in Dhading. This is faster (often same-day) but fees are typically 3–7 percent.

### Digital Remittance Apps

Services like Wise (formerly TransferWise), Remitly, WorldRemit, and Nepali-focused platforms have dramatically reduced costs. The worker opens an app, enters the amount, and the money arrives directly in the recipient's bank account or mobile wallet within minutes to hours. Fees are typically 1–3 percent, with transparent exchange rates.

### The Hundi (Informal) Channel

The hundi system is an ancient informal money transfer mechanism. A hundi operator in Qatar receives cash from the worker. A partner operator in Nepal pays the equivalent amount (often at a better exchange rate) to the family. No money actually crosses borders — the operators settle their accounts periodically through trade invoicing or other means. Hundi is illegal but has historically been popular because it is faster, cheaper, and avoids the formal banking system. In 2017, the IOM estimated that over 80 percent of Nepali workers in South Korea used hundi. Digitization and formal channel incentives have significantly reduced hundi use, but it persists.

### Nepal's Remittance Cost Progress

Nepal has made significant progress in reducing remittance costs. The cost of sending $200 to Nepal fell to 3.7 percent in 2022, approaching the SDG target of less than 3 percent. This improvement has been driven by competition among digital platforms, NRB regulations encouraging formal channels, and increased financial access.

## Part 12: What Should Nepal Do? — The Ideal and the Achievable

### The Lofty Goal: If Wishes Were Fishes

In a perfect world, here is what Nepal's payment and economic strategy would look like:

**A national real-time payment system comparable to UPI or Pix.** Nepal would build (or adapt from India) a universal, interoperable, instant payment system that works across all banks and wallets. Every merchant, from a Thamel trekking agency to a tea stall in Jumla, would accept QR payments. The system would be free for individuals and very low-cost for merchants. It would be interoperable with UPI (India), Alipay (China), and eventually Wero (Europe) for tourist payments.

**A CBDC (digital Nepali rupee).** Nepal Rastra Bank would issue a digital NPR that works offline (critical for areas without reliable internet), supports financial inclusion for the unbanked, and integrates with the real-time payment system. It would be interoperable with India's eventual CBDC (the "digital rupee" that RBI has been piloting).

**Dramatically reduced remittance costs.** The cost of sending money to Nepal would fall below 1 percent through a combination of digital channels, blockchain-based settlement, and competition. Workers would send money directly from their smartphone to their family's digital wallet in seconds, for pennies.

**Productive use of remittances.** Rather than flowing primarily into consumption and real estate, remittances would be channeled into productive investment: agriculture modernization, small and medium enterprises, education, and healthcare. Financial products would be designed specifically for remittance-receiving households — savings accounts, micro-investment products, insurance.

**Reduced dependence on remittance.** Nepal would develop its manufacturing, tourism, IT, and hydroelectric power sectors to diversify its foreign exchange earnings. The "demographic dividend" would be harnessed domestically rather than exported. Young Nepalis would have meaningful employment opportunities at home.

**Foreign exchange reform.** Nepal would gradually move toward a managed float, gaining more monetary policy independence while maintaining stability. The peg to the Indian rupee would evolve into a more flexible arrangement.

### The Achievable Reality

Wishes are not fishes. Here is what Nepal can realistically accomplish in the near term:

**Accelerate ConnectIPS and Fonepay interoperability.** Nepal already has the building blocks of a national payment system. ConnectIPS provides interbank real-time transfers; Fonepay provides merchant QR infrastructure; eSewa, Khalti, and IME Pay provide the user-facing apps. The missing piece is full, seamless interoperability — the ability for any wallet to pay any merchant on any network, as UPI enables in India. NRB can mandate this interoperability, as Brazil's central bank mandated Pix participation.

**Deepen UPI integration.** Nepal should negotiate bilateral payment linkage with India that allows both Indian tourists to pay in Nepal and Nepali users to receive remittances directly via UPI rails. This could dramatically reduce remittance costs for the Nepal-India corridor (which is massive, given the open border and the large Nepali diaspora in India, though much of this flow is informal and unmeasured).

**Improve digital infrastructure.** Internet penetration in Nepal is growing but uneven. Investing in 4G/5G coverage in rural areas is a prerequisite for digital payment adoption. The government's Digital Nepal Framework should prioritize payment infrastructure alongside connectivity.

**Financial literacy campaigns.** NRB and fintech companies should invest in digital and financial literacy, particularly for women, elderly people, and rural populations. The goal is not just adoption but understanding — knowing how to protect yourself from fraud, how to save, how to invest.

**Incentivize productive use of remittances.** Tax incentives for remittance-receiving households who invest in registered businesses. Matching savings programs. Agricultural credit products designed for families receiving remittance income.

**Prepare for CBDC thoughtfully.** Nepal should study the lessons from China's e-CNY pilot and India's digital rupee experiments, but there is no need to rush. A Nepali CBDC should be designed for Nepal's specific needs — offline capability for rural areas, interoperability with India, integration with the existing wallet ecosystem.

## Part 13: The Global Context — Where All of This Fits Together

We are living through a moment of remarkable divergence in how the world moves money.

**The United States** has been the slowest major economy to adopt real-time payments. FedNow launched in July 2023, but adoption remains limited. Americans still write paper checks at a rate that baffles the rest of the world. The card networks remain dominant, and there is no serious US CBDC initiative — in fact, the political environment is hostile to the idea.

**China** has the most advanced digital payment ecosystem on Earth, with the e-CNY, Alipay, and WeChat Pay creating a cashless society in major cities. The digital yuan is now interest-bearing and expanding internationally.

**India** has the highest volume of real-time payments, with UPI processing over 228 billion transactions in 2025. India is also exporting UPI as a technology stack to other countries.

**Brazil** has the fastest-growing real-time payment system, with Pix approaching 8 billion monthly transactions and fundamentally disrupting credit card usage.

**Europe** is building two parallel systems — Wero for immediate merchant payments and the digital euro for a sovereign CBDC — in an explicit effort to reduce dependence on American card networks.

**Africa** has pioneered mobile money through M-Pesa (Kenya) and its successors, enabling financial inclusion for hundreds of millions of unbanked people.

**The Gulf states** (UAE, Saudi Arabia, Qatar) are investing heavily in fintech infrastructure while participating in cross-border CBDC experiments like mBridge.

**Nepal** sits at the intersection of many of these trends. It is a remittance-dependent economy with a rapidly growing digital wallet ecosystem, a fixed currency peg to India, a massive diaspora in the Gulf and increasingly in the West, and a central bank that is supportive of innovation but resource-constrained. Nepal's challenge is not to pick one of these models to copy, but to learn from all of them and build something suited to its own realities.

## Part 14: For Nepali People — Whether in Nepal or Abroad

If you are Nepali, this article is not abstract. It is about your money, your family, and your country's future. Here are some practical takeaways:

### If You Work Abroad

**Use digital remittance channels.** Apps like Wise, Remitly, WorldRemit, and IME's digital services offer lower fees and better exchange rates than traditional bank transfers or money transfer agents. Compare rates before every transfer.

**Avoid hundi.** Yes, it might offer a marginally better exchange rate. But hundi money is unrecorded, unprotected, and contributes nothing to Nepal's formal financial system. It also carries legal risk for both sender and receiver.

**Consider the destination bank carefully.** If your family uses eSewa or Khalti, check whether the remittance service can deliver directly to their wallet, avoiding bank transfer fees and delays.

**Think about what the money is used for.** This is delicate — it is your family's money and they can use it as they wish. But if there is an opportunity to direct some remittance into savings, education, or a small business rather than solely consumption, the long-term benefit is enormous.

### If You Receive Remittance in Nepal

**Get a digital wallet if you do not have one.** eSewa, Khalti by IME, and ConnectIPS are all useful for different purposes. Having at least one allows you to receive money faster and transact digitally.

**Understand exchange rate fluctuations.** When the NPR weakens against the dollar, your remittance buys more in local terms. When the NPR strengthens, it buys less. This is worth tracking, especially for larger transfers.

**Financial literacy matters.** If your bank or wallet provider offers savings products, insurance, or investment options, learn about them. Remittance sitting idle in a current account is losing value to inflation every day.

### If You Are a Developer or Entrepreneur in Nepal

**Nepal's fintech space is ripe for innovation.** Payment gateway integration (eSewa, Khalti, Fonepay) is well-documented and accessible. The merger of Khalti and IME Pay signals consolidation — which means fewer, larger platforms with bigger user bases and more opportunity for third-party developers.

**Cross-border payment is the biggest unsolved problem.** Building tools that make it easier, cheaper, and faster to send money to Nepal — especially from corridors like South Korea, Japan, Australia, the UK, and the US — is a meaningful opportunity.

**QR payments are the growth frontier.** QR-based Fonepay transactions are roughly doubling every couple of years. Building merchant tools, analytics, and loyalty programs on top of QR payments is a near-term opportunity.

## Part 15: Looking Forward — The Next Decade

The payment landscape is being reshaped by several converging forces:

**Real-time becomes the default.** By 2030, instant settlement will be the baseline expectation, not a premium feature. SWIFT is adapting (Swift Go, gpi, blockchain integration). Domestic systems like UPI and Pix are expanding internationally.

**CBDCs mature.** China's e-CNY will continue expanding. The digital euro will launch. India's digital rupee pilot will scale. Smaller countries will launch their own CBDCs or adopt shared infrastructure. Nepal will eventually have a digital NPR — the question is when, not if.

**Sovereignty drives fragmentation.** The era of Visa and Mastercard's uncontested dominance is ending. Not because their technology is inferior, but because governments do not want American companies controlling their payment infrastructure. This will lead to a more fragmented but more resilient global system.

**AI transforms fraud detection, compliance, and personalization.** Machine learning models are already flagging fraudulent transactions in real time. AI will also make it easier for small businesses to manage payments, reconcile accounts, and access credit.

**Stablecoins find a niche.** Dollar-pegged stablecoins will continue to serve as the "lingua franca" of crypto-native cross-border payments, particularly in corridors underserved by traditional banking. But sovereign CBDCs will eventually absorb much of this use case.

For Nepal, the most important question is not which technology to adopt. It is whether the country can use the current moment — when digital payment infrastructure is cheap, adaptable, and proven — to build a financial system that serves its people: the worker in Doha, the student in Kathmandu, the farmer in Dhading, the shopkeeper in Pokhara, the nurse in Sydney, and the software developer in Virginia who sends money home and wonders, every single time, why it still takes three days and costs twenty-five dollars.

The technology to fix this exists. Brazil built it in two years. India built it in five. Nepal has the building blocks. What it needs now is the will.

## Resources

- **SWIFT**: [swift.com](https://www.swift.com) — Official SWIFT network and documentation.
- **Nepal Rastra Bank**: [nrb.org.np](https://www.nrb.org.np) — Current macroeconomic and financial situation reports.
- **World Bank Remittance Data**: [data.worldbank.org](https://data.worldbank.org/indicator/BX.TRF.PWKR.CD.DT?locations=NP) — Nepal remittance inflows.
- **NPCI / UPI**: [npci.org.in](https://www.npci.org.in) — Unified Payments Interface documentation.
- **Central Bank of Brazil / Pix**: [bcb.gov.br/en](https://www.bcb.gov.br/en) — Pix statistics and documentation.
- **ECB Digital Euro**: [ecb.europa.eu/euro/digital_euro](https://www.ecb.europa.eu/euro/digital_euro/progress/html/index.en.html) — Digital euro project progress.
- **European Payments Initiative (Wero)**: [wero.eu](https://www.wero.eu) — Wero information and participating banks.
- **Atlantic Council CBDC Tracker**: [atlanticcouncil.org/cbdctracker](https://www.atlanticcouncil.org/cbdctracker/) — Global CBDC development status across 134 countries.
- **Worldpay Global Payments Report 2026**: Published March 31, 2026 — comprehensive data on global payment method shares across 42 markets.
- **EBANX Pix Research**: [ebanx.com](https://www.ebanx.com) — Detailed Pix statistics and projections.
- **IOM Nepal Remittance Report**: [iom.int](https://roasiapacific.iom.int) — Financial inclusion and remittance cost data for Nepal.
- **Kathmandu Post**: [kathmandupost.com](https://kathmandupost.com) — Nepal economic reporting, including remittance and labor migration coverage.
