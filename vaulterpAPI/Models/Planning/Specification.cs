namespace vaulterpAPI.Models.Planning
{
    public class Specification
    {
        public int Id { get; set; }
        public string SpecificationName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}