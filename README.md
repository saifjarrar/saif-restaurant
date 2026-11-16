# Saif Restaurant

A bilingual restaurant web application for browsing the menu, booking tables, and managing kitchen orders. The project combines an Angular frontend and an ASP.NET Core API in a single repository.

[Live demo](https://saif-restaurant-frontend.onrender.com) · [Customer experience](#customer-experience) · [Run locally](#run-locally) · [API reference](#api-reference)

![Saif Restaurant hero photograph](frontend/public/images/hero.jpg)

## What I built

I built the customer website, a separate workflow for restaurant staff, and the API that connects them. The project covers responsive interface design, Arabic and English localization, authentication and authorization, data modeling and migrations, and deployment.

### Customer experience

- **Menu and offers** — browse dishes by category, see photos and prices, and explore promotional offers.
- **Accounts** — sign up or log in through validated forms that show progress and explain failures.
- **Reservations** — pick a day, time, party size, and seating area, then submit the booking to the API.
- **Contact** — submit a message with a subject and reply address. Email delivery requires a configured provider.
- **Arabic and English** — switch languages without leaving the page. The app remembers the preference and applies right-to-left layout in Arabic.

### Restaurant operations

- **Dashboard** — see kitchen tickets, tonight’s reservations, and the most ordered dishes.
- **Order entry** — select a table and dish quantities. The API validates dish names, looks up unit prices, and calculates totals and estimated preparation times.
- **Kitchen workflow** — advance tickets from Received to In the Kitchen to Served, or cancel eligible orders. The orders view includes status counts, filters, and a receipt-style detail panel.
- **Staff access** — ASP.NET Core Identity manages accounts. JWT bearer authentication and API role checks protect staff operations. Registration creates a customer account, and the API requires the Admin role for dashboard and order management.

## Stack

| Layer | Technologies |
| --- | --- |
| Frontend | Angular 22, TypeScript, RxJS, standalone components, lazy-loaded routes, reactive forms, HTML and CSS |
| API | ASP.NET Core on .NET 10, controllers, DTO validation, OpenAPI and Scalar |
| Authentication | ASP.NET Core Identity, JWT bearer tokens, Customer and Admin roles |
| Data | Entity Framework Core, SQL Server or PostgreSQL via Npgsql, provider-specific migrations |
| Hosting | Render Static Site, Docker web service, Neon PostgreSQL |
| Contact delivery | MailKit SMTP or the Brevo HTTPS API |

The browser calls the API over HTTPS in development and production, using the configured API URL. For authenticated calls, an Angular interceptor attaches the JWT. The API validates the token and role before accessing application data through EF Core.

## Repository layout

```text
saif-restaurant/
├── frontend/
│   ├── public/                 # Images and public assets
│   ├── src/app/
│   │   ├── pages/              # Customer pages, dashboard, and app layout
│   │   ├── features/           # Orders and offers
│   │   ├── i18n/               # Arabic translations and language state
│   │   ├── guards/             # Frontend route access
│   │   ├── interceptors/       # Bearer-token handling
│   │   └── services/           # Session, menu, and API integration
│   ├── angular.json
│   └── package.json
└── backend/
    ├── Dockerfile
    ├── saifRestaurantApi.slnx
    └── saifRestaurantApi/
        ├── Controllers/        # Account, dashboard, orders, reservations, contact
        ├── Data/               # EF Core contexts and PostgreSQL tooling setup
        ├── Dto/                # API request and response models
        ├── Migrations/         # SQL Server migrations and a Postgres subfolder
        └── Models/             # Persisted entities
```

## Run locally

You’ll need Git, the **.NET 10 SDK**, **Node.js 24.21.0** (the version used for deployment), npm, and a database. The example below uses SQL Server LocalDB on Windows. Use a database you control for local development.

```powershell
git clone https://github.com/saifjarrar/saif-restaurant.git
cd saif-restaurant
```

### 1. Start the API

From the repository root, use PowerShell to create a local signing key and configure the database. User Secrets stores these values outside the repository.

```powershell
cd backend/saifRestaurantApi
dotnet restore
dotnet dev-certs https --trust

$jwtBytes = [byte[]]::new(32)
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtBytes)
$rng.Dispose()
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($jwtBytes))
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=SaifRestaurant;Trusted_Connection=True"

dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef database update --context AppDBContext
dotnet run --launch-profile https
```

If `dotnet-ef` is already installed, use your existing compatible 10.x version. For PostgreSQL, set `ConnectionStrings__Postgres` in your shell and run the migrations using `--context PostgresAppDBContext` instead. The app selects PostgreSQL when that connection string is set.

The development API listens at `https://localhost:7118`. Its OpenAPI document and Scalar UI are available in the Development environment at `/openapi/v1.json` and `/scalar/v1`.

### 2. Start the Angular frontend

Open a second terminal at the repository root.

```powershell
cd frontend
npm ci
npm start
```

Open `http://localhost:4200`. The Angular development configuration calls the local API. For a production build, run `npm run build` in `frontend`. The browser output is generated in `frontend/dist/anglogin/browser`.

## API reference

Paths below are relative to the API origin. Protected requests use the `Authorization: Bearer <token>` header.

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/Auth/register` | Public | Create a customer account |
| POST | `/api/Auth/login` | Public | Authenticate and receive a JWT |
| GET | `/api/Dashboard` | Admin | Load dashboard data, order tickets, and reservations |
| POST | `/api/Dashboard/orders` | Admin | Create an order ticket |
| PUT | `/api/Dashboard/orders/{id}/advance` | Admin | Move a ticket to the next status |
| PUT | `/api/Dashboard/orders/{id}/cancel` | Admin | Cancel an eligible ticket |
| POST | `/api/Dashboard/reservations` | Signed in | Submit a table reservation |
| POST | `/api/contact` | Public | Submit a contact message for email delivery |

## Deployment

The live demo uses a Render static frontend, a separate Dockerized API, and Neon PostgreSQL. The following settings apply when deploying from **this monorepo**.

| Render service | Root directory | Build and routing |
| --- | --- | --- |
| Angular Static Site | `frontend` | `npm ci && npm run build`. Publish `dist/anglogin/browser`. Rewrite `/*` to `/index.html` for direct links to Angular routes. |
| ASP.NET Core Web Service | `backend` | Docker runtime. Use `backend/Dockerfile` in the repository with `backend` as its build context. The container listens on port `10000`. |

Apply the PostgreSQL migrations before using a fresh database. If you deploy the API to a different URL, update [the production API URL](frontend/src/app/config/api-url.production.ts) before building the frontend.

| Environment variable | Purpose |
| --- | --- |
| `ConnectionStrings__Postgres` | PostgreSQL connection string for the API |
| `Jwt__Key` | Private signing key, at least 32 bytes for HS256 |
| `Cors__AllowedOrigins__0` | Exact deployed frontend origin, without a trailing slash |
| `Contact__Provider` | Set to `Brevo` to use HTTPS email delivery |
| `Contact__BrevoApiKey` | Brevo credential for the API server |
| `Contact__SenderEmail` | Verified Brevo sender |
| `Contact__RecipientEmail` | Inbox for contact messages |

For local SMTP, use `Contact:SmtpEmail` and `Contact:SmtpPassword` in User Secrets. Keep API keys, passwords, and database credentials in User Secrets locally and in Render’s environment settings in production. The menu and offers are currently defined in the frontend, while account, reservation, and order data are persisted by the API.
