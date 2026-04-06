---
title: "Relational Databases and Normalization: A Complete Guide from Messy Spreadsheets to Sixth Normal Form"
date: 2026-04-10
author: myblazor-team
summary: A comprehensive walkthrough of relational database normalization from UNF through 6NF, using a real Blazor Server contact management application as the running example. Includes C# code with Dapper and EF Core at every level, a critique of the starting schema, practical cost-benefit analysis for each normal form, and a deep dive into Entity-Attribute-Value.
tags:
  - databases
  - normalization
  - sql
  - dapper
  - entity-framework
  - deep-dive
  - csharp
  - best-practices
featured: true
---

You have a database. It works. Users can create contacts, add email addresses and phone numbers, filter and paginate, upload profile pictures, and everything saves correctly. The tests pass. The CI pipeline is green. You deploy on a Thursday afternoon and nothing catches fire.

But then someone asks, "Why is the country stored on every address row? Could we have a lookup table?" Or: "The `Label` column on emails says 'Work' fifty thousand times — is that not wasteful?" Or, more provocatively: "What normal form is this schema in, and what would we gain by going one level higher?"

These are normalization questions. They have been asked since 1970 when Edgar F. Codd published "A Relational Model of Data for Large Shared Data Banks" and introduced the first normal form. Codd extended the theory to second and third normal forms in 1971. He and Raymond F. Boyce defined Boyce-Codd Normal Form (BCNF) in 1974. Ronald Fagin introduced the fourth normal form in 1977 and the fifth in 1979. Christopher J. Date proposed the sixth normal form in 2003. The theory has been stable for decades. What changes is how we apply it in the context of modern application development — with ORMs like Entity Framework Core, micro-ORMs like Dapper (currently at version 2.1.72, last updated March 6, 2026), and application frameworks like .NET 10 and C# 14.

This article uses a real application as its running example: a Blazor Server contact management application called Virginia, built with .NET 10, Entity Framework Core with SQLite, ASP.NET Core Identity, and OpenTelemetry. We will present the full schema, critique it, then walk through every normal form — showing what changes at each level, what we gain, what we lose, and the C# code (both EF Core and raw SQL with Dapper) that implements each version. We will go as high as sixth normal form. We will also explore the Entity-Attribute-Value (EAV) pattern — what it is, where it shines, and where it becomes a maintenance nightmare.

You do not need to have seen the Virginia codebase before. Every entity, every table, and every line of SQL will be presented right here.

## Part 1: The Starting Schema — Virginia As It Stands

### The Domain

Virginia is an address book. It manages contacts — people with names, email addresses, phone numbers, mailing addresses, notes, and profile pictures. A contact can have multiple emails, multiple phones, multiple addresses, and multiple notes. Each child entity has a free-text `Label` field (like "Work," "Home," "Mobile") so the user can categorize them.

Here are the entity classes as they exist today:

```csharp
// Contact — the aggregate root
public sealed class Contact
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    public byte[]? ProfilePicture { get; set; }

    [MaxLength(50)]
    public string? ProfilePictureContentType { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ContactEmail> Emails { get; set; } = [];
    public List<ContactPhone> Phones { get; set; } = [];
    public List<ContactAddress> Addresses { get; set; } = [];
    public List<ContactNote> Notes { get; set; } = [];
}

// ContactEmail
public sealed class ContactEmail
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }  // "Work", "Home", "Personal"

    [MaxLength(254)]
    public required string Address { get; set; }

    public Contact Contact { get; set; } = null!;
}

// ContactPhone
public sealed class ContactPhone
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }  // "Mobile", "Home", "Office"

    [MaxLength(30)]
    public required string Number { get; set; }

    public Contact Contact { get; set; } = null!;
}

// ContactAddress
public sealed class ContactAddress
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }  // "Home", "Office", "Billing"

    [MaxLength(200)]
    public required string Street { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(100)]
    public string State { get; set; } = "";

    [MaxLength(20)]
    public required string PostalCode { get; set; }

    [MaxLength(100)]
    public required string Country { get; set; }

    public Contact Contact { get; set; } = null!;
}

// ContactNote
public sealed class ContactNote
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(4000)]
    public required string Content { get; set; }

    [MaxLength(450)]
    public required string CreatedByUserId { get; set; }

    [MaxLength(256)]
    public required string CreatedByUserName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Contact Contact { get; set; } = null!;
}
```

And the EF Core configuration in the `DbContext`:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactEmail> ContactEmails => Set<ContactEmail>();
    public DbSet<ContactPhone> ContactPhones => Set<ContactPhone>();
    public DbSet<ContactAddress> ContactAddresses => Set<ContactAddress>();
    public DbSet<ContactNote> ContactNotes => Set<ContactNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contact>(entity =>
        {
            entity.HasIndex(c => new { c.LastName, c.FirstName });

            entity.HasMany(c => c.Emails)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Phones)
                .WithOne(p => p.Contact)
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Contact)
                .HasForeignKey(a => a.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Notes)
                .WithOne(n => n.Contact)
                .HasForeignKey(n => n.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContactEmail>(e => e.HasIndex(x => x.Address));
        builder.Entity<ContactPhone>(e => e.HasIndex(x => x.Number));
        builder.Entity<ContactAddress>(e => e.HasIndex(x => new { x.City, x.State }));
        builder.Entity<ContactNote>(e => e.HasIndex(x => x.ContactId));
    }
}
```

The equivalent SQL DDL for these tables (SQLite syntax, as generated by EF Core):

```sql
CREATE TABLE Contacts (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName   TEXT    NOT NULL,
    LastName    TEXT    NOT NULL,
    ProfilePicture          BLOB,
    ProfilePictureContentType TEXT,
    CreatedAtUtc TEXT   NOT NULL,
    UpdatedAtUtc TEXT   NOT NULL
);
CREATE INDEX IX_Contacts_LastName_FirstName ON Contacts (LastName, FirstName);

