import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GameMode, GameState, MoveRequest, Scoreboard } from './models';

/** Thin wrapper over the REST API (see backend docs/API.md). */
@Injectable({ providedIn: 'root' })
export class GameApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  createGame(mode: GameMode): Observable<GameState> {
    return this.http.post<GameState>(`${this.base}/games`, { mode });
  }

  move(gameId: string, request: MoveRequest): Observable<GameState> {
    return this.http.post<GameState>(`${this.base}/games/${gameId}/moves`, request);
  }

  undo(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.base}/games/${gameId}/undo`, {});
  }

  reset(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.base}/games/${gameId}/reset`, {});
  }

  /** Resets the scoreboard of one mode; the other mode keeps its totals. */
  resetScoreboard(mode: GameMode): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.base}/scoreboard/reset`, {}, { params: { mode } });
  }
}
