using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.AIPlanning
{
    public class AnnualTarget
    {
        public int Id { get; set; }
        public string ManagerUserId { get; set; } = null!;
        public ApplicationUser Manager { get; set; } = null!;
        public int Year { get; set; }

        [Required]
        public string TargetDescription { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<MonthlyPlan> MonthlyPlans { get; set; } = new List<MonthlyPlan>();
    }
}
