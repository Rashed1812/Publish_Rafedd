using BLL.ServiceAbstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BLL.Service
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta";

        public GeminiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key not configured");
        }

        public async Task<AnnualPlanGenerationDto> GenerateAnnualPlanAsync(string goal, int year)
        {
            var prompt = $@"You are a business planning assistant for Arabic-speaking companies.

Break down the following annual goal into a detailed structured plan in Arabic:

Annual Goal: {goal}
Year: {year}

Requirements:
1. Generate 12 monthly goals (one for each month from January to December)
2. For each month, generate 4-5 weekly goals that contribute to the monthly goal
3. Each goal should be specific, measurable, achievable, relevant, and time-bound
4. Use professional Arabic business language
5. Ensure goals are progressive and build on each other

Return ONLY a valid JSON object in this exact format (no markdown, no code blocks):
{{
  ""monthlyGoals"": [
    {{
      ""month"": 1,
      ""description"": ""الهدف الشهري لشهر يناير"",
      ""weeklyGoals"": [
        {{""weekNumber"": 1, ""description"": ""الهدف الأسبوعي الأول""}},
        {{""weekNumber"": 2, ""description"": ""الهدف الأسبوعي الثاني""}},
        {{""weekNumber"": 3, ""description"": ""الهدف الأسبوعي الثالث""}},
        {{""weekNumber"": 4, ""description"": ""الهدف الأسبوعي الرابع""}}
      ]
    }}
  ]
}}";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                var result = ParseAnnualPlanResponse(response, goal, year);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating annual plan with Gemini");
                // Return fallback response
                return GenerateFallbackAnnualPlan(goal, year);
            }
        }

        public async Task<MonthlyReportGenerationDto> GenerateMonthlyReportAsync(MonthlyReportRequestDto request)
        {
            var completionRate = request.TotalTasks > 0
                ? (request.CompletedTasks * 100.0 / request.TotalTasks)
                : 0;

            var employeeStatsText = string.Join("\n", request.EmployeeStats.Select(e =>
                $"- {e.EmployeeName}: {e.TasksCompleted}/{e.TasksAssigned} مهام مكتملة ({e.CompletionRate:F1}%)"));

            var prompt = $@"You are a performance analysis assistant for Arabic-speaking companies.

Analyze the following employee performance data and generate comprehensive insights in Arabic:

Month: {request.Month}/{request.Year}
Goal: {request.Goal}

Performance Summary:
- Total Employees: {request.TotalEmployees}
- Total Tasks: {request.TotalTasks}
- Completed Tasks: {request.CompletedTasks}
- Overall Completion Rate: {completionRate:F1}%

Employee Performance:
{employeeStatsText}

Generate:
1. Overall performance summary (2-3 sentences in Arabic)
2. Top 3 performers with reasons
3. 3-5 areas that need improvement
4. 5-7 actionable recommendations for the manager

Return ONLY a valid JSON object (no markdown, no code blocks):
{{
  ""overallSummary"": ""ملخص الأداء العام"",
  ""topPerformers"": [""اسم الموظف: السبب""],
  ""areasForImprovement"": [""مجال التحسين""],
  ""recommendations"": [""توصية قابلة للتنفيذ""]
}}";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                var result = ParseMonthlyReportResponse(response, request.Month, request.Year);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report with Gemini");
                return GenerateFallbackMonthlyReport(request);
            }
        }

        public async Task<string> GeneratePerformanceInsightsAsync(EmployeePerformanceDataDto data)
        {
            var prompt = $@"Analyze this employee's performance and provide insights in Arabic:

Employee: {data.EmployeeName}
Tasks Completed: {data.TasksCompleted}
Tasks Missed: {data.TasksMissed}
Average Completion Time: {data.AverageCompletionTime} days
Recent Activity: {data.RecentActivity}

Provide a brief performance insight (2-3 sentences in Arabic) with constructive feedback.";

            try
            {
                return await CallGeminiAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance insights");
                return "الأداء ضمن المعدل المتوقع. يُنصح بمتابعة التقدم والتحسين المستمر.";
            }
        }

        public async Task<TaskAnalysisResultDto> AnalyzeTaskProgressAsync(TaskAnalysisRequestDto request)
        {
            var daysUntilDeadline = request.Deadline.HasValue
                ? (request.Deadline.Value - DateTime.UtcNow).Days
                : 999;

            var employeeUpdatesText = string.Join("\n", request.EmployeeUpdates
                .OrderBy(u => u.SubmittedAt)
                .Select(u => $"[{u.SubmittedAt:yyyy-MM-dd}] {u.EmployeeName}: {u.ReportText}"));

            var prompt = $@"أنت مساعد ذكي متخصص في تحليل أداء المهام وتقييم التقدم.

**معلومات المهمة:**
العنوان: {request.TaskTitle}
الوصف الأصلي: {request.TaskDescription ?? "لا يوجد وصف"}
تاريخ الإنشاء: {request.CreatedAt:yyyy-MM-dd}
الموعد النهائي: {(request.Deadline.HasValue ? request.Deadline.Value.ToString("yyyy-MM-dd") : "لا يوجد موعد نهائي محدد")}
الأيام المتبقية: {daysUntilDeadline}

**تحديثات الموظف:**
{(request.EmployeeUpdates.Any() ? employeeUpdatesText : "لا توجد تحديثات من الموظف حتى الآن")}

**المطلوب منك تحليل شامل يتضمن:**

1. **نسبة الإنجاز** (0-100): حدد نسبة إنجاز المهمة بناءً على ما تم إنجازه مقارنةً بالمطلوب
2. **ملخص الحالة**: ملخص قصير (1-2 جملة) باللغة العربية عن حالة المهمة الحالية
3. **العناصر المكتملة**: قائمة بالعناصر أو الخطوات التي تم إنجازها
4. **العناصر المتبقية**: قائمة بما يجب إنجازه لإكمال المهمة
5. **المعوقات**: أي عوائق أو مشاكل تم ذكرها في التحديثات
6. **الحالة الإجمالية**:
   - ON_TRACK: إذا كانت المهمة تسير وفق الخطة بشكل جيد
   - AT_RISK: إذا كانت هناك مخاطر محتملة أو تأخير بسيط
   - DELAYED: إذا كانت المهمة متأخرة بشكل واضح
7. **درجة مخاطر الموعد النهائي** (0-100):
   - 0-30: خطر منخفض (سير العمل جيد ووقت كافٍ)
   - 31-60: خطر متوسط (جدول زمني ضيق أو تأخير بسيط)
   - 61-100: خطر عالٍ (احتمال كبير لعدم الالتزام بالموعد أو معوقات كبيرة)
8. **الإجراءات المقترحة**: قائمة من 3-5 إجراءات عملية يمكن للمدير اتخاذها

**شروط مهمة جداً:**
- جميع النصوص يجب أن تكون باللغة العربية الفصحى
- التحليل يجب أن يكون موضوعياً ومبنياً على الأدلة من التحديثات
- القوائم يجب أن تكون محددة وليست عامة
- إذا لم تكن هناك تحديثات كافية، اذكر ذلك في التحليل

**أرجع النتيجة بصيغة JSON فقط بالشكل التالي (يجب أن تكون JSON صالح 100% بدون أي نص قبله أو بعده):**
{{
  ""completionPercentage"": 0,
  ""statusSummary"": ""ملخص قصير عن حالة المهمة"",
  ""doneItems"": [""عنصر مكتمل 1"", ""عنصر مكتمل 2""],
  ""remainingItems"": [""عنصر متبقي 1"", ""عنصر متبقي 2""],
  ""blockers"": [""معوق 1"", ""معوق 2""],
  ""overallStatus"": ""ON_TRACK"",
  ""deadlineRiskScore"": 0,
  ""suggestedNextActions"": [""إجراء مقترح 1"", ""إجراء مقترح 2"", ""إجراء مقترح 3""]
}}

**ملاحظات:**
- لا تضع أي نص قبل أو بعد JSON
- overallStatus يجب أن يكون أحد هذه القيم فقط: ON_TRACK أو AT_RISK أو DELAYED
- completionPercentage و deadlineRiskScore يجب أن يكونا أرقام بين 0 و 100
- جميع القوائم يمكن أن تكون فارغة إذا لم تكن هناك بيانات كافية";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                var result = ParseTaskAnalysisResponse(response, request.TaskId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing task {TaskId} with Gemini", request.TaskId);
                return GenerateFallbackTaskAnalysis(request);
            }
        }

        private async Task<string> CallGeminiAPIAsync(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 2048,
                }
            };

            var url = $"{_baseUrl}/models/gemini-pro:generateContent?key={_apiKey}";
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            // Clean up the response - remove markdown code blocks if present
            generatedText = generatedText.Trim();
            if (generatedText.StartsWith("```json"))
            {
                generatedText = generatedText.Substring(7);
            }
            if (generatedText.StartsWith("```"))
            {
                generatedText = generatedText.Substring(3);
            }
            if (generatedText.EndsWith("```"))
            {
                generatedText = generatedText.Substring(0, generatedText.Length - 3);
            }

            return generatedText.Trim();
        }

        private AnnualPlanGenerationDto ParseAnnualPlanResponse(string jsonResponse, string goal, int year)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AnnualPlanGenerationDto>(jsonResponse);
                if (response != null && response.MonthlyGoals != null && response.MonthlyGoals.Count > 0)
                {
                    response.Year = year;
                    response.Goal = goal;
                    return response;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Gemini response as JSON: {Response}", jsonResponse);
            }

            return GenerateFallbackAnnualPlan(goal, year);
        }

        private MonthlyReportGenerationDto ParseMonthlyReportResponse(string jsonResponse, int month, int year)
        {
            try
            {
                var response = JsonSerializer.Deserialize<MonthlyReportGenerationDto>(jsonResponse);
                if (response != null)
                {
                    response.Month = month;
                    response.Year = year;
                    return response;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Gemini monthly report response: {Response}", jsonResponse);
            }

            return new MonthlyReportGenerationDto
            {
                Month = month,
                Year = year,
                OverallSummary = "تم إنشاء التقرير بنجاح. الأداء العام ضمن المعدل المتوقع.",
                TopPerformers = new List<string> { "يتم تحليل البيانات..." },
                AreasForImprovement = new List<string> { "تحسين معدل إنجاز المهام" },
                Recommendations = new List<string> { "متابعة الأداء بشكل دوري" }
            };
        }

        private AnnualPlanGenerationDto GenerateFallbackAnnualPlan(string goal, int year)
        {
            var monthlyGoals = new List<MonthlyGoalDto>();

            for (int month = 1; month <= 12; month++)
            {
                var monthlyGoal = new MonthlyGoalDto
                {
                    Month = month,
                    Description = $"الهدف الشهري لشهر {GetMonthName(month)}: {goal}",
                    WeeklyGoals = new List<WeeklyGoalDto>()
                };

                for (int week = 1; week <= 4; week++)
                {
                    monthlyGoal.WeeklyGoals.Add(new WeeklyGoalDto
                    {
                        WeekNumber = week,
                        Description = $"الأسبوع {week}: العمل على تحقيق الهدف الشهري"
                    });
                }

                monthlyGoals.Add(monthlyGoal);
            }

            return new AnnualPlanGenerationDto
            {
                Year = year,
                Goal = goal,
                MonthlyGoals = monthlyGoals
            };
        }

        private MonthlyReportGenerationDto GenerateFallbackMonthlyReport(MonthlyReportRequestDto request)
        {
            var completionRate = request.TotalTasks > 0
                ? (request.CompletedTasks * 100.0 / request.TotalTasks)
                : 0;

            return new MonthlyReportGenerationDto
            {
                Month = request.Month,
                Year = request.Year,
                OverallSummary = $"تم إنجاز {request.CompletedTasks} من أصل {request.TotalTasks} مهمة بمعدل إنجاز {completionRate:F1}%",
                TopPerformers = request.EmployeeStats.OrderByDescending(e => e.CompletionRate).Take(3)
                    .Select(e => $"{e.EmployeeName}: معدل إنجاز {e.CompletionRate:F1}%").ToList(),
                AreasForImprovement = new List<string> { "تحسين التواصل بين الفريق", "زيادة معدل إنجاز المهام" },
                Recommendations = new List<string>
                {
                    "تخصيص وقت أكبر للمهام المعقدة",
                    "تقديم المزيد من الدعم للموظفين",
                    "مراجعة الأهداف الأسبوعية بانتظام"
                }
            };
        }

        private TaskAnalysisResultDto ParseTaskAnalysisResponse(string jsonResponse, int taskId)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };

                var response = JsonSerializer.Deserialize<TaskAnalysisResultDto>(jsonResponse, options);

                if (response != null)
                {
                    response.TaskId = taskId;
                    response.AnalyzedAt = DateTime.UtcNow;

                    // Validate bounds
                    response.CompletionPercentage = Math.Clamp(response.CompletionPercentage, 0, 100);
                    response.DeadlineRiskScore = Math.Clamp(response.DeadlineRiskScore, 0, 100);

                    // Ensure lists are not null
                    response.DoneItems ??= new List<string>();
                    response.RemainingItems ??= new List<string>();
                    response.Blockers ??= new List<string>();
                    response.SuggestedNextActions ??= new List<string>();

                    return response;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Gemini task analysis response: {Response}", jsonResponse);
            }

            return GenerateFallbackTaskAnalysis(new TaskAnalysisRequestDto { TaskId = taskId });
        }

        private TaskAnalysisResultDto GenerateFallbackTaskAnalysis(TaskAnalysisRequestDto request)
        {
            var hasUpdates = request.EmployeeUpdates?.Any() ?? false;
            var daysUntilDeadline = request.Deadline.HasValue
                ? (request.Deadline.Value - DateTime.UtcNow).Days
                : 999;

            var completionEstimate = hasUpdates ? 30 : 0;
            var riskScore = daysUntilDeadline < 3 ? 70 : daysUntilDeadline < 7 ? 40 : 20;

            return new TaskAnalysisResultDto
            {
                TaskId = request.TaskId,
                CompletionPercentage = completionEstimate,
                StatusSummary = hasUpdates
                    ? "المهمة قيد التنفيذ. تم تقديم تحديثات من الموظف."
                    : "لم يتم تقديم تحديثات بعد. يرجى المتابعة مع الموظف.",
                DoneItems = hasUpdates ? new List<string> { "تم البدء في المهمة" } : new List<string>(),
                RemainingItems = new List<string> { "متابعة تنفيذ المهمة", "تقديم تحديثات دورية" },
                Blockers = new List<string>(),
                OverallStatus = daysUntilDeadline < 3 ? TaskOverallStatus.AT_RISK : TaskOverallStatus.ON_TRACK,
                DeadlineRiskScore = riskScore,
                SuggestedNextActions = new List<string>
                {
                    "التواصل مع الموظف للحصول على تحديث",
                    "مراجعة التقدم المحرز",
                    "تقديم الدعم اللازم"
                },
                AnalyzedAt = DateTime.UtcNow
            };
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "يناير",
                2 => "فبراير",
                3 => "مارس",
                4 => "أبريل",
                5 => "مايو",
                6 => "يونيو",
                7 => "يوليو",
                8 => "أغسطس",
                9 => "سبتمبر",
                10 => "أكتوبر",
                11 => "نوفمبر",
                12 => "ديسمبر",
                _ => ""
            };
        }

        // Gemini API Response Models
        private class GeminiResponse
        {
            public List<Candidate>? Candidates { get; set; }
        }

        private class Candidate
        {
            public Content? Content { get; set; }
        }

        private class Content
        {
            public List<Part>? Parts { get; set; }
        }

        private class Part
        {
            public string? Text { get; set; }
        }
    }
}
