namespace SchedulediaryApi.Models
{
    public class CompletedScheduleItem
    {
        public int Id { get; set; } // 對應 DoneId
        public int UserId { get; set; }
        public DateTime CompletedDateTime { get; set; } // 完成時間
        public string Title { get; set; } = "";
        public string Note { get; set; } = ""; // 用來記錄附註或備註
        public DateTime CreatedAt { get; set; } // 創建時間，與 ScheduleItem 的 CreatedAt 對應
    }
}
