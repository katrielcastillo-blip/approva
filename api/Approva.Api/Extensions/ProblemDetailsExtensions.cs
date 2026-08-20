using System.Net;
using Approva.Application.Common.Exceptions;
using Approva.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Approva.Application.Common.Exceptions.ValidationException;

namespace Approva.Api.Extensions;

/// <summary>Maps every exception type our handlers throw to the right RFC 7807
/// ProblemDetails status code, in one place, instead of try/catch in every endpoint.</summary>
public class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Error de validación"),
            Approva.Application.Common.Exceptions.NotFoundException => (HttpStatusCode.NotFound, "No encontrado"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Prohibido"),
            UnauthorizedException => (HttpStatusCode.Unauthorized, "No autorizado"),
            ConflictException => (HttpStatusCode.Conflict, "Conflicto"),
            DomainException => (HttpStatusCode.BadRequest, "Regla de negocio violada"),
            _ => (HttpStatusCode.InternalServerError, "Error interno")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Excepción no controlada en {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
            problemDetails.Extensions["errors"] = validationException.Errors;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
