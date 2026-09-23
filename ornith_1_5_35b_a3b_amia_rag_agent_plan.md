# Implementation Plan: Context-Efficient RAG for Ornith-1.5-35B-A3B

## Purpose

Build a local retrieval system for `ornith-ai/Ornith-1.5-35B-A3B` that helps the model work effectively on the **AmiaReforged** codebase without stuffing large repository maps, documentation files, or arbitrary code chunks into the prompt.

The target repository is:

- https://github.com/amia-team/AmiaReforged

The RAG system is primarily a **searchable external code memory with progressive disclosure**.

Its job is not to make Ornith "know the entire repo" at all times. Its job is to let Ornith cheaply answer:

- Where is this feature implemented?
- What type or service owns this behavior?
- Which files are relevant?
- Where is this symbol referenced?
- What does this specific implementation do?
- Which tests cover this code?
- What adjacent implementation should I inspect next?

Only the source actually needed for the current task should enter the model context.

---

# 1. Design goals

## 1.1 Primary goals

The implementation MUST:

1. Keep persistent repository knowledge **outside** the LLM context.
2. Prefer structural and lexical retrieval before spending tokens on large semantic chunks.
3. Let Ornith retrieve information iteratively instead of injecting a fixed top-N chunk set.
4. Make paths, projects, namespaces, symbols, and line ranges first-class data.
5. Support exact symbol lookup and reference navigation.
6. Support natural-language semantic search when the user does not know the relevant identifier.
7. Avoid generic fixed-size code chunking when language structure is available.
8. Track the indexed Git commit and update incrementally.
9. Run locally.
10. Remain usable from an OpenAI-compatible/tool-calling chat frontend.
11. Be frontend-agnostic enough that Open WebUI can be used without making it a hard dependency.
12. Minimize framework complexity.

## 1.2 Non-goals for v1

Do NOT attempt to:

- train Ornith;
- fine-tune an embedding model;
- summarize the entire repository into a permanent prompt;
- build a general-purpose autonomous coding agent;
- index Git history, issues, or pull requests unless explicitly added later;
- infer architectural facts and save them as truth without source evidence;
- build a distributed vector-search cluster;
- add a knowledge graph database unless simple relational metadata proves insufficient.

---

# 2. Model assumptions

The implementation should assume:

- Model: `ornith-ai/Ornith-1.5-35B-A3B`
- Ornith is a reasoning-capable MoE model.
- The official model card documents OpenAI-style tool-call parsing in supported serving stacks.
- The checkpoint advertises a native context length up to 262,144 tokens.
- The local deployment MAY use a much smaller practical context window because of quantization, KV-cache memory, runtime configuration, or desired performance.

Therefore:

> Never design retrieval around the theoretical maximum context length.

All context budgets MUST be configurable and conservative.

Suggested initial retrieval budget:

- search/navigation results: 200-800 tokens per tool result;
- source read: 500-1,500 tokens per call unless explicitly requested otherwise;
- maximum source added during one retrieval iteration: ~2,000 tokens;
- soft maximum cumulative retrieved source for an ordinary coding question: ~4,000-6,000 tokens;
- hard limits configurable by the user.

These are starting values, not immutable constants.

Official model reference:

- https://huggingface.co/ornith-ai/Ornith-1.5-35B-A3B

---

# 3. High-level architecture

```text
                   +-----------------------+
                   |  Ornith-1.5-35B-A3B   |
                   +-----------+-----------+
                               |
                         tool calls only
                               |
            +------------------+------------------+
            |                                     |
            v                                     v
   +------------------+                  +------------------+
   | navigation tools |                  | source-read tools|
   +------------------+                  +------------------+
   | repo_search      |                  | repo_read        |
   | repo_symbol      |                  | repo_read_symbol |
   | repo_tree        |                  | repo_references  |
   | repo_files       |                  | repo_related     |
   +--------+---------+                  +---------+--------+
            |                                      |
            +------------------+-------------------+
                               |
                    +----------v-----------+
                    | Retrieval Service    |
                    +----------+-----------+
                               |
             +-----------------+------------------+
             |                 |                  |
             v                 v                  v
      +-------------+   +-------------+    +-------------+
      | structural  |   | lexical     |    | dense       |
      | index       |   | search      |    | embeddings  |
      +-------------+   +-------------+    +-------------+
             \               |                 /
              \              |                /
               +-------------v---------------+
                             |
                    ranked candidate set
                             |
                  +----------v-----------+
                  | indexed repo records |
                  +----------------------+
```

