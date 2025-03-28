using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchedulediaryApi.DTOs;
using SchedulediaryApi.Models;
using SchedulediaryApi.Services;
using System;

namespace SchedulediaryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleRepository _repo;
        private readonly UserRepository _userRepo;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(ScheduleRepository repo, UserRepository userRepo, ILogger<ScheduleController> logger)
        {
            _repo = repo;
            _userRepo = userRepo;
            _logger = logger;
        }

        private bool IsAuthorizedUser(int userId)
        {
            var subClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("JWT subClaim: {SubClaim}，傳入的 userId: {UserId}", subClaim, userId);

            if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out int currentUserId))
            {
                _logger.LogWarning("無法解析當前用戶 ID，預設為未授權");
                return false;
            }
            return currentUserId == userId;
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] int userId, [FromQuery] string keyword, [FromQuery] bool? includeCompleted = false)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程"); 
            }

            _logger.LogInformation("收到模糊搜尋請求，UserId: {UserId}, Keyword: {Keyword}, IncludeCompleted: {IncludeCompleted}", userId, keyword, includeCompleted);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                _logger.LogWarning("搜尋失敗：關鍵字為空");
                return BadRequest("請輸入搜尋關鍵字");
            }

            try
            {
                var items = _repo.Search(userId, keyword, includeCompleted ?? false);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋行程失敗，UserId: {UserId}, Keyword: {Keyword}", userId, keyword);
                return StatusCode(500, $"搜尋行程失敗：{ex.Message}");
            }
        }

        
        [HttpGet]
        public IActionResult GetByDate([FromQuery] int userId, [FromQuery] DateTime date, [FromQuery] int? priorityLevel = null, [FromQuery] string sortByPriority = "asc")
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到查詢行程請求，UserId: {UserId}, Date: {Date}, PriorityLevel: {PriorityLevel}, SortByPriority: {SortByPriority}", userId, date, priorityLevel, sortByPriority);
            var items = _repo.GetByDate(userId, date, priorityLevel, sortByPriority);
            return Ok(items);
        }

        [HttpGet("all")]
        public IActionResult GetAll([FromQuery] int userId, [FromQuery] int? priorityLevel = null, [FromQuery] string sortByPriority = "asc")
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到查詢所有行程請求，UserId: {UserId}, PriorityLevel: {PriorityLevel}, SortByPriority: {SortByPriority}", userId, priorityLevel, sortByPriority);
            var items = _repo.GetAll(userId, priorityLevel, sortByPriority);
            return Ok(items);
        }

        [HttpGet("byPriority")]
        public IActionResult GetByPriority([FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到按優先權分類查詢請求，UserId: {UserId}", userId);
            var items = _repo.GetByPriority(userId);
            return Ok(items);
        }

        [HttpGet("completed")]
        public IActionResult GetCompleted([FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到查詢已完成行程請求，UserId: {UserId}", userId);
            var items = _repo.GetCompleted(userId);
            return Ok(items);
        }

        [HttpGet("byCategory")]
        public IActionResult GetByCategory([FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到按分類查詢請求，UserId: {UserId}", userId);
            var items = _repo.GetByCategory(userId);
            return Ok(items);
        }

        [HttpGet("byDateRange")]
        public IActionResult GetByDateRange([FromQuery] int userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到日期區間查詢請求，UserId: {UserId}, Start: {StartDate}, End: {EndDate}", userId, startDate, endDate);
            if (startDate > endDate)
            {
                _logger.LogWarning("查詢失敗：開始日期晚於結束日期");
                return BadRequest("開始日期必須早於結束日期");
            }

            var items = _repo.GetByDateRange(userId, startDate, endDate);
            return Ok(items);
        }

        [HttpGet("date")]
        public IActionResult GetByDateWithCompletion(
    [FromQuery] int userId,
    [FromQuery] DateTime date,
    [FromQuery] bool isCompleted)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權查詢他人行程，UserId={UserId}", userId);
                return StatusCode(403, "無權查詢他人行程");
            }

            _logger.LogInformation("查詢某日行程，UserId={UserId}, Date={Date}, 已完成={IsCompleted}", userId, date.ToShortDateString(), isCompleted);
            var items = _repo.GetByDateAndCompletion(userId, date, isCompleted);
            return Ok(items);
        }


        [HttpPost]
        public IActionResult Add(ScheduleDto dto)
        {
            if (!IsAuthorizedUser(dto.UserId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", dto.UserId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到新增行程請求，UserId: {UserId}, Title: {Title}", dto.UserId, dto.Title);

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                _logger.LogWarning("新增行程失敗：標題為空");
                return BadRequest("標題不能為空");
            }

            if (dto.Date == default)
            {
                _logger.LogWarning("新增行程失敗：日期為空");
                return BadRequest("日期不能為空");
            }

            if (!_userRepo.IsUserIdExist(dto.UserId))
            {
                _logger.LogWarning("新增行程失敗：UserId {UserId} 不存在", dto.UserId);
                return BadRequest("指定的 UserId 不存在");
            }

            try
            {
                var item = new ScheduleItem
                {
                    UserId = dto.UserId,
                    Date = dto.Date,
                    Title = dto.Title,
                    Content = dto.Content,
                    PriorityLevel = dto.PriorityLevel,
                    Category = dto.Category
                };
                int newId = _repo.Add(item);
                _logger.LogInformation("成功新增行程，Id: {Id}, UserId: {UserId}, Title: {Title}", newId, dto.UserId, dto.Title);
                return Ok(new { message = "新增成功", id = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增行程失敗，UserId: {UserId}, Title: {Title}", dto.UserId, dto.Title);
                return StatusCode(500, $"新增行程失敗：{ex.Message}");
            }
        }

        [HttpGet("stats")]
        public IActionResult GetDailyStats([FromQuery] int userId, [FromQuery] DateTime date)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權查詢統計資訊，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("查詢統計資料，UserId={UserId}, Date={Date}", userId, date);
            var result = _repo.GetDailyStats(userId, date);
            return Ok(result);
        }

        [HttpGet("monthly-stats")]
        public IActionResult GetMonthlyStats([FromQuery] int userId, [FromQuery] int year, [FromQuery] int month)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權查詢使用者 {UserId} 的月統計", userId);
                return StatusCode(403, "無權操作");
            }

            _logger.LogInformation("查詢整月統計：UserId={UserId}, Year={Year}, Month={Month}", userId, year, month);
            var list = _repo.GetMonthlyStats(userId, year, month);
            return Ok(list);
        }


        [HttpPut("{id}")]
        public IActionResult Update(int id, ScheduleDto dto)
        {
            if (!IsAuthorizedUser(dto.UserId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", dto.UserId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到更新行程請求，Id: {Id}, UserId: {UserId}", id, dto.UserId);

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                _logger.LogWarning("更新行程失敗：標題為空");
                return BadRequest("標題不能為空");
            }

            if (dto.Date == default)
            {
                _logger.LogWarning("更新行程失敗：日期為空");
                return BadRequest("日期不能為空");
            }

            try
            {
                var item = new ScheduleItem
                {
                    Id = id,
                    UserId = dto.UserId,
                    Date = dto.Date,
                    Title = dto.Title,
                    Content = dto.Content,
                    PriorityLevel = dto.PriorityLevel,
                    Category = dto.Category
                };
                _repo.Update(item);
                _logger.LogInformation("成功更新行程，Id: {Id}, UserId: {UserId}", id, dto.UserId);
                return Ok("修改成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新行程失敗，Id: {Id}, UserId: {UserId}", id, dto.UserId);
                if (ex.Message.Contains("不存在或無權"))
                    return BadRequest("行程不存在或無權更新");
                return StatusCode(500, $"修改行程失敗：{ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id, [FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到刪除行程請求，Id: {Id}, UserId: {UserId}", id, userId);

            try
            {
                _repo.Delete(id, userId);
                _logger.LogInformation("成功刪除行程，Id: {Id}, UserId: {UserId}", id, userId);
                return Ok("刪除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除行程失敗，Id: {Id}, UserId: {UserId}", id, userId);
                if (ex.Message.Contains("不存在或無權"))
                    return BadRequest("行程不存在或無權刪除");
                return StatusCode(500, $"刪除行程失敗：{ex.Message}");
            }
        }

        [HttpPost("complete/{id}")]
        public IActionResult Complete(int id, [FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到標記行程為完成請求，Id: {Id}, UserId: {UserId}", id, userId);

            try
            {
                _repo.Complete(id, userId);
                _logger.LogInformation("成功標記行程為完成，Id: {Id}, UserId: {UserId}", id, userId);
                return Ok("標記完成成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "標記行程為完成失敗，Id: {Id}, UserId: {UserId}", id, userId);
                if (ex.Message.Contains("不存在或已完成"))
                    return BadRequest("行程不存在或已完成");
                return StatusCode(500, $"標記行程為完成失敗：{ex.Message}");
            }
        }

        [HttpPost("uncomplete/{id}")]
        public IActionResult Uncomplete(int id, [FromQuery] int userId)
        {
            if (!IsAuthorizedUser(userId))
            {
                _logger.LogWarning("無權操作此行程，UserId={UserId}", userId);
                return StatusCode(403, "無權操作此行程");
            }

            _logger.LogInformation("收到取消完成請求，Id: {Id}, UserId: {UserId}", id, userId);

            try
            {
                _repo.Uncomplete(id, userId);
                _logger.LogInformation("成功取消完成，Id: {Id}, UserId: {UserId}", id, userId);
                return Ok("已取消完成標記");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消完成失敗，Id: {Id}, UserId: {UserId}", id, userId);
                if (ex.Message.Contains("行程不存在"))
                    return BadRequest("行程不存在");
                if (ex.Message.Contains("尚未完成"))
                    return BadRequest("該行程尚未完成，無法取消完成狀態");
                return StatusCode(500, $"取消完成失敗：{ex.Message}");
            }
        }
    }
}