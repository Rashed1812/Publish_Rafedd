using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOS.Admin;
using Shared.DTOS.Common;
using Shared.DTOS.Subscription;
using Shared.DTOS.Users;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    //[Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserActivityRepository _userActivityRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeService _employeeService;
        private readonly IManagerService _managerService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IDataSeederService _dataSeederService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            ISubscriptionRepository subscriptionRepository,
            IPaymentRepository paymentRepository,
            IUserActivityRepository userActivityRepository,
            IManagerRepository managerRepository,
            IEmployeeRepository employeeRepository,
            IEmployeeService employeeService,
            IManagerService managerService,
            ISubscriptionService subscriptionService,
            IDataSeederService dataSeederService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _subscriptionRepository = subscriptionRepository;
            _paymentRepository = paymentRepository;
            _userActivityRepository = userActivityRepository;
            _managerRepository = managerRepository;
            _employeeRepository = employeeRepository;
            _employeeService = employeeService;
            _managerService = managerService;
            _subscriptionService = subscriptionService;
            _dataSeederService = dataSeederService;
            _logger = logger;
        }

        // Subscription Statistics
        [HttpGet("subscriptions/stats")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetSubscriptionStats()
        {
            var allSubscriptions = await _subscriptionRepository.GetAllWithDetailsAsync();
            var totalSubscriptions = allSubscriptions.Count;
            var activeSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsCountAsync();
            var totalManagers = (await _managerRepository.GetAllActiveAsync()).Count;
            var allEmployees = await _employeeRepository.GetAllAsync();
            var totalEmployees = allEmployees.Count(e => e.IsActive);
            var totalUsers = await _context.Users.CountAsync(u => u.IsActive);

            var activeSubs = allSubscriptions.Where(s => s.IsActive).ToList();
            var planStats = activeSubs
                .GroupBy(s => s.Plan!.Name)
                .Select(g => new
                {
                    PlanName = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var stats = new
            {
                TotalSubscriptions = totalSubscriptions,
                ActiveSubscriptions = activeSubscriptions,
                TotalManagers = totalManagers,
                TotalEmployees = totalEmployees,
                TotalUsers = totalUsers,
                PlanStatistics = planStats
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats, "تم الحصول على إحصائيات الاشتراكات بنجاح"));
        }

        // Revenue Statistics
        [HttpGet("revenue/stats")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetRevenueStats()
        {
            var totalRevenue = await _paymentRepository.GetTotalRevenueAsync();
            var monthlyRevenue = await _paymentRepository.GetMonthlyRevenueAsync(DateTime.UtcNow.AddMonths(-1));
            
            var allPayments = await _paymentRepository.GetRevenueStatisticsAsync();
            var paymentStats = allPayments
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            var stats = new
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                PaymentStatistics = paymentStats
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats, "تم الحصول على إحصائيات الإيرادات بنجاح"));
        }

        // User Activity
        [HttpGet("activity")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetUserActivity(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var skip = (page - 1) * pageSize;

            var activities = await _userActivityRepository.GetAllWithUsersAsync(skip, pageSize);
            var activitiesDto = activities.Select(ua => new
            {
                Id = ua.Id,
                UserName = ua.User.FullName,
                UserEmail = ua.User.Email,
                ActionType = ua.ActionType,
                Description = ua.Description,
                Timestamp = ua.Timestamp
            }).ToList();

            var totalCount = await _userActivityRepository.GetTotalCountAsync();

            var result = new
            {
                Activities = activitiesDto,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "تم الحصول على نشاط المستخدمين بنجاح"));
        }

        // Get All Managers with Filtering (NEW)
        [HttpGet("managers")]
        [ProducesResponseType(typeof(PagedResponse<ManagerDto>), 200)]
        public async Task<ActionResult<PagedResponse<ManagerDto>>> GetAllManagers(
            [FromQuery] ManagerFilterParams? filterParams)
        {
            // If no filter params provided, create empty one (all defaults)
            filterParams ??= new ManagerFilterParams();

            var result = await _managerService.GetManagersAsync(filterParams);
            return Ok(result);
        }

        // Get All Companies (Alias for Managers) - Frontend expects /companies route
        [HttpGet("companies")]
        [ProducesResponseType(typeof(PagedResponse<ManagerDto>), 200)]
        public async Task<ActionResult<PagedResponse<ManagerDto>>> GetAllCompanies(
            [FromQuery] ManagerFilterParams? filterParams)
        {
            // Companies are represented as Managers in the system
            // This is just an alias route for frontend compatibility
            filterParams ??= new ManagerFilterParams();

            var result = await _managerService.GetManagersAsync(filterParams);
            return Ok(result);
        }

        // Get Manager by ID with Employees (Super Admin)
        [HttpGet("managers/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<ActionResult<ApiResponse<object>>> GetManagerById(string id)
        {
            var manager = await _managerRepository.GetWithDetailsAsync(id);
            if (manager == null)
            {
                throw new NotFoundException("المدير غير موجود");
            }

            var employees = await _employeeRepository.GetByManagerAsync(id);
            var employeesDto = employees.Select(e => new
            {
                id = e.UserId,
                name = e.User.FullName,
                email = e.User.Email,
                phone = e.User.PhoneNumber ?? "",
                position = e.Position,
                department = e.Department
            }).ToList();

            var result = new
            {
                success = true,
                data = new
                {
                    id = manager.UserId,
                    name = manager.User.FullName,
                    email = manager.User.Email,
                    phone = manager.User.PhoneNumber ?? "",
                    role = "manager",
                    position = "مدير عام",
                    companyId = manager.Id.ToString(),
                    subscription = manager.Subscription != null ? new
                    {
                        id = manager.Subscription.Id.ToString(),
                        planName = manager.Subscription.Plan?.Name ?? "",
                        status = manager.Subscription.IsActive ? "active" : "inactive",
                        startDate = manager.Subscription.StartDate,
                        endDate = manager.Subscription.EndDate
                    } : null,
                    employees = employeesDto,
                    employeeCount = manager.Employees.Count(e => e.IsActive), // Calculate from employees collection
                    createdAt = manager.User.CreatedAt
                }
            };

            return Ok(result);
        }

        // Get All Employees with Filtering (NEW)
        [HttpGet("employees")]
        [ProducesResponseType(typeof(PagedResponse<EmployeeDto>), 200)]
        public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetAllEmployees(
            [FromQuery] EmployeeFilterParams? filterParams)
        {
            // If no filter params provided, create empty one (all defaults)
            filterParams ??= new EmployeeFilterParams();

            var result = await _employeeService.GetEmployeesAsync(filterParams);
            return Ok(result);
        }

        // All Subscriptions with Advanced Filtering
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(PagedResponse<SubscriptionDto>), 200)]
        public async Task<ActionResult<PagedResponse<SubscriptionDto>>> GetAllSubscriptions(
            [FromQuery] SubscriptionFilterParams? filterParams)
        {
            filterParams ??= new SubscriptionFilterParams();

            var result = await _subscriptionService.GetSubscriptionsAsync(filterParams);
            return Ok(result);
        }

        // All Payments
        [HttpGet("payments")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetAllPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? status = null)
        {
            var allPayments = await _paymentRepository.GetAllWithDetailsAsync(status);
            var totalCount = allPayments.Count;

            var skip = (page - 1) * pageSize;
            var payments = allPayments
                .Skip(skip)
                .Take(pageSize)
                .Select(p => new
                {
                    Id = p.Id,
                    TransactionId = p.TransactionId,
                    ManagerName = p.Subscription.Manager.User.FullName,
                    ManagerEmail = p.Subscription.Manager.User.Email,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    PaidAt = p.PaidAt
                })
                .ToList();

            var result = new
            {
                Payments = payments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "تم الحصول على المدفوعات بنجاح"));
        }

        // Seed Data Endpoints
        [HttpPost("seed/all")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> SeedAllData()
        {
            try
            {
                var result = await _dataSeederService.SeedAllDataAsync();
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم إنشاء Seed Data بنجاح (Subscription Plans, Admin Users, Manager Users, Employee Users)"));
                }
                
                throw new BadRequestException("حدث خطأ أثناء إنشاء Seed Data. تحقق من السجلات (Logs)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding all data");
                throw new BadRequestException($"حدث خطأ أثناء إنشاء Seed Data: {ex.Message}");
            }
        }

        [HttpPost("seed/subscription-plans")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> SeedSubscriptionPlans()
        {
            try
            {
                var result = await _dataSeederService.SeedSubscriptionPlansAsync();
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم إنشاء Subscription Plans بنجاح"));
                }
                
                throw new BadRequestException("حدث خطأ أثناء إنشاء Subscription Plans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding subscription plans");
                throw new BadRequestException($"حدث خطأ أثناء إنشاء Subscription Plans: {ex.Message}");
            }
        }

        [HttpPost("seed/admin-users")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> SeedAdminUsers()
        {
            try
            {
                var result = await _dataSeederService.SeedAdminUsersAsync();
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم إنشاء Admin Users بنجاح"));
                }
                
                throw new BadRequestException("حدث خطأ أثناء إنشاء Admin Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding admin users");
                throw new BadRequestException($"حدث خطأ أثناء إنشاء Admin Users: {ex.Message}");
            }
        }

        [HttpPost("seed/manager-users")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> SeedManagerUsers()
        {
            try
            {
                var result = await _dataSeederService.SeedManagerUsersAsync();
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم إنشاء Manager Users بنجاح مع الاشتراكات"));
                }
                
                throw new BadRequestException("حدث خطأ أثناء إنشاء Manager Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding manager users");
                throw new BadRequestException($"حدث خطأ أثناء إنشاء Manager Users: {ex.Message}");
            }
        }

        [HttpPost("seed/employee-users")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> SeedEmployeeUsers()
        {
            try
            {
                var result = await _dataSeederService.SeedEmployeeUsersAsync();
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم إنشاء Employee Users بنجاح"));
                }
                
                throw new BadRequestException("حدث خطأ أثناء إنشاء Employee Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding employee users");
                throw new BadRequestException($"حدث خطأ أثناء إنشاء Employee Users: {ex.Message}");
            }
        }

        // Approve Enterprise Subscription
        [HttpPost("subscriptions/{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> ApproveSubscription(int id, [FromBody] ApproveSubscriptionDto? dto = null)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(id);
                if (subscription == null)
                {
                    throw new NotFoundException("الاشتراك غير موجود");
                }

                // Check if subscription is pending (not active)
                if (subscription.IsActive)
                {
                    throw new BadRequestException("الاشتراك غير معلق للموافقة");
                }

                // TODO: Implement approval logic
                // - Update subscription status to "active"
                // - Set custom price if provided
                // - Update subscription dates
                // - Create invoice if needed

                subscription.IsActive = true;
                if (dto?.CustomPrice.HasValue == true)
                {
                    // Update subscription price
                }

                _subscriptionRepository.Update(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم الموافقة على الاشتراك بنجاح",
                    data = new
                    {
                        id = subscription.Id.ToString(),
                        status = "active",
                        approvedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving subscription");
                throw new BadRequestException("حدث خطأ أثناء الموافقة على الاشتراك");
            }
        }

        // Reject Enterprise Subscription
        [HttpPost("subscriptions/{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> RejectSubscription(int id, [FromBody] RejectSubscriptionDto? dto = null)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(id);
                if (subscription == null)
                {
                    throw new NotFoundException("الاشتراك غير موجود");
                }

                // Check if subscription is pending (not active)
                if (subscription.IsActive)
                {
                    throw new BadRequestException("الاشتراك غير معلق للموافقة");
                }

                // TODO: Implement rejection logic
                subscription.IsActive = false;

                _subscriptionRepository.Update(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم رفض طلب الاشتراك",
                    data = new
                    {
                        id = subscription.Id.ToString(),
                        status = "cancelled",
                        rejectedAt = DateTime.UtcNow,
                        rejectionReason = dto?.Reason
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting subscription");
                throw new BadRequestException("حدث خطأ أثناء رفض الاشتراك");
            }
        }

        // Admin Settings
        [HttpGet("settings")]
        [ProducesResponseType(typeof(ApiResponse<AdminSettingsDto>), 200)]
        public ActionResult<ApiResponse<AdminSettingsDto>> GetAdminSettings()
        {
            // For now, return default settings
            // In future, these should be stored in database
            var settings = new AdminSettingsDto
            {
                Platform = new PlatformSettings
                {
                    MaintenanceMode = false,
                    SystemAnnouncement = null,
                    AllowNewRegistrations = true,
                    RequireEmailVerification = true
                },
                Defaults = new DefaultConfigurations
                {
                    TrialPeriodDays = 14,
                    DefaultPlanId = 1,
                    SessionTimeoutMinutes = 60,
                    MaxLoginAttempts = 5
                },
                Features = new FeatureFlags
                {
                    EnableNotifications = true,
                    EnableReports = true,
                    EnablePayments = true,
                    EnableUserActivities = true
                }
            };

            return Ok(ApiResponse<AdminSettingsDto>.SuccessResponse(settings, "تم الحصول على إعدادات النظام بنجاح"));
        }

        [HttpPut("settings")]
        [ProducesResponseType(typeof(ApiResponse<AdminSettingsDto>), 200)]
        public ActionResult<ApiResponse<AdminSettingsDto>> UpdateAdminSettings([FromBody] AdminSettingsDto settings)
        {
            // For now, just return the same settings
            // In future, save to database
            _logger.LogInformation("Admin settings updated (not persisted yet - requires database schema)");

            return Ok(ApiResponse<AdminSettingsDto>.SuccessResponse(settings, "تم تحديث إعدادات النظام بنجاح"));
        }
    }

    public class ApproveSubscriptionDto
    {
        public decimal? CustomPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class RejectSubscriptionDto
    {
        public string? Reason { get; set; }
    }
}