The central architectural principle is:

> Searching is cheap. Reading source spends context.

Do not combine those two operations into one large automatic RAG injection.

---

# 4. Recommended implementation shape

Keep the RAG implementation outside the AmiaReforged application runtime.

Suggested workspace:

```text
amia-rag/
├── README.md
├── config.example.toml
├── docker-compose.yml             # optional local services only
├── indexer/
│   ├── AmiaRag.Indexer.csproj
│   └── ...
├── server/
│   └── ...
├── schema/
│   └── ...
├── scripts/
│   ├── index.sh
│   └── update-index.sh
├── eval/
│   ├── queries.jsonl
│   └── expected.jsonl
└── data/
    └── .gitignore
```

## Why a small .NET/Roslyn indexer

AmiaReforged is primarily C#.

Use Roslyn for C# indexing instead of regex or generic token chunking because Roslyn can reliably expose:

- namespaces;
- classes;
- records;
- interfaces;
- enums;
- constructors;
- methods;
- properties;
- fields;
- signatures;
- containing symbols;
- source line spans;
- project membership;
- project references;
- symbol references when a compilation is available.

The retrieval server does not have to be .NET, but keeping the C# structural extraction in Roslyn is strongly preferred.

---

# 5. Storage strategy

Start with the smallest practical local design.

## 5.1 Metadata and lexical search

Use SQLite.

Use normal relational tables for:

- repositories;
- files;
- projects;
- symbols;
- symbol relationships;
- chunks;
- index state;
- content hashes.

Use SQLite FTS5 for:

- symbol names;
- namespaces;
- paths;
- signatures;
- source text;
- comments/documentation;
- filenames.

## 5.2 Dense vector storage

Preferred v1 choices, in order:

1. `sqlite-vec`, if it is stable in the selected runtime;
2. a small local vector library/index persisted beside SQLite;
3. Qdrant only if the simpler options prove inadequate.

Avoid introducing a separate database server merely because RAG examples commonly use one.

## 5.3 Embedding provider

Make embeddings an interface.

Preferred behavior:

```text
POST /v1/embeddings
```

or an equivalent local provider interface.

This allows the user to swap between local embedding models without rebuilding the application.

A small code-capable embedding model is appropriate. `Qwen3-Embedding-0.6B` is a reasonable candidate, but the implementation MUST NOT hard-code the system to one model.

Store these with the index:

- embedding model identifier;
- vector dimension;
- normalization mode;
- index version.

If any of these change, require re-embedding.

---

# 6. Index schema

At minimum, create these logical record types.

## 6.1 Repository

```text
repository_id
root_path
remote_url
branch
indexed_commit_sha
indexed_at
index_version
```

## 6.2 Project

```text
project_id
name
path
target_framework
project_references[]
package_references[]
```

## 6.3 File

```text
file_id
path
extension
project_id?
content_hash
language
is_test
is_generated
size_bytes
last_indexed_commit
```

## 6.4 Symbol

```text
symbol_id
fully_qualified_name
short_name
kind
namespace
project_id
file_id
parent_symbol_id?
signature
start_line
end_line
accessibility
```

`kind` should distinguish at least:

- namespace
- class
- record
- struct
- interface
- enum
- delegate
- constructor
- method
- property
- field
- event

## 6.5 Source chunk

```text
chunk_id
file_id
symbol_id?
chunk_kind
start_line
end_line
content_hash
text
embedding?
```

Possible `chunk_kind` values:

- file_summary_record
- type_body
- method_body
- constructor_body
- enum
- interface
- razor_component
- javascript_block
- markdown_section
- config
- test
- fallback_text

## 6.6 Relationship

```text
source_symbol_id
relationship_kind
target_symbol_id?
target_name?
source_file_id
source_line
```

Start with a small relation vocabulary:

- contains
- inherits
- implements
- references
- calls
- constructed_by
- project_reference

Do not attempt a perfect static call graph in v1.

---

