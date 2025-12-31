using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BLL.Service
{
    public class HangfireBackgroundJobs
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HangfireBackgroundJobs> _logger;

        public HangfireBackgroundJobs(IServiceProvider serviceProvider, ILogger<HangfireBackgroundJobs> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task GenerateWeeklyPerformanceReports()
        {
            try
            {
                _logger.LogInformation("Starting weekly performance reports generation job at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var weeklyPlanRepository = scope.ServiceProvider.GetRequiredService<IWeeklyPlanRepository>();
                var performanceAnalysisService = scope.ServiceProvider.GetRequiredService<IPerformanceAnalysisService>();

                // Get previous week (Sunday night job should analyze previous week)
                var now = DateTime.UtcNow;
                var currentDayOfWeek = now.DayOfWeek;
                
                // Calculate previous week
                var daysToSubtract = ((int)currentDayOfWeek + 7) % 7 + 7; // Go back to previous Sunday
                var previousWeekEnd = now.Date.AddDays(-daysToSubtract).AddDays(6); // Previous Sunday
                var previousWeekStart = previousWeekEnd.AddDays(-6); // Previous Monday

                var year = previousWeekStart.Year;
                var month = previousWeekStart.Month;
                
                // Calculate week number (1-4) within the month
                var weekNumber = ((previousWeekStart.Day - 1) / 7) + 1;
                if (weekNumber > 4) weekNumber = 4;

                _logger.LogInformation("Analyzing performance for Week {WeekNumber} of Month {Month} in Year {Year}", 
                    weekNumber, month, year);

                // Get all weekly plans for the previous week
                var weeklyPlans = await weeklyPlanRepository.GetByDateRangeAsync(previousWeekStart, previousWeekEnd);

                _logger.LogInformation("Found {Count} weekly plans to analyze", weeklyPlans.Count);

                foreach (var weeklyPlan in weeklyPlans)
                {
                    try
                    {
                        // Check if report already exists
                        var performanceReportRepository = scope.ServiceProvider.GetRequiredService<IPerformanceReportRepository>();
                        var existingReport = await performanceReportRepository.GetByWeeklyPlanAsync(weeklyPlan.Id);

                        if (existingReport != null)
                        {
                            _logger.LogInformation("Performance report already exists for WeeklyPlan {WeeklyPlanId}", weeklyPlan.Id);
                            continue;
                        }

                        // Generate performance report
                        _logger.LogInformation("Generating performance report for WeeklyPlan {WeeklyPlanId}", weeklyPlan.Id);
                        await performanceAnalysisService.GenerateWeeklyPerformanceReportAsync(weeklyPlan.Id);
                        
                        _logger.LogInformation("Successfully generated performance report for WeeklyPlan {WeeklyPlanId}", weeklyPlan.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating performance report for WeeklyPlan {WeeklyPlanId}", weeklyPlan.Id);
                        // Continue with other plans
                    }
                }

                _logger.LogInformation("Completed weekly performance reports generation job at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in weekly performance reports generation job");
                throw; // Let Hangfire handle retry
            }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task GenerateMonthlyPerformanceReports()
        {
            try
            {
                _logger.LogInformation("Starting monthly performance reports generation job at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var monthlyPlanRepository = scope.ServiceProvider.GetRequiredService<IMonthlyPlanRepository>();
                var monthlyPerformanceAnalysisService = scope.ServiceProvider.GetRequiredService<IMonthlyPerformanceAnalysisService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get previous month (job runs on 1st day of month to analyze previous month)
                var now = DateTime.UtcNow;
                var previousMonth = now.AddMonths(-1);
                var year = previousMonth.Year;
                var month = previousMonth.Month;

                _logger.LogInformation("Analyzing monthly performance for Month {Month} in Year {Year}", month, year);

                // Get all monthly plans for the previous month that don't have reports yet
                var monthlyPlans = await context.MonthlyPlans
                    .Include(mp => mp.AnnualTarget)
                    .Include(mp => mp.MonthlyPerformanceReport)
                    .Where(mp => mp.Year == year && mp.Month == month && mp.MonthlyPerformanceReport == null)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} monthly plans to analyze", monthlyPlans.Count);

                foreach (var monthlyPlan in monthlyPlans)
                {
                    try
                    {
                        _logger.LogInformation("Generating monthly performance report for MonthlyPlan {MonthlyPlanId}", monthlyPlan.Id);
                        await monthlyPerformanceAnalysisService.GenerateMonthlyPerformanceReportAsync(monthlyPlan.Id);

                        _logger.LogInformation("Successfully generated monthly performance report for MonthlyPlan {MonthlyPlanId}", monthlyPlan.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating monthly performance report for MonthlyPlan {MonthlyPlanId}", monthlyPlan.Id);
                        // Continue with other plans
                    }
                }

                _logger.LogInformation("Completed monthly performance reports generation job at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monthly performance reports generation job");
                throw; // Let Hangfire handle retry
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task CheckSubscriptionExpirations()
        {
            try
            {
                _logger.LogInformation("Starting subscription expiration check job at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var today = now.Date;

                // Check for subscriptions expiring in 7, 3, or 1 day(s)
                var expirationDates = new[] { today.AddDays(7), today.AddDays(3), today.AddDays(1), today };

                foreach (var expirationDate in expirationDates)
                {
                    var subscriptions = await context.Subscriptions
                        .Include(s => s.Manager)
                            .ThenInclude(m => m.User)
                        .Include(s => s.Plan)
                        .Where(s => s.IsActive &&
                                   s.EndDate.Date == expirationDate &&
                                   s.Manager.IsActive)
                        .ToListAsync();

                    var daysUntilExpiration = (expirationDate - today).Days;

                    foreach (var subscription in subscriptions)
                    {
                        try
                        {
                            if (daysUntilExpiration > 0)
                            {
                                _logger.LogInformation(
                                    "Subscription {SubscriptionId} for Manager {ManagerName} expires in {Days} day(s)",
                                    subscription.Id, subscription.Manager.User.FullName, daysUntilExpiration);

                                // TODO: Send notification email/SMS to manager
                                // Can be implemented later with notification service
                            }
                            else if (daysUntilExpiration == 0)
                            {
                                // Subscription expired today - deactivate it
                                _logger.LogWarning(
                                    "Deactivating expired subscription {SubscriptionId} for Manager {ManagerName}",
                                    subscription.Id, subscription.Manager.User.FullName);

                                subscription.IsActive = false;
                                subscription.Manager.IsActive = false;
                                subscription.Manager.SubscriptionEndsAt = subscription.EndDate;

                                await context.SaveChangesAsync();

                                _logger.LogInformation(
                                    "Successfully deactivated subscription {SubscriptionId}",
                                    subscription.Id);

                                // TODO: Send expiration notification
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error processing subscription expiration for Subscription {SubscriptionId}",
                                subscription.Id);
                            // Continue with other subscriptions
                        }
                    }
                }

                _logger.LogInformation("Completed subscription expiration check job at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription expiration check job");
                throw; // Let Hangfire handle retry
            }
        }
    }
}

