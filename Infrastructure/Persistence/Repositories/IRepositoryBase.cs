using System.Data;

namespace Infrastructure.Persistence.Repositories
{
    public interface IRepositoryBase<T> where T : class
    {
        IQueryable<T> FindAll();
        IQueryable<T> FindByCondition(System.Linq.Expressions.Expression<Func<T, bool>> expression);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Attach(T entity);
        void AttachRange(IEnumerable<T> entities);
    }
}
