# AGENTS.md

## Project

This repository contains a professional multi-branch Point of Sale system.

Primary implementation namespace and product-neutral code naming:

```text
Pos.*
```

Do not introduce `Gengxin` into new implementation namespaces, class names, project names, APIs, database objects, or runtime identifiers.

Existing historical repository/document names do not need to be renamed unless explicitly requested.

---

## Communication

When responding interactively to Alan, begin responses with:

```text
Alan,
```

Prefer small, sequential implementation steps.

For substantial changes, report:

1. what was inspected;
2. what was changed;
3. files created or modified;
4. validation performed;
5. Git status;
6. any blockers or unresolved decisions.

Do not claim a change is correct based only on a summary. Inspect the actual implementation and relevant source-of-truth files.

---

## Repository paths

Windows repository:

```text
D:\gengxin
```

WSL repository:

```text
/mnt/d/gengxin
```

Backend solution:

```text
src/backend/Pos.Backend.sln
```

Backend application:

```text
src/backend/Pos.Api
```

---

## .NET environment

The backend targets .NET 10.

Preferred SDK environment is WSL.

Expected SDK:

```text
.NET SDK 10.x
```

From Windows PowerShell, backend commands should normally be executed through WSL when necessary:

```powershell
wsl bash -lc 'cd /mnt/d/gengxin && dotnet <command>'
```

Do not downgrade the backend to .NET 7 or another installed Windows SDK.

---

## Current backend infrastructure

Backend:

```text
ASP.NET Core 10
C#
.NET 10 LTS
```

Database:

```text
PostgreSQL 17
Npgsql
```

Current direct NuGet dependency:

```text
Npgsql 10.0.3
```

Do not add packages unless the current micro-hito genuinely requires them.

The application already has:

- PostgreSQL connection configuration;
- singleton `NpgsqlDataSource`;
- `X-Request-Id` infrastructure;
- shared internal error handling;
- public API error envelope;
- Idempotency-Key validation;
- `/health`;
- `/health/db`.

Local PostgreSQL integration may be unavailable depending on the current
development environment state.

Previously observed local blockers have included:

- PostgreSQL authentication failure (`SQLSTATE 28P01`);
- connection refused on `localhost:5432` when the local PostgreSQL service/container is not accepting connections.

Treat database connectivity failures as external/local environment issues unless
the current task explicitly concerns database connectivity.

Do not alter credentials, `.env`, Docker configuration, PostgreSQL users, or
secrets merely to make unrelated tests pass.

---

## Secrets

Never print or expose:

- passwords;
- `.env` values;
- connection-string passwords;
- tokens;
- API keys;
- authentication secrets;
- database credentials.

Reading configuration for structural inspection is allowed, but sensitive values must not be echoed.

Prefer `.env.example` or redacted configuration when reporting setup.

---

## Database source of truth

Current physical database version:

```text
db-4
```

Primary files:

```text
database/schema-v0.5-db-4.sql
database/validation-v0.5-db-4.sql
docs/database/modelo-fisico-v0.5-db-4.md
```

db-4 is:

```text
VALIDATED
FROZEN
```

Do not modify db-4.

Do not create db-5 unless Alan explicitly approves a physical schema change.

If implementation appears to require a schema modification:

1. stop that part of the implementation;
2. verify the real db-4 definition;
3. report the conflict;
4. do not silently alter the database model.

Never infer physical column types or constraints from older db versions when db-4 can be inspected directly.

---

## Database conventions

Important established conventions include:

- internal IDs use `BIGINT IDENTITY`;
- public identifiers are used where useful;
- money and quantities use exact numeric types;
- timestamps use `TIMESTAMPTZ`;
- inventory, cash, replenishment, and audit histories use append-oriented/ledger patterns;
- database constraints are final defenses, not replacements for application validation.

Do not introduce floating-point arithmetic for money or quantities.

---

## Transaction contracts

Frozen transaction contracts are authoritative for business semantics.

Important files:

```text
docs/transactions/confirmar-venta-v0.1.md
docs/transactions/confirmar-devolucion-v0.1.md
docs/transactions/confirmar-pedido-proveedor-v0.1.md
docs/transactions/confirmar-compra-v0.1.md
```

