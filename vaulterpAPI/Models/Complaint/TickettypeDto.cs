namespace vaulterpAPI.Models.Complaint
{
    public class TicketTypeDto
    {
        public int TicketTypeId { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; } = 0;
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int IsDeleted { get; set; } = 0;
    }
}
