using System.ComponentModel.DataAnnotations;

namespace DotNetLibrary.Shared.Payload.Users;

public class PagedRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public class CreateUserRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(25, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

}

public class UpdateUserRequest
{
    [Required]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UserListRequest : PagedRequest
{
}

public class ChangePasswordRequest
{
    [Required]
    public int Id { get; set; }

    [Required, StringLength(25, MinimumLength = 6)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(25, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyRegistrationOtpRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}

public class ResendRegistrationOtpRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
