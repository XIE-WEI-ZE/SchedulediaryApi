namespace SchedulediaryApi.Models
{
    public class ScheduleItem
    {
        public int Id { get; set; }           // 對應 ToDoId
        public int UserId { get; set; }
        public DateTime DueDateTime { get; set; }     // 任務的截止時間
        public DateTime CreatedAt { get; set; }     // 創建時間（原本的 DueDateTime 其實是創建時間）
        public string Title { get; set; } = "";
        public string Content { get; set; } = ""; // 對應 Description
        public int PriorityLevel { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public string Category { get; set; } = "";
    }
}
