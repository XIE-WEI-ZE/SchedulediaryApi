using SchedulediaryApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SchedulediaryApi.Services
{
    public class ScheduleRepository
    {
        private readonly string _connStr;

        public ScheduleRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        // 確保只有一個 Add 方法
        public int Add(ScheduleItem item)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                INSERT INTO ToDoEvents (UserId, DueDateTime, Title, Description, PriorityLevel, IsCompleted, CreatedAt, Category)
                OUTPUT INSERTED.ToDoId
                VALUES (@UserId, @DueDateTime, @Title, @Description, @PriorityLevel, 0, GETDATE(), @Category)
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = item.UserId;
            cmd.Parameters.Add("@DueDateTime", SqlDbType.DateTime).Value = item.Date;
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 100).Value = item.Title;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = item.Content;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = item.PriorityLevel;
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = item.Category ?? "";

            conn.Open();
            return (int)cmd.ExecuteScalar();
        }

        // 確保 Update 方法存在且正確
        public void Update(ScheduleItem item)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                UPDATE ToDoEvents 
                SET DueDateTime = @DueDateTime, Title = @Title, Description = @Description, PriorityLevel = @PriorityLevel, Category = @Category
                WHERE ToDoId = @Id AND UserId = @UserId
            ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = item.Id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = item.UserId;
            cmd.Parameters.Add("@DueDateTime", SqlDbType.DateTime).Value = item.Date;
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 100).Value = item.Title;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = item.Content;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = item.PriorityLevel;
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = item.Category ?? "";

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
                throw new Exception("行程不存在或無權更新");
        }

        // Search 方法（與之前修正一致）
        public List<ScheduleItem> Search(int userId, string keyword, bool includeCompleted)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId
                AND (Title LIKE @Keyword OR Description LIKE @Keyword OR Category LIKE @Keyword)
                AND (@IncludeCompleted = 1 OR IsCompleted = 0)
                ORDER BY DueDateTime
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar).Value = $"%{keyword}%";
            cmd.Parameters.Add("@IncludeCompleted", SqlDbType.Bit).Value = includeCompleted;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        // 其他方法保持不變（假設已正確定義）
        public List<ScheduleItem> GetByDate(int userId, DateTime date, int? priorityLevel = null, string sortByPriority = "asc")
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId 
                AND CONVERT(date, DueDateTime) = @Date 
                AND IsCompleted = 0
                AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
                ORDER BY PriorityLevel " + (string.Equals(sortByPriority, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC"), conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Date", SqlDbType.Date).Value = date.Date;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = (object?)priorityLevel ?? DBNull.Value;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        public List<ScheduleItem> GetAll(int userId, int? priorityLevel = null, string sortByPriority = "asc")
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId 
                AND IsCompleted = 0
                AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
                ORDER BY PriorityLevel " + (string.Equals(sortByPriority, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC") + ", DueDateTime", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = (object?)priorityLevel ?? DBNull.Value;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        public List<PrioritizedSchedule> GetByPriority(int userId)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId
                ORDER BY PriorityLevel, DueDateTime
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list.GroupBy(i => i.PriorityLevel)
                       .Select(g => new PrioritizedSchedule
                       {
                           PriorityLevel = g.Key,
                           Items = g.ToList()
                       })
                       .ToList();
        }

        public List<ScheduleItem> GetCompleted(int userId)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId AND IsCompleted = 1
                ORDER BY DueDateTime DESC
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        public List<CategorizedSchedule> GetByCategory(int userId)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId
                ORDER BY Category, DueDateTime
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list.GroupBy(i => i.Category)
                       .Select(g => new CategorizedSchedule
                       {
                           Category = g.Key,
                           Items = g.ToList()
                       })
                       .ToList();
        }

        public List<ScheduleItem> GetByDateRange(int userId, DateTime startDate, DateTime endDate)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT * FROM ToDoEvents
                WHERE UserId = @UserId
                AND DueDateTime BETWEEN @StartDate AND @EndDate
                ORDER BY DueDateTime
            ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
            cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        public List<ScheduleItem> GetByDateAndCompletion(int userId, DateTime date, bool isCompleted)
        {
            var list = new List<ScheduleItem>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
        SELECT * FROM ToDoEvents
        WHERE UserId = @UserId
        AND CONVERT(date, DueDateTime) = @Date
        AND IsCompleted = @IsCompleted
        ORDER BY DueDateTime
    ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Date", SqlDbType.Date).Value = date.Date;
            cmd.Parameters.Add("@IsCompleted", SqlDbType.Bit).Value = isCompleted;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
            }
            return list;
        }

        public DailyStats GetDailyStats(int userId, DateTime date)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
        SELECT
            SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedCount,
            COUNT(*) AS TotalCount
        FROM ToDoEvents
        WHERE UserId = @UserId AND CONVERT(date, DueDateTime) = @Date
    ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Date", SqlDbType.Date).Value = date.Date;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int completed = (int)reader["CompletedCount"];
                int total = (int)reader["TotalCount"];
                return new DailyStats
                {
                    Date = date.Date,
                    Completed = completed,
                    Total = total,
                    Status = $"{completed}/{total}"
                };
            }

            return new DailyStats
            {
                Date = date.Date,
                Completed = 0,
                Total = 0,
                Status = "0/0"
            };
        }

        public List<DailyStats> GetMonthlyStats(int userId, int year, int month)
        {
            var list = new List<DailyStats>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
        SELECT 
            CONVERT(date, DueDateTime) AS ScheduleDate,
            SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedCount,
            COUNT(*) AS TotalCount
        FROM ToDoEvents
        WHERE UserId = @UserId
        AND YEAR(DueDateTime) = @Year
        AND MONTH(DueDateTime) = @Month
        GROUP BY CONVERT(date, DueDateTime)
        ORDER BY ScheduleDate
    ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Year", SqlDbType.Int).Value = year;
            cmd.Parameters.Add("@Month", SqlDbType.Int).Value = month;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var date = (DateTime)reader["ScheduleDate"];
                int completed = (int)reader["CompletedCount"];
                int total = (int)reader["TotalCount"];

                list.Add(new DailyStats
                {
                    Date = date,
                    Completed = completed,
                    Total = total,
                    Status = $"{completed}/{total}"
                });
            }

            return list;
        }


        public void Delete(int id, int userId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                DELETE FROM ToDoEvents WHERE ToDoId = @Id AND UserId = @UserId
            ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
                throw new Exception("行程不存在或無權刪除");
        }

        public void Complete(int id, int userId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                UPDATE ToDoEvents 
                SET IsCompleted = 1 
                WHERE ToDoId = @Id AND UserId = @UserId AND IsCompleted = 0
            ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
                throw new Exception("無法完成該行程，可能不存在或已完成");
        }

        public void Uncomplete(int id, int userId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                UPDATE ToDoEvents 
                SET IsCompleted = 0 
                WHERE ToDoId = @Id AND UserId = @UserId AND IsCompleted = 1
            ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM ToDoEvents WHERE ToDoId = @Id AND UserId = @UserId", conn);
                checkCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                checkCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                int exists = (int)checkCmd.ExecuteScalar();
                if (exists == 0)
                    throw new Exception("行程不存在");
                throw new Exception("該行程尚未完成，無法取消完成狀態");
            }
        }

        private static ScheduleItem ReadScheduleItem(SqlDataReader reader)
        {
            return new ScheduleItem
            {
                Id = (int)reader["ToDoId"],
                UserId = (int)reader["UserId"],
                Date = (DateTime)reader["DueDateTime"],
                Title = reader["Title"]?.ToString() ?? "",
                Content = reader["Description"]?.ToString() ?? "",
                PriorityLevel = (int)reader["PriorityLevel"],
                IsCompleted = (bool)reader["IsCompleted"],
                Category = reader["Category"]?.ToString() ?? ""
            };
        }
    }
}