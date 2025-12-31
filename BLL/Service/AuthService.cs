using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.Subscription;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.DTOS.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Service
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IManagerRepository _managerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEmailService _emailService;

        public AuthService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            ISubscriptionService subscriptionService,
            IManagerRepository managerRepository,
            IEmployeeRepository employeeRepository,
            ISubscriptionRepository subscriptionRepository,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _managerRepository = managerRepository;
            _employeeRepository = employeeRepository;
            _subscriptionRepository = subscriptionRepository;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> RegisterManagerAsync(RegisterManagerDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
            }

            // Verify subscription plan exists and is active
            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == registerDto.SubscriptionPlanId && p.IsActive);
            
            if (plan == null)
            {
                throw new InvalidOperationException("خطة الاشتراك المحددة غير موجودة أو غير نشطة");
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"فشل إنشاء المستخدم: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign Manager role
            if (!await _roleManager.RoleExistsAsync("Manager"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Manager"));
            }

            await _userManager.AddToRoleAsync(user, "Manager");

            // Create Manager profile
            var manager = new Manager
            {
                UserId = user.Id,
                CompanyName = registerDto.CompanyName,
                BusinessType = registerDto.BusinessType ?? "غير محدد",
                BusinessDescription = registerDto.BusinessDescription,
                IsActive = true
            };

            _context.Managers.Add(manager);
            await _context.SaveChangesAsync();

            // Create subscription (full cycle)
            var subscription = await _subscriptionService.CreateSubscriptionAsync(user.Id, registerDto.SubscriptionPlanId);

            _logger.LogInformation("Manager registered: {UserId}, Subscription: {SubscriptionId}", 
                user.Id, subscription.Id);

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = "Manager"
                }
            };
        }

        public async Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto registerDto, string managerUserId)
        {
            // Start transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify manager exists and is active
                var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
                if (manager == null || !manager.IsActive)
                {
                    throw new InvalidOperationException("المدير غير موجود أو غير نشط");
                }

                // Check employee limit based on subscription
                var canAddEmployee = await _subscriptionService.CheckEmployeeLimitAsync(managerUserId, 1);
                if (!canAddEmployee)
                {
                    var subscription = await _subscriptionService.GetActiveSubscriptionAsync(managerUserId);
                    var maxEmployees = subscription?.MaxEmployees ?? 0;
                    var currentCount = await _employeeRepository.GetEmployeeCountByManagerAsync(managerUserId);

                    throw new InvalidOperationException(
                        $"تم الوصول للحد الأقصى من الموظفين المسموح به ({maxEmployees}). الحالي: {currentCount}");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FullName = registerDto.FullName,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"فشل إنشاء المستخدم: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Assign Employee role
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));
                }

                await _userManager.AddToRoleAsync(user, "Employee");

                // Create Employee profile
                var employee = new Employee
                {
                    UserId = user.Id,
                    ManagerUserId = managerUserId,
                    Position = registerDto.Position,
                    IsActive = true
                };

                _context.Employees.Add(employee);

                // No need to update CurrentEmployeeCount - we use database count directly
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("Employee registered: {UserId}, Manager: {ManagerUserId}",
                    user.Id, managerUserId);

                // Generate JWT token
                var token = await GenerateJwtTokenAsync(user);

                return new AuthResponseDto
                {
                    Token = token.Token,
                    RefreshToken = token.RefreshToken,
                    ExpiresAt = token.ExpiresAt,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email!,
                        Role = "Employee"
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to register employee for manager {ManagerUserId}", managerUserId);
                throw;
            }
        }

        public async Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"فشل إنشاء المستخدم: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign Admin role
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            // Create Admin profile
            var admin = new Admin
            {
                UserId = user.Id,
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin registered: {UserId}", user.Id);

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = "Admin"
                }
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign role
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
            {
                throw new InvalidOperationException($"Role {registerDto.Role} does not exist");
            }

            await _userManager.AddToRoleAsync(user, registerDto.Role);

            // Create role-specific profile
            switch (registerDto.Role.ToLower())
            {
                case "manager":
                    var manager = new Manager
                    {
                        UserId = user.Id,
                        CompanyName = registerDto.CompanyName ?? "شركة غير محددة",
                        BusinessType = registerDto.BusinessType ?? "غير محدد",
                        BusinessDescription = registerDto.BusinessDescription,
                        IsActive = true
                    };
                    _context.Managers.Add(manager);
                    break;

                case "employee":
                    if (string.IsNullOrEmpty(registerDto.ManagerUserId))
                    {
                        throw new InvalidOperationException("ManagerUserId is required for employee registration");
                    }

                    var employee = new Employee
                    {
                        UserId = user.Id,
                        ManagerUserId = registerDto.ManagerUserId,
                        Position = "موظف", // Default position, can be updated later
                        IsActive = true
                    };
                    _context.Employees.Add(employee);
                    break;

                case "admin":
                    var admin = new Admin
                    {
                        UserId = user.Id
                    };
                    _context.Admins.Add(admin);
                    break;
            }

            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = registerDto.Role
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Support both email and phone login
            ApplicationUser? user = null;
            if (loginDto.EmailOrPhone.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(loginDto.EmailOrPhone);
            }
            else
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == loginDto.EmailOrPhone);
            }

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
            {
                throw new UnauthorizedAccessException("البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            // Get user role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Employee";

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);

            var response = new AuthResponseDto
            {
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = role
                }
            };

            // Add subscription information for Managers
            if (role == "Manager")
            {
                var manager = await _managerRepository.GetByUserIdAsync(user.Id);
                if (manager != null)
                {
                    var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
                    var currentEmployeeCount = await _employeeRepository.GetEmployeeCountByManagerAsync(user.Id);

                    if (subscription != null)
                    {
                        var isActive = subscription.IsActive && subscription.EndDate > DateTime.UtcNow;
                        var daysRemaining = isActive ? (int)(subscription.EndDate - DateTime.UtcNow).TotalDays : 0;

                        response.HasActiveSubscription = isActive;
                        response.SubscriptionEndsAt = subscription.EndDate;
                        response.SubscriptionPlanId = subscription.SubscriptionPlanId;
                        response.SubscriptionPlanName = subscription.Plan?.Name;
                        response.MaxEmployees = subscription.Plan?.MaxEmployees;
                        response.CurrentEmployeeCount = currentEmployeeCount;
                        response.DaysRemaining = daysRemaining;
                    }
                    else
                    {
                        response.HasActiveSubscription = false;
                        response.CurrentEmployeeCount = currentEmployeeCount;
                    }
                }
            }

            return response;
        }
        
        public async Task<bool> LogoutAsync(string token)
        {
            // In a production system, you would blacklist the token
            // For now, we'll just validate it exists
            try
            {
                await ValidateTokenAsync(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // In a production system, you would validate the refresh token from database
            // For now, we'll generate a new token (simplified implementation)
            // TODO: Implement proper refresh token validation with database storage
            
            throw new NotImplementedException("Refresh token functionality needs proper implementation with token storage");
        }
        
        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("المستخدم غير موجود");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"فشل تغيير كلمة المرور: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return true;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured"));

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(string Token, string RefreshToken, DateTime ExpiresAt)> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Employee";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured"));
            var expires = DateTime.UtcNow.AddHours(double.Parse(_configuration["Jwt:ExpireHours"] ?? "24"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var refreshToken = Guid.NewGuid().ToString();

            return (tokenString, refreshToken, expires);
        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist for security reasons
                    _logger.LogWarning("Forgot password requested for non-existent email: {Email}", email);
                    // Return a fake token to prevent user enumeration
                    return "fake-token-" + Guid.NewGuid().ToString();
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Forgot password requested for inactive user: {Email}", email);
                    throw new InvalidOperationException("الحساب غير نشط. يرجى الاتصال بالدعم");
                }

                // Generate password reset token
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                _logger.LogInformation("Password reset token generated for user: {Email}", email);

                // Send password reset email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    email, 
                    resetToken, 
                    user.FullName ?? user.Email ?? "المستخدم"
                );

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send password reset email to: {Email}", email);
                    // Still return success to prevent user enumeration, but log the error
                }
                else
                {
                    _logger.LogInformation("Password reset email sent successfully to: {Email}", email);
                }

                // Return empty string instead of token for security
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating forgot password token for email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Reset password attempted for non-existent email: {Email}", email);
                    throw new InvalidOperationException("المستخدم غير موجود");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Reset password attempted for inactive user: {Email}", email);
                    throw new InvalidOperationException("الحساب غير نشط. يرجى الاتصال بالدعم");
                }

                // Reset the password
                var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password reset failed for user {Email}: {Errors}", email, errors);
                    throw new InvalidOperationException($"فشل إعادة تعيين كلمة المرور: {errors}");
                }

                // Update the security stamp to invalidate existing tokens
                await _userManager.UpdateSecurityStampAsync(user);

                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password reset successfully for user: {Email}", email);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", email);
                throw;
            }
        }
    }
}

