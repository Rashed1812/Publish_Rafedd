namespace BLL.ServiceAbstraction
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName);
    }
}