CREATE TABLE ContactEmails (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Label     TEXT    NOT NULL,
    Address   TEXT    NOT NULL
);
CREATE INDEX IX_ContactEmails_Address ON ContactEmails (Address);

CREATE TABLE ContactPhones (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Label     TEXT    NOT NULL,
    Number    TEXT    NOT NULL
);
CREATE INDEX IX_ContactPhones_Number ON ContactPhones (Number);

CREATE TABLE ContactAddresses (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId  INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Label      TEXT    NOT NULL,
    Street     TEXT    NOT NULL,
    City       TEXT    NOT NULL,
    State      TEXT    NOT NULL DEFAULT '',
    PostalCode TEXT    NOT NULL,
    Country    TEXT    NOT NULL
);
CREATE INDEX IX_ContactAddresses_City_State ON ContactAddresses (City, State);

CREATE TABLE ContactNotes (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId        INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Content          TEXT    NOT NULL,
    CreatedByUserId  TEXT    NOT NULL,
    CreatedByUserName TEXT   NOT NULL,
    CreatedAtUtc     TEXT    NOT NULL
);
CREATE INDEX IX_ContactNotes_ContactId ON ContactNotes (ContactId);
```

### A Candid Critique

This schema is well-designed for its purpose. It is already far beyond what most tutorials produce. The one-to-many relationships are correctly modeled with foreign keys and cascade deletes. There are indexes on the columns used for filtering. The data types have sensible max lengths. Timestamps are stored in UTC. The aggregate root pattern is clear — `Contact` owns everything, and deleting a contact cascades to all children.

But it is not perfect. Let us enumerate the normalization issues:

1. **The `Label` columns are free-text strings.** Every email has a `Label` like "Work" or "Home." Every phone has a `Label` like "Mobile" or "Office." Every address has a `Label` like "Billing" or "Shipping." These are stored as raw strings. If one user types "work" and another types "Work" and a third types "WORK," you have three distinct values in the database that mean the same thing. There is no referential integrity on label values.

2. **The `Country` column is a free-text string.** Addresses store `Country` as a `TEXT` field up to 100 characters. Some users might type "US," others "USA," others "United States," others "United States of America." There is no `Countries` lookup table enforcing consistent country codes (like ISO 3166-1 alpha-2).

3. **The `State` column has the same problem.** "VA" versus "Virginia" versus "virginia."

4. **`ProfilePicture` is a BLOB stored directly in the `Contacts` table.** This means every query that touches the `Contacts` table potentially involves loading megabytes of binary data into memory, even if you only want the contact's name. The `SELECT *` problem. EF Core's `AsNoTracking()` and explicit `Select()` projections mitigate this in practice (and Virginia does use projections), but the schema itself conflates metadata (name, timestamps) with large binary content.

5. **`ContactNote` stores `CreatedByUserName` alongside `CreatedByUserId`.** This is a denormalization — the user's name is stored redundantly. If the user changes their display name, all existing notes still show the old name. This might be intentional (capturing the name at the time of writing), but it is a design decision that should be explicit.

6. **Auto-incrementing integer primary keys.** The `Id` columns use `INTEGER PRIMARY KEY AUTOINCREMENT`. This works for a single-server SQLite database, but does not scale to distributed systems (where two servers might generate the same integer). It also leaks information — an attacker can infer how many contacts exist by observing IDs. For a contact management application, this is unlikely to matter. But for other domains (order IDs, invoice numbers), it can be a security concern. UUIDv7 (available via `Guid.CreateVersion7()` in .NET 9+) solves both problems: it is globally unique, time-sortable (so B-tree indexes still perform well), and does not leak sequence information.

Now, let us formalize these observations using normalization theory.

## Part 2: Unnormalized Form (UNF) — What Not to Do

Before we analyze where Virginia falls on the normal form spectrum, let us start from the very beginning. What would this data look like if we had no normalization at all — if we stored everything in a single spreadsheet?

```
| ContactId | FirstName | LastName | Email1Label | Email1Address     | Email2Label | Email2Address     | Phone1Label | Phone1Number | Address1Label | Address1Street | Address1City | Address1State | Address1Zip | Address1Country |
|-----------|-----------|----------|-------------|-------------------|-------------|-------------------|-------------|--------------|---------------|----------------|--------------|---------------|-------------|-----------------|
| 1         | Alice     | Johnson  | Work        | alice@acme.com    | Home        | alice@gmail.com   | Mobile      | 555-0100     | Home          | 123 Main St    | Richmond     | VA            | 23220       | US              |
| 2         | Bob       | Smith    | Work        | bob@company.com   | NULL        | NULL              | Office      | 555-0200     | NULL          | NULL           | NULL         | NULL          | NULL        | NULL            |
```

This is unnormalized form (UNF). The problems are immediate:

- **Repeating groups.** `Email1Label`, `Email1Address`, `Email2Label`, `Email2Address` — what if someone has three emails? Four? You would need to add more columns, and every existing row would have NULLs in the new columns.
- **Atomic violation.** Some designers try to solve the repeating group problem by stuffing multiple values into a single cell: `"alice@acme.com, alice@gmail.com"`. This makes querying, updating, and validating individual values extremely difficult.
- **Fixed limits.** The column-per-instance approach (Email1, Email2, Email3) imposes an arbitrary maximum on how many child items a contact can have.

In Dapper, querying this monstrosity would look like:

```csharp
// DON'T DO THIS — this is the UNF approach
using var connection = new SqliteConnection(connectionString);
var contacts = await connection.QueryAsync<UnnormalizedContact>(
    "SELECT * FROM ContactsFlat");

