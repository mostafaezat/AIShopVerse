using Microsoft.EntityFrameworkCore;

namespace Application.Features.AuthFeatures.Commands
{
    public class LogoutCommand : IRequest<Result<bool>>
    {
        public string RefreshToken { get; set; } = string.Empty;

        public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public LogoutCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
            {
                var storedToken = await _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>()
                    .FindByCondition(rt => rt.Token == request.RefreshToken)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storedToken == null)
                {
                    return Result<bool>.Falid(false, "Refresh token not found.");
                }

                storedToken.Revoked = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.Identity.RefreshToken>().Update(storedToken);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return Result<bool>.Success(true, "Logged out successfully.");
            }
        }
    }
}
