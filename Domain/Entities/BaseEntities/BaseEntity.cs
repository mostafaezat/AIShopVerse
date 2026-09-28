namespace Domain.Entities.BaseEntities
{
    public abstract class BaseEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
