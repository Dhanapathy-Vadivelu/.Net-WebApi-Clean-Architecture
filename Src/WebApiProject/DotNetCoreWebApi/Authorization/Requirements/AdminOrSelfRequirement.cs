using Microsoft.AspNetCore.Authorization;

namespace DotNetCoreWebApi.Authorization.Requirements;

public class AdminOrSelfRequirement : IAuthorizationRequirement
{
}
