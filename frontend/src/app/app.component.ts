import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { BoardComponent } from './board/board.component';
import { GameApiService } from './game-api.service';
import { GameMode, GameState, Scoreboard } from './models';
import { MoveHistoryComponent } from './move-history/move-history.component';
import { ScoreboardComponent } from './scoreboard/scoreboard.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [BoardComponent, ScoreboardComponent, MoveHistoryComponent],
  template: `
    <div class="app">
      <header class="top">
        <h1>Tic Tac Toe</h1>
        <div class="segmented" role="group" aria-label="Game mode"
             title="Changing the mode starts a new game; the scoreboard is kept">
          <button type="button" [class.selected]="mode() === 'TwoPlayer'"
                  [attr.aria-pressed]="mode() === 'TwoPlayer'"
                  [disabled]="busy()" (click)="setMode('TwoPlayer')">
            <svg class="check" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5" /></svg>
            Two players
          </button>
          <button type="button" [class.selected]="mode() === 'VsComputer'"
                  [attr.aria-pressed]="mode() === 'VsComputer'"
                  [disabled]="busy()" (click)="setMode('VsComputer')">
            <svg class="check" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5" /></svg>
            Vs computer
          </button>
        </div>
      </header>

      @if (error(); as message) {
        <div class="error" role="alert">
          <span>{{ message }}</span>
          <button type="button" (click)="error.set(null)">Dismiss</button>
        </div>
      }

      <div class="layout">
        <section class="score" aria-label="Scores">
          <app-scoreboard
            [score]="scoreboard()"
            [active]="activePlayer()"
            [winner]="game()?.winner ?? null"
            [mode]="mode()"
          />
          <button type="button" class="link" [disabled]="busy()" (click)="resetScoreboard()"
                  [title]="mode() === 'VsComputer' ? 'Resets the Vs computer scoreboard only' : 'Resets the Two players scoreboard only'">Reset scoreboard</button>
        </section>

        <section class="game" aria-label="Game">
          <p class="status" [class.tone-x]="tone() === 'X'" [class.tone-o]="tone() === 'O'" aria-live="polite">
            {{ statusText() }}
          </p>

          <div class="board-card">
            <app-board
              [board]="game()?.board ?? emptyBoard"
              [winningCells]="game()?.winningCells ?? null"
              [winner]="game()?.winner ?? null"
              [currentPlayer]="game()?.currentPlayer ?? 'X'"
              [disabled]="boardLocked()"
              (cellClick)="play($event)"
            />
          </div>

          <div class="actions">
            <button type="button" class="secondary" [disabled]="!canUndo()" (click)="undo()">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 7v6h6M3.5 13a9 9 0 1 0 2.2-8.4L3 7.5" /></svg>
              Undo
            </button>
            <button type="button" class="primary" [disabled]="!game() || busy()" (click)="reset()">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M21 12a9 9 0 1 1-2.6-6.4L21 8M21 3v5h-5" /></svg>
              Reset game
            </button>
          </div>
        </section>

        <app-move-history class="history" [moves]="game()?.moves ?? []" />
      </div>
    </div>
  `,
  styles: `
    .app { max-width: 900px; margin: 0 auto; padding: 20px 20px 56px; }

    .top { display: flex; flex-wrap: wrap; gap: 12px 24px; align-items: center; justify-content: space-between; margin-bottom: 24px; }
    h1 { margin: 0; font-size: 1.7rem; font-weight: 800; letter-spacing: -0.02em; }

    .segmented { display: inline-flex; gap: 4px; background: var(--card); border: 1.5px solid #3d5379; border-radius: 999px; padding: 4px; }
    .segmented button {
      display: inline-flex; align-items: center; gap: 6px; background: transparent; border: 0; border-radius: 999px;
      padding: 9px 18px; font-weight: 600; color: var(--chalk); cursor: pointer;
    }
    .segmented button:not(.selected):not(:disabled):hover { background: rgba(255, 255, 255, 0.1); color: var(--text); }
    .segmented button.selected { background: var(--text); color: var(--bg); font-weight: 800; box-shadow: 0 2px 10px rgba(0, 0, 0, 0.35); }
    .segmented button .check { display: none; width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 3; stroke-linecap: round; stroke-linejoin: round; }
    .segmented button.selected .check { display: block; }

    .error { display: flex; gap: 12px; align-items: center; justify-content: space-between;
             background: var(--danger-bg); color: var(--danger-ink); border-radius: 12px; padding: 10px 14px; margin-bottom: 16px; }
    .error button { background: transparent; border: 1px solid currentColor; border-radius: 8px; padding: 4px 10px; cursor: pointer; }

    /* Desktop: game on the left, scores above history on the right. */
    .layout {
      display: grid; gap: 20px 32px; align-items: start;
      grid-template-columns: minmax(0, 400px) minmax(0, 1fr);
      grid-template-areas:
        "game score"
        "game history";
    }
    .game    { grid-area: game; display: flex; flex-direction: column; align-items: center; gap: 14px; }
    .score   { grid-area: score; display: flex; flex-direction: column; align-items: stretch; gap: 8px; }
    .history { grid-area: history; }

    .status { margin: 0; font-size: clamp(1.4rem, 3.5vw, 1.75rem); font-weight: 800; letter-spacing: -0.01em; text-align: center; line-height: 1.15; }
    .status.tone-x { color: var(--x); }
    .status.tone-o { color: var(--o); }

    .board-card { width: 100%; background: var(--card); border: 1px solid var(--line); border-radius: 24px;
                  padding: 14px; box-shadow: 0 14px 32px rgba(0, 0, 0, 0.3); }

    .actions { display: flex; gap: 10px; width: 100%; }
    .actions button {
      flex: 1; display: inline-flex; align-items: center; justify-content: center; gap: 8px;
      border-radius: 14px; padding: 13px 16px; font-weight: 700; cursor: pointer;
      transition: transform 80ms, background 120ms, border-color 120ms;
    }
    .actions button svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2.2; stroke-linecap: round; stroke-linejoin: round; }
    .actions .secondary { background: var(--card-2); border: 1.5px solid #3d5379; color: var(--text); }
    .actions .secondary:not(:disabled):hover { border-color: var(--text); background: #243759; }
    .actions .primary { background: var(--text); border: 1.5px solid var(--text); color: var(--bg); }
    .actions .primary:not(:disabled):hover { background: #fff; box-shadow: 0 4px 14px rgba(255, 255, 255, 0.2); }
    .actions button:not(:disabled):active { transform: translateY(1px); }

    .link { align-self: flex-end; background: none; border: 0; padding: 0;
            color: var(--muted); font-size: 0.85rem; text-decoration: underline; cursor: pointer; }
    .link:hover { color: var(--text); }

    button:disabled { opacity: 0.35; cursor: not-allowed; }

    /* Narrow screens: scores, then game, then history, all one column. */
    @media (max-width: 760px) {
      .layout {
        grid-template-columns: minmax(0, 1fr);
        grid-template-areas: "score" "game" "history";
      }
      .game { max-width: 420px; width: 100%; margin: 0 auto; }
    }
  `,
})
export class AppComponent implements OnInit {
  private readonly api = inject(GameApiService);

