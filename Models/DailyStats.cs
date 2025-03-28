namespace SchedulediaryApi.Models
{
    public class DailyStats
    {
        public DateTime Date { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
        public string Status { get; set; } = "";
    }
}
