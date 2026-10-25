-- Reference DDL for the Tic Tac Toe SQLite database.
-- The app creates the same structure automatically via EF Core (EnsureCreated);
-- this file is for reviewers and for creating the DB by hand.
PRAGMA foreign_keys = ON;

CREATE TABLE Games (
    Id              TEXT    NOT NULL PRIMARY KEY,            -- GUID
    Mode            TEXT    NOT NULL,                        -- TwoPlayer | VsComputer
    Status          TEXT    NOT NULL,                        -- InProgress | Won | Draw
    CurrentPlayer   TEXT    NOT NULL,                        -- X | O
    Winner          TEXT        NULL,                        -- X | O, only when Won
    WinningCells    TEXT        NULL,                        -- e.g. '0,4,8', only when Won
    RecordedOutcome TEXT        NULL,                        -- XWin | OWin | Draw once scored
    CreatedAt       TEXT    NOT NULL,
    UpdatedAt       TEXT    NOT NULL,
    Version         INTEGER NOT NULL DEFAULT 1,              -- optimistic concurrency token

    CONSTRAINT CK_Games_Mode          CHECK (Mode IN ('TwoPlayer','VsComputer')),
    CONSTRAINT CK_Games_Status        CHECK (Status IN ('InProgress','Won','Draw')),
    CONSTRAINT CK_Games_CurrentPlayer CHECK (CurrentPlayer IN ('X','O')),
    CONSTRAINT CK_Games_WinnerMatchesStatus CHECK (
        (Status = 'Won' AND Winner IS NOT NULL AND WinningCells IS NOT NULL)
        OR (Status <> 'Won' AND Winner IS NULL AND WinningCells IS NULL))
);

CREATE TABLE Moves (
    Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameId     TEXT    NOT NULL REFERENCES Games(Id) ON DELETE CASCADE,
    MoveNumber INTEGER NOT NULL,                             -- 1-based
    Player     TEXT    NOT NULL,                             -- X | O
    CellIndex  INTEGER NOT NULL,                             -- 0-8, row * 3 + column
    CreatedAt  TEXT    NOT NULL,

    CONSTRAINT CK_Moves_CellIndex  CHECK (CellIndex BETWEEN 0 AND 8),
    CONSTRAINT CK_Moves_MoveNumber CHECK (MoveNumber BETWEEN 1 AND 9),
    CONSTRAINT CK_Moves_Player     CHECK (Player IN ('X','O'))
);

CREATE UNIQUE INDEX IX_Moves_GameId_MoveNumber ON Moves (GameId, MoveNumber);
CREATE UNIQUE INDEX IX_Moves_GameId_CellIndex  ON Moves (GameId, CellIndex);

-- One scoreboard per game mode.
CREATE TABLE Scoreboard (
    Mode      TEXT    NOT NULL PRIMARY KEY,                  -- TwoPlayer | VsComputer
    XWins     INTEGER NOT NULL DEFAULT 0,
    OWins     INTEGER NOT NULL DEFAULT 0,
    Draws     INTEGER NOT NULL DEFAULT 0,
    UpdatedAt TEXT    NOT NULL,
    Version   INTEGER NOT NULL DEFAULT 1,

    CONSTRAINT CK_Scoreboard_Mode        CHECK (Mode IN ('TwoPlayer','VsComputer')),
    CONSTRAINT CK_Scoreboard_NonNegative CHECK (XWins >= 0 AND OWins >= 0 AND Draws >= 0)
);

INSERT INTO Scoreboard (Mode, UpdatedAt) VALUES
    ('TwoPlayer',  strftime('%Y-%m-%d %H:%M:%f','now')),
    ('VsComputer', strftime('%Y-%m-%d %H:%M:%f','now'));