  readonly emptyBoard: (null)[] = Array(9).fill(null);

  readonly game = signal<GameState | null>(null);
  readonly mode = signal<GameMode>('TwoPlayer');
  readonly scoreboard = signal<Scoreboard>({ xWins: 0, oWins: 0, draws: 0 });
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly boardLocked = computed(() => {
    const g = this.game();
    return this.busy() || !g || g.status !== 'InProgress';
  });

  /** Player whose turn it is, or null when the game is over. */
  readonly activePlayer = computed(() => {
    const g = this.game();
    return g && g.status === 'InProgress' ? g.currentPlayer : null;
  });

  /** Colour of the status line: the player to move, or the winner. */
  readonly tone = computed(() => {
    const g = this.game();
    if (!g) return null;
    return g.status === 'Won' ? g.winner : g.status === 'InProgress' ? g.currentPlayer : null;
  });

  readonly canUndo = computed(() => {
    const g = this.game();
    return !this.busy() && !!g && g.moves.length > 0;
  });

  readonly statusText = computed(() => {
    const g = this.game();
    if (!g) return this.error() ? 'No game running' : 'Starting game…';

    const vsComputer = g.mode === 'VsComputer';
    if (g.status === 'Draw') return "It's a draw";
    if (g.status === 'Won') {
      return vsComputer ? (g.winner === 'X' ? 'You win' : 'Computer wins') : `${g.winner} wins`;
    }
    return vsComputer ? 'Your turn (X)' : `${g.currentPlayer}'s turn`;
  });

  ngOnInit(): void {
    this.run(this.api.createGame(this.mode()));
  }

  setMode(mode: GameMode): void {
    if (mode === this.mode() && this.game()) return;
    this.mode.set(mode);
    this.run(this.api.createGame(mode));
  }

  play(index: number): void {
    const g = this.game();
    if (!g || this.boardLocked() || g.board[index] !== null) return;

    this.run(
      this.api.move(g.id, {
        player: g.currentPlayer,
        row: Math.floor(index / 3) + 1,
        column: (index % 3) + 1,
      }),
    );
  }

  undo(): void {
    const g = this.game();
    if (g && this.canUndo()) this.run(this.api.undo(g.id));
  }

  reset(): void {
    const g = this.game();
    if (g) this.run(this.api.reset(g.id));
  }

  resetScoreboard(): void {
    const label = this.mode() === 'VsComputer' ? 'Vs computer' : 'Two players';
    if (!confirm(`Reset the ${label} scoreboard to zero?`)) return;

    this.busy.set(true);
    this.error.set(null);
    this.api.resetScoreboard(this.mode()).subscribe({
      next: (score) => {
        this.scoreboard.set(score);
        this.busy.set(false);
      },
      error: (e) => this.fail(e),
    });
  }

  /** Runs a call that returns the new game state and applies it. */
  private run(call: Observable<GameState>): void {
    this.busy.set(true);
    this.error.set(null);
    call.subscribe({
      next: (state) => {
        this.game.set(state);
        this.scoreboard.set(state.scoreboard);
        this.mode.set(state.mode);
        this.busy.set(false);
      },
      error: (e) => this.fail(e),
    });
  }

  private fail(e: unknown): void {
    this.busy.set(false);
    if (e instanceof HttpErrorResponse) {
      this.error.set(
        e.status === 0
          ? 'Cannot reach the game server. Start the backend on http://localhost:5080.'
          : (e.error?.detail ?? 'Something went wrong. Try again.'),
      );
    } else {
      this.error.set('Something went wrong. Try again.');
    }
  }
}
