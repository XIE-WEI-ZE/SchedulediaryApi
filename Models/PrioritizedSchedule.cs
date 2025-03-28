namespace SchedulediaryApi.Models
{
    public class PrioritizedSchedule
    {
        public int PriorityLevel { get; set; }
        public List<ScheduleItem> Items { get; set; } = new();
    }
}