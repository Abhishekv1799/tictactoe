# Tic Tac Toe API contract

Base URL (local): `http://localhost:5080` — interactive docs at `/swagger`.
All bodies are JSON. Enums are strings. Cells are addressed by **1-based row and column**
in requests; `board` and `winningCells` use a **0-8 row-major index** (`(row-1)*3 + (column-1)`).

## Endpoints

| Method | Endpoint | Body | Success | Purpose |
| --- | --- | --- | --- | --- |
| POST | `/api/games` | `{ "mode": "TwoPlayer" \| "VsComputer" }` | 201 + GameResponse | Create a game |
| GET | `/api/games/{id}` | – | 200 + GameResponse | Get current state |
| POST | `/api/games/{id}/moves` | `{ "player": "X", "row": 1, "column": 1 }` | 200 + GameResponse | Make a move |
| POST | `/api/games/{id}/undo` | – | 200 + GameResponse | Undo |
| POST | `/api/games/{id}/reset` | – | 200 + GameResponse | Reset game, keep scoreboard |
| GET | `/api/scoreboard?mode=TwoPlayer\|VsComputer` | – | 200 + `{ xWins, oWins, draws }` | Get one mode's scoreboard |
| POST | `/api/scoreboard/reset?mode=TwoPlayer\|VsComputer` | – | 200 + `{ xWins, oWins, draws }` | Zero one mode's scoreboard |

## GameResponse

```json
{
  "id": "3f0c…",
  "mode": "VsComputer",
  "status": "InProgress",          // InProgress | Won | Draw
  "currentPlayer": "X",
  "winner": null,                   // "X" | "O" when status = Won
  "winningCells": null,             // e.g. [0,4,8] when status = Won
  "board": ["X", null, null, null, "O", null, null, null, null],
  "moves": [
    { "moveNumber": 1, "player": "X", "row": 1, "column": 1, "cellIndex": 0 },
    { "moveNumber": 2, "player": "O", "row": 2, "column": 2, "cellIndex": 4 }
  ],
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

## Behaviour

- **Turns:** X always starts; `currentPlayer` is derived from the move count.
- **VsComputer:** the human is X. After a valid human move that doesn't end the game,
  the computer (O) replies in the same response. Priority: win → block X → center → corner → any cell.
- **Undo:** TwoPlayer removes one move. VsComputer removes the computer's move and the human
  move before it (only one move if the human's move ended the game). Status, winner and turn are
  recalculated. Undoing a finished game also reverses its scoreboard entry.
- **Scoreboard:** each mode (`TwoPlayer`, `VsComputer`) has its own scoreboard. A game updates only its own mode's totals, once, when it completes. `scoreboard` in a game response is that game's mode's scoreboard. Reset Game never changes it, and resetting one mode's scoreboard leaves the other alone.
- **Reset:** clears board, history and result; X to move; same game id and mode.

## Errors (RFC 7807 problem details)

| Status | When |
| --- | --- |
| 400 | Row/column outside 1-3, invalid player or mode, missing or invalid `mode` query on scoreboard routes |
| 404 | Unknown game id |
| 409 | Occupied cell, wrong player's turn, game already over, nothing to undo, concurrent modification |

Invalid moves never change the game state or whose turn it is.
