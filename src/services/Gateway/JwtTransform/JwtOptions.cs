using System.ComponentModel.DataAnnotations;

namespace Gateway.JwtTransform;

public class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Required]
    [MinLength(32, ErrorMessage = "Key must be at least 32 characters for HMAC-SHA256.")]
    public string Key { get; set; } = default!;

    [Range(
        typeof(TimeSpan),
        "00:01:00",
        "23:59:59",
        ErrorMessage = "Lifetime must be between {1} and {2}",
        MinimumIsExclusive = false,
        MaximumIsExclusive = false
    )]
    public TimeSpan Lifetime { get; set; }
}