# 7. Repository filtering

Index source that can plausibly help coding/navigation.

## Include initially

- `.cs`
- `.csproj`
- `.sln`
- `.razor`
- `.js`
- `.json`
- `.yaml`
- `.yml`
- `.toml`
- `.md`

## Exclude by default

- `.git/`
- `bin/`
- `obj/`
- package caches
- generated build artifacts
- binary assets
- minified JavaScript
- source maps
- vendored dependencies
- lock/cache files that do not help navigation
- secrets
- local environment files containing credentials

The indexer MUST have configurable include/exclude globs.

Do not index `.env` files or secret-bearing deployment material by default.

---

# 8. C# structural indexing

## 8.1 Load the solution

Attempt to load:

```text
AmiaReforged.sln
```

with `MSBuildWorkspace`.

If the full semantic compilation cannot be created because of environment/dependency issues:

1. continue with syntax-only indexing;
2. mark semantic-reference data as unavailable;
3. do not fail the entire index build.

## 8.2 Produce symbol records

For every relevant C# declaration, capture:

- project;
- file path;
- namespace;
- containing type;
- symbol name;
- fully qualified name;
- signature;
- line range.

## 8.3 Code chunks MUST follow syntax boundaries

Do not split C# every N tokens.

Preferred rules:

### Small type

If a class/record/interface/enum is below the configurable size threshold, index the whole declaration as one source chunk.

### Large type

Split by members:

```text
type header + fields
constructor
method A
method B
method C
property group if needed
```

Each member chunk should carry metadata pointing to its parent type.

### Huge method

Only if an individual method exceeds the maximum chunk size:

- split at statement/block boundaries;
- preserve the method signature in each derived chunk;
- include exact line ranges;
- avoid arbitrary overlap where possible.

---

# 9. Non-C# chunking

## Markdown

Markdown files are index corpus, NOT persistent model context.

Chunk by heading hierarchy.

Store:

- path;
- heading path;
- section line range;
- content.

Example:

```text
README.md
  Setup
    Database
```

Only inject a Markdown section when retrieval actually selects it.

## Razor

Prefer:

- component metadata;
- `@code` blocks;
- important markup regions;
- backing `.razor.cs` symbol relationships where present.

## JavaScript

Use syntax-aware chunking if convenient.

For v1, function/class boundaries are sufficient.

## JSON/config

Store whole small files.

For large structured files, split by major top-level object/array entries.

---

# 10. Create two retrieval layers

The implementation MUST conceptually separate:

## Layer A: Navigation retrieval

Returns small metadata records.

Used for:

- "where is this?"
- identifying candidate files;
- finding symbols;
- finding references;
- locating related systems.

Typical output:

```text
1. TechniqueFactory
   class
   AmiaReforged.Classes/Monk/Techniques/TechniqueFactory.cs
   lines 8-64

2. AttackTechniqueService
   class
   AmiaReforged.Classes/Monk/Services/AttackTechniqueService.cs
   lines 11-103
```

This result should be cheap.

## Layer B: Source retrieval

Returns actual source text.

Used only after a candidate is selected.

Typical calls:

```text
repo_read_symbol("TechniqueFactory")
repo_read(path, 20, 95)
```

This separation is mandatory.

Do not have `repo_search` dump full source bodies by default.

---

# 11. Retrieval ranking

Use a hybrid ranker.

Recommended initial ranking precedence:

1. exact fully qualified symbol match;
2. exact short symbol match;
3. exact filename/path segment match;
4. prefix/fuzzy identifier match;
5. FTS/BM25 lexical match;
6. dense semantic similarity;
7. optional lightweight reranker.

## 11.1 Exact identifier boost

Queries containing code-like tokens should heavily boost lexical retrieval.

Examples:

```text
ChangeAppearanceService
TechniqueFactory
NwCreature
OnModuleLoad
WorldEngineEditor
```

Do not let an embedding search outrank an exact symbol hit unless there is a compelling reason.

## 11.2 Natural-language query

Example:

```text
"code that applies bonuses to summoned creatures"
```

Here, dense retrieval becomes more important.

Still merge dense candidates with lexical/path results.

## 11.3 Fusion

Use Reciprocal Rank Fusion or another simple deterministic fusion method for v1.