public class UnnormalizedContact
{
    public int ContactId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email1Label { get; set; }
    public string? Email1Address { get; set; }
    public string? Email2Label { get; set; }
    public string? Email2Address { get; set; }
    // ... and so on for every possible email, phone, address
}
```

The C# class mirrors the table's ugliness. Adding a third email slot requires changing the table, the class, every query, and every form. This is the problem that normalization solves.

## Part 3: First Normal Form (1NF) — Atomic Values and No Repeating Groups

### The Rule

A table is in 1NF if:

1. Every column contains only atomic (indivisible) values — no lists, no comma-separated strings, no JSON arrays stuffed into a text column.
2. There are no repeating groups of columns (no `Email1`, `Email2`, `Email3`).
3. Each row is uniquely identifiable (there is a primary key).

### Applying 1NF to Our Data

The unnormalized flat table becomes multiple tables. Each repeating group (emails, phones, addresses) gets its own table with a foreign key back to the parent:

```sql
CREATE TABLE Contacts (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName  TEXT NOT NULL
);

CREATE TABLE ContactEmails (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id),
    Label     TEXT NOT NULL,
    Address   TEXT NOT NULL
);

CREATE TABLE ContactPhones (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id),
    Label     TEXT NOT NULL,
    Number    TEXT NOT NULL
);
```

This is exactly what Virginia already does. The child entities are in separate tables. Each column contains a single atomic value. Each row has a primary key. No repeating groups.

**Virginia's schema is already in 1NF.**

### What 1NF Gives Us

The move from UNF to 1NF eliminates the fixed-limit problem. A contact can now have zero, one, fifty, or a thousand email addresses — the `ContactEmails` table simply has more rows. Adding a new category of child data (like adding `ContactNotes`) requires creating a new table, not modifying existing ones.

With Dapper, querying contacts and their emails in 1NF looks like:

```csharp
using var connection = new SqliteConnection(connectionString);

const string sql = """
    SELECT c.Id, c.FirstName, c.LastName,
           e.Id, e.ContactId, e.Label, e.Address
    FROM Contacts c
    LEFT JOIN ContactEmails e ON e.ContactId = c.Id
    ORDER BY c.LastName, c.FirstName
    """;

var contactDictionary = new Dictionary<int, Contact>();

var contacts = await connection.QueryAsync<Contact, ContactEmail, Contact>(
    sql,
    (contact, email) =>
    {
        if (!contactDictionary.TryGetValue(contact.Id, out var existing))
        {
            existing = contact;
            existing.Emails = [];
            contactDictionary[contact.Id] = existing;
        }
        if (email is not null)
            existing.Emails.Add(email);
        return existing;
    },
    splitOn: "Id");

var result = contactDictionary.Values.ToList();
```

Dapper's multi-mapping (`QueryAsync<Contact, ContactEmail, Contact>`) handles the one-to-many JOIN by letting us accumulate child objects into the parent's collection. The `splitOn: "Id"` parameter tells Dapper where the `Contact` columns end and the `ContactEmail` columns begin in the result set.

## Part 4: Second Normal Form (2NF) — Eliminating Partial Dependencies

### The Rule

A table is in 2NF if:

1. It is already in 1NF.
2. Every non-key column depends on the **entire** primary key, not just part of it.

Partial dependencies only occur when a table has a composite primary key (a primary key made of two or more columns). If a table has a single-column primary key, it is automatically in 2NF once it satisfies 1NF.

### Does Virginia Have Partial Dependencies?

Look at Virginia's tables. Every table has a single-column surrogate primary key (`Id`). There are no composite primary keys. Therefore, **partial dependencies cannot exist,** and every table in Virginia's schema is automatically in 2NF.

But let us construct a scenario to understand 2NF. Imagine we had designed `ContactEmails` without a surrogate key, using a composite primary key instead:

```sql
-- Hypothetical design with composite PK (ContactId, Address)
CREATE TABLE ContactEmails (
    ContactId    INTEGER NOT NULL REFERENCES Contacts(Id),
    Address      TEXT NOT NULL,
    Label        TEXT NOT NULL,
    ContactName  TEXT NOT NULL,  -- PROBLEM: depends only on ContactId
    PRIMARY KEY (ContactId, Address)
);
```

Here, `ContactName` depends only on `ContactId`, not on the full composite key `(ContactId, Address)`. That is a partial dependency — a 2NF violation. The fix is to remove `ContactName` from this table (it belongs in the `Contacts` table) or to use a surrogate key.

**Virginia already avoids this by using surrogate integer keys everywhere. All tables are in 2NF.**

### The Cost-Benefit of 2NF

The cost of reaching 2NF from 1NF is usually zero — it is a matter of not making a design mistake in the first place. The benefit is that you cannot have update anomalies where changing a contact's name requires updating every email row.

## Part 5: Third Normal Form (3NF) — Eliminating Transitive Dependencies

### The Rule

A table is in 3NF if:

1. It is already in 2NF.
2. Every non-key column depends directly on the primary key — not on another non-key column.

A transitive dependency occurs when column A determines column B, and column B determines column C. In that case, C transitively depends on A through B. The fix is to extract B and C into their own table.

### Where Virginia Falls Short of 3NF

Look at the `ContactNotes` table:

```sql
CREATE TABLE ContactNotes (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId         INTEGER NOT NULL REFERENCES Contacts(Id),
    Content           TEXT NOT NULL,
    CreatedByUserId   TEXT NOT NULL,
    CreatedByUserName TEXT NOT NULL,
    CreatedAtUtc      TEXT NOT NULL
);
```

`CreatedByUserName` depends on `CreatedByUserId`, not on the note's `Id`. If we know the `CreatedByUserId`, we can look up the user's name in the `AspNetUsers` table (which ASP.NET Core Identity already maintains). Storing `CreatedByUserName` alongside `CreatedByUserId` is a transitive dependency:

```
NoteId → CreatedByUserId → CreatedByUserName
```

This is a 3NF violation.

Now, there is a legitimate counterargument: you might *want* to capture the user's name at the time the note was created, as a historical snapshot. If the user later changes their display name, the note should still show who wrote it under the name they were using at the time. This is an intentional denormalization for historical accuracy, and it is a valid design choice. But it should be documented as such.

Similarly, `ContactAddress` stores `Country` as a free-text field. In a strictly normalized schema, countries would be a lookup table:

```sql
CREATE TABLE Countries (
    Code TEXT PRIMARY KEY,  -- 'US', 'CA', 'GB'
    Name TEXT NOT NULL       -- 'United States', 'Canada', 'United Kingdom'
);
```

And `ContactAddresses.Country` would become `ContactAddresses.CountryCode` with a foreign key reference. The same applies to `State`, which could reference a `StateProvinces` table.

### Normalizing to 3NF

Here is the ContactNotes table normalized to 3NF:

```sql
-- Remove CreatedByUserName; JOIN to AspNetUsers instead
CREATE TABLE ContactNotes (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId       INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Content         TEXT    NOT NULL,
    CreatedByUserId TEXT    NOT NULL REFERENCES AspNetUsers(Id),
    CreatedAtUtc    TEXT    NOT NULL
);
```

And here is the address table with lookup tables for Country and State:

```sql
CREATE TABLE Countries (
    Code TEXT PRIMARY KEY,
    Name TEXT NOT NULL
);

