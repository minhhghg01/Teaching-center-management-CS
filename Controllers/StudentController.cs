using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qltt.Data;
using Qltt.Models;

namespace Qltt.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(ApplicationDbContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var user = await _context.Users.FindAsync(userId);
            return View(user);
        }

        public IActionResult Tests()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Start(int testId = 3)
        {
            try
            {
                _logger.LogInformation($"Fetching questions for TestId={testId}, ClassId=1");

                var questions = await _context.Questions
                    .Include(q => q.Test)
                    .Where(q => q.TestId == testId && q.Test.ClassId == 1)
                    .OrderBy(r => Guid.NewGuid())
                    .Take(10)
                    .ToListAsync();

                if (!questions.Any())
                {
                    _logger.LogWarning($"No questions found for TestId={testId}, ClassId=1");
                    TempData["Error"] = "Không tìm thấy câu hỏi nào!";
                    return RedirectToAction("Tests");
                }

                _logger.LogInformation($"Found {questions.Count} questions");
                return View(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Tests");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int correctCount, int totalQuestions, int testId)
        {

            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);



            // Quy đổi sang thang điểm 10
            decimal score = (correctCount / totalQuestions) * 10;

            // Lưu kết quả
            var studentTest = new StudentTest
            {
                StudentId = studentId,
                TestId = testId,
                Score = score
            };

            _context.StudentTests.Add(studentTest);
            await _context.SaveChangesAsync();


            return RedirectToAction("Result", new { id = studentTest.StudentTestId });
        }

        public async Task<IActionResult> Result(int id)
        {
            // var studentTest = await _context.StudentTests
            //     .Include(st => st.Test)
            //     .FirstOrDefaultAsync(st => st.StudentTestId == id);

            // if (studentTest == null)
            // {
            //     return NotFound();
            // }

            return View();
        }
    }
}

