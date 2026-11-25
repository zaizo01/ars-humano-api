using FluentValidation;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Middleware;

/// <summary>Traduce excepciones a respuestas ProblemDetails (RFC 9457) consistentes.</summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Uno o más datos no son válidos.",
            },
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Recurso no encontrado.", Detail = exception.Message },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflicto.", Detail = exception.Message },
            DomainException de => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Regla de negocio incumplida.",
                Detail = de.Message,
                Extensions = { ["code"] = de.Code },
            },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Error interno del servidor." },
        };

        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Excepción no controlada");
        }
        else
        {
            logger.LogInformation("Request rechazado ({Status}): {Message}", problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
