# WHO PCR Tests Information System (Heap File & Linear Hashing Engine)

[![C#](https://img.shields.io/badge/Language-C%23%2013-239120.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET 9](https://img.shields.io/badge/Framework-.NET%209.0-512BD4.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![UI](https://img.shields.io/badge/UI-WPF%20(Windows)-0078D6.svg)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![IDE](https://img.shields.io/badge/IDE-Visual%20Studio%202022-purple.svg)](https://visualstudio.microsoft.com/)
[![Course](https://img.shields.io/badge/Course-Algorithms%20%26%20Data%20Structures%202-orange.svg)](https://fri.uniza.sk)

High-performance C# / WPF desktop application and custom **disk-based file organization library** (`Heap File` and `Linear Hash File`) designed for offline-capable storage, indexing, and management of PCR test results and patient records on devices with constrained RAM.

This project was developed as a university semester project (**Semester Project S2**) for the **Algorithms and Data Structures 2 (AUS 2)** course at the Faculty of Management Science and Informatics, University of Žilina (**FRI UNIZA**).

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Storage Architecture & Binary Layout](#storage-architecture--binary-layout)
- [Disk I/O Complexity (Block Access Analysis)](#disk-io-complexity-block-access-analysis)
- [Implemented Domain Operations](#implemented-domain-operations)
- [Installation & How to Run](#installation--how-to-run)
- [GUI Controls & Usage](#gui-controls--usage)
- [Documentation](#documentation)
- [Screenshots](#screenshots)

---

## Overview

Building upon the first semester project, this system models an offline-first epidemiological database for the World Health Organization (WHO) tailored for mobile or embedded devices with minimal RAM capacity. Instead of holding datasets in main memory, records (`Person` and `PCRTest`) are persisted directly on disk in fixed-size binary blocks.

The storage engine is implemented from scratch without external databases or recursion, featuring:
1. **Unordered Binary Heap File (`BaseHeapFile` / `BHeapFile<T>`):** Manages raw fixed-size byte blocks (`FileStream`) with in-memory free-space tracking (`partlyClearBlocks` and `fullyClearBlocks`) and automatic end-of-file truncation.
2. **Dynamic Linear Hash File (`LinHashFile<T>`):** Combines a primary bucket heap file (`MainHeapFile<T>`) with a linked overflow heap file (`OverflowHeapFile<T>`), dynamically scaling via **Split**, **Merge**, and **Bucket Compaction (*Striasanie*)** to guarantee fast $O(1)$ average disk lookups.

---

## Key Features

- **Custom Fixed-Size Binary Serialization (`IData<T>`):**
  - Deterministic byte-level serialization (`GetBytes`, `FromBytes`, `GetSize`) using `BitConverter` with fixed-length padded strings and explicit valid-character counters.
  - Zero data redundancy on disk: `Person` stores an array of up to 6 `TestCode` foreign keys (`int[6]`), while `PCRTest` stores only the 10-character `PersonId` reference.
- **Space-Efficient Heap File (`BHeapFile<T>`):**
  - Configurable block size (in bytes) with automatic blocking factor calculation based on record size and block header overhead.
  - Priority-based insertion: reuses **partially full blocks** first, then **completely empty blocks**, and appends a new block at the end of the file only when necessary.
  - Automatic file shrinking (`RemoveAllEmptyBlocksFromEnd`) truncates trailing empty blocks after deletions.
- **Dynamic Linear Hashing (`LinHashFile<T>`):**
  - Dual-hash addressing functions governed by level and splitPointer.
  - **Automatic Split:** Triggered when total valid overflow records exceed **80%** of total valid main records.
  - **Automatic Merge:** Triggered upon deletion when total valid overflow records fall below **50%** of total valid main records.
  - **Overflow Chain Compaction (*Striasanie*):** After deleting a record, `TryCompactBucket` checks whether the bucket's records can fit into fewer overflow blocks and compacts the last overflow block into preceding blocks in the chain.
- **Full Session Persistence & Metadata Files:**
  - All structural parameters (block sizes, blocking factors, free-block address lists, split pointer, hashing level, and ID/test code sequences) are saved into dedicated `.metadata` binary files upon closing and restored seamlessly on reopen.
- **Strict Core / GUI Separation & Sequential Block Inspector:**
  - Two WPF desktop clients (`GUI_PCRSystem` for the full WHO PCR system and `GUI_HeapFIle` for standalone Heap File testing) communicating through dedicated controllers.
  - Built-in sequential dump of all main and overflow files showing every block's address, valid record count, overflow pointer chain, and formatted records.
- **Automated Stress & Property-Based Testers (`Tester.cs`):**
  - Randomized 100,000-operation test suite for `BHeapFile<Person>` and 10,000-operation test suite for `LinHashFile<Person>` verifying data integrity after every single `Insert`, `Get`, and `Delete`.

---

## Storage Architecture & Binary Layout

### Class & Block Hierarchy

```
BaseBlock<T> (ValidCount: 4 B)
 └── Block<T> (Heap File Block — Header: 4 B)
      └── OverflowHFBlock<T> (+ OverflowAddress: 8 B — Header: 12 B)
           └── MainHFBlock<T> (+ OverflowBlocksCount: 4 B, OverflowValidRecordsCount: 4 B — Header: 20 B)

BaseHeapFile<T, BlockT>
 ├── BHeapFile<T>        (uses Block<T>)
 ├── OverflowHeapFile<T> (uses OverflowHFBlock<T>)
 └── MainHeapFile<T>     (uses MainHFBlock<T>)

LinHashFile<T>
 ├── MainHeapFile<T>     (Primary buckets addressed by Hash0 / Hash1)
 └── OverflowHeapFile<T> (Chained overflow blocks per bucket)
```

### Binary Record Sizes (`IData<T>`)

| Entity | Persisted Attributes & Binary Layout | Total Record Size |
| :--- | :--- | :---: |
| **`Person`** | `firstNameValidChars` (`4 B`) + `lastNameValidChars` (`4 B`) + `idValidChars` (`4 B`) + `FirstName` (`15 × 2 B`) + `LastName` (`14 × 2 B`) + `Id` (`10 × 2 B`) + `BirthDate.DayNumber` (`4 B`) + `TestsCount` (`4 B`) + `TestsCodes[6]` (`6 × 4 B`) | **122 B** |
| **`PCRTest`** | `TestDateTime.Ticks` (`8 B`) + `patientIdValidChars` (`4 B`) + `PersonId` (`10 × 2 B`) + `TestCode` (`4 B`) + `Result` (`1 B`) + `Value` (`8 B`) + `noteValidChars` (`4 B`) + `Note` (`11 × 2 B`) | **71 B** |

### Block Headers & Blocking Factor

For a given `BlockSize` (in bytes), the number of records per block (`BlockingFactor`) is computed as:

$$\text{BlockingFactor} = \left\lfloor \frac{\text{BlockSize} - \text{HeaderSize}}{\text{RecordSize}} \right\rfloor$$

| Block Type | Header Fields | Header Size |
| :--- | :--- | :---: |
| `Block<T>` (Standard Heap File) | `ValidCount` (`int`) | **4 B** |
| `OverflowHFBlock<T>` (Overflow File) | `ValidCount` (`int`) + `OverflowAddress` (`long`) | **12 B** |
| `MainHFBlock<T>` (Main Bucket File) | `ValidCount` (`int`) + `OverflowAddress` (`long`) + `OverflowBlocksCount` (`int`) + `OverflowValidRecordsCount` (`int`) | **20 B** |

---

## Disk I/O Complexity (Block Access Analysis)

Exact counts of disk block reads and writes ($1 \text{ access} = 1 \text{ block read or write}$) for every file operation:

### 1. Heap File (`BHeapFile<T>`)

| Operation | Scenario | Disk Accesses | Explanation |
| :--- | :--- | :---: | :--- |
| **Insert** | Into partially full block | $2$ | $1\times$ read partially full block + $1\times$ write updated block |
| **Insert** | Into completely empty block | $2$ | $1\times$ read empty block + $1\times$ write updated block |
| **Insert** | Append new block at EOF | $1$ | Construct block in RAM + $1\times$ write at end of file |
| **Get** | Read record at given address | $1$ | $1\times$ read target block at `address` |
| **Delete** | Block remains non-empty (or not at EOF) | $2$ | $1\times$ read block + $1\times$ write updated block |
| **Delete** | Block becomes empty at EOF | $3$ | $1\times$ read block + $1\times$ write block + $1\times$ file truncation (`SetLength`) |

### 2. Linear Hash File (`LinHashFile<T>`)

| Operation | Scenario | Disk Accesses | Variables Legend |
| :--- | :--- | :---: | :--- |
| **Insert** | Into main bucket block | $2$ | $1\times$ read main block + $1\times$ write main block |
| **Insert** | Into existing overflow block | $1 + p + 2$ | $p$ = overflow blocks read until free space is found; writes overflow + main block |
| **Insert** | Into newly allocated overflow block | $1 + p_{\max} + 3$ | $p_{\max}$ = total overflow blocks in chain before insert; writes new overflow block, previous tail block (if any), and main block |
| **Split** | Rehash bucket chain at `splitPointer` | $1 + p + 1 + r + 1 + u$ | $p$ = old overflow blocks in chain $S$; $r$ = new overflow blocks in $S$; $u$ = new overflow blocks in $S'$ |
| **Get** | Point lookup by key | $1 + p$ | $p$ = overflow blocks traversed until target record is found |
| **Update** | Modify non-key attributes in-place | $1 + p + 1$ | $p$ = overflow blocks traversed until target record is found + $1\times$ write updated block |
| **Delete** | Record found in main block | $2$ | $1\times$ read main block + $1\times$ write updated main block |
| **Delete** | Record found in overflow block | $1 + p + 2$ | $p$ = overflow blocks read; writes updated overflow block (or predecessor on free) + main block |
| **Compact** | *Striasanie* of bucket chain | $1 + p + 1 + r$ | $p$ = overflow blocks read in chain; $r$ = overflow blocks modified during compaction |
| **Merge** | Merge last bucket $S$ into $S'$ | $1 + p + 1 + r + 1 + t + 1 + u + 1$ | $p$ = overflow blocks in $S$; $r$ = overflow blocks in $S'$ before merge; $t$ = modified existing overflow blocks in $S'$; $u$ = newly created overflow blocks in $S'$ |

---

## Implemented Domain Operations

All 8 required epidemiological operations (plus sequential database inspection and synthetic generation) are implemented in `PCRSystem`:

| # | Operation | Description |
| :-: | :--- | :--- |
| **1** | **Insert PCR Test** | Generates a unique `TestCode`, verifies the patient exists and has $< 6$ tests, inserts the `PCRTest` into `pcrTestsLHF`, and appends the test code to the patient's record in `personsLHF`. |
| **2** | **Find Person & Tests** | Retrieves a `Person` by their unique `PersonID` from `personsLHF` and fetches all of their associated `PCRTest` records (up to 6) from `pcrTestsLHF`. |
| **3** | **Find PCR Test & Patient** | Retrieves a `PCRTest` by its unique `TestCode` from `pcrTestsLHF` and fetches the corresponding `Person` record from `personsLHF`. |
| **4** | **Insert Person** | Generates a unique 10-character `PersonID` and inserts the new `Person` record into `personsLHF`. |
| **5** | **Delete PCR Test** | Permanently deletes a `PCRTest` by `TestCode` from `pcrTestsLHF` and removes its reference from the associated patient's `TestsCodes` array in `personsLHF`. |
| **6** | **Delete Person (Cascade)** | Deletes a `Person` by `PersonID` from `personsLHF` and cascades deletion to all of their performed PCR tests in `pcrTestsLHF`. |
| **7** | **Find & Edit Person** | Looks up a `Person` by `PersonID` and allows in-place editing of non-key attributes (`FirstName`, `LastName`, `BirthDate`) via `personsLHF.Update`. |
| **8** | **Find & Edit PCR Test** | Looks up a `PCRTest` by `TestCode` and allows in-place editing of non-key attributes (`TestDateTime`, `Result`, `Value`, `Note`) via `pcrTestsLHF.Update`. |

---

## Installation & How to Run

### Prerequisites

- **Operating System:** Windows 10 / 11
- **SDK:** [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (`net9.0` and `net9.0-windows`)
- **IDE:** [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *.NET desktop development* workload)

### Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/lubomirbojda/Heap-File-and-Linear-Hashing-Engine
   cd Heap-File-and-Linear-Hashing-Engine
   ```

2. **Open the solution:**
   - Double-click `AUS2_S2.sln` to open the solution in Visual Studio 2022.

3. **Choose the Startup Project:**
   - **WHO PCR System GUI (Linear Hashing + Full System):** Right-click `GUI_PCRSystem` in Solution Explorer &rarr; **Set as Startup Project**.
   - **Standalone Heap File GUI:** Right-click `GUI_HeapFIle` in Solution Explorer &rarr; **Set as Startup Project**.
   - **Automated Console Stress Tester:** Right-click `AUS2_S2` in Solution Explorer &rarr; **Set as Startup Project** (toggle `t.TestHeapFile()` or `t.TestLinHashFile()` in `Program.cs`).

4. **Build & Run:**
   - Select `Release` or `Debug` configuration and press `F5` or `Ctrl + F5`.
   - Or run directly from the CLI:
     ```bash
     # Launch the main WHO PCR System WPF application
     dotnet run --project GUI_PCRSystem -c Release

     # Launch the standalone Heap File WPF application
     dotnet run --project GUI_HeapFIle -c Release

     # Run the console property-based stress tester
     dotnet run --project AUS2_S2 -c Release
     ```

---

## GUI Controls & Usage

### 1. Main Application (`GUI_PCRSystem`)
- **Top Menu (`File`):**
  - **`File -> New`:** Creates a new database directory (`.data`) and prompts for custom block sizes (`personsMain`, `personsOverflow`, `pcrTestsMain`, `pcrTestsOverflow`) and initial bucket counts.
  - **`File -> Open`:** Opens an existing PCR system file and restores all linear hash files and metadata from disk.
  - **`File -> Close and save opened file`:** Flushes all buffers and saves `.metadata` files so the database can be reopened later.
  - **`File -> Generate`:** Populates the opened database with synthetic patients and PCR tests.
- **Sequential Database Inspector (`Print file`):**
  - Four dedicated buttons (**`Persons main file`**, **`Persons overflow file`**, **`PCR tests main file`**, **`PCR tests overflow file`**) open a sequential block viewer displaying block size, blocking factor, block index, byte address, overflow chain links, valid record counts, and formatted records.
- **Operations Selector (`Choose operation`):**
  - Opens a context menu to switch between operations `01` through `08` (Insert/Find/Delete/Edit for both `PCRTest` and `Person`).

### 2. Standalone Heap File Inspector (`GUI_HeapFIle`)
- Dedicated testing utility for `BHeapFile<Person>` supporting **`File -> New`** (with custom block size), **`File -> Open`**, **`File -> Generate`**, **`File -> Close`**, and interactive **`Print heapfile`**, **`Insert`**, **`Get`** (by block byte address + ID), and **`Delete`**.

---

## Documentation

Complete technical documentation (in Slovak) is included in the repository root:

- **[Project Documentation (PDF)](https://github.com/lubomirbojda/Heap-File-and-Linear-Hashing-Engine/blob/main/Documentation.pdf)**
  - Complete UML Class Diagram of the architecture
  - Step-by-step analysis and exact disk I/O formulas for Heap File (`Insert`, `Get`, `Delete`) and Linear Hash File (`Insert`, `Split`, `Get`, `Update`, `Delete`, `Merge`, `Compact`)
  - Explanation of auxiliary linear lists used for free-block management and bucket reorganization

> **Note:** PDF documentation is written in the **Slovak language**.

---

## Screenshots

<p align="center">
  <img width="977" height="610" alt="Snímka obrazovky 2026-10-29 101024" src="https://github.com/user-attachments/assets/2fa70f70-dfaf-4996-a3c9-4717a3b980bb" />
  <img width="1140" height="637" alt="image" src="https://github.com/user-attachments/assets/475d5b57-0dc9-40f5-bdf5-c557a239588a" />

</p>
