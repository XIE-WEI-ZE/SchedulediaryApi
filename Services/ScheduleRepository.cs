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
            cmd.Parameters.Add("@DueDateTime", SqlDbType.DateTime).Value = item.DueDateTime; // 使用 DueDateTime 作為事件日期
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
        SET DueDateTime = @DueDateTime, 
            Title = @Title, 
            Description = @Description, 
            PriorityLevel = @PriorityLevel, 
            Category = @Category,
            IsCompleted = @IsCompleted
        WHERE ToDoId = @Id AND UserId = @UserId
    ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = item.Id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = item.UserId;
            cmd.Parameters.Add("@DueDateTime", SqlDbType.DateTime).Value = item.DueDateTime;  // 更新 DueDateTime
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 100).Value = item.Title;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = item.Content;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = item.PriorityLevel;
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = item.Category ?? "";
            cmd.Parameters.Add("@IsCompleted", SqlDbType.Bit).Value = item.IsCompleted;  // 更新 IsCompleted

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
                throw new Exception("行程不存在或無權更新");
        }



        // Search 方法
        public (List<ScheduleItem> Data, int TotalCount) Search(
            int userId,
            string keyword,
            bool includeCompleted,
            DateTime? startDate,
            DateTime? endDate,
            int? priorityLevel,
            string tag,
            string searchType,
            int page = 1,
            int pageSize = 10)
        {
            var list = new List<ScheduleItem>();
            int totalCount = 0;

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
    WITH Filtered AS (
        SELECT *,
               COUNT(*) OVER () AS TotalCount
        FROM ToDoEvents
        WHERE UserId = @UserId
        AND (@IncludeCompleted = 1 OR IsCompleted = 0)
        AND (@Keyword IS NULL OR (
            (@SearchType = 'any' AND (Title LIKE @Keyword OR Description LIKE @Keyword OR Category LIKE @Keyword))
            OR (@SearchType = 'title' AND Title LIKE @Keyword)
            OR (@SearchType = 'content' AND Description LIKE @Keyword)
            OR (@SearchType = 'tag' AND Category LIKE @Keyword)
        ))
        AND (@StartDate IS NULL OR CreatedAt >= @StartDate)  -- 使用 CreatedAt 篩選
        AND (@EndDate IS NULL OR CreatedAt <= @EndDate)      -- 使用 CreatedAt 篩選
        AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
        AND (@Tag IS NULL OR Category LIKE @Tag)
    )
    SELECT * FROM Filtered
    ORDER BY CreatedAt DESC  -- 按照創建時間排序
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(keyword) ? DBNull.Value : $"%{keyword}%";
            cmd.Parameters.Add("@IncludeCompleted", SqlDbType.Bit).Value = includeCompleted;
            cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.HasValue ? startDate.Value : DBNull.Value;
            cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.HasValue ? endDate.Value : DBNull.Value;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = priorityLevel.HasValue ? priorityLevel.Value : DBNull.Value;
            cmd.Parameters.Add("@Tag", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(tag) ? DBNull.Value : $"%#{tag}%";
            cmd.Parameters.Add("@SearchType", SqlDbType.NVarChar).Value = searchType;
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    list.Add(ReadScheduleItem(reader));
                    if (list.Count == 1)  // 只在第一行取得 TotalCount
                        totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                }
            }

            reader.Close();

            if (list.Count == 0) // 若無資料，執行獨立查詢取得總筆數
            {
                using var countCmd = new SqlCommand(@"
        SELECT COUNT(*) FROM ToDoEvents
        WHERE UserId = @UserId
        AND (@IncludeCompleted = 1 OR IsCompleted = 0)
        AND (@Keyword IS NULL OR (
            (@SearchType = 'any' AND (Title LIKE @Keyword OR Description LIKE @Keyword OR Category LIKE @Keyword))
            OR (@SearchType = 'title' AND Title LIKE @Keyword)
            OR (@SearchType = 'content' AND Description LIKE @Keyword)
            OR (@SearchType = 'tag' AND Category LIKE @Keyword)
        ))
        AND (@StartDate IS NULL OR CreatedAt >= @StartDate)  -- 使用 CreatedAt 篩選
        AND (@EndDate IS NULL OR CreatedAt <= @EndDate)      -- 使用 CreatedAt 篩選
        AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
        AND (@Tag IS NULL OR Category LIKE @Tag)
    ", conn);
                countCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                countCmd.Parameters.Add("@Keyword", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(keyword) ? DBNull.Value : $"%{keyword}%";
                countCmd.Parameters.Add("@IncludeCompleted", SqlDbType.Bit).Value = includeCompleted;
                countCmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.HasValue ? startDate.Value : DBNull.Value;
                countCmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.HasValue ? endDate.Value : DBNull.Value;
                countCmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = priorityLevel.HasValue ? priorityLevel.Value : DBNull.Value;
                countCmd.Parameters.Add("@Tag", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(tag) ? DBNull.Value : $"%#{tag}%";
                countCmd.Parameters.Add("@SearchType", SqlDbType.NVarChar).Value = searchType;
                totalCount = (int)countCmd.ExecuteScalar();
            }

            return (list, totalCount);
        }


        // 依造日期
        public (List<ScheduleItem> Data, int TotalCount) GetByDate(
            int userId,
            DateTime date,
            int? priorityLevel = null,
            string sortByPriority = "asc",
            string? sortBy = null, // 新增參數，控制整體排序方式
            bool? isCompleted = null,
            int page = 1,
            int pageSize = 10)
        {
            var list = new List<ScheduleItem>();
            int totalCount = 0;

            using var conn = new SqlConnection(_connStr);
            // 根據 sortBy 動態構建 ORDER BY 子句
            string orderByClause = sortBy == "priorityThenCreatedAt"
                ? "PriorityLevel DESC, CreatedAt DESC" // 優先級降序，創建時間降序
                : $"PriorityLevel {(string.Equals(sortByPriority, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}";

            using var cmd = new SqlCommand(@"
        WITH Filtered AS (
            SELECT *,
                   COUNT(*) OVER () AS TotalCount
            FROM ToDoEvents
            WHERE UserId = @UserId 
            AND CONVERT(date, DueDateTime) = @Date 
            AND (@IsCompleted IS NULL OR IsCompleted = @IsCompleted)
            AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
        )
        SELECT * FROM Filtered
        ORDER BY " + orderByClause + @"
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
    ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Date", SqlDbType.Date).Value = date.Date;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = (object?)priorityLevel ?? DBNull.Value;
            cmd.Parameters.Add("@IsCompleted", SqlDbType.Bit).Value = (object?)isCompleted ?? DBNull.Value;
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
                if (list.Count == 1) totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            }

            return (list, totalCount);
        }
        public (List<ScheduleItem> Data, int TotalCount) GetAll(
           int userId,
           int? priorityLevel = null,
           string sortByPriority = "asc",
           string orderBy = "date_desc", // ✅ 新增參數
           int page = 1,
           int pageSize = 10)
        {
            var list = new List<ScheduleItem>();
            int totalCount = 0;

            using var conn = new SqlConnection(_connStr);

            // ✅ 根據 orderBy 設定排序條件
            string orderClause = orderBy switch
            {
                "date_asc" => "DueDateTime ASC",
                "date_desc" => "DueDateTime DESC",
                _ => "PriorityLevel " + (string.Equals(sortByPriority, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC") + ", DueDateTime"
            };

            using var cmd = new SqlCommand($@"
        WITH Filtered AS (
            SELECT *,
                   COUNT(*) OVER () AS TotalCount
            FROM ToDoEvents
            WHERE UserId = @UserId
            AND (@PriorityLevel IS NULL OR PriorityLevel = @PriorityLevel)
        )
        SELECT * FROM Filtered
        ORDER BY {orderClause}
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
    ", conn);

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@PriorityLevel", SqlDbType.Int).Value = (object?)priorityLevel ?? DBNull.Value;
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadScheduleItem(reader));
                if (list.Count == 1) totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            }

            return (list, totalCount);
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
                DueDateTime = (DateTime)reader["DueDateTime"],  // 保留 DueDateTime 用作任務的日期
                Title = reader["Title"]?.ToString() ?? "",
                Content = reader["Description"]?.ToString() ?? "",
                PriorityLevel = (int)reader["PriorityLevel"],
                IsCompleted = (bool)reader["IsCompleted"],
                Category = reader["Category"]?.ToString() ?? "",
                CreatedAt = (DateTime)reader["CreatedAt"] // 新增 CreatedAt
            };
        }

        public ScheduleItem? GetById(int id, int userId)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
        SELECT * FROM ToDoEvents WHERE ToDoId = @Id AND UserId = @UserId
    ", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadScheduleItem(reader);
            }
            return null;
        }

    }
}