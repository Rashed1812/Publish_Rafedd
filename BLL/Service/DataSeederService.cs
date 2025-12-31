using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.Subscription;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BLL.Service
{
    public class DataSeederService : IDataSeederService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IManagerRepository _managerRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataSeederService> _logger;
        private readonly string _dataSeedPath;

        public DataSeederService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ISubscriptionService subscriptionService,
            IManagerRepository managerRepository,
            IConfiguration configuration,
            ILogger<DataSeederService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _subscriptionService = subscriptionService;
            _managerRepository = managerRepository;
            _configuration = configuration;
            _logger = logger;
            
            // Get the base directory - try multiple paths
            var baseDirectory = AppContext.BaseDirectory;
            var possiblePaths = new[]
            {
                // From bin folder (during runtime)
                Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "DAL", "Data", "DataSeed")),
                // From current directory
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "DAL", "Data", "DataSeed")),
                // From project root (if running from project directory)
                Path.GetFullPath("DAL/Data/DataSeed"),
                // Absolute path fallback
                Path.Combine(Path.GetDirectoryName(typeof(DataSeederService).Assembly.Location) ?? "", "..", "..", "..", "..", "DAL", "Data", "DataSeed")
            };

            string? foundPath = null;
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    foundPath = fullPath;
                    break;
                }
            }

            // Set the path (use found path or default fallback)
            _dataSeedPath = foundPath ?? Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "DAL", "Data", "DataSeed"));
            
            if (foundPath != null)
            {
                _logger.LogInformation("DataSeed path found: {Path}", _dataSeedPath);
            }
            else
            {
                _logger.LogWarning("DataSeed directory not found in any expected location, using default path: {Path}", _dataSeedPath);
            }
        }

        public async Task<bool> SeedSubscriptionPlansAsync()
        {
            try
            {
                var jsonPath = Path.Combine(_dataSeedPath, "SubscriptionPlans.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("SubscriptionPlans.json not found at {Path}", jsonPath);
                    return false;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var plans = JsonSerializer.Deserialize<List<SubscriptionPlanSeed>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (plans == null || !plans.Any())
                {
                    _logger.LogWarning("No subscription plans found in JSON file");
                    return false;
                }

                int addedCount = 0;
                foreach (var planData in plans)
                {
                    var existingPlan = await _context.SubscriptionPlans
                        .FirstOrDefaultAsync(p => p.Id == planData.Id || p.Name == planData.Name);

                    if (existingPlan == null)
                    {
                        // Use raw SQL to insert with explicit ID value
                        await _context.Database.ExecuteSqlRawAsync(@"
                            SET IDENTITY_INSERT SubscriptionPlans ON;
                            INSERT INTO SubscriptionPlans (Id, Name, PricePerMonth, MaxEmployees, Description, IsActive)
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5});
                            SET IDENTITY_INSERT SubscriptionPlans OFF;",
                            planData.Id,
                            planData.Name,
                            planData.PricePerMonth,
                            planData.MaxEmployees,
                            planData.Description ?? "",
                            planData.IsActive);

                        addedCount++;
                    }
                    else
                    {
                        // Update existing plan
                        existingPlan.PricePerMonth = planData.PricePerMonth;
                        existingPlan.MaxEmployees = planData.MaxEmployees;
                        existingPlan.Description = planData.Description;
                        existingPlan.IsActive = planData.IsActive;
                        _context.SubscriptionPlans.Update(existingPlan);
                        await _context.SaveChangesAsync();
                    }
                }
                _logger.LogInformation("Seeded {Count} subscription plans", addedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding subscription plans");
                return false;
            }
        }

        public async Task<bool> SeedAdminUsersAsync()
        {
            try
            {
                var jsonPath = Path.Combine(_dataSeedPath, "AdminUsers.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("AdminUsers.json not found at {Path}", jsonPath);
                    return false;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var adminUsers = JsonSerializer.Deserialize<List<AdminUserSeed>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (adminUsers == null || !adminUsers.Any())
                {
                    _logger.LogWarning("No admin users found in JSON file");
                    return false;
                }

                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                int addedCount = 0;
                foreach (var adminData in adminUsers)
                {
                    var existingUser = await _userManager.FindByEmailAsync(adminData.Email);
                    if (existingUser == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = adminData.Email,
                            Email = adminData.Email,
                            FullName = adminData.FullName,
                            PhoneNumber = adminData.PhoneNumber,
                            IsActive = adminData.IsActive
                        };

                        var result = await _userManager.CreateAsync(user, adminData.Password);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");

                            var admin = new Admin
                            {
                                UserId = user.Id,
                                FullName = adminData.FullName,
                                Email = adminData.Email,
                                PhoneNumber = adminData.PhoneNumber,
                                IsActive = adminData.IsActive
                            };

                            _context.Admins.Add(admin);
                            addedCount++;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create admin user {Email}: {Errors}", 
                                adminData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} admin users", addedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding admin users");
                return false;
            }
        }

        public async Task<bool> SeedManagerUsersAsync()
        {
            try
            {
                var jsonPath = Path.Combine(_dataSeedPath, "ManagerUsers.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("ManagerUsers.json not found at {Path}", jsonPath);
                    return false;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var managerUsers = JsonSerializer.Deserialize<List<ManagerUserSeed>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (managerUsers == null || !managerUsers.Any())
                {
                    _logger.LogWarning("No manager users found in JSON file");
                    return false;
                }

                // Ensure Manager role exists
                if (!await _roleManager.RoleExistsAsync("Manager"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Manager"));
                }

                int addedCount = 0;
                foreach (var managerData in managerUsers)
                {
                    var existingUser = await _userManager.FindByEmailAsync(managerData.Email);
                    if (existingUser == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = managerData.Email,
                            Email = managerData.Email,
                            FullName = managerData.FullName,
                            IsActive = managerData.IsActive
                        };

                        var result = await _userManager.CreateAsync(user, managerData.Password);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "Manager");

                            var manager = new Manager
                            {
                                UserId = user.Id,
                                CompanyName = managerData.CompanyName,
                                BusinessType = managerData.BusinessType,
                                BusinessDescription = managerData.BusinessDescription,
                                IsActive = managerData.IsActive
                            };

                            _context.Managers.Add(manager);
                            await _context.SaveChangesAsync();

                            // Create subscription using the service
                            try
                            {
                                await _subscriptionService.CreateSubscriptionAsync(user.Id, managerData.SubscriptionPlanId);
                                addedCount++;
                                _logger.LogInformation("Created manager {Email} with subscription plan {PlanId}", 
                                    managerData.Email, managerData.SubscriptionPlanId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create subscription for manager {Email}", managerData.Email);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create manager user {Email}: {Errors}", 
                                managerData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                _logger.LogInformation("Seeded {Count} manager users", addedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding manager users");
                return false;
            }
        }

        public async Task<bool> SeedEmployeeUsersAsync()
        {
            try
            {
                var jsonPath = Path.Combine(_dataSeedPath, "EmployeeUsers.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("EmployeeUsers.json not found at {Path}", jsonPath);
                    return false;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var employeeUsers = JsonSerializer.Deserialize<List<EmployeeUserSeed>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (employeeUsers == null || !employeeUsers.Any())
                {
                    _logger.LogWarning("No employee users found in JSON file");
                    return false;
                }

                // Ensure Employee role exists
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));
                }

                int addedCount = 0;
                foreach (var employeeData in employeeUsers)
                {
                    var existingUser = await _userManager.FindByEmailAsync(employeeData.Email);
                    if (existingUser == null)
                    {
                        // Find manager by email
                        var managerUser = await _userManager.FindByEmailAsync(employeeData.ManagerEmail);
                        if (managerUser == null)
                        {
                            _logger.LogWarning("Manager with email {Email} not found for employee {EmployeeEmail}", 
                                employeeData.ManagerEmail, employeeData.Email);
                            continue;
                        }

                        var user = new ApplicationUser
                        {
                            UserName = employeeData.Email,
                            Email = employeeData.Email,
                            FullName = employeeData.FullName,
                            IsActive = employeeData.IsActive
                        };

                        var result = await _userManager.CreateAsync(user, employeeData.Password);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "Employee");

                            var employee = new Employee
                            {
                                UserId = user.Id,
                                ManagerUserId = managerUser.Id,
                                Position = employeeData.Position,
                                IsActive = employeeData.IsActive
                            };

                            _context.Employees.Add(employee);

                            // Update manager employee count
                            // No need to update CurrentEmployeeCount - removed from Manager model
                            // Employee count is now queried directly from database

                            addedCount++;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create employee user {Email}: {Errors}", 
                                employeeData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} employee users", addedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding employee users");
                return false;
            }
        }

        public async Task<bool> SeedAllDataAsync()
        {
            _logger.LogInformation("Starting seed data process...");

            var results = new Dictionary<string, bool>
            {
                { "SubscriptionPlans", await SeedSubscriptionPlansAsync() },
                { "AdminUsers", await SeedAdminUsersAsync() },
                { "ManagerUsers", await SeedManagerUsersAsync() },
                { "EmployeeUsers", await SeedEmployeeUsersAsync() }
            };

            var successCount = results.Count(r => r.Value);
            var totalCount = results.Count;

            _logger.LogInformation("Seed data process completed. {Success}/{Total} succeeded", successCount, totalCount);

            return successCount == totalCount;
        }

        // Seed data models
        private class SubscriptionPlanSeed
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public decimal PricePerMonth { get; set; }
            public int MaxEmployees { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }
        }

        private class AdminUserSeed
        {
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string? PhoneNumber { get; set; }
            public bool IsActive { get; set; }
        }

        private class ManagerUserSeed
        {
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string CompanyName { get; set; } = null!;
            public string BusinessType { get; set; } = null!;
            public string? BusinessDescription { get; set; }
            public int SubscriptionPlanId { get; set; }
            public bool IsActive { get; set; }
        }

        private class EmployeeUserSeed
        {
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string Position { get; set; } = null!;
            public string ManagerEmail { get; set; } = null!;
            public bool IsActive { get; set; }
        }
    }
}

