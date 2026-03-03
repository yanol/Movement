using Microsoft.AspNetCore.Mvc;

namespace DataService
{
    [ApiController]
    [Route("data")]
    public class DataController : ControllerBase
    {
        private readonly IDataService _dataService;

        public DataController(IDataService dataService) => _dataService = dataService;

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DataRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Content))
                return BadRequest(new { Message = "Content must not be empty." });

            var id = await _dataService.SaveAsync(request.Content);
            return CreatedAtAction(
                     nameof(Get),
                     new { id },
                     new { Id = id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _dataService.GetAsync(id);
            return result != null ? Ok(result) : NotFound(new { Message = $"Data with ID {id} not found." });
        }
    }

    public class DataRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
