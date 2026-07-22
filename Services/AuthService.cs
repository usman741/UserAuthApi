using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserAuthApi.DTOs;
using UserAuthApi.Models;
using UserAuthApi.Repositories;

namespace UserAuthApi.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        string normalizedEmail = registerDto.Email.Trim().ToLowerInvariant();
        string normalizedPhone = registerDto.PhoneNumber.Trim();

        User? existingEmail = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (existingEmail is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        User? existingPhone =
            await _userRepository.GetByPhoneNumberAsync(normalizedPhone);

        if (existingPhone is not null)
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        User user = new()
        {
            Name = registerDto.Name.Trim(),
            PhoneNumber = normalizedPhone,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
await _userRepository.SaveChangesAsync();

        return CreateAuthResponse(user, "Registration successful.");
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        string normalizedEmail = loginDto.Email.Trim().ToLowerInvariant();

        User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        bool passwordValid = BCrypt.Net.BCrypt.Verify(
            loginDto.Password,
            user.PasswordHash
        );

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return CreateAuthResponse(user, "Login successful.");
    }

    private AuthResponseDto CreateAuthResponse(User user, string message)
    {
        return new AuthResponseDto
        {
            Message = message,
            Token = GenerateJwtToken(user),
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        string? jwtKey = _configuration["Jwt:Key"];
        string? issuer = _configuration["Jwt:Issuer"];
        string? audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is missing.");
        }

        Claim[] claims =
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name)
        };

        SymmetricSecurityKey key =
            new(Encoding.UTF8.GetBytes(jwtKey));

        SigningCredentials credentials =
            new(key, SecurityAlgorithms.HmacSha256);

        int durationInMinutes =
            _configuration.GetValue<int>("Jwt:DurationInMinutes");

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}