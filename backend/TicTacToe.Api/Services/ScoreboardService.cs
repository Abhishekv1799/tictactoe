using TicTacToe.Api.Contracts;
using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Data.Repositories;

namespace TicTacToe.Api.Services;

public sealed class ScoreboardService(IScoreboardRepository scoreboards, IUnitOfWork unitOfWork)
{
    public async Task<ScoreboardDto> GetAsync(GameMode mode, CancellationToken ct)
    {
        ValidateMode(mode);
        return GameMapper.ToDto(await scoreboards.GetAsync(mode, ct));
    }

    public async Task<ScoreboardDto> ResetAsync(GameMode mode, CancellationToken ct)
    {
        ValidateMode(mode);
        await scoreboards.ResetAsync(mode, ct);
        await unitOfWork.CommitAsync(ct);
        return GameMapper.ToDto(await scoreboards.GetAsync(mode, ct));
    }

    private static void ValidateMode(GameMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw GameException.BadRequest("Mode must be TwoPlayer or VsComputer.");
    }
}
