# Tic Tac Toe

A browser-based Tic Tac Toe game with Two Player and Vs Computer modes, move history, undo,
and a scoreboard. **Angular 18** frontend, **.NET 8 Web API** backend, **SQLite** storage,
communicating over a REST API.

The backend owns the game session, move history, status and scoreboards. The frontend only
displays state and sends actions.

```
tictactoe/
├── backend/
│   ├── TicTacToe.sln
│   ├── TicTacToe.Api/         # Web API (EF Core + SQLite)
│   └── TicTacToe.Api.Tests/   # xUnit tests
└── frontend/                  # Angular app
```

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 18 (standalone components, signals), TypeScript |
| Backend | .NET 8 Web API (minimal APIs), Swagger/OpenAPI |
| API style | REST, JSON, RFC 7807 error responses |
| Storage | SQLite via EF Core (file `tictactoe.db`, created on first start) |
| Tests | xUnit (rules + services against in-memory SQLite) |
| Source control | GitHub |

## Features implemented

- 3×3 board; empty cells are clickable, filled cells are locked.
- Alternating turns (X first) with the current player shown.
- Win detection (rows, columns, diagonals) with the winning cells highlighted; no moves after the game ends.
- Draw detection.
- **Two Player mode** and **Vs Computer mode** (human is X, computer is O).
- Computer move priority: win, block X, center, corner, any free cell.
- Move history table (move number, player, position).
- Undo, behaving according to the mode (below), with status, winner and turn restored.
- Scoreboard (X wins, O wins, draws), kept **separately for each mode**.
- Reset Game and Reset Scoreboard, as separate actions.
- Backend validation of every move (out of bounds, occupied cell, wrong turn, game over).

## Run the backend

Prerequisite: .NET 8 SDK.

```bash
cd backend/TicTacToe.Api
dotnet run
```

The API listens on **http://localhost:5080**; Swagger UI is at `/swagger`. The SQLite file is
created on first start. Delete `tictactoe.db` to reset all data (also needed after a schema change).

Or open `backend/TicTacToe.sln` in Visual Studio and run the **http** profile.

## Run the frontend

Prerequisite: Node 18.19+ (or 20+) and npm. Start the backend first.

```bash
cd frontend
npm install
npm start
```

Open **http://localhost:4200**. The dev server proxies `/api` to the backend
(`frontend/proxy.conf.json`), so no CORS setup is needed.

## Run the tests

```bash
cd backend
dotnet test
```

The tests cover the rules (all 8 winning lines, draw, computer move priority) and run the real
services and repositories against in-memory SQLite: turn order, invalid moves, win and draw
scoring, undo in both modes, reset, scoring exactly once, and separate scoreboards per mode.
There are no frontend unit tests (see limitations).

## API endpoint summary

Full contract with request/response examples: [`backend/TicTacToe.Api/docs/API.md`](backend/TicTacToe.Api/docs/API.md).

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/games` | Create a game (`TwoPlayer` or `VsComputer`) |
| GET | `/api/games/{id}` | Get current state |
| POST | `/api/games/{id}/moves` | Submit a move |
| POST | `/api/games/{id}/undo` | Undo the last move (or move pair vs computer) |
| POST | `/api/games/{id}/reset` | Reset the game; scoreboard unchanged |
| GET | `/api/scoreboard?mode=…` | Get a mode's scoreboard |
| POST | `/api/scoreboard/reset?mode=…` | Reset a mode's scoreboard |

A game state response contains the game id, mode, status, current player, winner, winning cells,
board, move history and that mode's scoreboard.

## Design decisions

- **Moves are the source of truth.** The board, status, winner and current player are recomputed
  from the move list, so new moves and undo share one code path and cannot drift apart.
- **Undo choice: Option B (undo allowed after completion).** Undoing a finished game reverses its
  scoreboard entry, so the scoreboard never counts a result that was taken back. Replaying the game
  scores it once again.
- **Scoring happens once.** A finished game stores what it contributed to the scoreboard
  (`RecordedOutcome`); recording is skipped if that is already set, and undo reverses exactly that.
- **Undo in Vs Computer mode** removes the computer's move and the human move before it. If the
  human's move ended the game there is no computer reply, so only that one move is removed.
- **Reset Game** keeps the same game id and mode, clears moves and result, and never changes a scoreboard.
- **Invalid moves never change state.** Out-of-range cells return 400; occupied cells, wrong turn
  and moves after the game ends return 409, as problem-details responses.
- **Integrity in the database:** CHECK and unique constraints (a cell can be played once per game;
  a Won game must have a winner), plus optimistic-concurrency tokens that turn conflicting edits into 409s.
- **Layering:** `Domain` (pure rules) → `Services` (use cases) → `Data` (repositories, one unit of
  work so a game and its scoreboard save atomically) → `Endpoints` (HTTP only).

## Clarifications and assumptions

- In Vs Computer mode the human always plays X and moves first; the computer plays O.
- The computer replies inside the same request as the human's move (no separate polling).
- Requests address a cell by 1-based **row and column**; responses also include a 0-8 cell index.
- Each mode has its own scoreboard, shared by all games of that mode (the app is single-user and has no login).
- Changing the mode in the UI starts a new game and keeps both scoreboards.
- SQLite was chosen over pure in-memory storage so scores survive a restart (the problem statement allows either).

## AI tools and prompt summary

**Tool:** Claude (Anthropic), used as a pair-programming assistant while I directed the design and checked the output.

**Workflow**
- Turned the problem statement into rules first: moves are the source of truth, Undo is allowed after completion (Option B) and reverses the score, and the computer plays win, block, center, corner, then any free cell.
- Built in layers: database schema, backend APIs, Angular frontend, then tests and README.
- Reviewed each layer, then refined it through several rounds of feedback on the UI and on the scoreboard design.

**What the AI generated:** the backend, Angular components, xUnit tests, API contract and first README draft.

**What I changed or verified myself**
- Ran the build and tests locally, which caught a wrong test board and a nullability warning.
- Tried both modes in the browser and drove the UI redesigns.
- Decided each mode should keep its own scoreboard.

**Reviewed most carefully:** undo and scoring consistency, move validation, and undo behaviour in computer mode.

**Trade-offs:** SQLite instead of pure in-memory so scores survive restarts, a rule-based computer player rather than minimax, and no frontend tests.

## Known limitations

- No authentication; the scoreboards are global to the running instance.
- The frontend does not remember the game id, so refreshing the page starts a new game
  (the scoreboards persist in the backend).
- The computer follows the specified priority rules; it is not an unbeatable minimax player.
- No frontend unit or end-to-end tests; backend tests only.
- The database is created with `EnsureCreated`, with no migrations, so a schema change means deleting `tictactoe.db`.
- CORS and the proxy are set up for local development only.

## Future improvements

- Resume a game after refresh (store the id, or put it in the URL).
- EF Core migrations, and a CI pipeline (GitHub Actions) that builds and runs the tests.
- Frontend component tests and an end-to-end test (Playwright).
- Difficulty levels or a minimax computer opponent.
- Per-user scoreboards, and Docker support for one-command startup.