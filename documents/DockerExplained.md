# 🐳 Docker & Docker Compose — Line-by-Line Teaching Guide

---

## Part 1: The Dockerfile

Think of a **Dockerfile** as a recipe. It tells Docker **how to build an image** of your application — step by step, layer by layer.

This Dockerfile uses a technique called **multi-stage build** (two `FROM` instructions), which keeps the final image small by separating the *build tools* from the *runtime*.

---

### 🔨 Stage 1 — Build Stage (lines 1–11)

```dockerfile
# ---- Build Stage ----
```
> **Line 1:** A comment. Comments start with `#`. This labels the first stage.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
```
> **Line 2:** `FROM` picks a **base image** — like choosing which OS and tools to start with. Here we pick Microsoft's **.NET 8 SDK image**, which contains everything needed to *compile* C# code (compilers, NuGet, MSBuild, etc.). `AS build` gives this stage a name so we can reference it later.

```dockerfile
WORKDIR /src
```
> **Line 3:** `WORKDIR` sets the **working directory** inside the container to `/src`. All following commands run from here. If the folder doesn't exist, Docker creates it automatically.

```dockerfile
COPY StoreApi.csproj .
```
> **Line 4:** `COPY` copies a file from your computer (the *host*) into the container. We copy **only the `.csproj` file first** — this is a trick! Since the project file rarely changes, Docker can **cache** the next step and skip it on rebuilds.

```dockerfile
RUN dotnet restore
```
> **Line 5:** `RUN` executes a command inside the container. `dotnet restore` downloads all **NuGet packages** listed in the `.csproj`. Because we copied the `.csproj` separately, this layer is cached unless dependencies change — saving time on rebuilds.

```dockerfile
COPY . .
```
> **Line 6:** Now we copy **everything else** (source code, config files, etc.) into the container. This is done *after* restore so that changing a `.cs` file doesn't invalidate the restore cache.

```dockerfile
RUN dotnet publish -c Release -o /app/publish --no-restore
```
> **Line 7:** Compile and publish the app:
> - `-c Release` — build in **Release mode** (optimized, no debug symbols)
> - `-o /app/publish` — output the published files to `/app/publish`
> - `--no-restore` — skip restore since we already did it above
>
> After this line, a fully compiled app sits at `/app/publish`.

---

### 🚀 Stage 2 — Runtime Stage (lines 13–27)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
```
> **Line 13:** Start a **brand new image**, this time using the **ASP.NET runtime** image. This is much **smaller** than the SDK image (~220 MB vs ~900 MB) because it only contains what's needed to *run* the app, not build it. The build tools from Stage 1 are thrown away.

```dockerfile
WORKDIR /app
```
> **Line 14:** Set the working directory to `/app` in this new container.

```dockerfile
EXPOSE 8080
```
> **Line 15:** `EXPOSE` is **documentation** — it tells humans and tools that this container listens on port **8080**. It doesn't actually open the port; that's done with `-p` at run time (or `ports:` in Compose).

```dockerfile
COPY --from=build /app/publish .
```
> **Line 16:** This is the **multi-stage magic**. `--from=build` reaches back into Stage 1 and copies the published output from `/app/publish` into the current directory (`/app`). Only the compiled DLLs and configs come over — no source code, no SDK.

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
```
> **Line 17:** `ENV` sets an **environment variable** inside the container. This tells ASP.NET Core to listen on **port 8080** on all network interfaces (`+` means any IP address).

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
```
> **Line 18:** Sets the environment to **Production**. This controls which `appsettings.{env}.json` file is loaded and disables developer features like Swagger UI and detailed error pages.

```dockerfile
ENTRYPOINT ["dotnet", "StoreApi.dll"]
```
> **Line 19:** `ENTRYPOINT` is the **command that runs when the container starts**. It launches your application by running `dotnet StoreApi.dll`. The JSON array form (`["dotnet", "StoreApi.dll"]`) is preferred because it runs the process directly (no shell wrapping).

