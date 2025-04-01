using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchedulediaryApi.DTOs;
using SchedulediaryApi.Models;
using SchedulediaryApi.Services;
using System.Security.Claims;

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

        private int? GetUserIdFromClaims()
        {
            var subClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(subClaim, out int userId))
                return userId;

            return null;
        }

        [HttpGet]
        public IActionResult GetByDate(
            [FromQuery] DateTime date,
            [FromQuery] int? priorityLevel = null,
            [FromQuery] string sortByPriority = "asc",
            [FromQuery] bool? isCompleted = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            _logger.LogInformation("查詢行程，UserId: {UserId}, Date: {Date}", userId, date);
            var (data, totalCount) = _repo.GetByDate(userId.Value, date, priorityLevel, sortByPriority, isCompleted, page, pageSize);
            return Ok(new
            {
                Data = data,
                TotalCount = totalCount
            });
        }


        [HttpGet("all")]
        public IActionResult GetAll(
            [FromQuery] int? priorityLevel = null,
            [FromQuery] string sortByPriority = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var (data, totalCount) = _repo.GetAll(userId.Value, priorityLevel, sortByPriority, page, pageSize);
            return Ok(new
            {
                Data = data,
                TotalCount = totalCount
            });
        }


        [HttpPost]
        public IActionResult Add(ScheduleDto dto)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("無法取得使用者資訊");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("標題不能為空");

            if (dto.Date == default)
                return BadRequest("日期不能為空");

            if (!_userRepo.IsUserIdExist(userId.Value))
                return BadRequest("使用者不存在");

            try
            {
                var item = new ScheduleItem
                {
                    UserId = userId.Value,
                    Date = dto.Date,
                    Title = dto.Title,
                    Content = dto.Content,
                    PriorityLevel = dto.PriorityLevel,
                    Category = dto.Category
                };
                int newId = _repo.Add(item);
                return Ok(new { message = "新增成功", id = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增行程失敗，UserId: {UserId}", userId);
                return StatusCode(500, $"新增失敗: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, ScheduleDto dto)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("無法取得使用者資訊");

            var existingItem = _repo.GetById(id, userId.Value);
            if (existingItem == null)
                return NotFound("找不到該行程或無權限修改");

            // 預設使用原本的資料，如果 dto 裡有新值才覆蓋
            existingItem.Date = dto.Date != default ? dto.Date : existingItem.Date;
            existingItem.Title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : existingItem.Title;
            existingItem.Content = dto.Content ?? existingItem.Content;
            existingItem.PriorityLevel = dto.PriorityLevel;
            existingItem.Category = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : existingItem.Category;
            existingItem.IsCompleted = dto.IsCompleted;

            try
            {
                _repo.Update(existingItem);
                return Ok("修改成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新失敗");
                return StatusCode(500, "更新失敗：" + ex.Message);
            }
        }


        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("無法取得使用者資訊");

            try
            {
                _repo.Delete(id, userId.Value);
                return Ok("刪除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除失敗，UserId: {UserId}", userId);
                return StatusCode(500, $"刪除失敗: {ex.Message}");
            }
        }

        // 模糊搜尋
        [HttpGet("search")]
        public IActionResult Search(
            [FromQuery] string keyword = "",
            [FromQuery] bool includeCompleted = true,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? priorityLevel = null,
            [FromQuery] string tag = "",
            [FromQuery] string searchType = "any",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var (data, totalCount) = _repo.Search(userId.Value, keyword, includeCompleted, startDate, endDate, priorityLevel, tag, searchType, page, pageSize);
            return Ok(new
            {
                Data = data,
                TotalCount = totalCount
            });
        }

        // 標記完成
        [HttpPut("{id}/complete")]
        public IActionResult Complete(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            try
            {
                _repo.Complete(id, userId.Value);
                return Ok("已標記為完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "標記完成失敗");
                return BadRequest(ex.Message);
            }
        }

        // 取消完成
        [HttpPut("{id}/uncomplete")]
        public IActionResult Uncomplete(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            try
            {
                _repo.Uncomplete(id, userId.Value);
                return Ok("已取消完成標記");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消完成失敗");
                return BadRequest(ex.Message);
            }
        }

        // 查詢某天完成/未完成行程
        [HttpGet("completion")]
        public IActionResult GetByCompletion([FromQuery] DateTime date, [FromQuery] bool isCompleted)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var result = _repo.GetByDateAndCompletion(userId.Value, date, isCompleted);
            return Ok(result);
        }

        // 類別分組
        [HttpGet("category")]
        public IActionResult GetByCategory()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var result = _repo.GetByCategory(userId.Value);
            return Ok(result);
        }

        // 優先度分組
        [HttpGet("priority")]
        public IActionResult GetByPriority()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var result = _repo.GetByPriority(userId.Value);
            return Ok(result);
        }

        // 取得某日完成統計（例如 3/5）
        [HttpGet("daily-stats")]
        public IActionResult GetDailyStats([FromQuery] DateTime date)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var stats = _repo.GetDailyStats(userId.Value, date);
            return Ok(stats);
        }

        // 取得某月完成統計（每日統計）
        [HttpGet("monthly-stats")]
        public IActionResult GetMonthlyStats([FromQuery] int year, [FromQuery] int month)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var stats = _repo.GetMonthlyStats(userId.Value, year, month);
            return Ok(stats);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized("無法取得使用者資訊");

            var item = _repo.GetById(id, userId.Value);
            if (item == null)
                return NotFound("找不到該行程或無權限存取");

            return Ok(item);
        }

    }
}
