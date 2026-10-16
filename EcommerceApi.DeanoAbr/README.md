# Ecommerce API — C# Academy project 18

A retail backend for **Bean & Barrel Coffee Roasters**: an ASP.NET Core Web API over EF Core
(SQLite) with **Products**, **Categories** and **Sales** (many-to-many through `SaleItems`),
pagination, filtering/sorting, soft deletes, immutable product prices, a seeded catalogue, an xunit
test suite, a committed Postman collection, and a console client.

---

## Running it

```bash
cd EcommerceApi
dotnet run
```

The API listens on **http://localhost:5276** (`Properties/launchSettings.json` pins the port) and on
first run creates `ecommerce.db` and seeds it. Swagger UI is enabled in Development:
<http://localhost:5276/swagger>.

Nothing else is required — there is no authentication and no manual data entry step.

```bash
dotnet test EcommerceApi.slnx                  # 43 tests, all green
cd EcommerceApi.Console && dotnet run          # menu-driven client for the running API
```

---

## Requirements traceability

| Brief requirement | Where it lives |
|---|---|
| ASP.NET Core Web API | `EcommerceApi/Program.cs`, `EcommerceApi/EcommerceApi.csproj` (`Microsoft.NET.Sdk.Web`) |
| Entity Framework, SQLite | `EcommerceApi/Data/EcommerceDbContext.cs` (`UseSqlite`), `EcommerceApi/Migrations/` |
| Dependency Injection | `Program.cs`: `AddDbContext`, `AddScoped<ICategoryService, CategoryService>` (+ Product, Sale), `AddExceptionHandler`, controllers |
| At least three tables: Products, Categories, Sales | `EcommerceApi/Models/{Category,Product,Sale}.cs` plus the `SaleItem` join table = 4 tables |
| Products ↔ Sales many-to-many | `SaleItem` (`SaleId` + `ProductId`), configured in `EcommerceDbContext.OnModelCreating` |
| Products have a price; several products per sale | `Product.Price`, `Sale.SaleItems` (one sale holds many lines) |
| Postman collection with all requests, in the PR | `docs/EcommerceApi.postman_collection.json` |
| No UI needed to consume the API | *challenge* satisfied anyway: `EcommerceApi.Console/` |
| Pagination on GetProducts and GetSales | `EcommerceApi/Dtos/PagedResult.cs`, `EcommerceApi/Services/Paging.cs`, `?page=&pageSize=` |
| Prevent deletion of records (soft deletes) | `IsDeleted` on Category, Product and Sale; every `DELETE` action sets it |
| Do not update product prices | no `PUT /api/products/{id}` (returns 405), `SaleItem.UnitPrice` snapshot |
| Seed data from the beginning | `EcommerceApi/Data/DbSeeder.cs` (5 categories, 12 products, 3 sales), run at startup |
| Challenge: filtering and sorting | `ProductQuery`, `SalesQuery` — `categoryId`, `minPrice`, `maxPrice`, `search`, `from`, `to`, `sortBy`, `sortDir` |
| Challenge: console UI consuming the API | `EcommerceApi.Console/` |

### Project layout

```
EcommerceApi/                  Web API (the only project with business logic)
  Controllers/                 routing + status codes only, one thin action per endpoint
  Services/                    business rules, EF queries, exceptions, exception handler, paging
  Dtos/                        request/response contracts and PagedResult<T>
  Models/                      Category, Product, Sale, SaleItem
  Data/                        EcommerceDbContext, DbSeeder
  Migrations/                  InitialCreate
EcommerceApi.Tests/            xunit + WebApplicationFactory<Program> over in-memory SQLite (43 tests)
EcommerceApi.Console/          HTTP-only console client (no project reference to the API)
docs/                          Postman collection
```

---

## Endpoints

Every error response is `{"error": "<message>"}`.

### Categories — `/api/categories`

| Verb + route | Success | Failures |
|---|---|---|
| `GET /api/categories` | 200 list ordered by name, each with `productCount` | — |
| `GET /api/categories/{id}` | 200 | 404 `Category {id} was not found.` |
| `POST /api/categories` | 201 + `Location` | 400 `Category name is required.` / 409 `A category named '{name}' already exists.` |
| `PUT /api/categories/{id}` | 200 | 400 / 404 / 409 as above |
| `DELETE /api/categories/{id}` | 204 (soft) | 404 / 409 `Category {id} still has {n} active products.` |

### Products — `/api/products`

| Verb + route | Success | Failures |
|---|---|---|
| `GET /api/products?page&pageSize&categoryId&minPrice&maxPrice&search&sortBy&sortDir` | 200 `PagedResult` | 400 `page must be at least 1.`, `pageSize must be between 1 and 50.`, `sortBy must be one of: name, price, stock, createdAt.`, `sortDir must be 'asc' or 'desc'.` |
| `GET /api/products/{id}` | 200 | 404 `Product {id} was not found.` |
| `POST /api/products` | 201 + `Location` | 400 `Category {id} does not exist.` / `Price must be greater than 0.` / `Product name is required.` / `Stock quantity cannot be negative.`, 409 duplicate name |
| `PUT /api/products/{id}` | — | **405** — prices are immutable, see below |
| `DELETE /api/products/{id}` | 204 (soft) | 404 |

`sortBy` defaults to `createdAt` (newest first); `sortDir` otherwise defaults to `asc`.

### Sales — `/api/sales`

