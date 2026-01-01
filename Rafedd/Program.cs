using BLL.Service;
using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryClasses;
using DAL.Repositories.RepositoryIntrfaces;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Rafedd.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Rafedd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "(Rafedd) API",
                    Version = "v1",
                    Description = "The First AI-Powered Arabic Team Performance & Goal Management Platform"
                });

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Database Context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("DAL")));

            // Identity Configuration
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // JWT Authentication
            var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RafeddAPI";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RafeddUsers";
            var jwtExpireHours = double.Parse(builder.Configuration["Jwt:ExpireHours"] ?? "24");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // HttpClient Factory for external API calls
            builder.Services.AddHttpClient();

            // Hangfire Configuration - Environment-based storage
            if (builder.Environment.IsDevelopment())
            {
                // Use in-memory storage for development (no SQL dependency for Hangfire)
                builder.Services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMemoryStorage());
            }
            else
            {
                // Use SQL Server storage for production
                builder.Services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        PrepareSchemaIfNecessary = true
                    }));
            }

            builder.Services.AddHangfireServer();

            // Repository Pattern - Generic
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            // Repository Pattern - Specific Repositories
            builder.Services.AddScoped<IAnnualTargetRepository, AnnualTargetRepository>();
            builder.Services.AddScoped<IMonthlyPlanRepository, MonthlyPlanRepository>();
            builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
            builder.Services.AddScoped<IManagerRepository, ManagerRepository>();
            builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<ITaskReportRepository, TaskReportRepository>();
            builder.Services.AddScoped<IPerformanceReportRepository, PerformanceReportRepository>();
            builder.Services.AddScoped<IMonthlyPerformanceReportRepository, MonthlyPerformanceReportRepository>();
            builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IUserActivityRepository, UserActivityRepository>();
            builder.Services.AddScoped<IImportantNoteRepository, ImportantNoteRepository>();

            // Business Logic Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<IManagerService, ManagerService>();
            builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
            builder.Services.AddScoped<ISubscriptionValidationService, SubscriptionValidationService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IGeminiAIService, GeminiAIService>();
            builder.Services.AddScoped<IGeminiService, GeminiService>();
            builder.Services.AddScoped<IAnnualTargetService, AnnualTargetService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IPerformanceAnalysisService, PerformanceAnalysisService>();
            builder.Services.AddScoped<IMonthlyPerformanceAnalysisService, MonthlyPerformanceAnalysisService>();
            builder.Services.AddScoped<IDataSeederService, DataSeederService>();
            builder.Services.AddScoped<IImportantNoteService, ImportantNoteService>();
            builder.Services.AddScoped<ITaskAnalysisService, TaskAnalysisService>();
            builder.Services.AddScoped<IEmailService, EmailService>();


            // Background Jobs
            builder.Services.AddScoped<HangfireBackgroundJobs>();

            // CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            
            // Global Exception Handler (must be first)
            app.UseGlobalExceptionHandler();

            // HTTPS Redirection (before Swagger)
            app.UseHttpsRedirection();

            // CORS
            app.UseCors("AllowAll");

            // Static Files (for file uploads)
            app.UseStaticFiles();

            // Swagger UI (only in Development)
            
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rafedd API v1");
                    c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
                });
            

            app.UseAuthentication();
            app.UseAuthorization();

            // Hangfire Dashboard (only in Development or with authentication)
            if (app.Environment.IsDevelopment())
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[] { new HangfireAuthorizationFilter() }
                });
            }

            app.MapControllers();

            // Schedule recurring jobs (using background task to avoid blocking startup)
            Task.Run(() =>
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time"); // UTC+3

                    // Weekly Performance Reports - Every Sunday at 11 PM
                    RecurringJob.AddOrUpdate<HangfireBackgroundJobs>(
                        "weekly-performance-reports",
                        job => job.GenerateWeeklyPerformanceReports(),
                        "0 23 * * 0",  // Every Sunday at 11 PM
                        new RecurringJobOptions
                        {
                            TimeZone = timeZone
                        });

                    // Monthly Performance Reports - 1st day of each month at 1 AM
                    RecurringJob.AddOrUpdate<HangfireBackgroundJobs>(
                        "monthly-performance-reports",
                        job => job.GenerateMonthlyPerformanceReports(),
                        "0 1 1 * *",  // 1st day of every month at 1 AM
                        new RecurringJobOptions
                        {
                            TimeZone = timeZone
                        });

                    // Subscription Expiration Check - Daily at 9 AM
                    RecurringJob.AddOrUpdate<HangfireBackgroundJobs>(
                        "subscription-expiration-check",
                        job => job.CheckSubscriptionExpirations(),
                        "0 9 * * *",  // Every day at 9 AM
                        new RecurringJobOptions
                        {
                            TimeZone = timeZone
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to schedule recurring jobs: {ex.Message}");
                }
            });

            app.Run();
        }
    }

    // Simple authorization filter for Hangfire Dashboard (can be enhanced)
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // For development, allow all. In production, implement proper authentication
            return true;
        }
    }
}