Avoid an opaque LLM-based ranking stage until baseline retrieval has been measured.

---

# 12. Tool API for Ornith

Expose a deliberately small tool surface.

The exact transport may be MCP, OpenAPI, or another function-calling interface, but the semantics should remain stable.

## 12.1 `repo_search`

Purpose:

Find candidate symbols/files/sections.

Input:

```json
{
  "query": "summoned creature bonuses",
  "scope": null,
  "limit": 8
}
```

Output should contain metadata and very short snippets only.

Never return full files.

## 12.2 `repo_symbol`

Purpose:

Resolve one symbol name.

Input:

```json
{
  "name": "TechniqueFactory"
}
```

Output:

- matching fully qualified symbols;
- kind;
- signature;
- path;
- line range;
- project.

## 12.3 `repo_tree`

Purpose:

Navigate repository/project/directory structure.

Input:

```json
{
  "path": "AmiaReforged.Classes/Monk",
  "depth": 2
}
```

Output must be compact.

## 12.4 `repo_read`

Purpose:

Read an explicit file line range.

Input:

```json
{
  "path": "AmiaReforged.Classes/Monk/Techniques/TechniqueFactory.cs",
  "start_line": 1,
  "end_line": 100
}
```

Enforce a maximum line/token limit unless an override is explicitly permitted.

## 12.5 `repo_read_symbol`

Purpose:

Read the source span belonging to a symbol.

Input:

```json
{
  "symbol": "TechniqueFactory"
}
```

Prefer this to whole-file reads.

## 12.6 `repo_references`

Purpose:

Return locations that reference a symbol.

Input:

```json
{
  "symbol": "TechniqueFactory",
  "limit": 20
}
```

Default result should be locations/signatures, not full bodies.

## 12.7 Optional `repo_related`

Purpose:

Return structurally related candidates:

- containing type;
- implemented interfaces;
- base type;
- tests;
- common callers;
- same project/namespace neighbors.

Keep this optional until core retrieval works.

---

# 13. Progressive disclosure policy

The agent-facing instructions should explicitly tell Ornith:

1. Search before reading broadly.
2. Prefer symbol and path lookup over semantic search when an identifier is known.
3. Read only the smallest source span that answers the question.
4. Use reference lookup before reading many neighboring files.
5. Do not re-read source already present in context.
6. Expand outward only when the current source contains unresolved dependencies.

Expected behavior:

```text
question
  -> repo_search
  -> candidate list
  -> repo_read_symbol(candidate)
  -> repo_references(if needed)
  -> repo_read_symbol(next dependency)
  -> answer/edit
```

Not:

```text
question
  -> retrieve top 20 chunks
  -> inject everything
```

---

# 14. Context-budget enforcement

The retrieval service MUST estimate tokens before returning source.

Configuration example:

```toml
[context]
search_result_token_limit = 700
read_default_token_limit = 1200
read_hard_token_limit = 3000
ordinary_task_retrieval_soft_limit = 5000
max_search_results = 10
```

The retrieval server should truncate or reject oversized reads with a useful response:

```text
Requested range is approximately 5,400 tokens.
Maximum read size is 3,000.
Suggested subranges:
- 1-120
- 121-245
```

Do not silently dump oversized context.

---

# 15. Avoid duplicate context

Track retrieved content during a chat/session when the frontend allows it.

At minimum, identify chunks by:

```text
content_hash + path + line range
```

If Ornith requests a chunk it already received, respond with:

```text
Already retrieved in this session:
TechniqueFactory.cs lines 1-64
```

Optionally allow `force=true`.

If session tracking is difficult at the tool layer, expose stable chunk IDs so the orchestrator can deduplicate.

---

# 16. Incremental indexing

The system MUST not require a complete re-embedding after every Git pull.

## Initial index

1. record current commit SHA;
2. enumerate included files;
3. hash contents;
4. parse/index;
5. embed eligible chunks;
6. commit the index transaction;
7. save the indexed SHA.

## Update

Compare:

```text
indexed_commit_sha..HEAD
```

Use Git diff to identify:

- added files;
- modified files;
- renamed files;
- deleted files.

For each changed file:

