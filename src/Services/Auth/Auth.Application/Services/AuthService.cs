using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using RecruitmentPlatform.Common;
using RecruitmentPlatform.Contracts.Auth;
using RecruitmentPlatform.Contracts.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.DTOs;

namespace Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly JwtSettings _jwtSettings;
    private readonly IUserProfileClient _userProfileClient;
    private readonly IEmailService _emailService;

    public AuthService(IAuthRepository repository, IOptions<JwtSettings> jwtSettings, IUserProfileClient userProfileClient, IEmailService emailService)
    {
        _repository = repository;
        _jwtSettings = jwtSettings.Value;
        _userProfileClient = userProfileClient;
        _emailService = emailService;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _repository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new Exception("Email already exists");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Status = UserStatus.Active
        };

        await _repository.CreateAsync(user);

        return await GenerateTokenResponse(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Invalid email or password");
        }

        if (user.Status == UserStatus.Locked)
        {
            throw new Exception("Account is locked");
        }

        return await GenerateTokenResponse(user);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _repository.GetRefreshTokenAsync(refreshToken);
        
        if (token == null || token.Revoked || token.ExpiredAt < DateTime.UtcNow)
        {
            throw new Exception("Invalid or expired refresh token");
        }

        await _repository.RevokeRefreshTokenAsync(refreshToken);
        
        return await GenerateTokenResponse(token.User);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        await _repository.RevokeRefreshTokenAsync(refreshToken);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new Exception("Invalid old password");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _repository.UpdateAsync(user);
    }

    public async Task LockUserAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.Status = UserStatus.Locked;
        await _repository.UpdateAsync(user);
    }

    public async Task UnlockUserAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.Status = UserStatus.Active;
        await _repository.UpdateAsync(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _repository.GetAllUsersAsync();
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        });
    }

    public async Task<InternalUserInfoDto?> GetInternalUserInfoByIdAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        return user == null ? null : MapToInternalUserInfoDto(user);
    }

    public async Task<IEnumerable<InternalUserInfoDto>> GetInternalUserInfosByIdsAsync(IEnumerable<int> userIds)
    {
        var users = await _repository.GetByIdsAsync(userIds);
        return users.Select(MapToInternalUserInfoDto);
    }

    public async Task<IEnumerable<InternalUserInfoDto>> GetInternalUserInfosByRolesAsync(IEnumerable<string> roles)
    {
        var parsedRoles = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => Enum.TryParse<UserRole>(r.Trim(), true, out var role) ? role : (UserRole?)null)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .Distinct()
            .ToList();

        if (parsedRoles.Count == 0)
        {
            return Enumerable.Empty<InternalUserInfoDto>();
        }

        var users = await _repository.GetByRolesAsync(parsedRoles);
        return users
            .Where(u => u.Status == UserStatus.Active)
            .Select(MapToInternalUserInfoDto);
    }

    private async Task<LoginResponse> GenerateTokenResponse(User user)
    {
        int? employeeId = null;
        int? companyId = null;

        if (user.Role == UserRole.USER)
            employeeId = await _userProfileClient.GetEmployeeIdByUserIdAsync(user.Id);
        else if (user.Role == UserRole.RECRUITER)
            companyId = await _userProfileClient.GetCompanyIdByUserIdAsync(user.Id);

        var accessToken = GenerateAccessToken(user, employeeId, companyId);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        await _repository.AddRefreshTokenAsync(refreshTokenEntity);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                EmployeeId = employeeId,
                CompanyId = companyId
            }
        };

        if (user.Role == UserRole.USER && employeeId == null)
            response.Message = "Chưa nhập thông tin người dùng";
        else if (user.Role == UserRole.RECRUITER && companyId == null)
            response.Message = "Chưa nhập thông tin công ty";

        return response;
    }

    private string GenerateAccessToken(User user, int? employeeId = null, int? companyId = null)
    {
        var claimsList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        if (employeeId.HasValue)
            claimsList.Add(new Claim("employeeId", employeeId.Value.ToString()));

        if (companyId.HasValue)
            claimsList.Add(new Claim("companyId", companyId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claimsList,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static InternalUserInfoDto MapToInternalUserInfoDto(User user)
    {
        return new InternalUserInfoDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            CreateAt = user.CreatedAt
        };
    }

    // =========== Forgot Password ===========

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email);
        // Luôn trả về thành công để tránh lộ thông tin email có tồn tại hay không
        if (user == null) return;

        if (user.Status == UserStatus.Locked)
            throw new Exception("Tài khoản đã bị khóa");

        // Hủy tất cả token cũ chưa dùng
        await _repository.InvalidatePasswordResetTokensAsync(request.Email);

        // Tạo token ngẫu nhiên 6 chữ số (OTP)
        var otp = GenerateOtp();

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = otp,
            ExpiredAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        await _repository.AddPasswordResetTokenAsync(resetToken);
        await _emailService.SendPasswordResetEmailAsync(request.Email, otp);
    }

    public async Task<bool> VerifyResetTokenAsync(VerifyResetTokenRequest request)
    {
        var token = await _repository.GetValidPasswordResetTokenAsync(request.Email, request.Token);
        return token != null;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var token = await _repository.GetValidPasswordResetTokenAsync(request.Email, request.Token);
        if (token == null)
            throw new Exception("Mã OTP không hợp lệ hoặc đã hết hạn");

        var user = await _repository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("Người dùng không tồn tại");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _repository.UpdateAsync(user);

        // Đánh dấu token đã dùng
        token.IsUsed = true;
        await _repository.InvalidatePasswordResetTokensAsync(request.Email);
    }

    private static string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return value.ToString("D6");
    }
}
