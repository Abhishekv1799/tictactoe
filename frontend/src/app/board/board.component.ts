import { Component, computed, input, output } from '@angular/core';
import { Player } from '../models';

@Component({
  selector: 'app-board',
  standalone: true,
  template: `
    <div class="board">
      <svg class="grid" viewBox="0 0 300 300" aria-hidden="true">
        <path d="M100 12V288M200 12V288M12 100H288M12 200H288" />
      </svg>

      <div class="cells" role="group" aria-label="Tic Tac Toe board">
        @for (cell of board(); track $index) {
          <button
            type="button"
            class="cell"
            [class.win-x]="isWinning($index) && cell === 'X'"
            [class.win-o]="isWinning($index) && cell === 'O'"
            [disabled]="cell !== null || disabled()"
            [attr.aria-label]="label($index, cell)"
            (click)="cellClick.emit($index)"
          >
            @if (cell === 'X') {
              <svg class="mark real x" viewBox="0 0 100 100" aria-hidden="true">
                <path pathLength="1" d="M27 27L73 73M73 27L27 73" />
              </svg>
            } @else if (cell === 'O') {
              <svg class="mark real o" viewBox="0 0 100 100" aria-hidden="true">
                <circle pathLength="1" cx="50" cy="50" r="24" />
              </svg>
            } @else if (!disabled()) {
              <svg
                class="mark ghost"
                [class.x]="currentPlayer() === 'X'"
                [class.o]="currentPlayer() === 'O'"
                viewBox="0 0 100 100"
                aria-hidden="true"
              >
                @if (currentPlayer() === 'X') {
                  <path d="M27 27L73 73M73 27L27 73" />
                } @else {
                  <circle cx="50" cy="50" r="24" />
                }
              </svg>
            }
          </button>
        }
      </div>

      @if (strike(); as s) {
        <svg class="strike" [class.x]="winner() === 'X'" [class.o]="winner() === 'O'"
             viewBox="0 0 300 300" aria-hidden="true">
          <line pathLength="1" [attr.x1]="s.x1" [attr.y1]="s.y1" [attr.x2]="s.x2" [attr.y2]="s.y2" />
        </svg>
      }
    </div>
  `,
  styles: `
    .board { position: relative; width: 100%; aspect-ratio: 1; }
    svg { display: block; }
    .grid, .strike { position: absolute; inset: 0; width: 100%; height: 100%; pointer-events: none; }
    .grid path { fill: none; stroke: var(--chalk); stroke-width: 5; stroke-linecap: round; opacity: 0.55; }

    .cells {
      position: absolute; inset: 0;
      display: grid; grid-template-columns: repeat(3, 1fr); grid-template-rows: repeat(3, 1fr);
    }
    .cell {
      background: transparent; border: 0; border-radius: 16px; margin: 3.5%;
      padding: 12%; cursor: pointer; display: grid; place-items: center;
      transition: background 120ms;
    }
    .cell:disabled { cursor: default; }
    .cell:not(:disabled):hover { background: rgba(255, 255, 255, 0.07); }
    .cell.win-x { background: var(--x-soft); }
    .cell.win-o { background: var(--o-soft); }

    .mark { width: 100%; height: 100%; overflow: visible; }
    .mark path, .mark circle {
      fill: none; stroke: currentColor; stroke-width: 10; stroke-linecap: round;
    }
    .x { color: var(--x); }
    .o { color: var(--o); }

    .mark.real path, .mark.real circle {
      stroke-dasharray: 1; stroke-dashoffset: 1;
      animation: draw 260ms ease-out forwards;
    }

    .ghost { opacity: 0; transition: opacity 120ms; }
    .cell:hover .ghost, .cell:focus-visible .ghost { opacity: 0.3; }

    .strike { color: var(--text); }
    .strike.x { color: var(--x); }
    .strike.o { color: var(--o); }
    .strike line {
      stroke: currentColor; stroke-width: 8; stroke-linecap: round;
      stroke-dasharray: 1; stroke-dashoffset: 1;
      filter: drop-shadow(0 0 6px currentColor);
      animation: draw 420ms ease-out 120ms forwards;
    }

    @keyframes draw { to { stroke-dashoffset: 0; } }
    @media (prefers-reduced-motion: reduce) {
      .mark.real path, .mark.real circle, .strike line { animation: none; stroke-dashoffset: 0; }
      .ghost, .cell { transition: none; }
    }
  `,
})
export class BoardComponent {
  readonly board = input.required<(Player | null)[]>();
  readonly winningCells = input<number[] | null>(null);
  readonly winner = input<Player | null>(null);
  readonly currentPlayer = input<Player>('X');
  readonly disabled = input(false);

  readonly cellClick = output<number>();

  /** Line drawn through the winning cells, in the 300x300 board coordinate space. */
  readonly strike = computed(() => {
    const cells = this.winningCells();
    if (!cells || cells.length < 3) return null;

    const center = (i: number) => ({ x: (i % 3) * 100 + 50, y: Math.floor(i / 3) * 100 + 50 });
    const a = center(cells[0]);
    const b = center(cells[cells.length - 1]);
    const len = Math.hypot(b.x - a.x, b.y - a.y);
    const ux = (b.x - a.x) / len;
    const uy = (b.y - a.y) / len;
    const overshoot = 28;

    return {
      x1: a.x - ux * overshoot,
      y1: a.y - uy * overshoot,
      x2: b.x + ux * overshoot,
      y2: b.y + uy * overshoot,
    };
  });

  isWinning(index: number): boolean {
    return this.winningCells()?.includes(index) ?? false;
  }

  label(index: number, cell: Player | null): string {
    const row = Math.floor(index / 3) + 1;
    const column = (index % 3) + 1;
    return `Row ${row}, column ${column}, ${cell ?? 'empty'}`;
  }
}
