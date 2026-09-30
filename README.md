<p align="center">
  <img src="src/EBikeManager.Web/wwwroot/img/logo.svg" alt="eBike Manager Logo" width="160" />
</p>

# eBike Manager

A self-hosted Blazor Server app for Bosch eBike Flow riders. It keeps a copy of every ride, with its FIT file, and shows your rides and your bike in one place, without depending on the Flow app.

> [!NOTE]
> eBike Manager isn't made by, affiliated with or endorsed by Bosch. It talks to the same cloud service the Bosch eBike Flow app uses, which Bosch doesn't document and may change at any time.

---

## Features

- **Ride sync** — fetches your rides from Bosch eBike Flow on a schedule you choose (every hour by default), for the bikes you pick
- **FIT backup** — downloads every ride's FIT file, checks it, and keeps it on your own disk next to Bosch's ride summary, filed by date. The first sync backs up your whole history
- **Dashboard** — distance, rides, moving time, calories and elevation for this week, this month, this year and all time, your latest rides and the state of the sync
- **Rides** — every ride with its distance, moving time, Bosch's calories, your share of the effort, and a download button for its FIT file
- **Bike** — the bike's picture, odometer, motor hours, battery (capacity, charge cycles, energy delivered), range in each assistance mode, and every component with its software version and serial number. With a ConnectModule it adds the live battery state and the last known location
- **Local by design** — every page is built from what's stored on your server. eBike Manager contacts Bosch only while syncing, and serves its own copy of your bike's picture and of the fonts it uses
- **Sign-in** — an optional password in front of the whole app
- **Docker-ready** — one container, one data folder, and a `/healthz` endpoint for Docker's health check

---

## Screenshots

### Dashboard

![Dashboard showing distance, rides, moving time, calories and elevation for four periods, recent rides and the sync status](docs/screenshots/dashboard.png)

### Rides

![Rides showing every ride with its distance, moving time, calories, rider share and FIT backup status](docs/screenshots/rides.png)

### Bike

![Bike page showing the bike, odometer, motor hours, battery, range per assistance mode and components](docs/screenshots/bike.png)

---

## Upcoming Features

Planned, but not built yet. There are no dates.

