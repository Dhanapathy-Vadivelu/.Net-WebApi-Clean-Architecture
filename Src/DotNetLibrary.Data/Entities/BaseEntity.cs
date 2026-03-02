namespace DotNetLibrary.Data.Entities
{
    public class BaseEntity
    {
        public long Id { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
