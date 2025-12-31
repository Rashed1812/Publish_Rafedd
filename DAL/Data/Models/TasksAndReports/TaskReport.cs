using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.TasksAndReports
{
    public class TaskReport
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        [Required]
        public string ReportText { get; set; } = null!;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
