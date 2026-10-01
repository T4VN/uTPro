# uTPro.Feature.VideoAnalyzer

Analyze YouTube videos (regular + Shorts) from a pasted URL: official metadata (YouTube Data
API v3) plus an AI-generated report (Gemini reads the video directly — visuals and audio),
with a Vietnamese age-suitability rating (P/K/C13/C16/C18, Decree 81/2023/ND-CP), timeline,
topics, mentions, keywords and a chat Q&A over the stored report and transcript.

Results are cached per (platform, videoId): a successful analysis by ANY member is served to
every member from the database without spending AI quota again. Fresh analyses are gated by a
per-member daily quota to protect the Gemini API key.

## Components

| Piece | Where |
|---|---|
| Feature module (composer, services, providers, API) | `uTPro.Feature.VideoAnalyzer/` |
| API endpoints | `POST /api/video-analyzer/analyze`, `GET /api/video-analyzer/jobs/{key}`, `GET /api/video-analyzer/reports/{key}`, `POST /api/video-analyzer/reports/{key}/chat`, `POST /api/video-analyzer/reports/{key}/refresh` |
| Member login/logout | `MemberAuthSurfaceController` (form posts from the `pageLogin` template) |
| Own tables | `vaVideoAnalysis`, `vaChatMessage` via package migration plan `uTPro.VideoAnalyzer` (cross-DB, works with the PostgreSQL provider) |
| Tool + login templates | `Views/uTPro/PageVideoAnalyzer.cshtml`, `Views/uTPro/PageLogin.cshtml` (main web project) |
| Front-end assets | `wwwroot/scripts/uTPro/video-analyzer.js`, `wwwroot/css/uTPro/Components/VideoAnalyzer.css` |
| uSync definitions | `uSync/v17/ContentTypes/utpro__pagevideoanalyzer.config`, `utpro__pagelogin.config`, `uSync/v17/Templates/pageVideoAnalyzer.config`, `pageLogin.config` |

## Setup

1. **Boot once** — the migration plan creates the `va*` tables and the composer ensures the
   `videoAnalyzerUsers` / `videoAnalyzerAdmins` member groups exist.
2. **API keys** — fill `uTPro:Feature:VideoAnalyzer` in appsettings (or better, environment
   variables / user-secrets; never commit real keys):
   - `YouTubeDataApiKey` — Google Cloud console, enable *YouTube Data API v3* (free 10,000 units/day).
   - `GeminiApiKey` — Google AI Studio (free tier works; Vietnam is in the supported regions).
3. **Create pages** — after uSync imports the doctypes, create two content nodes:
   - one of doctype *Page Video Analyzer* (renders the tool), and
   - one of doctype *Page Login*.
4. **Protect the tool page** — on the analyzer node: *Permissions → Public Access → Restrict
   public access* → single member group `videoAnalyzerUsers`; set the login page to the
   *Page Login* node.
5. **Members** — create members in the backoffice and assign them to `videoAnalyzerUsers`
   (daily quota) or `videoAnalyzerAdmins` (unlimited). There is deliberately no
   self-registration.
6. Optional settings: `CacheTtlDays` (0 = cache forever), `MaxVideoMinutes` (default 60),
   `DailyQuotaPerMember` (default 5), `StaleJobMinutes`, `MaxChatMessagesPerAnalysis`,
   `DefaultReportLanguage`, `PreferredTranscriptLanguages`.

## How it works

```
URL ──parse──► (platform, videoId)
                 │
                 ├─ cache hit (newest Success row, TTL ok) ──► serve, CacheHits++
                 ├─ active Pending/Processing row ──────────► join that job
                 └─ quota check (fresh analyses only) ──────► new job row ──► background pipeline
                                                                 metadata (Data API, YouTube scrape fallback)
                                                                 transcript (YoutubeExplode, best effort)
                                                                 Gemini: video-URL pass → transcript fallback
                                                                 report JSON persisted
```

Failed analyses are never cached; jobs stuck in Processing past `StaleJobMinutes` are marked
failed on the next poll so the video can be retried (retry is quota-exempt only after a
timeout/failure recovery — each new analysis otherwise counts against the daily quota).

## Notes & limitations

- Only YouTube (watch / youtu.be / shorts / embed / live) is supported. TikTok is recognized
  and reported as "coming later" — the provider abstraction is ready for it (yt-dlp → Gemini
  Files API in a later release).
- Private, unlisted, age-restricted or region-blocked videos cannot be analyzed (Gemini needs
  public access; the Data API refuses private videos).
- The age rating is advisory AI output and is always displayed with a disclaimer.
- Scraping captions via YoutubeExplode is a best-effort supplement; the official metadata
  always comes from the YouTube Data API.
