# Library Management System — Production Deployment

## Architecture

The three ASP.NET Core (.NET 10) microservices I've created function as separate processes on a single
AWS Elastic Beanstalk instance, fronted by one nginx reverse proxy. Each service
has its own PostgreSQL database on a shared RDS instance.

```
                        ┌─────────────────────────────────────────┐
                        │   Elastic Beanstalk (t3.medium, single) │
                        │                                         │
  Internet ──▶ nginx ───┼──▶ UserService        (:5000, "web")    │
                        │──▶ CatalogService      (:5002, "catalog")│
                        │──▶ ReservationService  (:5003, "reservation")│
                        └─────────────────────────────────────────┘
                                        │
                                        ▼
                        ┌─────────────────────────────────────────┐
                        │  RDS PostgreSQL (db.t3.micro, private)   │
                        │  UserServiceDb | CatalogServiceDb | ReservationServiceDb │
                        └─────────────────────────────────────────┘
```

- nginx routes `/` → UserService, `/catalog/*` → CatalogService, `/reservations/*` → ReservationService.
- All three processes talk to each other over `localhost` (same instance), never over the public internet.
- RDS is not publicly accessible; only the EB instance's security group can reach it.
- Services validate JWTs issued by UserService using a shared secret (`Jwt__Secret`), so a
  token from login works across all three services.

## Public Endpoints

Base URL: `http://library-microservices-env.eba-vtsnepwz.us-east-1.elasticbeanstalk.com`

| Service | Swagger | Health |
|---|---|---|
| UserService | `/swagger` | `/api/ping` |
| CatalogService | `/catalog/swagger` | `/catalog/api/ping` |
| ReservationService | `/reservations/swagger` | `/reservations/api/ping` |

## Environment Configuration

All three processes share one set of Elastic Beanstalk environment variables;
each service reads its own connection-string key so they don't collide:

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__UserDb` | UserService's Postgres connection string |
| `ConnectionStrings__CatalogDb` | CatalogService's Postgres connection string |
| `ConnectionStrings__ReservationDb` | ReservationService's Postgres connection string |
| `Jwt__Secret` / `Jwt__Issuer` / `Jwt__Audience` | Shared JWT config (UserService issues, ReservationService validates) |
| `ServiceUrls__UserService` | `http://localhost:5000` |
| `ServiceUrls__CatalogService` | `http://localhost:5002` |
| `ServiceUrls__ReservationService` | `http://localhost:5003` |

## Demo: Full Auth + Reservation Workflow

```bash
EB=http://library-microservices-env.eba-vtsnepwz.us-east-1.elasticbeanstalk.com

# 1. Health check all three services
curl -s $EB/api/ping | jq
curl -s $EB/catalog/api/ping | jq
curl -s $EB/reservations/api/ping | jq

# 2. Register a patron
curl -s -X POST $EB/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "demo@library.com",
    "password": "Demo123!",
    "firstName": "Demo",
    "lastName": "Patron",
    "phoneNumber": "+1-555-000-1234"
  }' | jq

# 3. Login and capture the JWT
PATRON_TOKEN=$(curl -s -X POST $EB/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@library.com","password":"Demo123!"}' \
  | jq -r .accessToken)

# 4. Profile endpoint — UserService calls ReservationService internally
#    for activeReservations / borrowingHistory counts
curl -s $EB/api/users/profile -H "Authorization: Bearer $PATRON_TOKEN" | jq

# 5. Browse the catalog — no auth required
curl -s $EB/catalog/api/catalog/books | jq

# 6. Create a reservation — ReservationService validates the user via
#    UserService, fetches title/author + decrements availability via
#    CatalogService
BOOK_ID="<pick a bookId from step 5>"
RESV=$(curl -s -X POST $EB/reservations/api/reservations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $PATRON_TOKEN" \
  -d "{\"bookId\":\"$BOOK_ID\"}")
echo "$RESV" | jq
RESV_ID=$(echo "$RESV" | jq -r .reservationId)

# 7. Patron attempts checkout — rejected, checkout is librarian-only
curl -s -X POST $EB/reservations/api/reservations/$RESV_ID/checkout \
  -H "Authorization: Bearer $PATRON_TOKEN" -d '{}' \
  -w "\nHTTP %{http_code}\n"
# => 403 FORBIDDEN

# 8. Login as the seeded librarian and check out for real
LIB_TOKEN=$(curl -s -X POST $EB/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"librarian@library.com","password":"Librarian@123"}' \
  | jq -r .accessToken)

curl -s -X POST $EB/reservations/api/reservations/$RESV_ID/checkout \
  -H "Authorization: Bearer $LIB_TOKEN" -d '{}' | jq
# => 200, status CHECKED_OUT

# 9. Return the book (librarian only)
curl -s -X POST $EB/reservations/api/reservations/$RESV_ID/return \
  -H "Authorization: Bearer $LIB_TOKEN" \
  -d '{"condition":"GOOD"}' | jq
```

## What This Demonstrates

- **Authentication**: register → login → JWT issuance (UserService).
- **Authorization**: unauthenticated catalog browsing vs. JWT-required reservations
  vs. librarian-only checkout/return (403 for patrons, 200 for librarians).
- **Inter-service communication**: profile (User→Reservation), reservation creation
  (Reservation→User validation, Reservation→Catalog availability update and title/author
  lookup), all over `localhost` inside one EB instance.
- **Data persistence**: all state lives in RDS Postgres, independent of app restarts.
- **Background job**: `WaitlistExpiryJob` runs on a timer inside ReservationService
  and logs its activity (verified via `journalctl -u reservation.service`).

## Fixed Since Initial Deploy

- `bookTitle`/`bookAuthor` previously came back empty in reservation/waitlist responses
  because `ReservationService` had no way to read book details from `CatalogService`
  (only a PATCH-availability call existed). Added `ICatalogServiceClient.GetBookAsync`
  (a `GET /api/catalog/books/{bookId}` call) and wired it into both `CreateReservation`
  and `JoinWaitlist` so title/author are populated from CatalogService at creation time.

## Known Cosmetic Issues (not blocking, not yet fixed)

- `PingController` health responses hardcode a stale `port` field and the literal
  string `"(InMemory)"` regardless of actual DB backend; the `inMemory` boolean
  field is accurate, the surrounding text is not.
