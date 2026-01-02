using BLL.ServiceAbstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using Shared.DTOS.Performance;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BLL.Service
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<GeminiAIService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AIPlanResponseDto> GenerateAnnualPlanAsync(string arabicTarget, int year)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini API Key is not configured");
                }

                var prompt = BuildPlanGenerationPrompt(arabicTarget, year);
                var responseText = await CallGeminiAPIAsync(apiKey, prompt);

                _logger.LogInformation("Gemini AI Response: {Response}", responseText);

                // Parse JSON response from Gemini
                var planResponse = ParsePlanResponse(responseText);

                // Calculate week dates
                CalculateWeekDates(planResponse, year);

                return planResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating annual plan with Gemini AI");
                throw;
            }
        }

        public async Task<AIPerformanceAnalysisDto> AnalyzeWeeklyPerformanceAsync(
            int weeklyPlanId,
            int year,
            int month,
            int weekNumber,
            List<WeeklyPerformanceData> employeeReports)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini API Key is not configured");
                }

                var prompt = BuildPerformanceAnalysisPrompt(employeeReports, year, month, weekNumber);
                var responseText = await CallGeminiAPIAsync(apiKey, prompt);

                _logger.LogInformation("Gemini AI Performance Analysis Response: {Response}", responseText);

                return ParsePerformanceAnalysis(responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing performance with Gemini AI");
                throw;
            }
        }

        public async Task<AIPerformanceAnalysisDto> AnalyzeMonthlyPerformanceAsync(
            int monthlyPlanId,
            int year,
            int month,
            string monthlyGoal,
            List<WeeklyProgressDto> weeklyProgress,
            List<WeeklyPerformanceData> allEmployeeReports,
            int totalTasks,
            int completedTasks,
            float achievementPercentage)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini API Key is not configured");
                }

                var prompt = BuildMonthlyPerformanceAnalysisPrompt(
                    year, month, monthlyGoal, weeklyProgress,
                    allEmployeeReports, totalTasks, completedTasks, achievementPercentage);

                var responseText = await CallGeminiAPIAsync(apiKey, prompt);

                _logger.LogInformation("Gemini AI Monthly Performance Analysis Response: {Response}", responseText);

                return ParsePerformanceAnalysis(responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing monthly performance with Gemini AI");
                throw;
            }
        }

        private async Task<string> CallGeminiAPIAsync(string apiKey, string prompt)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var modelName = _configuration["Gemini:ModelName"] ?? "gemini-1.5-pro";

            // FIXED: Add API key as query parameter in URL (not as header)
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Gemini API with model: {ModelName}", modelName);

            var response = await httpClient.PostAsync(apiUrl, content);

            // Better error handling and logging
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API Error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            // Parse response
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var contentElement) &&
                    contentElement.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var part = parts[0];
                    if (part.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }

            throw new InvalidOperationException("Invalid response format from Gemini API");
        }

        private string BuildPlanGenerationPrompt(string arabicTarget, int year)
        {
            return $@"أنت خبير استراتيجي محترف في التخطيط والإدارة. مهمتك هي تحويل الهدف الاستراتيجي السنوي التالي إلى خطة تنفيذية شاملة ومفصلة باللغة العربية.

الهدف الاستراتيجي للعام {year}:
{arabicTarget}

المطلوب منك:
1. **تحليل الهدف**: قم بتحليل الهدف بعمق وتحديد الخطوات الاستراتيجية المطلوبة لتحقيقه
2. **التقسيم الشهري**: قسم الهدف إلى 12 خطة شهرية واقعية ومنطقية، كل شهر يجب أن يكون:
   - مرتبط بشكل واضح بالهدف السنوي العام
   - قابلاً للقياس والتنفيذ
   - مبني على الشهور السابقة (تسلسل منطقي)
   - يحتوي على شرح واضح ومفصل للهدف الشهري
3. **التقسيم الأسبوعي**: لكل شهر من الـ 12 شهر، قسم الهدف الشهري إلى 4 أسابيع بالضبط:
   - أسبوع 1: الخطوات الأولية والبداية
   - أسبوع 2: التطوير والتنفيذ
   - أسبوع 3: المتابعة والتحسين
   - أسبوع 4: الإنجاز والتحضير للشهر القادم
   - كل هدف أسبوعي يجب أن يكون:
     * واضح ومحدد
     * قابل للتنفيذ خلال أسبوع
     * متصل ببقية أسابيع الشهر
     * يساهم في تحقيق الهدف الشهري

الشروط المهمة:
- جميع النصوص يجب أن تكون باللغة العربية الفصحى
- الخطط يجب أن تكون واقعية وقابلة للتنفيذ فعلياً
- يجب أن تكون متسلسلة ومنطقية (كل شهر يبني على الشهر السابق)
- كل شهر يجب أن يحتوي على 4 أسابيع بالضبط
- الأهداف يجب أن تكون مفصلة وواضحة وليست عامة

أرجع النتيجة بصيغة JSON فقط بالشكل التالي (يجب أن تكون JSON صالح 100%):
{{
  ""monthlyPlans"": [
    {{
      ""month"": 1,
      ""monthlyGoal"": ""شرح مفصل وواضح لهدف الشهر الأول وكيفية تحقيقه"",
      ""weeklyPlans"": [
        {{
          ""weekNumber"": 1,
          ""weeklyGoal"": ""هدف واضح ومحدد للأسبوع الأول من الشهر الأول""
        }},
        {{
          ""weekNumber"": 2,
          ""weeklyGoal"": ""هدف واضح ومحدد للأسبوع الثاني من الشهر الأول""
        }},
        {{
          ""weekNumber"": 3,
          ""weeklyGoal"": ""هدف واضح ومحدد للأسبوع الثالث من الشهر الأول""
        }},
        {{
          ""weekNumber"": 4,
          ""weeklyGoal"": ""هدف واضح ومحدد للأسبوع الرابع من الشهر الأول""
        }}
      ]
    }},
    {{
      ""month"": 2,
      ""monthlyGoal"": ""شرح مفصل لهدف الشهر الثاني"",
      ""weeklyPlans"": [
        {{ ""weekNumber"": 1, ""weeklyGoal"": ""هدف الأسبوع الأول من الشهر الثاني"" }},
        {{ ""weekNumber"": 2, ""weeklyGoal"": ""هدف الأسبوع الثاني من الشهر الثاني"" }},
        {{ ""weekNumber"": 3, ""weeklyGoal"": ""هدف الأسبوع الثالث من الشهر الثاني"" }},
        {{ ""weekNumber"": 4, ""weeklyGoal"": ""هدف الأسبوع الرابع من الشهر الثاني"" }}
      ]
    }}
    // ... استمر بنفس الطريقة لجميع الأشهر من 3 إلى 12
  ]
}}

ملاحظات مهمة جداً:
- يجب أن تكون الإجابة JSON صالح 100% بدون أي أخطاء
- لا تضع أي نص قبل أو بعد JSON
- يجب أن تكون جميع الحقول موجودة (12 شهر × 4 أسابيع = 48 أسبوع)
- الأهداف يجب أن تكون مفصلة وواضحة، وليست عامة مثل ""العمل على الهدف""
- استخدم أسماء الأشهر في الأهداف لتوضيح التسلسل الزمني";
        }

        private string BuildPerformanceAnalysisPrompt(List<WeeklyPerformanceData> employeeReports, int year, int month, int weekNumber)
        {
            var reportsSummary = string.Join("\n", employeeReports.Select(r =>
                $"- {r.EmployeeName}: {r.CompletedTaskCount} من {r.TaskCount} مهمة مكتملة. التقارير: {string.Join("; ", r.ReportTexts.Take(3))}"));

            return $@"أنت خبير في تحليل الأداء الفريقي. مهمتك هي تحليل أداء الفريق للأسبوع {weekNumber} من الشهر {month} في العام {year}.

ملخص التقارير:
{reportsSummary}

المطلوب:
1. حساب نسبة الإنجاز (0-100%)
2. كتابة ملخص شامل بالأداء باللغة العربية الفصحى
3. تحديد نقاط القوة (قائمة)
4. تحديد نقاط الضعف (قائمة)
5. تقديم توصيات عملية للتحسين (قائمة)

أرجع النتيجة بصيغة JSON فقط بالشكل التالي:
{{
  ""achievementPercentage"": 75.5,
  ""summary"": ""ملخص شامل بالأداء..."",
  ""strengths"": [""نقطة قوة 1"", ""نقطة قوة 2""],
  ""weaknesses"": [""نقطة ضعف 1"", ""نقطة ضعف 2""],
  ""recommendations"": [""توصية 1"", ""توصية 2""]
}}

مهم جداً: يجب أن تكون الإجابة JSON صالح فقط، بدون أي نص إضافي قبل أو بعد JSON.";
        }

        private string BuildMonthlyPerformanceAnalysisPrompt(
            int year,
            int month,
            string monthlyGoal,
            List<WeeklyProgressDto> weeklyProgress,
            List<WeeklyPerformanceData> allEmployeeReports,
            int totalTasks,
            int completedTasks,
            float achievementPercentage)
        {
            var weeklyProgressSummary = string.Join("\n", weeklyProgress.Select(wp =>
                $"- الأسبوع {wp.WeekNumber}: {wp.AchievementPercentage:F1}% إنجاز"));

            var employeeSummary = string.Join("\n", allEmployeeReports
                .GroupBy(r => r.EmployeeName)
                .Select(g => $"- {g.Key}: {g.Sum(r => r.CompletedTaskCount)} من {g.Sum(r => r.TaskCount)} مهمة مكتملة عبر {g.Count()} تقرير"));

            return $@"أنت خبير في تحليل الأداء الشهري للفرق. مهمتك هي تحليل أداء الفريق للشهر {month} من العام {year} بشكل شامل.

**الهدف الشهري:**
{monthlyGoal}

**الإنجاز الإجمالي:**
- إجمالي المهام: {totalTasks}
- المهام المكتملة: {completedTasks}
- نسبة الإنجاز: {achievementPercentage:F1}%

**تقدم الأداء الأسبوعي (4 أسابيع):**
{weeklyProgressSummary}

**ملخص أداء الموظفين:**
{employeeSummary}

**المطلوب منك:**
1. تحليل شامل للأداء الشهري مقارنةً بالهدف الشهري
2. تقييم التقدم عبر الأسابيع الأربعة (هل هناك تحسن؟ تراجع؟ استقرار؟)
3. تحديد نقاط القوة الرئيسية التي ساهمت في الإنجاز
4. تحديد نقاط الضعف أو التحديات التي واجهها الفريق
5. تقديم توصيات عملية وقابلة للتطبيق للشهر القادم

أرجع النتيجة بصيغة JSON فقط بالشكل التالي:
{{
  ""achievementPercentage"": {achievementPercentage:F1},
  ""summary"": ""تحليل شامل ومفصل للأداء الشهري مقارنة بالهدف، مع ذكر الاتجاهات والأنماط الملاحظة..."",
  ""strengths"": [""نقطة قوة رئيسية 1"", ""نقطة قوة رئيسية 2"", ""نقطة قوة رئيسية 3""],
  ""weaknesses"": [""تحدي أو نقطة ضعف 1"", ""تحدي أو نقطة ضعف 2""],
  ""recommendations"": [""توصية عملية 1"", ""توصية عملية 2"", ""توصية عملية 3""]
}}

مهم جداً:
- يجب أن تكون الإجابة JSON صالح فقط، بدون أي نص إضافي قبل أو بعد JSON
- التحليل يجب أن يكون باللغة العربية الفصحى
- ركز على المقارنة مع الهدف الشهري المحدد
- ركز على الاتجاهات والأنماط عبر الأسابيع الأربعة";
        }

        private AIPlanResponseDto ParsePlanResponse(string responseText)
        {
            try
            {
                // Try to extract JSON from response if it contains markdown code blocks
                var jsonText = ExtractJsonFromResponse(responseText);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<AIPlanResponseDto>(jsonText, options);

                if (result == null || result.MonthlyPlans.Count != 12)
                {
                    throw new InvalidOperationException("Invalid plan response structure");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing plan response: {Response}", responseText);
                throw new InvalidOperationException("Failed to parse AI response", ex);
            }
        }

        private AIPerformanceAnalysisDto ParsePerformanceAnalysis(string responseText)
        {
            try
            {
                var jsonText = ExtractJsonFromResponse(responseText);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<AIPerformanceAnalysisDto>(jsonText, options);

                if (result == null)
                {
                    throw new InvalidOperationException("Invalid performance analysis response structure");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing performance analysis: {Response}", responseText);
                throw new InvalidOperationException("Failed to parse AI response", ex);
            }
        }

        private string ExtractJsonFromResponse(string responseText)
        {
            // Remove markdown code blocks if present
            var jsonText = responseText.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }

            return jsonText.Trim();
        }

        private void CalculateWeekDates(AIPlanResponseDto planResponse, int year)
        {
            var currentDate = new DateTime(year, 1, 1);

            // Find the first Monday of the year
            while (currentDate.DayOfWeek != DayOfWeek.Monday)
            {
                currentDate = currentDate.AddDays(1);
            }

            foreach (var monthlyPlan in planResponse.MonthlyPlans)
            {
                var monthStart = new DateTime(year, monthlyPlan.Month, 1);

                foreach (var weeklyPlan in monthlyPlan.WeeklyPlans)
                {
                    // Calculate week start (Monday) and week end (Sunday)
                    var daysOffset = (weeklyPlan.WeekNumber - 1) * 7;
                    var weekStart = monthStart.AddDays(-(int)monthStart.DayOfWeek + 1 + (int)DayOfWeek.Monday + daysOffset);
                    if (weekStart < monthStart)
                    {
                        weekStart = monthStart;
                    }

                    // Ensure week start is a Monday
                    while (weekStart.DayOfWeek != DayOfWeek.Monday)
                    {
                        weekStart = weekStart.AddDays(-1);
                    }

                    var weekEnd = weekStart.AddDays(6); // Sunday

                    weeklyPlan.WeekStartDate = weekStart;
                    weeklyPlan.WeekEndDate = weekEnd;
                }
            }
        }
    }
}