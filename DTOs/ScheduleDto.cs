namespace SchedulediaryApi.DTOs
{
    public class ScheduleDto
    {
        public DateTime DueDateTime { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";

        public int PriorityLevel { get; set; } = 0;
        public string Category { get; set; } = "";
        public bool IsCompleted { get; set; } = false; //  新增此行
    }
}
