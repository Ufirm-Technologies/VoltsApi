namespace vaulterpAPI.Models.Process
{
    public class ProcessWithOperationsDto
    {
        public int ProcessId { get; set; } // ✅ Added ProcessId
        public string ProcessName { get; set; }
        public int OfficeId { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }

        public List<OperationDto> Operations { get; set; }
    }

    public class OperationDto
    {
        public int OperationId { get; set; }
        public int StepOrder { get; set; }
    }
}