Do not silently reinterpret:

- invariants;
- lock order;
- idempotency behavior;
- reconciliation order;
- error semantics;
- atomicity;
- persistence effects.

When API documentation and transaction documentation both apply, preserve the semantics of both.

---

## API contracts

Important API documents:

```text
docs/api/conventions-v0.1.md
docs/api/authorization-and-context-v0.1.md
docs/api/error-model-v0.1.md
docs/api/idempotency-and-preconditions-v0.1.md
docs/api/transport-v0.1.md
docs/api/public-error-codes-v0.1.md
docs/api/commands/confirm-sale-v0.1.md
```

Frozen documents must not be changed merely to fit an implementation.

Implementation must conform to frozen contracts.

---

## Public API principles

The public API exposes capabilities, not database CRUD.

Do not expose internal `BIGINT` IDs as public resource identifiers unless a frozen contract explicitly requires it.

Prefer public IDs at the transport boundary.

Do not expose raw SQL errors, stack traces, internal exceptions, secrets, request hashes, or database implementation details.

Public error envelope:

```json
{
  "error": {
    "code": "...",
    "message": "...",
    "category": "...",
    "retryable": false
  }
}
```

---

## Confirm Sale

Primary API contract:

```text
docs/api/commands/confirm-sale-v0.1.md
```

Endpoint reserved by contract:

```text
POST /api/v1/sales/confirm
```

Do not create a fake or incomplete route merely to demonstrate progress.

Implement the endpoint only when its required context and command flow are coherent.

---

## Confirm Sale ordering

For Confirm Sale, preserve the frozen ordering.

Minimum trusted HTTP/context boundary occurs before historical access.

For SAME KEY terminal `COMPLETED` or `FAILED`:

- compare the request hash;
- replay historical result/error;
- do not re-resolve current branch resources;
- do not revalidate current cash session;
- do not revalidate current customer;
- do not revalidate current price list;
- do not revalidate current products;
- do not revalidate current quotation;
- do not revalidate current permissions;
- do not require the same historical terminal.

For NEW or recoverable `IN_PROGRESS`:

1. establish trusted business context;
2. canonicalize/hash;
3. resolve/reserve idempotency;
4. enforce idempotency ownership/lease rules;
5. resolve branch when necessary;
6. acquire the `(branch_id, client_operation_id)` barrier;
7. search for historical sale;
8. reconcile historical sale if present;
9. only if no historical sale exists, perform current authorization/domain validations.

Do not introduce authorization-before-reconciliation.

---

## Confirm Sale idempotency

Physical scope:

```text
(business_id, operation_type, idempotency_key)
```

For Confirm Sale:

```text
operation_type = 'CONFIRM_SALE'
```

`idempotency_key` and `client_operation_id` are different concepts.

Request hash comparison happens before state-specific behavior.

Same key + different request hash:

```text
SALE_IDEMPOTENCY_KEY_REUSED
```

regardless of whether the row is:

```text
IN_PROGRESS
COMPLETED
FAILED
```

`IN_PROGRESS` lease for Confirm Sale:

```text
30 seconds
```

`COMPLETED` and `FAILED` retention:

```text
30 days
```

`expires_at` is not a locking mechanism.

---

## Confirm Sale canonical request

Approved implementation:

```text
src/backend/Pos.Api/Sales/ConfirmSaleCanonicalRequestSerializer.cs
```

Do not change canonicalization casually.

Rules include:

- deterministic UTF-8 JSON;
- deterministic property order;
- `lines` preserve received order;
- `payments` are canonicalized and sorted by canonical byte representation;
- duplicate payments remain duplicates;
- optional absent properties remain absent;
- strings are not trimmed;
- Unicode is not normalized;
- decimals remain canonical exact strings.

Do not include in the hash:

- `Idempotency-Key`;
- `X-Request-Id`;
- auth headers;
- `business_id`;
- `user_id`;
- `terminal_id`;
- internal database IDs;
- generated folio;
- generated timestamps;
- calculated backend totals;
- result data;
- idempotency state.