CREATE TABLE StateProvinces (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    CountryCode TEXT    NOT NULL REFERENCES Countries(Code),
    Code        TEXT    NOT NULL,
    Name        TEXT    NOT NULL,
    UNIQUE (CountryCode, Code)
);

CREATE TABLE ContactAddresses (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId       INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    Label           TEXT    NOT NULL,
    Street          TEXT    NOT NULL,
    City            TEXT    NOT NULL,
    StateProvinceId INTEGER REFERENCES StateProvinces(Id),
    PostalCode      TEXT    NOT NULL,
    CountryCode     TEXT    NOT NULL REFERENCES Countries(Code)
);
```

The C# entities after normalization to 3NF:

```csharp
public sealed class Country
{
    [MaxLength(2)]
    public required string Code { get; set; }  // PK: "US", "CA", "GB"

    [MaxLength(100)]
    public required string Name { get; set; }
}

public sealed class StateProvince
{
    public int Id { get; set; }

    [MaxLength(2)]
    public required string CountryCode { get; set; }

    [MaxLength(10)]
    public required string Code { get; set; }  // "VA", "CA", "ON"

    [MaxLength(100)]
    public required string Name { get; set; }  // "Virginia", "California", "Ontario"

    public Country Country { get; set; } = null!;
}

public sealed class ContactAddress
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }

    [MaxLength(200)]
    public required string Street { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    public int? StateProvinceId { get; set; }

    [MaxLength(20)]
    public required string PostalCode { get; set; }

    [MaxLength(2)]
    public required string CountryCode { get; set; }

    public Contact Contact { get; set; } = null!;
    public StateProvince? StateProvince { get; set; }
    public Country Country { get; set; } = null!;
}
```

Querying this with Dapper:

```csharp
const string sql = """
    SELECT a.Id, a.ContactId, a.Label, a.Street, a.City,
           a.PostalCode, a.CountryCode,
           sp.Id, sp.Code, sp.Name,
           co.Code, co.Name
    FROM ContactAddresses a
    LEFT JOIN StateProvinces sp ON sp.Id = a.StateProvinceId
    INNER JOIN Countries co ON co.Code = a.CountryCode
    WHERE a.ContactId = @ContactId
    """;

var addresses = await connection.QueryAsync<ContactAddress, StateProvince, Country, ContactAddress>(
    sql,
    (address, state, country) =>
    {
        address.StateProvince = state;
        address.Country = country;
        return address;
    },
    new { ContactId = contactId },
    splitOn: "Id,Code");
```

### The Cost-Benefit of 3NF

**What we gain:**
- **Data consistency.** "US" is always "US." No more "United States" vs. "USA" vs. "U.S." A dropdown in the UI pulls from the `Countries` table, and the user cannot invent new country names.
- **Storage efficiency.** A 2-character country code is stored instead of a 100-character string. With 50,000 addresses, that is a measurable space saving.
- **Easier querying.** "Show me all contacts in Canada" becomes `WHERE a.CountryCode = 'CA'` instead of `WHERE a.Country IN ('Canada', 'CA', 'CAN', 'canada')`.

**What we lose:**
- **Query complexity.** Every address query now requires JOINs to `Countries` and `StateProvinces`. The SQL is longer, and the Dapper multi-mapping is more complex.
- **Seeding and maintenance.** You need to populate the `Countries` and `StateProvinces` lookup tables. That is 249 countries and thousands of state/province subdivisions. You need to keep them up to date (countries change names, new subdivisions are created).
- **Development velocity.** A simple "save an address" operation now involves validating foreign keys against lookup tables instead of just writing a string.

### When to Normalize to 3NF

Normalize to 3NF when data consistency matters more than development convenience. For a personal address book with 200 contacts, the free-text country field is probably fine. For a shipping system processing 10,000 orders per day across 40 countries, lookup tables for countries and states are essential.

Similarly, normalize the `Label` fields if you want consistent categorization:

```sql
CREATE TABLE LabelTypes (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE  -- 'Email', 'Phone', 'Address'
);

