namespace SchedulediaryApi.DTOs
{
    public class ScheduleDto
    {
        public int UserId { get; set; }
        public DateTime Date { get; set; } // 對應 DueDateTime
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int PriorityLevel { get; set; } = 0;
        public string Category { get; set; } = "";
    }
}