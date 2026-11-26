using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PhysicianBooking.Services.Implementation;

namespace PhysicianBooking.API.Controllers
{
    /// <summary>
    /// Data-import endpoints that call the PAID Google Places API and save results to the database.
    /// Disabled unless "Features:GooglePlacesImport" is true. Keep it OFF in Production:
    /// the site reads from the database, so production never needs Google.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GooglePlaceController : ControllerBase, IActionFilter
    {
        private readonly GooglePlacesService _service;
        private readonly bool _importEnabled;

        public GooglePlaceController(GooglePlacesService service, IConfiguration configuration)
        {
            _service = service;
            _importEnabled = configuration.GetValue<bool>("Features:GooglePlacesImport");
        }

        [NonAction]
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_importEnabled)
            {
                context.Result = NotFound();
            }
        }

        [NonAction]
        public void OnActionExecuted(ActionExecutedContext context) { }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string category, string zip)
        {
            var results = await _service.SearchAndSaveBusinesses(category, zip);
            return Ok(results);
        }

        [HttpGet("searchhours")]
        public async Task<IActionResult> SearchHours()
        {
            var results = await _service.GetWeekdayDescriptionsAsync();
            return Ok(results);
        }
    }
}
