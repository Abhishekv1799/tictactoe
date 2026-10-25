import { Component, input } from '@angular/core';
import { MoveEntry } from '../models';

@Component({
  selector: 'app-move-history',
  standalone: true,
  template: `
    <section class="panel" aria-labelledby="history-title">
      <h2 id="history-title">Move history</h2>
      @if (moves().length === 0) {
        <p class="empty">No moves yet. Pick a cell to start.</p>
      } @else {
        <div class="scroll" tabindex="0" role="region" aria-labelledby="history-title">
          <table>
            <thead>
              <tr><th scope="col">Move</th><th scope="col">Player</th><th scope="col">Position</th></tr>
            </thead>
            <tbody>
              @for (m of moves(); track m.moveNumber) {
                <tr>
                  <td>{{ m.moveNumber }}</td>
                  <td [class.x]="m.player === 'X'" [class.o]="m.player === 'O'">{{ m.player }}</td>
                  <td>Row {{ m.row }}, Column {{ m.column }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: `
    .panel { background: var(--card); border: 1px solid var(--line); border-radius: 20px; padding: 16px; }
    h2 { margin: 0 0 10px; font-size: 1.05rem; font-weight: 700; }
    .empty { margin: 0; color: var(--muted); }
    .scroll { max-height: 300px; overflow-y: auto; border-radius: 10px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 9px 10px; }
    th { position: sticky; top: 0; background: var(--card); color: var(--muted);
         font-size: 0.85rem; font-weight: 600; border-bottom: 1px solid var(--line); }
    tbody tr:nth-child(even) { background: rgba(255, 255, 255, 0.03); }
    td:first-child, th:first-child { width: 4rem; font-variant-numeric: tabular-nums; color: var(--muted); }
    td.x { color: var(--x); font-weight: 800; }
    td.o { color: var(--o); font-weight: 800; }
  `,
})
export class MoveHistoryComponent {
  readonly moves = input.required<MoveEntry[]>();
}