---

### 📊 Visual Summary of the Dockerfile

```
┌─────────────────────────────────────┐
│  Stage 1: BUILD  (sdk:8.0)         │
│                                     │
│  1. Copy .csproj → restore packages│
│  2. Copy source  → publish app     │
│  3. Output: /app/publish/          │
└──────────────┬──────────────────────┘
               │  COPY --from=build
               ▼
┌─────────────────────────────────────┐
│  Stage 2: RUNTIME  (aspnet:8.0)    │
│                                     │
│  • Only compiled DLLs              │
│  • Listens on port 8080            │
│  • Runs: dotnet StoreApi.dll       │
│  • ~220 MB (vs ~900 MB with SDK)   │
└─────────────────────────────────────┘
```

---

---

## Part 2: The docker-compose.yml

**Docker Compose** lets you define and run **multiple containers** as a single application. Instead of running long `docker run` commands, you describe everything in a YAML file.

---

### 🗄️ Service 1 — SQL Server Database

```yaml
services:
```
> **Line 1:** The top-level key. Everything under `services:` defines a **container** to run.

```yaml
  sqlserver:
```
> **Line 2:** The **name** of the first service. This also becomes the **DNS hostname** inside the Docker network — other containers can reach it using `sqlserver` as the address.

```yaml
    image: mcr.microsoft.com/mssql/server:2022-latest
```
> **Line 3:** Instead of building from a Dockerfile, we pull a **pre-built image** — Microsoft's official **SQL Server 2022** image for Linux.

```yaml
    container_name: storeapi-db
```
> **Line 4:** Gives the container a **fixed name** (`storeapi-db`) instead of a random one. Makes it easier to find with `docker ps`.

```yaml
    environment:
      MSSQL_SA_PASSWORD: "Strong@Passw0rd!"
      ACCEPT_EULA: "Y"
      MSSQL_PID: "Developer"
```
> **Lines 5–8:** Environment variables that **configure SQL Server**:
> - `MSSQL_SA_PASSWORD` — the password for the `sa` (System Administrator) account. Must meet complexity requirements.
> - `ACCEPT_EULA` — you must accept Microsoft's license agreement (`"Y"`).
> - `MSSQL_PID` — the edition. `"Developer"` is free and has all features (for non-production use).

```yaml
    ports:
      - "1433:1433"
```
> **Line 9:** **Port mapping** — `host:container`. Maps port **1433** on your machine to port **1433** inside the container. This lets you connect from tools like SSMS or Azure Data Studio on your host machine.

```yaml
    volumes:
      - sqlserver-data:/var/opt/mssql
```
> **Line 10:** **Named volume** — persists database files. Without this, all data is lost when the container is removed. The data is stored in a Docker-managed volume called `sqlserver-data`, mapped to SQL Server's data directory inside the container.

```yaml
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Strong@Passw0rd!" -C -Q "SELECT 1" -b || exit 1
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 40s
```
> **Lines 11–16:** A **health check** — Docker periodically runs this command to see if SQL Server is actually ready:
> - `test:` — runs `sqlcmd` to execute `SELECT 1`. If it succeeds, the database is healthy.
> - `interval: 10s` — check every 10 seconds
> - `timeout: 5s` — if a check takes more than 5 seconds, it's considered failed
> - `retries: 10` — the container is marked "unhealthy" after 10 consecutive failures
> - `start_period: 40s` — wait 40 seconds before starting health checks (SQL Server needs time to boot)

---

### 🌐 Service 2 — The API Application

```yaml
  api:
```
> **Line 17:** The second service — our .NET API application.

```yaml
    build:
      context: .
      dockerfile: Dockerfile
```
> **Lines 18–20:** Instead of pulling a pre-built image, **build from a Dockerfile**:
> - `context: .` — the **build context** is the current directory (all files Docker can access)
> - `dockerfile: Dockerfile` — use the Dockerfile we explained above