- delete stale symbol/chunk/reference rows;
- reparse;
- hash chunks;
- reuse cached embeddings for unchanged content hashes;
- embed only changed/new chunks.

For deleted files:

- delete all associated records.

Update the indexed commit only after the operation succeeds.

---

# 17. Embedding cache

Maintain an embedding cache keyed by:

```text
embedding_model_id
embedding_dimension
content_hash
```

This makes reindexing after path moves or metadata changes cheap.

Do not re-embed identical content unnecessarily.

---

# 18. Index freshness

Every search response should make index freshness inspectable.

Expose:

```text
indexed_commit
current_repo_commit
is_stale
```

The tool does not need to repeat this on every result if that adds noise, but it must be queryable.

Add:

```text
repo_status()
```

or an equivalent health endpoint.

---

# 19. AmiaReforged-specific indexing priorities

The repository currently has multiple important project boundaries. Treat these as strong ranking metadata rather than flattening the repo into one corpus.

Examples include:

- `AmiaReforged.Core`
- `AmiaReforged.Classes`
- `AmiaReforged.DMS`
- `AmiaReforged.PwEngine`
- `AmiaReforged.Races`
- `AmiaReforged.System`
- `AmiaReforged.AdminPanel`
- `AmiaReforged.AdminPanel.Tests`
- `AmiaReforged.PluginTests`
- `AmiaReforged.Core.Specs`
- `WorldSimulator`
- `WorldSimulator.Tests`

The retrieval system should be able to scope searches:

```json
{
  "query": "monk augmentation",
  "project": "AmiaReforged.Classes"
}
```

Project/path proximity should be a ranking signal.

Tests should be linked to likely production symbols/files where feasible.

---

# 20. Query behavior examples

## Example A: exact symbol

User:

```text
Where is ChangeAppearanceService used?
```

Desired tool flow:

```text
repo_symbol("ChangeAppearanceService")
repo_references(resolved_symbol)
```

No embedding query is required unless symbol resolution fails.

## Example B: fuzzy architectural question

User:

```text
Where is monk technique selection implemented?
```

Desired flow:

```text
repo_search("monk technique selection")
```

Candidate result might identify:

```text
TechniqueFactory
TechniqueType
AttackTechniqueService
CastTechniqueService
```

Ornith then reads the most likely symbol.

## Example C: behavioral question

User:

```text
How do summoned creatures receive their bonuses?
```

Desired flow:

```text
repo_search("summoned creatures bonuses")
repo_read_symbol(best candidate)
repo_references(relevant symbol)
repo_read_symbol(next required implementation)
```

The system should not initially inject every summoning-related file.

---

# 21. Agent instructions for implementation

The coding agent implementing this system should follow these rules.

## Rule 1: Build vertical slices

Do not build the entire architecture before testing retrieval.

Implement in this order:

1. repository/file inventory;
2. Roslyn symbol extraction;
3. SQLite schema;
4. exact symbol/path search;
5. `repo_symbol`, `repo_tree`, `repo_read_symbol`;
6. FTS5 lexical search;
7. semantic embeddings;
8. hybrid ranking;
9. reference lookup;
10. incremental indexing;
11. frontend/tool integration;
12. evaluation tuning.

At the end of each phase, run a real query against AmiaReforged.

## Rule 2: Measure before adding infrastructure

Do not add:

- Qdrant;
- Elasticsearch;
- LangChain;
- LlamaIndex;
- a graph database;
- an LLM reranker;

unless a measured retrieval failure demonstrates a need.

## Rule 3: Preserve debuggability

Every retrieval result MUST be explainable with data such as:

```text
exact_symbol_score
path_score
bm25_score
vector_score
final_rank
```

A debug mode should expose ranking components.

## Rule 4: Never hide provenance

Every source result must include:

- repository-relative path;
- line range;
- symbol where applicable;
- indexed commit or index version.

## Rule 5: Keep the model out of indexing decisions where deterministic tooling works

Use Roslyn, Git, FTS, and embedding similarity before introducing model-generated metadata.

---

# 22. Implementation phases

## Phase 0 — Baseline and test corpus

Create a small evaluation set BEFORE tuning retrieval.

Write 30-50 questions based on real AmiaReforged work.

Categories:

- exact symbol location;
- fuzzy feature location;
- cross-project architecture;
- call/reference tracing;
- test discovery;
- implementation explanation.