- **Ride details** — a map of the route, all the ride's figures, and charts of elevation, speed, cadence and power
- **Google Health integration** — upload rides to Google Health with Bosch's calories, which account for the motor's help, and optionally replace the ride your watch recorded at the same time

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Docker + Docker Compose | Recommended deployment method |
| A Bosch eBike Flow account | For a bike with the Bosch smart system, the one the eBike Flow app works with |
| A desktop or laptop browser | Needed once, to sign in to Bosch (see [Connecting Bosch eBike Flow](#connecting-bosch-ebike-flow)) |
| Internet access | To reach Bosch's cloud service |

---

## Quick Start (Docker Compose)

1. **Get the source and build the image**

   ```bash
   git clone <this repository>
   cd <repository folder>
   docker compose up -d --build
   ```

   The included `docker-compose.yml` builds the image and mounts `./data` for everything eBike Manager keeps:

   ```yaml
   services:
     ebike-manager:
       build:
         context: .
         dockerfile: Dockerfile
       image: ebike-manager:latest
       container_name: ebike-manager
       restart: unless-stopped
       ports:
         - "${PORT:-2004}:${PORT:-2004}"
       volumes:
         - ./data:/app/data
       environment:
         - PORT=${PORT:-2004}
   ```

   **Volume notes:**
   - `./data/storage.db` — the SQLite database with your settings, rides, bike details and bike pictures.
   - `./data/keys` — the keys that encrypt your Bosch login and keep sign-in cookies valid across restarts. Keep them with the database; without them you have to connect Bosch again.
   - `./data/fit` — the FIT backup, filed as `fit/2026/09/2026-09-30_0943_<ride id>.fit` in the ride's local time, each with a `.json` file holding Bosch's summary of the ride.

2. **Open the app**

   It's available on port `2004` of the host you deployed it on (e.g. `http://192.168.1.100:2004`).

3. **Complete the welcome steps**

   The first visit walks you through connecting your Bosch eBike Flow account, choosing your bikes and picking how often to sync. When you finish, the first sync starts and backs up your whole ride history.

4. **Review the Settings page**

   Open **Settings** to change the schedule, add a bike or require a password (see [Configuration](#configuration) below).

---

## Configuration

Everything is configured in the browser and stored in the database. A few environment variables only control how the app is hosted.

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `PORT` | `2004` | Port the app listens on. Change the port mapping in `docker-compose.yml` to match |
| `DATA_DIRECTORY` | `data` in the working directory | Folder for the database, keys and FIT backup. The Docker image mounts a volume at `/app/data`, so leave it unset there |
| `DISABLE_AUTH` | — | Set to `true` to stop asking for the password. Use it if you've forgotten it, then set a new one under **Settings → Security** |

### Connecting Bosch eBike Flow

Bosch's login only hands its result to the eBike Flow app, so eBike Manager can't receive it directly. Instead you copy it from your browser's developer tools, the same method the [ha-bosch-ebike-flow](https://github.com/marq24/ha-bosch-ebike-flow) Home Assistant integration uses:

1. Use a desktop or laptop browser. On a phone, the Flow app takes over the login.
2. Click **Open Bosch login**. It opens in a new tab.
3. In that tab, open the developer tools (<kbd>F12</kbd>, or <kbd>Cmd</kbd>+<kbd>Option</kbd>+<kbd>I</kbd> on a Mac) and select the **Network** tab before signing in.
4. Sign in with your Bosch eBike Flow account. After the password the page stops loading; that's expected.
5. Type `oauth2redirect` into the Network filter box, right-click the request that shows up, and choose **Copy → Copy URL**.
6. Paste it into eBike Manager and click **Connect** straight away. The code in it expires after about a minute.

eBike Manager then keeps itself signed in. The login is stored encrypted in the data folder. If Bosch ever stops accepting it, the sync reports it and **Settings → Bosch** asks you to sign in again.

---

### Tab: Bosch

| Field | Description |
|---|---|
| **Bosch eBike Flow account** | Which account is connected, and **Sign in again** if Bosch stopped accepting the login |
| **Bikes** | The bikes whose rides are synced. Adding a bike reads its whole ride history on the next sync |

### Tab: Schedule

| Field | Description |
|---|---|
| **Sync schedule** | Every 30 minutes, every hour (default), every 3 hours, every 6 hours, once a day, or your own cron expression. Cron expressions are evaluated in UTC. The next sync time is shown below the choices |
| **Sync now** | Fetches new rides straight away. The **Sync now** button in the header does the same |

Each sync reads Bosch's ride list, newest first, and stops at the first page with nothing new. Rides from the last 7 days are always read again, because Bosch sometimes finishes processing a ride after you've stopped. FIT files that are missing are then downloaded, and your bikes' details refreshed. A bike's picture is downloaded only when Bosch's picture changes.

### Tab: Security

| Field | Description |
|---|---|
| **Password** | Require a password before anyone can use eBike Manager. Recommended unless only you can reach it. Changing or removing the password signs everyone out |

### Tab: About

The version, and the projects eBike Manager is built with.

---

## Development Setup

### Requirements

- .NET 10 SDK

### Run locally

```bash
dotnet run --project src/EBikeManager.Web
```

The app listens on `http://localhost:2004`, or on `PORT` if it's set. Its data goes to `src/EBikeManager.Web/data`.

### Tests

The unit tests (`tests/EBikeManager.UnitTests`) use fakes only. The integration tests (`tests/EBikeManager.IntegrationTests`) run real parts together: SQLite, the app over HTTP, and the app in a browser. None of them reach Bosch. Run both with:

```bash
dotnet test
```

The browser tests drive the app in Chromium with [Playwright](https://playwright.dev/dotnet/). Install Chromium once after building, or they're skipped:

```bash
pwsh tests/EBikeManager.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

To leave them out, add `--filter-not-trait "Category=Browser"` to the test command.

### Database migrations

EF Core migrations are applied automatically on startup. To add a new migration during development:

```bash
dotnet ef migrations add <MigrationName> --project src/EBikeManager.Application --startup-project src/EBikeManager.Web --output-dir Data/Migrations
```

### Docker image

The `docker-compose.yml` in this repository builds the image from source:

```bash
docker compose up -d --build
```

---

## Technology Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10, Blazor Server (Interactive Server render mode) |
| UI components | [MudBlazor](https://mudblazor.com/) with the [Modernity CSS](https://github.com/russkyc/modernity-css-experiment) patch, and the [Inter](https://rsms.me/inter/) typeface served locally |
| Database | SQLite via Entity Framework Core |
| Scheduling | [Cronos](https://github.com/HangfireIO/Cronos) |
| FIT files | [Garmin FIT SDK](https://developer.garmin.com/fit/) |
| Bosch sign-in | Based on [ha-bosch-ebike-flow](https://github.com/marq24/ha-bosch-ebike-flow) |
| Tests | xUnit v3, FakeItEasy, Playwright for the browser tests |
| Containers | Docker + Docker Compose |

---

## FAQ

### Why do I have to copy a URL from the developer tools?

Bosch's login sends its result to an address only the eBike Flow app can open. eBike Manager can't register an address of its own with Bosch, so you hand it over by copying it. You only do this once; after that eBike Manager keeps the login fresh by itself.

---

### Which bikes work?

Bikes with the Bosch smart system, the ones the eBike Flow app supports. Older bikes that use the eBike Connect app don't.

---

### Why are Bosch's calories so much lower than my watch's?

On an eBike the motor does part of the work. Bosch works out your calories from your own effort, measured by the bike, while a watch estimates them from your heart rate as if you had done it all yourself. The FIT files Bosch exports contain no calories at all, so eBike Manager always uses the figure from Bosch's ride summary.

---

### Some bike details are missing. Why?

Bosch only reports what your bike has. The live battery state and the last known location come from a ConnectModule, and some figures only update after the bike has synced with the eBike Flow app.

Without a ConnectModule, Bosch's cloud doesn't know your battery's charge level at all. The eBike Flow app shows it because it reads it from the bike over Bluetooth, which eBike Manager can't do.

---

### What happens if Bosch changes its service?

Syncing may stop until eBike Manager is updated. Your backed-up FIT files, and everything already in the database, stay where they are.

---

## A Note on AI

This project was built with extensive help from [Claude Code](https://claude.com/claude-code). I have been a C# developer for more than 20 years, I understand the code fully, and I refactor and rewrite it as I see fit.