CREATE TABLE Labels (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    LabelTypeId INTEGER NOT NULL REFERENCES LabelTypes(Id),
    Name        TEXT NOT NULL,
    UNIQUE (LabelTypeId, Name)
);
-- Seed: (1, 'Email'), (2, 'Phone'), (3, 'Address')
-- Labels: (1, 1, 'Work'), (2, 1, 'Home'), (3, 2, 'Mobile'), (4, 2, 'Office'), ...
```

Then `ContactEmails.Label` becomes `ContactEmails.LabelId REFERENCES Labels(Id)`. This guarantees label consistency but adds a JOIN to every email query. Again, the trade-off is consistency versus simplicity.

## Part 6: Boyce-Codd Normal Form (BCNF) — A Stricter 3NF

### The Rule

A table is in BCNF if, for every non-trivial functional dependency `X → Y`, X is a superkey. This is stricter than 3NF, which allows certain exceptions when the dependency involves part of a candidate key.

In practice, 3NF and BCNF differ only when a table has multiple overlapping candidate keys. For most application tables with surrogate primary keys and no composite candidate keys, 3NF and BCNF are equivalent.

### Does Virginia Have BCNF Violations?

After normalizing to 3NF (with lookup tables for countries and states), we need to check for overlapping candidate keys. Consider the `StateProvinces` table:

```sql
CREATE TABLE StateProvinces (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    CountryCode TEXT NOT NULL REFERENCES Countries(Code),
    Code        TEXT NOT NULL,
    Name        TEXT NOT NULL,
    UNIQUE (CountryCode, Code)
);
```

This table has two candidate keys: `{Id}` and `{CountryCode, Code}`. The functional dependency `Code → Name` would be a BCNF violation if `Code` alone were not a superkey — and it is not, because the same state code can appear in different countries ("CA" is both California in the US and a province designation in other countries).

However, the actual functional dependency is `{CountryCode, Code} → Name`, and `{CountryCode, Code}` *is* a candidate key (it is declared `UNIQUE`). So this table is in BCNF.

In Virginia's domain, BCNF violations are unlikely because the schema uses surrogate keys throughout. The gap between 3NF and BCNF is narrow in practice, and **Virginia's 3NF schema is already in BCNF.**

### The Cost of BCNF

The cost of reaching BCNF from 3NF is typically zero for schemas with surrogate keys. In rare cases where you have overlapping composite candidate keys, BCNF may require decomposing a table into two. The classic example is a course-scheduling scenario where `{Student, Subject} → Teacher` and `Teacher → Subject`. Decomposing into `{Student, Teacher}` and `{Teacher, Subject}` resolves the BCNF violation but may make some queries less intuitive.

## Part 7: Fourth Normal Form (4NF) — Multi-Valued Dependencies

### The Rule

A table is in 4NF if it is in BCNF and has no multi-valued dependencies. A multi-valued dependency occurs when one attribute independently determines two or more sets of values.

### Example in the Contact Domain

Imagine we add two new features to Virginia: contacts can have multiple **languages** they speak, and multiple **hobbies**. If we naively store these in a single table:

```sql
CREATE TABLE ContactAttributes (
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id),
    Language  TEXT,
    Hobby     TEXT,
    PRIMARY KEY (ContactId, Language, Hobby)
);

-- Alice speaks English and Spanish, and enjoys hiking and painting
-- We end up with a Cartesian product:
INSERT INTO ContactAttributes VALUES (1, 'English', 'Hiking');
INSERT INTO ContactAttributes VALUES (1, 'English', 'Painting');
INSERT INTO ContactAttributes VALUES (1, 'Spanish', 'Hiking');
INSERT INTO ContactAttributes VALUES (1, 'Spanish', 'Painting');
```

This is a 4NF violation. Languages and hobbies are independent of each other, but the table forces us to store every combination. Adding a third language requires adding two more rows (one per hobby). This is redundant and error-prone.

The 4NF fix decomposes the table:

```sql
CREATE TABLE ContactLanguages (
    ContactId  INTEGER NOT NULL REFERENCES Contacts(Id),
    Language   TEXT NOT NULL,
    PRIMARY KEY (ContactId, Language)
);

CREATE TABLE ContactHobbies (
    ContactId INTEGER NOT NULL REFERENCES Contacts(Id),
    Hobby     TEXT NOT NULL,
    PRIMARY KEY (ContactId, Hobby)
);
```

Now Alice's languages and hobbies are stored independently. Adding a third language does not affect hobbies.

### Does Virginia Have 4NF Violations?

No. Virginia's child tables (emails, phones, addresses, notes) each represent a single multi-valued fact about a contact. Emails are independent of phones. Addresses are independent of notes. There are no tables that combine two independent multi-valued facts about the same entity.

**Virginia's schema, after reaching BCNF, is already in 4NF.**

The cost of 4NF is additional tables. The benefit is elimination of the Cartesian product problem. In practice, 4NF violations are rare if you follow the basic principle of "one table per fact type."

## Part 8: Fifth Normal Form (5NF) — Join Dependencies

### The Rule

A table is in 5NF if it is in 4NF and every join dependency is implied by the candidate keys. In simpler terms: the table cannot be decomposed into smaller tables and then reconstructed via JOINs without losing or gaining information.

5NF matters when three or more entities are related and the relationship cannot be expressed as a combination of binary relationships.

### Example

Consider a table tracking which suppliers can provide which products to which franchisee locations:

```sql
CREATE TABLE SupplierProductLocation (
    SupplierId  INTEGER NOT NULL,
    ProductId   INTEGER NOT NULL,
    LocationId  INTEGER NOT NULL,
    PRIMARY KEY (SupplierId, ProductId, LocationId)
);
```

If the business rule is "a supplier supplies a product to a location only if the supplier supplies that product AND the supplier supplies to that location AND the product is available at that location," then this three-way relationship can be decomposed into three binary relationships. That decomposition is 5NF.

If the business rule is "a supplier supplies a product to a location" as an atomic, three-way fact, then the table is already in 5NF and should not be decomposed.

### Does Virginia Need 5NF?

No. Virginia's data model consists of one-to-many relationships (contact → emails, contact → phones). There are no three-way relationships between independent entities. **Virginia is in 5NF by default.**

The cost of pursuing 5NF in schemas that do not have three-way relationships is zero — you are already there. In schemas with complex many-to-many-to-many relationships, 5NF requires careful decomposition and testing to ensure no spurious tuples appear when joining the decomposed tables back together.

## Part 9: Sixth Normal Form (6NF) — One Column Per Table

### The Rule

A table is in 6NF if it is in 5NF and every non-trivial join dependency is trivial — which in practice means each table has at most one non-key column (plus the primary key).

6NF was proposed by Christopher J. Date in 2003, primarily for handling temporal data (tracking changes to attributes over time). It is the most extreme form of normalization.

### What 6NF Would Look Like for Virginia

Taking the `Contacts` table:

```sql
-- 6NF decomposition of Contacts
CREATE TABLE ContactFirstNames (
    ContactId INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    FirstName TEXT NOT NULL
);