Each evaluation item should contain:

```json
{
  "query": "Where is monk technique selection implemented?",
  "expected_paths": [
    "..."
  ],
  "expected_symbols": [
    "..."
  ],
  "category": "feature-location"
}
```

Do not require one single correct file if several are legitimately relevant.

### Exit criteria

- evaluation format committed;
- at least 30 useful questions;
- no retrieval code tuned against only one or two examples.

---

## Phase 1 — Structural index

Implement:

- repo scan;
- solution/project discovery;
- Roslyn C# parsing;
- file records;
- symbol records;
- line ranges;
- project metadata.

Implement tools:

- `repo_tree`
- `repo_symbol`
- `repo_read`
- `repo_read_symbol`

### Exit criteria

The system can answer exact navigation questions without embeddings.

Examples:

```text
Where is TechniqueFactory?
Where is SummoningService?
Show me the implementation of X.
```

---

## Phase 2 — Lexical retrieval

Add SQLite FTS5.

Index:

- path;
- filename;
- symbol;
- fully qualified symbol;
- signature;
- comments/docs;
- source text.

Implement `repo_search`.

Add ranking boosts for:

- exact symbol;
- exact filename;
- path segment;
- project match.

### Exit criteria

Feature-location questions using ordinary words return useful candidates in the top 5.

---

## Phase 3 — Dense semantic retrieval

Add embedding provider abstraction.

Embed:

- symbol cards;
- method/type chunks;
- documentation sections;
- other supported source chunks.

Do NOT embed gigantic whole files merely for convenience.

Combine dense and lexical results with deterministic fusion.

### Exit criteria

Natural-language queries with vocabulary different from the implementation still locate useful code.

---

## Phase 4 — References and relationships

Use Roslyn semantic data where available.

Implement:

- symbol references;
- inheritance;
- interface implementation;
- containing-symbol relationships;
- project references.

Expose `repo_references`.

### Exit criteria

Ornith can navigate from a type to likely callers/usages without searching the entire corpus textually.

---

## Phase 5 — Progressive disclosure integration

Expose the tool API to Ornith.

The integration should support an OpenAI-compatible tool-calling workflow or MCP/OpenAPI equivalent.

System/tool instructions must tell Ornith to:

- search first;
- read narrowly;
- expand only as needed;
- prefer symbol reads;
- avoid whole-file reads unless justified.

### Exit criteria

A multi-step Amia question can be answered with multiple small retrieval calls and no automatic large prompt injection.

---

## Phase 6 — Incremental updates

Implement Git-aware updates and embedding reuse.

Add:

```text
rag index
rag update
rag status
```

or equivalent commands.

### Exit criteria

After editing one C# file, an update reprocesses only the affected file/chunks plus necessary semantic relationship data.

---

## Phase 7 — Evaluation and tuning

Measure at least:

### Navigation Recall@K

Does a correct file/symbol appear in:

- top 1;
- top 3;
- top 5;
- top 10?

### Mean Reciprocal Rank

Useful for exact navigation queries.

### Context cost

Measure total retrieved tokens before an answer.

This is a first-class metric.

A retrieval method that improves Recall@5 by 1% but doubles injected source may be a regression for this project.

### Tool steps

Measure typical number of retrieval calls before Ornith has enough evidence.

### Answer-grounding audit

For a sample of questions, verify that Ornith's answer is actually supported by retrieved source.

### Exit criteria

Define thresholds from baseline results rather than inventing arbitrary production numbers before measurement.

---

# 23. Suggested evaluation scorecard

Track results in a table like:

```text
Query ID
Category
Correct result @1
Correct result @3
Correct result @5
MRR
Search tokens
Source tokens
Tool calls
Answer correct
Unsupported claims
```

Compare configurations:

```text
structural only
structural + BM25
structural + BM25 + dense
hybrid + reranking
```

Keep the simplest configuration that performs well.

---

# 24. Frontend integration

The retrieval service should not depend on a particular chat UI.

Preferred interfaces:

1. OpenAPI/function calling;
2. MCP if the local frontend supports it cleanly;
3. a thin adapter for Open WebUI.

The frontend should expose the tools to Ornith rather than automatically attaching the entire knowledge base.