| Verb + route | Success | Failures |
|---|---|---|
| `GET /api/sales?page&pageSize&customer&from&to&sortBy&sortDir` | 200 `PagedResult` | 400 the paging messages above plus `from must be on or before to.` |
| `GET /api/sales/{id}` | 200 | 404 `Sale {id} was not found.` |
| `POST /api/sales` | 201 + `Location`, total computed, stock decremented | 400 `A sale must contain at least one item.` / `Each product may appear only once in a sale.` / `Product {id} does not exist.` / `Customer name is required.` / `Quantity must be at least 1.`, 409 `Only {n} of '{name}' are in stock.` |
| `PUT /api/sales/{id}` | — | 405 (a wrong sale is voided and re-entered, never edited) |
| `DELETE /api/sales/{id}` | 204 — voids the sale **and returns its stock** | 404 |

`sortBy` for sales is `date` (default, newest first), `total` or `customer`.

---

## Design decisions a reviewer will ask about

**Prices are immutable, and sales carry their own snapshot.** There is deliberately no update action
for a product: the brief says not to change prices, and "what would happen if you made a sale and
later updated the price?" is answered in the data model. Each `SaleItem` stores `ProductName` and
`UnitPrice` as they were at the moment of sale, so a sale's total can never be rewritten by later
catalogue changes — the sale is a receipt. `PUT /api/products/{id}` returning 405 is asserted by a
test rather than left as an accident.

**Soft deletes, without EF global query filters.** Retail keeps records: Categories, Products and
Sales carry `IsDeleted`, and services filter with explicit `!IsDeleted` predicates. A global
`HasQueryFilter` on `Product` was rejected because `SaleItem.Product` is a required navigation — EF
warns about it and historical sale lines would silently disappear from reports. (Sale lines don't
join back to Products at read time anyway; they render from their snapshot.)

**Voiding a sale restores stock.** `POST /api/sales` checks stock and decrements it inside the same
`SaveChanges` call; `DELETE /api/sales/{id}` voids the sale and adds the quantities back. A voided
sale disappears from reads, exactly as a soft-deleted product does.

**Products belong to one category.** The brief's many-to-many requirement is Products ↔ Sales;
making products multi-category would add a join table no endpoint needs yet.

**One place maps errors to status codes.** Services throw `ValidationException` /
`NotFoundException` / `ConflictException`; `ApiExceptionHandler` turns them into 400 / 404 / 409 with
`{"error": "..."}`. Model-state failures are routed through the same shape
(`InvalidModelStateResponseFactory`), so a client parses one error format.

**`decimal` for money.** SQLite stores `decimal` as TEXT; `HasPrecision(10, 2)` documents the intent,
and the price-range and price-sort tests assert that comparisons and ordering still behave.

---

## Verification

### Tests

```bash
dotnet test EcommerceApi.slnx
```

**43 tests, 0 failed**, in three classes that exercise real HTTP through
`WebApplicationFactory<Program>` against in-memory SQLite (the same provider as production, not the
InMemory one):

- `CategoriesEndpointTests` (8) — list/get/create/duplicate/invalid/update/soft delete/delete guard
- `ProductsEndpointTests` (18) — pagination metadata and consistency, paging and sort validation,
  category/price/search filters, ordering, create validation, 405 on `PUT`, soft delete
- `SalesEndpointTests` (17) — seeding, paging/filter/sort, snapshots, total computation, stock
  decrement on sale and restore on void, insufficient stock, 404s, 405 on `PUT`

Each slice was written test-first, and the important behaviours were re-checked by breaking the
implementation and confirming the test goes red (e.g. removing the stock decrement fails
`Create_DecrementsStock` and `Delete_VoidsSaleAndRestoresStock`).

### Postman collection

`docs/EcommerceApi.postman_collection.json` — 30 requests covering every endpoint, in execution
order, with **68 stored assertions** including negative paths (400, 404, 409, 405) that assert the
error text verbatim. Requests chain ids through collection variables (`categoryId`, `productId`,
`saleId`), and `baseUrl` has no trailing slash.

Verified headlessly with Newman (the engine inside Postman):

```bash
cd EcommerceApi && dotnet run           # terminal 1
npx --yes newman run docs/EcommerceApi.postman_collection.json
```

```
│                requests │               30 │                0 │
│              assertions │               68 │                0 │
```

Exit code `0`. Documented result: **30 requests, 68 assertions, 0 failures.** With the API stopped
the same command reports `connect ECONNREFUSED 127.0.0.1:5276` for every request — the failure path.

To drive it in the Postman GUI: import the JSON, confirm `baseUrl` is `http://localhost:5276`, then
hover the collection → `⋯` → **Run** with the defaults. Note that Postman paints *any* 4xx red, so
the `400`/`404`/`409`/`405` rows look like failures while passing — the real signal is
`Failed: 0` / `Errors: 0`. The run creates and then retires its own category and product and voids its
own sale, so it is safe to repeat on a seeded database.

**Honesty note:** the collection was verified with Newman, not clicked through in the Postman GUI.

### Console client

```bash
cd EcommerceApi.Console && dotnet run
```

Browse products with next/previous paging, search and filter, view details, list categories, create
categories and products, record a sale line by line, browse sales, inspect a sale, void a sale and
retire a product. It talks HTTP only (`Services/ApiClient.cs` — no project reference to the API, so
the JSON contract stays honest), and every failure prints the API's own error message.

### Continuous integration

`.github/workflows/ci.yml` restores, builds and tests `EcommerceApi.slnx` in Release on
`ubuntu-latest` with .NET 10.0.401 — the same three commands were run locally before pushing.