---

## Confirm Sale request hash

Approved implementation:

```text
src/backend/Pos.Api/Sales/ConfirmSaleRequestHasher.cs
```

Algorithm:

```text
SHA-256
```

Stored representation:

```text
sha256:<64 lowercase hexadecimal characters>
```

Total length:

```text
71 characters
```

Hash the exact bytes returned by:

```text
ConfirmSaleCanonicalRequestSerializer.Serialize(...)
```

Do not double-hash or reserialize the payload.

---

## Confirm Sale idempotency decision resolver

Approved implementation:

```text
src/backend/Pos.Api/Sales/ConfirmSaleIdempotencyResolver.cs
```

Decision precedence:

```text
existing? -> hash -> status -> lease
```

Different hash:

```text
KeyReused
```

Same hash + `COMPLETED`:

```text
CompletedReplay
```

Same hash + `FAILED`:

```text
FailedReplay
```

Same hash + `IN_PROGRESS` and:

```text
locked_until > now
```

returns:

```text
InProgress
```

Same hash + `IN_PROGRESS` and:

```text
locked_until <= now
```

returns:

```text
RecoveryCheckRequired
```

`RecoveryCheckRequired` does NOT authorize recovery.

It only means persistent recovery checks are now required.

Real recovery still requires database locking/ownership checks, historical-sale reconciliation, and the frozen transaction sequence.

---

## Authentication and authorization

Authentication mechanism/provider is NOT yet frozen.

Do not independently choose or introduce:

- ASP.NET Identity;
- JWT;
- bearer tokens;
- cookies;
- OAuth;
- OIDC;
- refresh tokens;
- terminal credentials;
- authentication database tables.

unless the current task explicitly closes that architectural decision.

Current conceptual trusted context includes:

- business;
- user/actor;
- terminal.

Authentication presence and current command authorization are different concepts.

Do not use current authorization to hide valid historical reconciliation when the frozen contract says reconciliation comes first.

---

## Frontend architecture

Current direction:

```text
React 19
TypeScript
Vite 8
Tailwind CSS 4
shadcn/ui
TanStack Query 5
React Router
```

There are conceptually separate clients:

- POS Desktop;
- Web Logistics/Admin.

Hardware integrations belong to the Desktop client.

Do not begin implementing frontend infrastructure during backend-only micro-hitos unless explicitly requested.

---

## Backend architecture

Use a modular monolith.

Prefer explicit, understandable application flows.

For transaction-critical operations, explicit SQL with Npgsql is acceptable and preferred where precise PostgreSQL locking/transaction behavior matters.

Do not introduce an ORM merely for convenience during a transaction-critical micro-hito.

Do not introduce repositories, abstractions, generic frameworks, CQRS infrastructure, mediator libraries, or service layers unless they solve a current concrete requirement.

Avoid speculative architecture.

---

## PostgreSQL concurrency

Follow transaction contracts exactly.

Do not invent:

- arbitrary lock order;
- SERIALIZABLE isolation;
- process-local mutexes;
- `SemaphoreSlim` as a database concurrency substitute;
- advisory-lock hashing algorithms before their contract/implementation is intentionally chosen.

Physical UNIQUE constraints remain final race-condition defenses.

When using `ON CONFLICT`, row locks, or advisory locks, reason explicitly about concurrent transactions.

A lease expiration alone never proves that no transaction remains active.

---

## SQL

All external values must be parameterized.

Never build SQL with:

- string interpolation;
- concatenated user input;
- unescaped external identifiers.

Prefer explicit Npgsql parameter types when useful.

Inspect db-4 before writing SQL against a table.

Do not guess column names.

---

## Time

For deterministic pure logic, inject/pass time explicitly.

Do not call the system clock from pure decision functions.

For persisted transaction timestamps and leases, prefer PostgreSQL transaction time when appropriate to the transaction contract.

Do not mix multiple independent clocks without a reason.

---

## Cancellation

Async database/application operations should propagate `CancellationToken`.

Do not swallow `OperationCanceledException`.

---

## Error handling

