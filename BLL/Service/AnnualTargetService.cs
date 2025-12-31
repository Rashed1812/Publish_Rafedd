using AutoMapper;
using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using Shared.DTOS.AnnualTarget;

namespace BLL.Service
{
    public class AnnualTargetService : IAnnualTargetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnnualTargetRepository _annualTargetRepository;
        private readonly IMonthlyPlanRepository _monthlyPlanRepository;
        private readonly IWeeklyPlanRepository _weeklyPlanRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IGeminiAIService _geminiAIService;
        private readonly ILogger<AnnualTargetService> _logger;

        public AnnualTargetService(
            ApplicationDbContext context,
            IAnnualTargetRepository annualTargetRepository,
            IMonthlyPlanRepository monthlyPlanRepository,
            IWeeklyPlanRepository weeklyPlanRepository,
            IManagerRepository managerRepository,
            IGeminiAIService geminiAIService,
            ILogger<AnnualTargetService> logger)
        {
            _context = context;
            _annualTargetRepository = annualTargetRepository;
            _monthlyPlanRepository = monthlyPlanRepository;
            _weeklyPlanRepository = weeklyPlanRepository;
            _managerRepository = managerRepository;
            _geminiAIService = geminiAIService;
            _logger = logger;
        }

        public async Task<AnnualTargetResponseDto> CreateAnnualTargetAsync(string managerUserId, CreateAnnualTargetDto dto)
        {
            // Check if manager exists
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            // Check if annual target for this year already exists
            var existingTarget = await _annualTargetRepository.GetByManagerAndYearAsync(managerUserId, dto.Year);

            if (existingTarget != null)
            {
                throw new InvalidOperationException($"Annual target for year {dto.Year} already exists");
            }

            // Generate plan using Gemini AI
            _logger.LogInformation("Generating annual plan using Gemini AI for manager {ManagerId}, year {Year}", managerUserId, dto.Year);
            var aiPlan = await _geminiAIService.GenerateAnnualPlanAsync(dto.TargetDescription, dto.Year);

            // Create AnnualTarget
            var annualTarget = new AnnualTarget
            {
                ManagerUserId = managerUserId,
                Year = dto.Year,
                TargetDescription = dto.TargetDescription,
                CreatedAt = DateTime.UtcNow
            };

            await _annualTargetRepository.AddAsync(annualTarget);
            await _annualTargetRepository.SaveChangesAsync();

            // Create MonthlyPlans and WeeklyPlans
            foreach (var monthlyPlanData in aiPlan.MonthlyPlans)
            {
                var monthlyPlan = new MonthlyPlan
                {
                    AnnualTargetId = annualTarget.Id,
                    Month = monthlyPlanData.Month,
                    MonthlyGoal = monthlyPlanData.MonthlyGoal,
                    Year = dto.Year
                };

                await _monthlyPlanRepository.AddAsync(monthlyPlan);
                await _monthlyPlanRepository.SaveChangesAsync();

                foreach (var weeklyPlanData in monthlyPlanData.WeeklyPlans)
                {
                    var weeklyPlan = new WeeklyPlan
                    {
                        MonthlyPlanId = monthlyPlan.Id,
                        WeekNumber = weeklyPlanData.WeekNumber,
                        WeeklyGoal = weeklyPlanData.WeeklyGoal,
                        Year = dto.Year,
                        Month = monthlyPlanData.Month,
                        WeekStartDate = weeklyPlanData.WeekStartDate,
                        WeekEndDate = weeklyPlanData.WeekEndDate
                    };

                    await _weeklyPlanRepository.AddAsync(weeklyPlan);
                }
            }

            await _weeklyPlanRepository.SaveChangesAsync();

            // Return response
            return await GetAnnualTargetResponseAsync(annualTarget.Id);
        }

        public async Task<AnnualTargetResponseDto?> GetAnnualTargetByYearAsync(string managerUserId, int year)
        {
            var annualTarget = await _annualTargetRepository.GetByManagerAndYearAsync(managerUserId, year);

            if (annualTarget == null)
            {
                return null;
            }

            return MapToResponseDto(annualTarget);
        }

        public async Task<List<AnnualTargetResponseDto>> GetAllAnnualTargetsAsync(string managerUserId)
        {
            var annualTargets = await _annualTargetRepository.GetAllByManagerAsync(managerUserId);

            return annualTargets.Select(MapToResponseDto).ToList();
        }

        private async Task<AnnualTargetResponseDto> GetAnnualTargetResponseAsync(int annualTargetId)
        {
            var annualTarget = await _annualTargetRepository.GetWithDetailsAsync(annualTargetId);

            if (annualTarget == null)
            {
                throw new InvalidOperationException("Annual target not found");
            }

            return MapToResponseDto(annualTarget);
        }

        private AnnualTargetResponseDto MapToResponseDto(AnnualTarget annualTarget)
        {
            return new AnnualTargetResponseDto
            {
                Id = annualTarget.Id,
                Year = annualTarget.Year,
                TargetDescription = annualTarget.TargetDescription,
                CreatedAt = annualTarget.CreatedAt,
                MonthlyPlans = annualTarget.MonthlyPlans
                    .OrderBy(mp => mp.Month)
                    .Select(mp => new MonthlyPlanDto
                    {
                        Id = mp.Id,
                        Month = mp.Month,
                        MonthlyGoal = mp.MonthlyGoal,
                        WeeklyPlans = mp.WeeklyPlans
                            .OrderBy(wp => wp.WeekNumber)
                            .Select(wp => new WeeklyPlanDto
                            {
                                Id = wp.Id,
                                WeekNumber = wp.WeekNumber,
                                WeeklyGoal = wp.WeeklyGoal,
                                WeekStartDate = wp.WeekStartDate,
                                WeekEndDate = wp.WeekEndDate,
                                AchievementPercentage = wp.AchievementPercentage
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}

