using Infrastructure.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Features.ProductFeatures.Commands
{
    public class UploadImageCommand : IRequest<Result<string>>
    {
        public IFormFile File { get; set; } = null!;

        public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, Result<string>>
        {
            private readonly IFileRepository _fileRepository;

            public UploadImageCommandHandler(IFileRepository fileRepository)
            {
                _fileRepository = fileRepository;
            }

            public async Task<Result<string>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
            {
                if (request.File == null || request.File.Length == 0)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.Failed);

                await using var stream = request.File.OpenReadStream();

                var url = await _fileRepository.UploadAsync(stream, request.File.FileName, request.File.ContentType, cancellationToken);

                return Result<string>.Success(url, ResourcesLocalizationKeys.AddSuccess);
            }
        }
    }
}