Do not create new public error codes unless supported by the API/error contracts.

Do not translate every database exception into a domain error.

Technical failures must not automatically become deterministic `FAILED` idempotency results.

Never expose raw exception messages to API consumers.

---

## Health endpoints

Expected:

```text
GET /health
```

returns:

```json
{"status":"ok"}
```

Do not make unrelated micro-hitos depend on `/health/db` succeeding while local PostgreSQL connectivity is unavailable.

---

## Testing

Temporary harnesses are allowed outside the repository.

Do not leave temporary harness files in the repository unless explicitly requested.

For every implementation micro-hito:

- test the exact new behavior;
- test boundary conditions;
- test invariants;
- verify existing behavior was not accidentally changed;
- build the full backend solution.

Expected build:

```text
0 Warning(s)
0 Error(s)
```

---

## Git workflow

Git discipline is mandatory.

Before work:

```bash
git status --short
git log -3 --oneline
```

Do not assume a clean tree.

Preserve unrelated user work.

Never use destructive commands such as:

```text
git reset --hard
git clean -fd
git checkout -- .
git restore .
rebase
amend
force push
```

unless Alan explicitly authorizes them.

Do not stage, commit, or push unless the current instruction explicitly authorizes it.

Default implementation tasks end with:

```text
NO staging
NO commit
NO push
```

---

## Before staging

When staging is explicitly authorized, stage only files belonging to the current micro-hito.

Then inspect:

```bash
git diff --cached --check
git diff --cached --stat
git diff --cached --summary
git status --short
```

For important files also inspect:

```bash
git ls-files --stage <file>
```

Expected ordinary source-file mode:

```text
100644
```

Do not approve a commit based only on a generated summary.

Inspect the actual diff/content first.

---

## Commits

Use one coherent semantic change per commit.

Commit messages should follow the existing style, for example:

```text
feat(sales): ...
feat(api): ...
refactor(api): ...
chore(backend): ...
test(database): ...
docs(...): ...
```

Do not bundle unrelated changes.

After commit verify:

```bash
git status --short
git log -1 --oneline
```

Do not automatically push after commit unless explicitly instructed.

After push verify:

```bash
git status -sb
```

Expected synchronized state:

```text
## main...origin/main
```

---

## Line endings

The repository may be used simultaneously from Windows and WSL.

Git warnings about automatic `LF` / `CRLF` conversion are not by themselves a
reason to alter source files or repository configuration.

Examples include:

```text
LF will be replaced by CRLF
CRLF will be replaced by LF
```

Do not change `.gitattributes` or global Git line-ending configuration during an
unrelated micro-hito.

---

## Scope discipline

Every implementation request should be treated as a micro-hito.

Do only what the current micro-hito requires.

Do not opportunistically implement:

- future endpoints;
- auth;
- extra tables;
- migrations;
- frontend;
- generic infrastructure;
- unrelated refactors;
- logging frameworks;
- caching;
- deployment changes.

If a useful improvement is outside scope, report it as a future step instead of silently implementing it.

---

## Documentation conflicts

When code, frozen docs, and db-4 appear inconsistent:

1. inspect the current actual files;
2. identify whether the conflict is physical, API, or transactional;
3. do not silently choose one interpretation;
4. report the exact conflict;
5. avoid modifying frozen sources without explicit approval.

Current frozen contracts and db-4 take precedence over older historical drafts.

---

## Definition of done for a micro-hito

An implementation micro-hito is not complete until:

- requested implementation exists;
- actual files were inspected;
- relevant contract/schema was checked;
- tests/harness pass;
- backend restore succeeds;
- backend build succeeds with zero warnings/errors;
- no unintended package was added;
- `/health` remains functional when relevant;
- `git diff --check` is clean;
- Git status is reported;
- no unauthorized staging/commit/push occurred.

Documentation-only, repository-metadata, or other non-runtime micro-hitos should
run the validations relevant to the files they actually change; they do not
require unrelated backend restore/build/health checks unless those checks are
material to the task.

If integration testing is blocked by external credentials or infrastructure, report the blocker explicitly instead of altering unrelated configuration.
