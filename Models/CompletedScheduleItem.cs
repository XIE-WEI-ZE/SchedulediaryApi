namespace SchedulediaryApi.Models
{
    public class CompletedScheduleItem
    {
        public int Id { get; set; } //對應 DoneId
        public int UserId { get; set; }
        public DateTime CompletedDateTime { get; set; }
        public string Title { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}