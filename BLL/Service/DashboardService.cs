using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Dashboard;

namespace BLL.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IManagerRepository _managerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAnnualTargetRepository _annualTargetRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskReportRepository _taskReportRepository;
        private readonly IWeeklyPlanRepository _weeklyPlanRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            IManagerRepository managerRepository,
            IEmployeeRepository employeeRepository,
            IAnnualTargetRepository annualTargetRepository,
            ITaskRepository taskRepository,
            ITaskReportRepository taskReportRepository,
            IWeeklyPlanRepository weeklyPlanRepository,
            ISubscriptionRepository subscriptionRepository,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _managerRepository = managerRepository;
            _employeeRepository = employeeRepository;
            _annualTargetRepository = annualTargetRepository;
            _taskRepository = taskRepository;
            _taskReportRepository = taskReportRepository;
            _weeklyPlanRepository = weeklyPlanRepository;
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }

        public async Task<ManagerDashboardDto> GetManagerDashboardAsync(string managerUserId)
        {
            var manager = await _managerRepository.GetWithDetailsAsync(managerUserId);

            if (manager == null)
                throw new InvalidOperationException("Manager not found");

            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;


            var daysIntoMonth = now.Day;
            var currentWeek = ((daysIntoMonth - 1) / 7) + 1;
            if (currentWeek > 4) currentWeek = 4;


            var currentAnnualTarget =
                await _annualTargetRepository.GetByManagerAndYearAsync(managerUserId, currentYear);

            var monthlyPlan = currentAnnualTarget?.MonthlyPlans
                .FirstOrDefault(mp => mp.Month == currentMonth);

            var weekInfos = new List<WeekInfoDto>();

            if (monthlyPlan != null)
            {
                var allTasks = await _context.Tasks
                    .Where(t => t.CreatedById == managerUserId &&
                                t.Year == currentYear &&
                                t.Month == currentMonth)
                    .ToListAsync();

                var allReports = await _context.TaskReports
                    .Include(tr => tr.TaskItem)
                    .Include(tr => tr.Employee)
                    .Where(tr => tr.Employee.ManagerUserId == managerUserId &&
                                 tr.TaskItem.Year == currentYear &&
                                 tr.TaskItem.Month == currentMonth)
                    .ToListAsync();

                foreach (var weeklyPlan in monthlyPlan.WeeklyPlans.OrderBy(wp => wp.WeekNumber))
                {
                    var tasks = allTasks
                        .Where(t => t.WeekNumber == weeklyPlan.WeekNumber)
                        .ToList();

                    var reports = allReports
                        .Where(r => r.TaskItem.WeekNumber == weeklyPlan.WeekNumber)
                        .ToList();

                    var isCurrentWeek =
                        weeklyPlan.WeekNumber == currentWeek &&
                        weeklyPlan.Year == currentYear &&
                        weeklyPlan.Month == currentMonth;

                    weekInfos.Add(new WeekInfoDto
                    {
                        WeekNumber = weeklyPlan.WeekNumber,
                        WeekStartDate = weeklyPlan.WeekStartDate,
                        WeekEndDate = weeklyPlan.WeekEndDate,
                        AchievementPercentage = weeklyPlan.AchievementPercentage,
                        IsCurrentWeek = isCurrentWeek,
                        TasksCount = tasks.Count,
                        CompletedTasksCount = tasks.Count(t => t.IsCompleted),
                        ReportsCount = reports.Count
                    });
                }
            }
            else
            {
                // Default weeks if no monthly plan
                for (int week = 1; week <= 4; week++)
                {
                    var weekStart = GetWeekStartDate(currentYear, currentMonth, week);
                    var weekEnd = weekStart.AddDays(6);

                    weekInfos.Add(new WeekInfoDto
                    {
                        WeekNumber = week,
                        WeekStartDate = weekStart,
                        WeekEndDate = weekEnd,
                        IsCurrentWeek = week == currentWeek
                    });
                }
            }

            var currentWeekInfo = weekInfos.FirstOrDefault(w => w.IsCurrentWeek);

            // Get total employees
            var totalEmployees =
                await _employeeRepository.GetEmployeeCountByManagerAsync(managerUserId);

            // Get active subscription
            var activeSubscription =
                await _subscriptionRepository.GetByManagerIdAsync(manager.Id);

            return new ManagerDashboardDto
            {
                CurrentYear = currentYear,
                CurrentMonth = currentMonth,
                CurrentWeek = currentWeek,
                CurrentWeekInfo = currentWeekInfo,
                MonthWeeks = weekInfos,
                TotalEmployees = totalEmployees,
                ActiveSubscriptions = activeSubscription != null ? 1 : 0,
                CompanyName = manager.CompanyName,
                CurrentAnnualTarget = currentAnnualTarget != null
                    ? new AnnualTargetSummaryDto
                    {
                        Id = currentAnnualTarget.Id,
                        Year = currentAnnualTarget.Year,
                        TargetDescription = currentAnnualTarget.TargetDescription
                    }
                    : null
            };
        }


        private DateTime GetWeekStartDate(int year, int month, int weekNumber)
        {
            var firstDayOfMonth = new DateTime(year, month, 1);
            var firstMonday = firstDayOfMonth;
            
            // Find first Monday
            while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            {
                firstMonday = firstMonday.AddDays(1);
            }

            // If first Monday is not in the first week, adjust
            if (firstMonday.Day > 7)
            {
                firstMonday = firstMonday.AddDays(-7);
            }

            // Calculate week start
            var weekStart = firstMonday.AddDays((weekNumber - 1) * 7);
            
            // Ensure week start is within the month
            if (weekStart.Month != month)
            {
                weekStart = new DateTime(year, month, 1);
            }

            return weekStart;
        }
    }
}