CREATE TABLE ContactLastNames (
    ContactId INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    LastName  TEXT NOT NULL
);

CREATE TABLE ContactProfilePictures (
    ContactId   INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    Picture     BLOB NOT NULL,
    ContentType TEXT NOT NULL
);

CREATE TABLE ContactTimestamps (
    ContactId    INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);
```

Wait — `ContactTimestamps` has two non-key columns. In strict 6NF:

```sql
CREATE TABLE ContactCreatedTimestamps (
    ContactId    INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    CreatedAtUtc TEXT NOT NULL
);

CREATE TABLE ContactUpdatedTimestamps (
    ContactId    INTEGER PRIMARY KEY REFERENCES Contacts(Id),
    UpdatedAtUtc TEXT NOT NULL
);
```

Now every table has exactly one non-key column.

### The 6NF C# Code

Querying a contact in 6NF with Dapper would look like:

```csharp
const string sql = """
    SELECT c.Id,
           fn.FirstName,
           ln.LastName,
           pp.Picture IS NOT NULL AS HasPhoto,
           ct.CreatedAtUtc,
           ut.UpdatedAtUtc
    FROM Contacts c
    LEFT JOIN ContactFirstNames fn ON fn.ContactId = c.Id
    LEFT JOIN ContactLastNames ln ON ln.ContactId = c.Id
    LEFT JOIN ContactProfilePictures pp ON pp.ContactId = c.Id
    LEFT JOIN ContactCreatedTimestamps ct ON ct.ContactId = c.Id
    LEFT JOIN ContactUpdatedTimestamps ut ON ut.ContactId = c.Id
    WHERE c.Id = @Id
    """;

var contact = await connection.QuerySingleOrDefaultAsync<ContactDetailDto>(sql, new { Id = id });
```

Six JOINs just to reconstruct a single contact's basic information. Every query pays this cost.

### When 6NF Actually Makes Sense

6NF makes sense in exactly two scenarios:

**Temporal databases.** When you need to track the history of every individual attribute change independently. If a contact changes their last name on January 15 and their city on March 3, 6NF lets you store each change independently with its own effective date range. In a columnar/temporal data warehouse, this is powerful.

**Columnar data stores.** Data warehouses that store data column-by-column (like ClickHouse, Vertica, or BigQuery) effectively use 6NF internally. Each column is stored as a separate physical structure, enabling extreme compression and fast aggregation queries.

**For OLTP (transactional) applications like Virginia, 6NF is impractical.** The proliferation of tables (a single entity with N attributes becomes N tables), the cost of JOINs on every query, and the complexity of INSERT/UPDATE operations (which must touch N tables) make 6NF unsuitable for applications that serve interactive users.

### The Cost-Benefit Summary of 6NF

**Cost:** N tables per entity, N JOINs per read, N writes per insert/update, dramatically increased query complexity, no ORM support out of the box.

**Benefit:** Perfect temporal tracking of individual attribute changes, optimal compression in columnar stores, zero redundancy.

**Recommendation:** Do not use 6NF for OLTP applications. If you need temporal data, use a temporal table feature (SQL Server temporal tables, PostgreSQL temporal extensions) or an audit/history table pattern rather than decomposing your schema to 6NF.

## Part 10: Entity-Attribute-Value (EAV) — The Anti-Pattern That Sometimes Works

### What Is EAV?

The Entity-Attribute-Value pattern stores data in three columns: **Entity** (the thing being described), **Attribute** (the property name), and **Value** (the property value).

```sql
CREATE TABLE ContactProperties (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ContactId   INTEGER NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    AttributeName  TEXT NOT NULL,
    AttributeValue TEXT NOT NULL
);

-- Alice's properties
INSERT INTO ContactProperties (ContactId, AttributeName, AttributeValue)
VALUES (1, 'NickName', 'Ali'),
       (1, 'Birthday', '1990-05-15'),
       (1, 'PreferredLanguage', 'English'),
       (1, 'TwitterHandle', '@alice_j');
```

Instead of a fixed schema with columns for `NickName`, `Birthday`, `PreferredLanguage`, and `TwitterHandle`, all properties are stored as rows. The schema is infinitely flexible — you can add new attributes without changing the database schema.

### The C# Code for EAV

Reading EAV data with Dapper:

```csharp
const string sql = """
    SELECT AttributeName, AttributeValue
    FROM ContactProperties
    WHERE ContactId = @ContactId
    """;

var properties = (await connection.QueryAsync<(string Name, string Value)>(sql, new { ContactId = id }))
    .ToDictionary(p => p.Name, p => p.Value);

// Access properties dynamically
var nickName = properties.GetValueOrDefault("NickName");
var birthday = properties.TryGetValue("Birthday", out var b)
    ? DateOnly.Parse(b)
    : (DateOnly?)null;
```

Writing EAV data:

```csharp
const string upsertSql = """
    INSERT INTO ContactProperties (ContactId, AttributeName, AttributeValue)
    VALUES (@ContactId, @AttributeName, @AttributeValue)
    ON CONFLICT (ContactId, AttributeName)
    DO UPDATE SET AttributeValue = @AttributeValue
    """;

