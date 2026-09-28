using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepositoryBase<T> Repository<T>() where T : class;
        Task<int> CompleteAsync(CancellationToken cancellationToken);
        Task<int> CompleteAsync();
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task<int> DecrementStockAtomicallyAsync(string productId, int quantity, CancellationToken cancellationToken = default);
        Task<int> DecrementVariantStockAtomicallyAsync(string variantId, int quantity, CancellationToken cancellationToken = default);
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IRepositoryBase<T> Repository<T>() where T : class
        {
            if (_repositories.ContainsKey(typeof(T)))
            {
                return (IRepositoryBase<T>)_repositories[typeof(T)];
            }

            var repository = new RepositoryBase<T>(_dbContext);
            _repositories.Add(typeof(T), repository);
            return repository;
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public Task<int> DecrementStockAtomicallyAsync(string productId, int quantity, CancellationToken cancellationToken = default)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [catalog].[Product] SET StockQuantity = StockQuantity - {quantity}, LastModifiedAt = {DateTime.UtcNow} WHERE Id = {productId} AND StockQuantity >= {quantity}",
                cancellationToken);
        }

        public Task<int> DecrementVariantStockAtomicallyAsync(string variantId, int quantity, CancellationToken cancellationToken = default)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [catalog].[ProductVariant] SET StockQuantity = StockQuantity - {quantity} WHERE Id = {variantId} AND StockQuantity >= {quantity}",
                cancellationToken);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _dbContext.Dispose();
        }
    }
}
