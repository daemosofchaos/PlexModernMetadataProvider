# Plex Modern Metadata Provider for Plex

A modern, Docker-friendly **custom Plex metadata provider** built with **C# / ASP.NET Core / .NET 10**.

This project exists to fix one of the most annoying match problems in Plex metadata: when multiple movies or shows share the same title, Plex can sometimes latch onto an older title even when the file clearly refers to the current one.

This provider uses a **date-aware ranking strategy** so that if the filename does **not** contain a year, the provider prefers the title whose release date is **closest to the current date**.

## Example

If metadata sources contain both of these:

- `Rooster (2013)`
- `Rooster (2026)`

and Plex sends only `Rooster` with no year, this provider will choose:

- **`Rooster (2026)`**

If the file contains a year, such as `Rooster.2013.1080p.mkv`, the provider strongly prefers the matching year instead.

---

## Why this project exists

Plex introduced **Custom Metadata Providers** as HTTP-based providers that can run locally, remotely, or inside Docker. Plex also recommends splitting **movie** and **TV** support into separate providers, which this project does.

This implementation is intended to be:

- easier to maintain for .NET developers
- easy to run in Docker
- a solid base for open-source collaboration
- explicit and predictable about ambiguous-title matching
- flexible about **where metadata comes from**

---

## Features

### Core

- Separate custom providers for:
  - **Movies** at `/movie`
  - **TV** at `/tv`
- **ASP.NET Core / .NET 10** implementation
- **Docker** support
- Source adapters behind a **provider abstraction layer**
- In-memory caching for upstream lookups

### Current metadata source adapters

- **OMDb** for movies
- **TVMaze** for TV
- **TMDb** as an optional richer fallback / secondary source

### Matching behavior

- Exact external ID matching when Plex supplies:
  - `tmdb://...`
  - `imdb://...`
  - `tvdb://...` for TV
- Year extraction from filenames
- Season / episode extraction from filenames
- Air-date extraction from filenames
- Manual search mode with ranked results
- Exact-title preference over loose title matches
- Date-aware ambiguous-title selection
- Global ranking across the configured source order

### TV support

- Show matching
- Season matching
- Episode matching
- Show metadata
- Season metadata
- Episode metadata
- Children endpoints for shows and seasons
- Image endpoints

### Movie support

- Movie matching
- Movie metadata
- Image endpoints

### Quality

- Unit tests for:
  - filename parsing
  - rating key parsing
  - ambiguous-title ranking
- Solution file included
- Verified with `dotnet build`, `dotnet test`, and `dotnet publish`

---

## Matching rules

This is the heart of the project.

### Automatic matching

1. **If Plex supplies a supported external ID, that wins first.**
2. If a **year** exists in the match request or filename, exact or nearest-year matches are strongly preferred.
3. If **no year** exists, the provider ranks same-title results by **absolute distance to today** using:
   - `release_date` for movies
   - `first_air_date` for TV shows
4. Ties are broken by:
   1. newer release date
   2. higher popularity
   3. stable title ordering

### Manual matching

Manual searches return multiple candidates, but they are still sorted using the exact same logic.

### Filename behavior

Examples the parser understands:

```text
Rooster.2013.1080p.WEB-DL.mkv
Rooster.S01E02.1080p.WEB-DL.mkv
Rooster.1x02.1080p.WEB-DL.mkv
Daily.Report.2026-03-23.1080p.WEB-DL.mkv
```

---

## Provider architecture

The project now uses a **source abstraction layer** so movie and TV metadata providers can be mixed and ordered independently.

### Movie source contract

- search candidates
- resolve by external ID
- retrieve metadata by source ID

### TV source contract

- search shows
- resolve by external ID
- retrieve show metadata
- retrieve season metadata
- retrieve episode metadata
- retrieve episode by air date

### Why this matters

This means the project is no longer hard-wired to TMDb. Users can prefer more privacy-friendly or simpler sources while still keeping TMDb available as an optional fallback.

---

## Current source strategy