await connection.ExecuteAsync(upsertSql, new
{
    ContactId = contactId,
    AttributeName = "NickName",
    AttributeValue = "Ali"
});
```

### Why EAV Is Tempting

EAV is attractive when:

1. **The set of attributes is unknown or user-defined.** If your application lets users create custom fields ("Add a field called 'LinkedIn URL'"), EAV handles this without schema changes.
2. **Different entities have vastly different attributes.** A "product catalog" where laptops have screen sizes and RAM, but shirts have fabric types and collar styles. The attribute set varies by entity type.
3. **The database does not support JSON columns.** Before PostgreSQL's `jsonb` and SQLite's `json_extract()`, EAV was the primary way to store schema-free data in a relational database.

### Why EAV Is Usually a Mistake

EAV has severe drawbacks:

**No type safety.** The `AttributeValue` column is `TEXT`. A birthday, a boolean, a decimal price, and a URL are all stored as strings. You lose database-level type checking, and your application must parse and validate every value at runtime.

**No constraints.** You cannot declare `NOT NULL` or `CHECK` constraints on individual attributes. The database cannot enforce that every contact must have a `Birthday`, or that `Birthday` must be a valid date. All validation moves to application code.

**Queries are painful.** "Find all contacts whose birthday is in May" becomes:

```sql
SELECT c.Id, c.FirstName, c.LastName
FROM Contacts c
INNER JOIN ContactProperties cp ON cp.ContactId = c.Id
WHERE cp.AttributeName = 'Birthday'
  AND substr(cp.AttributeValue, 6, 2) = '05';
```

Compare that to a normalized column: `WHERE c.BirthMonth = 5` or `WHERE c.Birthday BETWEEN '2026-05-01' AND '2026-05-31'`.

**Pivoting is expensive.** To reconstruct a flat view of a contact with all its properties as columns, you need a PIVOT query or multiple LEFT JOINs — one per attribute:

```sql
SELECT c.Id, c.FirstName, c.LastName,
       nick.AttributeValue AS NickName,
       bday.AttributeValue AS Birthday,
       lang.AttributeValue AS PreferredLanguage
FROM Contacts c
LEFT JOIN ContactProperties nick ON nick.ContactId = c.Id AND nick.AttributeName = 'NickName'
LEFT JOIN ContactProperties bday ON bday.ContactId = c.Id AND bday.AttributeName = 'Birthday'
LEFT JOIN ContactProperties lang ON lang.ContactId = c.Id AND lang.AttributeName = 'PreferredLanguage';
```

Every additional attribute requires another LEFT JOIN. With 20 custom fields, the query has 20 JOINs.

**Indexing is limited.** You can index `(ContactId, AttributeName)`, but you cannot create a targeted index like "index on Birthday column for range queries." A generic index on `AttributeValue` is useless because it spans all attribute types.

**ORM support is weak.** Entity Framework Core has no native support for EAV. You cannot write `context.Contacts.Where(c => c.Properties["Birthday"] > someDate)` and have it translate to SQL. You end up writing raw SQL or building custom LINQ providers.

### When EAV Is Actually the Right Choice

EAV is appropriate when:

1. The attribute set is genuinely dynamic and user-configurable at runtime.
2. You are building a platform (like Shopify, WordPress, or Salesforce) where end users define their own data models.
3. The number of custom attributes is modest (dozens, not thousands per entity).
4. You accept the query complexity trade-off and do not need high-performance filtering or aggregation on custom attributes.

For Virginia's contact management application, EAV is overkill. The attribute set (name, email, phone, address, notes) is well-known and stable. Fixed columns with proper types and constraints are the right choice.

### The Modern Alternative: JSON Columns

Most modern databases support JSON columns, which give you the flexibility of EAV with better performance and tooling:

```sql
-- SQLite with JSON support
ALTER TABLE Contacts ADD COLUMN CustomFields TEXT DEFAULT '{}';

-- PostgreSQL with jsonb
ALTER TABLE Contacts ADD COLUMN custom_fields JSONB DEFAULT '{}';
```

```csharp
// Store custom fields as JSON
contact.CustomFields = JsonSerializer.Serialize(new Dictionary<string, string>
{
    ["NickName"] = "Ali",
    ["Birthday"] = "1990-05-15"
});

// Query with json_extract (SQLite)
const string sql = """
    SELECT * FROM Contacts
    WHERE json_extract(CustomFields, '$.Birthday') LIKE '%-05-%'
    """;