```yaml
    container_name: storeapi-app
```
> **Line 21:** Fixed container name for easy identification.

```yaml
    ports:
      - "5124:8080"
```
> **Line 22:** Maps port **5124** on your host to port **8080** in the container. You access the API at `http://localhost:5124`. The container internally listens on 8080 (as set in the Dockerfile).

```yaml
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=storedb;User Id=sa;Password=Strong@Passw0rd!;TrustServerCertificate=True;"
```
> **Lines 23–25:** Environment variables that **override** appsettings.json:
> - `ASPNETCORE_ENVIRONMENT: "Development"` — overrides the Dockerfile's `Production` setting, enabling Swagger UI and detailed errors.
> - `ConnectionStrings__DefaultConnection` — the **double underscore** (`__`) represents a `:` in .NET's configuration hierarchy. So this is equivalent to `ConnectionStrings:DefaultConnection` in appsettings.json. Notice the server is `sqlserver,1433` — that's the **service name** from above, because Docker Compose puts both containers on the same network!

```yaml
    depends_on:
      sqlserver:
        condition: service_healthy
```
> **Lines 26–28:** **Dependency and startup order**. The API won't start until the `sqlserver` service passes its health check. Without `condition: service_healthy`, Docker would only wait for the container to *start*, not for SQL Server to be *ready* — which would cause connection errors.

---

### 📦 Volumes Section

```yaml
volumes:
  sqlserver-data:
```
> **Lines 29–30:** Declares the **named volume** used by the sqlserver service. Docker manages where this data is physically stored on disk. This ensures your database survives `docker compose down` and `docker compose up` cycles. To truly delete the data, you'd run `docker compose down -v`.

---

### 📊 Visual Summary of Docker Compose

```
┌──────────────────────────────────────────────────────┐
│                Docker Compose Network                │
│                                                      │
│  ┌──────────────┐         ┌──────────────────────┐   │
│  │  sqlserver    │◄────────│  api                 │   │
│  │  (SQL Server) │ depends │  (StoreApi)          │   │
│  │  Port: 1433   │  on     │  Port: 8080          │   │
│  │  📁 Volume    │ healthy │  Built from          │   │
│  │              │         │  Dockerfile           │   │
│  └──────────────┘         └──────────────────────┘   │
│        │                          │                   │
└────────┼──────────────────────────┼───────────────────┘
         │                          │
    Host:1433                  Host:5124
    (SSMS/DBeaver)            (Browser/Postman)
```

---

## 🎓 Key Takeaways

| Concept | Dockerfile | Docker Compose |
|---|---|---|
| **Purpose** | Build **one** image | Orchestrate **multiple** containers |
| **Input** | Source code → Image | Images/Dockerfiles → Running system |
| **Analogy** | A recipe for one dish | A meal plan combining multiple dishes |
| **Command** | `docker build` | `docker compose up` |
| **Networking** | Not handled | Automatic DNS between services |
| **Data persistence** | Not handled | Volumes |
| **Health checks** | Not handled | Built-in support |

---

## 🧪 Common Commands Cheat Sheet

| Command                          | What It Does                                       |
| -------------------------------- | -------------------------------------------------- |
| `docker compose up --build`      | Build images and start all services                |
| `docker compose up -d`           | Start in **detached** mode (background)            |
| `docker compose down`            | Stop and remove containers (keeps volumes)         |
| `docker compose down -v`         | Stop, remove containers **and delete volumes**     |
| `docker compose logs api`        | View logs for the `api` service                    |
| `docker compose logs -f`         | Follow logs in real time (all services)            |
| `docker compose ps`              | List running containers and their status           |
| `docker compose exec api bash`   | Open a shell inside the running `api` container    |
| `docker compose restart api`     | Restart only the `api` service                     |
