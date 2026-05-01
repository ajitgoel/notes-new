using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Problem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientService _patientService;

        public PatientsController(PatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpGet("active/{state}")]
        public async Task<IActionResult> GetActiveByState(string state)
        {
            var patients = await _patientService.GetActivePatientsByStateAsync(state);
            return Ok(patients);
        }

        [HttpPost("deactivate/{id}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _patientService.DeactivatePatientAsync(id);
            return NoContent();
        }
    }
}