```

JSON columns combine the flexibility of EAV (arbitrary attributes without schema changes) with better performance (single column read, no JOINs to reconstruct) and database-level extraction functions. PostgreSQL's `jsonb` even supports indexing on specific JSON paths via GIN indexes.

## Part 11: UUIDv7 — When and Where to Use It

The prompt mentioned UUIDv7 as primary keys "where necessary." Let us be precise about when it is necessary and when integer auto-increment keys are fine.

### What Is UUIDv7?

UUIDv7, defined in RFC 9562, is a 128-bit identifier that embeds a Unix timestamp in the high-order bits followed by random data. This makes UUIDv7 values time-sortable — IDs generated later have higher values. In .NET 9 and later (including .NET 10), you create them with:

```csharp
Guid id = Guid.CreateVersion7();                           // uses DateTime.UtcNow
Guid id = Guid.CreateVersion7(DateTimeOffset.UtcNow);      // explicit timestamp
```

### When to Use UUIDv7

Use UUIDv7 when:

1. **Distributed ID generation.** Multiple servers, microservices, or clients need to generate IDs independently without coordination. Integer sequences require a central authority (the database); UUIDs do not.
2. **Merge/sync scenarios.** Offline-capable applications that sync data later need IDs that will not collide.
3. **Security.** Sequential integer IDs leak information (how many records exist, when they were created relative to each other). UUIDs are opaque.
4. **Cross-system references.** When IDs are exposed in APIs, URLs, or exports and need to be globally unique.

### When Integer Auto-Increment Is Fine

For Virginia's contact management application — a single-server Blazor Server app backed by a single SQLite file — integer auto-increment keys are perfectly appropriate. There is no distributed ID generation, no offline sync, and the IDs are only used internally (they appear in URLs like `/contacts/42`, but the application requires authentication, so information leakage is minimal).

If you were to migrate Virginia to a multi-server architecture with PostgreSQL, switching to UUIDv7 would be a good idea:

```csharp
public sealed class Contact
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(100)]
    public required string FirstName { get; set; }
    // ...
}
```

```sql
CREATE TABLE Contacts (
    Id        BLOB PRIMARY KEY,  -- 16 bytes for UUID in SQLite
    FirstName TEXT NOT NULL,
    LastName  TEXT NOT NULL
    -- ...
);
```

In PostgreSQL, you would use the native `uuid` type:

```sql
CREATE TABLE contacts (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name TEXT NOT NULL,
    last_name  TEXT NOT NULL
);
```

And in your C#, you would generate UUIDv7 in the application rather than relying on the database default:

```csharp
var contact = new Contact
{
    Id = Guid.CreateVersion7(),
    FirstName = "Alice",
    LastName = "Johnson"
};
```

### Performance Consideration

UUIDv7's time-sortable nature means it performs well as a clustered index key (B-tree insertions are approximately sequential). Random UUIDv4 (`Guid.NewGuid()`) causes random insertions into the B-tree, leading to page splits and poor cache locality. Always prefer UUIDv7 over UUIDv4 for primary keys.

## Part 12: Denormalization — When to Walk It Back

### Why Denormalize?

Every JOIN has a cost. Network round trips, CPU time for hash joins or merge joins, memory for intermediate result sets. In read-heavy applications, denormalization trades storage space (and some data consistency risk) for query performance.

### Common Denormalization Patterns

**Materialized views / computed columns.** Store the "primary email" directly on the `Contacts` table as a cached value that is updated whenever emails change:

```sql
ALTER TABLE Contacts ADD COLUMN PrimaryEmail TEXT;
```

This avoids the JOIN to `ContactEmails` for list views that only need one email per contact. The cost is keeping it in sync — you need a trigger or application logic to update `PrimaryEmail` when emails change.

**Pre-computed aggregates.** Store counts: `EmailCount`, `PhoneCount`, `AddressCount` on the `Contacts` table. This avoids `COUNT(*)` subqueries in list views.

**Snapshot columns.** Store a copy of related data at the time of an event — like `CreatedByUserName` in `ContactNotes`. This is denormalization for historical accuracy, which is often the right trade-off.

### The Rule of Thumb

Normalize until it hurts (queries become too slow, too complex, or too numerous). Then denormalize just enough to fix the specific performance problem, and document why.

Virginia's current schema sits at approximately **3NF with intentional denormalization of the user name in notes.** This is a pragmatic, well-balanced position for its use case. Going higher (4NF, 5NF) gains nothing because the schema does not have multi-valued or join dependency violations. Going to full 6NF would be actively harmful — it would make every query a five-table JOIN for no benefit.

## Part 13: Putting It All Together — A Recommendation for Virginia

Here is what we recommend for the Virginia application, given its scope (personal/small-team contact management, single-server SQLite deployment):

1. **Keep the current 1NF/2NF/3NF structure.** It is sound. The child tables for emails, phones, addresses, and notes are correctly designed.

2. **Add a `Countries` lookup table** if you care about address data consistency. Populate it with ISO 3166-1 codes. This is a small change with a large payoff for data quality.

3. **Normalize the `Label` fields to a lookup table** if you want consistent labeling and plan to build reporting features. If the labels are purely for display and you do not query on them, free-text labels are acceptable.

4. **Keep `CreatedByUserName` in `ContactNotes`** as an intentional denormalization for historical snapshots, but add a code comment explaining the design decision.

5. **Keep integer auto-increment primary keys** for the SQLite deployment. If migrating to PostgreSQL for multi-server use, switch to UUIDv7 (`Guid.CreateVersion7()`).

6. **Do not pursue 4NF, 5NF, or 6NF** — the schema has no violations at those levels, and the decomposition would add complexity for zero benefit.

7. **Do not adopt EAV** unless you add a user-defined custom fields feature. If you do, prefer a JSON column over a traditional EAV table.

8. **Extract `ProfilePicture` into a separate table** if you observe that queries on contacts are slower than expected due to the BLOB column being selected unnecessarily. For now, EF Core projections mitigate this.

## Part 14: Resources

- **Edgar Codd's original paper**: "A Relational Model of Data for Large Shared Data Banks" (1970) — the foundation of relational database theory
- **Database normalization on Wikipedia**: [en.wikipedia.org/wiki/Database_normalization](https://en.wikipedia.org/wiki/Database_normalization) — comprehensive coverage of all normal forms with examples
- **Dapper on NuGet**: [nuget.org/packages/Dapper](https://www.nuget.org/packages/Dapper) — version 2.1.72 (March 2026)
- **Dapper documentation**: [dapperlib.github.io/Dapper](https://dapperlib.github.io/Dapper/) — official docs
- **Entity Framework Core documentation**: [learn.microsoft.com/en-us/ef/core](https://learn.microsoft.com/en-us/ef/core/) — Microsoft's ORM for .NET
- **Guid.CreateVersion7 API reference**: [learn.microsoft.com/en-us/dotnet/api/system.guid.createversion7](https://learn.microsoft.com/en-us/dotnet/api/system.guid.createversion7) — UUIDv7 in .NET 9+
- **Virginia source code**: [github.com/collabskus/virginia](https://github.com/collabskus/virginia) — the contact management application used as this article's running example
- **RFC 9562 — Universally Unique IDentifiers (UUIDs)**: [datatracker.ietf.org/doc/rfc9562](https://datatracker.ietf.org/doc/rfc9562/) — the specification defining UUIDv7
- **SQLite documentation**: [sqlite.org/docs.html](https://sqlite.org/docs.html) — the database engine used by Virginia
- **PostgreSQL documentation**: [postgresql.org/docs](https://www.postgresql.org/docs/) — the recommended upgrade path for production use
