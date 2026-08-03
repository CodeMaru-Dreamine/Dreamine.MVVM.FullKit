using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace FamiliesApp.Services;

/// <summary>Maps the HTTP multipart path used by the mobile-safe uploader.</summary>
public static class FamilyDirectUploadEndpoints
{
    // The admin policy allows a 2 GiB video. Leave room for multipart headers so a file
    // at that configured limit is not rejected by the transport layer first.
    public const long MaximumRequestBytes = (2L * 1024L * 1024L * 1024L) + (32L * 1024L * 1024L);

    public static IEndpointConventionBuilder MapFamilyDirectUpload(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/api/families/uploads", HandleUploadAsync)
            // The unguessable, one-use ticket is the CSRF capability. Some existing family
            // tenants still use the legacy per-album password and therefore have no OAuth cookie.
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleUploadAsync(
        HttpContext context,
        FamilyDirectUploadService uploadService,
        ILogger<FamilyDirectUploadService> logger,
        CancellationToken ct)
    {
        try
        {
            if (!context.Request.HasFormContentType)
            {
                return UploadError("invalid_form", StatusCodes.Status400BadRequest);
            }

            if (context.Request.ContentLength is > MaximumRequestBytes)
            {
                return UploadError("too_large", StatusCodes.Status413PayloadTooLarge);
            }

            var ticket = context.Request.Headers["X-Family-Upload-Ticket"].ToString();
            if (string.IsNullOrWhiteSpace(ticket))
            {
                return UploadError(FamilyUploadErrorCodes.TicketExpired, StatusCodes.Status410Gone);
            }

            // Validate and consume the opaque capability before ReadFormAsync buffers any body.
            var requestUser = FamilyUserContext.FromPrincipal(context.User);
            var reservation = await uploadService.ReserveTicketAsync(ticket, requestUser, ct)
                .ConfigureAwait(false);

            var form = await context.Request.ReadFormAsync(ct).ConfigureAwait(false);
            if (form.Files.Count != 1)
            {
                return UploadError("invalid_file_count", StatusCodes.Status400BadRequest);
            }

            var file = form.Files[0];
            await using var stream = file.OpenReadStream();
            var result = await uploadService.UploadAsync(
                reservation,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (FamilyUploadTicketException ex)
        {
            return UploadError(ex.ErrorCode, StatusCodes.Status410Gone);
        }
        catch (FamilyUploadValidationException ex)
        {
            var statusCode = ex.ErrorCode == FamilyUploadErrorCodes.Unauthorized
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return UploadError(ex.ErrorCode, statusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return UploadError(FamilyUploadErrorCodes.Unauthorized, StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException)
        {
            return UploadError("invalid_upload", StatusCodes.Status400BadRequest);
        }
        catch (InvalidDataException)
        {
            return UploadError("too_large", StatusCodes.Status413PayloadTooLarge);
        }
        catch (BadHttpRequestException ex)
        {
            return UploadError("invalid_request", ex.StatusCode);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "A direct family upload could not be persisted.");
            return UploadError("storage_failed", StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult UploadError(string errorCode, int statusCode) =>
        Results.Json(new { error = errorCode }, statusCode: statusCode);
}
