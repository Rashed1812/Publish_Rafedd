using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.AnnualTarget
{
    public class CreateAnnualTargetDto
    {
        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(2000)]
        public string TargetDescription { get; set; } = null!;
    }
}

