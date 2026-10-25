import { Component, computed, input } from '@angular/core';
import { GameMode, Player, Scoreboard } from '../models';

@Component({
  selector: 'app-scoreboard',
  standalone: true,
  template: `
    <section class="board" [attr.aria-label]="title()">
    <h2 class="title">
      <span class="badge" [class.computer]="vsComputer()">{{ vsComputer() ? 'Vs computer' : 'Two players' }}</span>
      Scoreboard
    </h2>
    <div class="strip">
      <div class="tile x" [class.active]="active() === 'X'" [class.won]="winner() === 'X'">
        <div class="who">
          <svg viewBox="0 0 100 100" aria-hidden="true"><path d="M24 24L76 76M76 24L24 76" /></svg>
          <span>{{ vsComputer() ? 'You' : 'Player X' }}</span>
        </div>
        <div class="score"><strong>{{ score().xWins }}</strong><span>{{ score().xWins === 1 ? 'win' : 'wins' }}</span></div>
      </div>

      <div class="tile draws">
        <div class="who"><span>Draws</span></div>
        <div class="score"><strong>{{ score().draws }}</strong></div>
      </div>

      <div class="tile o" [class.active]="active() === 'O'" [class.won]="winner() === 'O'">
        <div class="who">
          <svg viewBox="0 0 100 100" aria-hidden="true"><circle cx="50" cy="50" r="27" /></svg>
          <span>{{ vsComputer() ? 'Computer' : 'Player O' }}</span>
        </div>
        <div class="score"><strong>{{ score().oWins }}</strong><span>{{ score().oWins === 1 ? 'win' : 'wins' }}</span></div>
      </div>
    </div>
    </section>
  `,
  styles: `
    .title { display: flex; align-items: center; gap: 10px; margin: 0 0 10px; font-size: 1.05rem; font-weight: 700; }
    .badge { background: var(--x-soft); color: var(--x); border-radius: 999px; padding: 2px 10px; font-size: 0.8rem; font-weight: 700; }
    .badge.computer { background: var(--o-soft); color: var(--o); }
    .strip { display: grid; grid-template-columns: 1fr 0.7fr 1fr; gap: 10px; width: 100%; }
    .tile {
      background: var(--card); border: 1px solid var(--line); border-radius: 16px;
      padding: 12px 14px; transition: border-color 150ms, box-shadow 150ms, background 150ms;
    }
    .who { display: flex; align-items: center; gap: 8px; color: var(--muted); font-size: 0.9rem; font-weight: 600; min-height: 24px; }
    .who svg { width: 22px; height: 22px; flex: none; }
    .who path, .who circle { fill: none; stroke: currentColor; stroke-width: 13; stroke-linecap: round; }
    .score { display: flex; align-items: baseline; gap: 6px; margin-top: 2px; }
    .score strong { font-size: 2rem; font-weight: 800; line-height: 1.1; font-variant-numeric: tabular-nums; }
    .score span { color: var(--muted); font-size: 0.85rem; }

    .x .who svg, .x .score strong { color: var(--x); }
    .o .who svg, .o .score strong { color: var(--o); }

    .tile.x.active { border-color: var(--x); box-shadow: 0 0 0 3px rgba(255, 200, 61, 0.22); }
    .tile.o.active { border-color: var(--o); box-shadow: 0 0 0 3px rgba(56, 217, 200, 0.22); }
    .tile.x.won { background: var(--x-soft); border-color: var(--x); }
    .tile.o.won { background: var(--o-soft); border-color: var(--o); }
    .tile.active .who, .tile.won .who { color: var(--text); }

    @media (max-width: 420px) {
      .strip { gap: 6px; }
      .tile { padding: 10px; }
      .score strong { font-size: 1.8rem; }
    }
    @media (prefers-reduced-motion: reduce) { .tile { transition: none; } }
  `,
})
export class ScoreboardComponent {
  readonly score = input.required<Scoreboard>();
  /** Whose turn it is, or null when the game is over. */
  readonly active = input<Player | null>(null);
  readonly winner = input<Player | null>(null);
  readonly mode = input<GameMode>('TwoPlayer');

  readonly vsComputer = computed(() => this.mode() === 'VsComputer');
  readonly title = computed(() => (this.vsComputer() ? 'Vs computer scoreboard' : 'Two player scoreboard'));
}
