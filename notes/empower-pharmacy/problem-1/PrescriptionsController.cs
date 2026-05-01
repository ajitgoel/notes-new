using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Problem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PrescriptionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null) return NotFound();
            return Ok(prescription);
        }

        [HttpGet]
        public async Task<IActionResult> GetByStatus([FromQuery] string? status)
        {
            var query = _context.Prescriptions.AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }
            var results = await query.ToListAsync();
            return Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
        {
            var prescription = new Prescription
            {
                PatientId = dto.PatientId,
                MedicationName = dto.MedicationName,
                Dosage = dto.Dosage,
                Notes = dto.Notes,
                Status = "pending"
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = prescription.Id }, new { id = prescription.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null) return NoContent();

            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
