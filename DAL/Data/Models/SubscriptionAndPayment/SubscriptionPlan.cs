using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Models.Subscription
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;
        public decimal PricePerMonth { get; set; }
        public int MaxEmployees { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