### Default movie order

```text
Omdb,Tmdb
```

### Default TV order

```text
TvMaze,Tmdb
```

### What that means

- **Movies** try **OMDb first**, then **TMDb** if needed
- **TV** tries **TVMaze first**, then **TMDb** if needed

### Current source trade-offs

- **OMDb** is lighter-weight and easier to access, but less rich than TMDb
- **TVMaze** is great for TV matching and episode data, but it is **TV-only**
- **TMDb** remains the richest overall source, but is now optional rather than mandatory

---

## Project layout

```text
PlexModernMetadataProvider/
├─ src/
│  └─ PlexModernMetadataProvider.Api/
│     ├─ Models/
│     ├─ Options/
│     ├─ Services/
│     ├─ appsettings.json
│     ├─ PlexModernMetadataProvider.Api.csproj
│     └─ Program.cs
├─ tests/
│  └─ PlexModernMetadataProvider.Tests/
│     ├─ Services/
│     └─ PlexModernMetadataProvider.Tests.csproj
├─ Dockerfile
├─ docker-compose.yml
├─ PlexModernMetadataProvider.slnx
└─ README.md
```

---

## Requirements

### Runtime

- **.NET 10 SDK** for local development
- or **Docker**
- a Plex Media Server version that supports **Custom Metadata Providers**
- depending on your source order:
  - **TVMaze**: no API key required for the public API
  - **OMDb**: API key required
  - **TMDb**: API key or read access token required

### Verified locally

This project was verified with:

```text
dotnet --version
10.0.103
```

---

## Configuration

Copy the example environment file:

```powershell
Copy-Item .env.example .env
```

### Source order

You can independently control movie and TV source priority:

```text
Provider__MovieSourceOrder=Omdb,Tmdb
Provider__TvSourceOrder=TvMaze,Tmdb
```

### Minimal privacy-friendly setup

For many users this is enough:

```text
Provider__MovieSourceOrder=Omdb
Provider__TvSourceOrder=TvMaze
Provider__OMDb__ApiKey=your_omdb_api_key
```

### Richer hybrid setup

```text
Provider__MovieSourceOrder=Omdb,Tmdb
Provider__TvSourceOrder=TvMaze,Tmdb
Provider__OMDb__ApiKey=your_omdb_api_key
Provider__TMDb__ReadAccessToken=your_tmdb_read_access_token
```

### Available settings

```text
ASPNETCORE_URLS=http://+:3000
Provider__DefaultLanguage=en-US
Provider__DefaultCountry=US
Provider__MaxManualMatches=10
Provider__MovieSourceOrder=Omdb,Tmdb
Provider__TvSourceOrder=TvMaze,Tmdb
Provider__OMDb__ApiKey=
Provider__OMDb__RequestTimeoutSeconds=15
Provider__OMDb__CacheTtlMinutes=15
Provider__TVMaze__RequestTimeoutSeconds=15
Provider__TVMaze__CacheTtlMinutes=15
Provider__TMDb__ApiKey=
Provider__TMDb__ReadAccessToken=
Provider__TMDb__RequestTimeoutSeconds=15
Provider__TMDb__CacheTtlMinutes=15
```

---

## Running locally

### Restore

```powershell
dotnet restore .\PlexModernMetadataProvider.slnx
```

### Build

```powershell
dotnet build .\PlexModernMetadataProvider.slnx
```

### Run

```powershell
dotnet run --project .\src\PlexModernMetadataProvider.Api\PlexModernMetadataProvider.Api.csproj
```

### Run tests

```powershell
dotnet test .\PlexModernMetadataProvider.slnx
```

### Publish

```powershell
dotnet publish .\src\PlexModernMetadataProvider.Api\PlexModernMetadataProvider.Api.csproj -c Release -o .\publish
```

---

## Running in Docker

### Build and start

```powershell
docker compose up --build -d
```

### Stop

```powershell
docker compose down
```

The container listens on:

```text
http://localhost:3000
```

Health endpoint:

```text
GET /health
```

