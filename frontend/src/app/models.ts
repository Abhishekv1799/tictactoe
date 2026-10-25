export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface MoveEntry {
  moveNumber: number;
  player: Player;
  row: number;
  column: number;
  cellIndex: number;
}

export interface GameState {
  id: string;
  mode: GameMode;
  status: GameStatus;
  currentPlayer: Player;
  winner: Player | null;
  winningCells: number[] | null;
  /** 9 cells, row-major; null = empty. */
  board: (Player | null)[];
  moves: MoveEntry[];
  scoreboard: Scoreboard;
}

export interface MoveRequest {
  player: Player;
  row: number;
  column: number;
}
