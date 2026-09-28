using Application.DTO.AuthDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AuthFeatures.Queries
{
    public class GetCurrentUserQuery : IRequest<Result<UserDto>>
    {
        public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ICurrentUserService _currentUserService;

            public GetCurrentUserQueryHandler(
                UserManager<ApplicationUser> userManager,
                ICurrentUserService currentUserService)
            {
                _userManager = userManager;
                _currentUserService = currentUserService;
            }

            public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
            {
                if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
                {
                    return Result<UserDto>.Falid(null, "Not authenticated.");
                }

                var user = await _userManager.FindByIdAsync(_currentUserService.UserId);
                if (user == null)
                {
                    return Result<UserDto>.Falid(null, "User not found.");
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Result<UserDto>.Success(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                });
            }
        }
    }
}
