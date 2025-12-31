namespace BLL.ServiceAbstraction
{
    public interface IDataSeederService
    {
        Task<bool> SeedSubscriptionPlansAsync();
        Task<bool> SeedAdminUsersAsync();
        Task<bool> SeedManagerUsersAsync();
        Task<bool> SeedEmployeeUsersAsync();
        Task<bool> SeedAllDataAsync();
    }
}

