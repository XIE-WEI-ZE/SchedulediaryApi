namespace SchedulediaryApi.Models
{
    public class CategorizedSchedule
    {
        public string Category { get; set; } = "";
        public List<ScheduleItem> Items { get; set; } = new();
    }
}