Avoid attaching repository Markdown or a generated repository map as permanent model context.

---

# 25. Security and privacy

Even though AmiaReforged is FOSS, build the tool correctly for future private codebases.

Requirements:

- default to local-only network binding;
- do not transmit source to remote embedding services unless explicitly configured;
- exclude secrets and `.env` files;
- allow ignore patterns;
- log paths/tool actions but avoid dumping whole source into logs by default;
- treat repository contents as untrusted text, not executable instructions.

---

# 26. Observability

Provide a debug mode.

For every search query, optionally show:

```text
query
parsed identifier candidates
exact matches
BM25 candidates
vector candidates
fusion scores
final ranking
estimated returned tokens
```

Provide index stats:

```text
files indexed
symbols indexed
chunks indexed
embedded chunks
index size
embedding model
indexed commit
```

This is important because retrieval quality cannot be improved intelligently if ranking is opaque.

---

# 27. Failure behavior

The service must fail clearly.

Examples:

## Symbol ambiguous

Return candidates:

```text
TechniqueFactory
  Project A
  path...

TechniqueFactory
  Project B
  path...
```

Do not guess.

## Index stale

Return the result but expose that the repo HEAD differs from the indexed commit.

## Semantic compilation unavailable

Continue with syntax/lexical retrieval and report that reference resolution is degraded.

## Embedding service unavailable

Fall back to structural + FTS search.

The core navigation system should remain useful without embeddings.

---

# 28. Deliverables

The implementation agent should finish with:

## Code

- Roslyn indexer;
- local retrieval server;
- SQLite schema/migrations;
- embedding adapter;
- tool endpoints;
- incremental update logic;
- evaluation runner.

## Commands

At minimum:

```text
rag index <repo>
rag update
rag status
rag eval
rag serve
```

Names may differ, but equivalent operations must exist.

## Documentation

Document:

- prerequisites;
- indexing;
- updating;
- choosing an embedding model;
- configuring token limits;
- connecting Ornith;
- connecting Open WebUI;
- debugging retrieval;
- rebuilding the index.

## Tests

Include unit/integration tests for:

- C# symbol extraction;
- line ranges;
- exact symbol resolution;
- lexical ranking;
- hybrid ranking;
- deleted-file cleanup;
- incremental indexing;
- context limits.

---

# 29. Definition of done

The RAG is complete enough for daily use when all of the following are true:

1. Ornith can locate exact Amia symbols without source being permanently present in its context.
2. Natural-language feature queries usually surface relevant files/symbols within the first few candidates.
3. Search results are compact and do not automatically dump implementation bodies.
4. Ornith can explicitly read a selected symbol or line range.
5. Ornith can ask for symbol references/usages.
6. C# chunks respect structural boundaries.
7. The index updates incrementally after Git changes.
8. Retrieval token use is measured and bounded.
9. The system remains useful if the embedding model is unavailable.
10. Every returned source span carries path and line provenance.
11. Index freshness is visible.
12. Evaluation shows a clear improvement over plain text/vector chunk retrieval at comparable or lower context cost.

---

# 30. Preferred first implementation pass

The implementation agent should NOT attempt every advanced feature at once.

Build this first:

```text
AmiaReforged.sln
      |
      v
Roslyn indexer
      |
      +--> files
      +--> symbols
      +--> signatures
      +--> line ranges
      +--> project membership
      |
      v
SQLite + FTS5
      |
      +--> exact symbol/path search
      +--> lexical search
      |
      v
small tool server
      |
      +--> repo_search
      +--> repo_symbol
      +--> repo_tree
      +--> repo_read_symbol
      +--> repo_read
      |
      v
Ornith
```

Evaluate that.

Then add:

```text
dense embeddings
      ->
hybrid fusion
      ->
reference graph
      ->
incremental optimization
```

This ordering matters. The structural/lexical system should already solve a large fraction of the "where is this?" problem before semantic machinery is introduced.

---

# 31. Guiding principle

When making implementation decisions, optimize for this:

> Maximize useful evidence per context token.

The repository belongs in the retrieval store.

Paths, symbols, and relationships belong in cheap navigation results.

Only source that Ornith has decided it needs belongs in the active context window.
