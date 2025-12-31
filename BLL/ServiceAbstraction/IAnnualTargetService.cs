using Shared.DTOS.AnnualTarget;

namespace BLL.ServiceAbstraction
{
    public interface IAnnualTargetService
    {
        Task<AnnualTargetResponseDto> CreateAnnualTargetAsync(string managerUserId, CreateAnnualTargetDto dto);
        Task<AnnualTargetResponseDto?> GetAnnualTargetByYearAsync(string managerUserId, int year);
        Task<List<AnnualTargetResponseDto>> GetAllAnnualTargetsAsync(string managerUserId);
    }
}

