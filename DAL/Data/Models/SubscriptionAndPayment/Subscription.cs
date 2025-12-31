using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.Subscription
{
    public class Subscription
    {
        public int Id { get; set; }
        public int ManagerId { get; set; }
        public Manager Manager { get; set; } = null!;
        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AutoRenew { get; set; } = true;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
