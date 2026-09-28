using Domain.Entities.PromotionEntities;

namespace Application.Features.PromotionFeatures.Commands
{
    public class AddCouponCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        public decimal DiscountValue { get; set; }

        public decimal? MinOrderValue { get; set; }
        public int MaxUses { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public class AddCouponCommandHandler : IRequestHandler<AddCouponCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public AddCouponCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(AddCouponCommand request, CancellationToken cancellationToken)
            {
                var existingCoupon = await _unitOfWork.Repository<Coupon>()
                    .FindByCondition(c => c.Code == request.Code)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingCoupon != null)
                    return Result<string>.Falid(null, "A coupon with this code already exists.");

                var coupon = new Coupon
                {
                    Code = request.Code.ToUpper(),
                    Description = request.Description,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    MinOrderValue = request.MinOrderValue,
                    MaxUses = request.MaxUses,
                    ValidFrom = request.ValidFrom,
                    ValidTo = request.ValidTo,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Coupon>().Create(coupon);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(coupon.Id, ResourcesLocalizationKeys.AddSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.AddFailed);
            }
        }
    }

    public class UpdateCouponCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int MaxUses { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;

        public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public UpdateCouponCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
            {
                var coupon = await _unitOfWork.Repository<Coupon>()
                    .FindByCondition(c => c.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (coupon == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                coupon.Code = request.Code.ToUpper();
                coupon.Description = request.Description;
                coupon.DiscountType = request.DiscountType;
                coupon.DiscountValue = request.DiscountValue;
                coupon.MinOrderValue = request.MinOrderValue;
                coupon.MaxUses = request.MaxUses;
                coupon.ValidFrom = request.ValidFrom;
                coupon.ValidTo = request.ValidTo;
                coupon.IsActive = request.IsActive;
                coupon.LastModifiedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Coupon>().Update(coupon);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(coupon.Id, ResourcesLocalizationKeys.UpdateSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.UpdateFailed);
            }
        }
    }

    public class DeleteCouponCommand : IRequest<Result<string>>
    {
        public string Id { get; set; } = string.Empty;

        public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public DeleteCouponCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
            {
                var coupon = await _unitOfWork.Repository<Coupon>()
                    .FindByCondition(c => c.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (coupon == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                _unitOfWork.Repository<Coupon>().Delete(coupon);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, ResourcesLocalizationKeys.DeleteSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.DeleteFailed);
            }
        }
    }

    public class ToggleCouponCommand : IRequest<Result<string>>
    {
        public string Id { get; set; } = string.Empty;

        public class ToggleCouponCommandHandler : IRequestHandler<ToggleCouponCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public ToggleCouponCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(ToggleCouponCommand request, CancellationToken cancellationToken)
            {
                var coupon = await _unitOfWork.Repository<Coupon>()
                    .FindByCondition(c => c.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (coupon == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                coupon.IsActive = !coupon.IsActive;
                coupon.LastModifiedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Coupon>().Update(coupon);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, coupon.IsActive ? "Coupon activated." : "Coupon deactivated.")
                    : Result<string>.Falid(null, "Failed to toggle coupon.");
            }
        }
    }
}
