using System.Text.Json.Serialization;

namespace TicTacToe.Api.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameMode { TwoPlayer, VsComputer }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameStatus { InProgress, Won, Draw }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Player { X, O }

/// <summary>What was added to the scoreboard for a finished game.</summary>
public enum ScoreOutcome { XWin, OWin, Draw }
