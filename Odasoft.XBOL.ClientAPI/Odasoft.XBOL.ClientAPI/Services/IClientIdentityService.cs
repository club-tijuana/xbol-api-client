using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Responses;
using System.Security.Claims;

namespace Odasoft.XBOL.ClientAPI.Services;

public interface IClientIdentityService
{
    Task<ClientDTO?> TryResolveCurrentClientAsync(ClaimsPrincipal principal);

    Task<ClientDTO> RequireCurrentClientAsync(ClaimsPrincipal principal);

    Task<AuthMeResponse> GetMeAsync(ClaimsPrincipal principal);

    Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<AuthMeResponse> CompleteLoginAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
