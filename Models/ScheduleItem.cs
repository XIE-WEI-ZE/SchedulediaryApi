namespace SchedulediaryApi.Models
{
    public class ScheduleItem
    {
        public int Id { get; set; }           // 對應 ToDoId
        public int UserId { get; set; }
        public DateTime Date { get; set; }     // 對應 DueDateTime
        public string Title { get; set; } = "";
        public string Content { get; set; } = ""; // 對應 Description
        public int PriorityLevel { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public string Category { get; set; } = "";
    }
}