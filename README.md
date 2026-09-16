# Work Request Management

ASP.NET Core + EF Core + SQLite backend and React + TypeScript frontend.
Create and edit requests, search by title, filter by status, and advance through New → In Progress → Completed. Required fields and failed operations show validation or error messages.

## Run

Requires .NET 9 SDK and Node.js 22.12+.

From the `work-requests` folder:

```sh
dotnet restore WorkRequests.sln --configfile NuGet.Config
dotnet run --project backend/WorkRequests.Api.csproj
```

In a second terminal:

```sh
cd frontend
npm ci
npm run dev
```

Open http://127.0.0.1:5173. API: http://localhost:5080/api/requests.
SQLite is created automatically; no database setup is needed.

If MSB3021 reports a locked executable, stop the existing backend with Ctrl+C or stop debugging in your IDE before restarting.

## Verify

From the repository root:

```sh
dotnet test WorkRequests.sln
npm --prefix frontend run build
```

## Notes

- SQLite uses `EnsureCreated`; existing databases are upgraded to remove the old priority column. Larger datasets would need pagination and database-side search.
- Codex assisted with visuals on the frontend side, SQLite integration, tests and this ReadMe file.
