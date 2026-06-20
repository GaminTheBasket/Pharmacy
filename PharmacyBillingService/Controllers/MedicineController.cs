using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Data;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MedicineController : ControllerBase
    {
        private readonly PharmacyDbContext _context;

        public MedicineController(PharmacyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Nurse,Doctor,Patient")]
        public ActionResult<IEnumerable<Medicine>> GetAll()
        {
            return Ok(_context.Medicines.ToList());
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Nurse,Doctor,Patient")]
        public ActionResult<Medicine> Get(int id)
        {
            var medicine = _context.Medicines.Find(id);
            if (medicine == null)
            {
                return NotFound();
            }
            return Ok(medicine);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create([FromBody] Medicine medicine)
        {
            _context.Medicines.Add(medicine);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = medicine.Id }, medicine);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id, [FromBody] Medicine medicine)
        {
            var existing = _context.Medicines.Find(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = medicine.Name;
            existing.ActiveIngredient = medicine.ActiveIngredient;
            existing.Unit = medicine.Unit;
            existing.Price = medicine.Price;
            existing.StockQuantity = medicine.StockQuantity;

            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var medicine = _context.Medicines.Find(id);
            if (medicine == null)
            {
                return NotFound();
            }

            _context.Medicines.Remove(medicine);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