---

## Plex setup

Plex recommends separate providers for movies and TV. This project exposes both.

### Provider URLs

If Plex runs on the **host** and the provider runs in Docker on the same machine:

```text
TV:    http://host.docker.internal:3000/tv
Movie: http://host.docker.internal:3000/movie
```

If Plex and the provider run on the **same Docker network**, use the service/container name instead:

```text
TV:    http://plex-modern-metadata-provider-dotnet:3000/tv
Movie: http://plex-modern-metadata-provider-dotnet:3000/movie
```

### Suggested setup flow

1. Open **Plex Settings**.
2. Go to **Metadata Agents** / **Custom Metadata Providers**.
3. Add the TV provider URL.
4. Add the Movie provider URL.
5. Create or assign custom agents for the relevant library types.
6. Make this provider the **primary** provider if you want its matching logic to drive metadata selection.
7. Keep local media assets enabled afterward if you also want local posters, backgrounds, or subtitle-related local data.
8. Refresh metadata for the target library.

---

## Endpoints

### Health

```text
GET /health
```

### Movie provider

```text
GET  /movie
POST /movie/library/metadata/matches
GET  /movie/library/metadata/{ratingKey}
GET  /movie/library/metadata/{ratingKey}/images
GET  /movie/library/metadata/{ratingKey}/extras
```

### TV provider

```text
GET  /tv
POST /tv/library/metadata/matches
GET  /tv/library/metadata/{ratingKey}
GET  /tv/library/metadata/{ratingKey}/images
GET  /tv/library/metadata/{ratingKey}/extras
GET  /tv/library/metadata/{ratingKey}/children
```

---

## Example match request

### Movie

```json
{
  "type": 1,
  "title": "Rooster",
  "filename": "Rooster.1080p.WEB-DL.mkv"
}
```

### TV episode

```json
{
  "type": 4,
  "grandparentTitle": "Rooster",
  "parentIndex": 1,
  "index": 2,
  "filename": "Rooster.S01E02.1080p.WEB-DL.mkv"
}
```

---

## Important behavior notes

### What this project intentionally does

- prefers **latest / most current** title when year is absent
- still respects explicit years when available
- still prioritizes exact title matches over weaker partial matches
- uses external IDs immediately when supplied
- keeps metadata retrieval tied to the source that originally matched the item

### What this project intentionally avoids

- blindly preferring an old title because the name happens to match
- assuming the oldest result is correct when Plex provides weak input
- mixing movie and TV provider definitions into one provider root
- scraping unofficial web pages instead of using public APIs

---

## Current limitations

- **OMDb** is currently used for **movies only**
- **TVMaze** is currently used for **TV only**
- **TMDb** still provides richer images and richer credits than the lighter-weight sources
- No collection endpoint yet
- No persistent cache yet
- No provider-side authentication layer yet
- Upstream metadata quality still depends on the chosen source

---

## Roadmap ideas

- Fanart.tv artwork adapter
- Wikidata supplemental adapter
- optional TheTVDB adapter
- persistent distributed cache
- collection support
- richer manual-match diagnostics
- provider-specific tuning rules

---

## Development status

Verified in this repo:

```powershell
dotnet build .\PlexModernMetadataProvider.slnx
dotnet test .\PlexModernMetadataProvider.slnx
dotnet publish .\src\PlexModernMetadataProvider.Api\PlexModernMetadataProvider.Api.csproj -c Release -o .\publish
```

All three completed successfully during project validation.

---

## References

- [Plex custom metadata providers announcement](https://forums.plex.tv/t/announcement-custom-metadata-providers/934384)
- [Plex Media Server developer docs](https://developer.plex.tv/pms/)
- [Plex TMDb example provider](https://github.com/plexinc/tmdb-example-provider)
- [TMDb API docs](https://developer.themoviedb.org/reference/intro/getting-started)
- [OMDb API](https://www.omdbapi.com/)
- [TVMaze API](https://www.tvmaze.com/api)

---

## License

MIT
