using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Api.Services;

/// <summary>Turns known exceptions into RFC 7807 problem responses; anything else becomes a 500.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        (int Status, string Title, string Detail)? mapped = exception switch
        {
            GameException g => (g.StatusCode, g.Title, g.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "The game was changed by another request. Reload it and try again."),
            _ => null
        };

        if (mapped is not { } m)
            return false;

        context.Response.StatusCode = m.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = m.Status, Title = m.Title, Detail = m.Detail }
        });
    }
}
