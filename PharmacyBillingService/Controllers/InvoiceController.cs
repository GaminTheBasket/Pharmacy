using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly PharmacyDbContext _context;

        public InvoiceController(PharmacyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Nurse,Doctor")]
        public ActionResult<IEnumerable<Invoice>> GetAll()
        {
            var invoices = _context.Invoices
                .Include(i => i.Items)
                .ThenInclude(item => item.Medicine)
                .ToList();
            return Ok(invoices);
        }

        [HttpGet("patient/{name}")]
        [Authorize(Roles = "Admin,Nurse")]
        public ActionResult<IEnumerable<Invoice>> GetByPatientName(string name)
        {
            var invoices = _context.Invoices
                .Include(i => i.Items)
                .ThenInclude(item => item.Medicine)
                .Where(i => i.PatientName == name)
                .ToList();

            return Ok(invoices);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public ActionResult<IEnumerable<Invoice>> GetMyInvoices()
        {
            var patientName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(patientName))
            {
                return Unauthorized();
            }

            var invoices = _context.Invoices
                .Include(i => i.Items)
                .ThenInclude(item => item.Medicine)
                .Where(i => i.PatientName == patientName)
                .ToList();

            return Ok(invoices);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Nurse,Doctor,Patient")]
        public ActionResult<Invoice> Get(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Items)
                .ThenInclude(item => item.Medicine)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Patient") && invoice.PatientName != User.Identity?.Name)
            {
                return Forbid();
            }

            return Ok(invoice);
        }

        [HttpPost]
        [Authorize(Roles = "Nurse")]
        public IActionResult Create([FromBody] Invoice invoice)
        {
            if (invoice.Items == null || !invoice.Items.Any())
            {
                return BadRequest("Invoice must contain at least one item.");
            }

            if (invoice.ConsultationFee < 0)
            {
                return BadRequest("Consultation fee must be non-negative.");
            }

            foreach (var item in invoice.Items)
            {
                var medicine = _context.Medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                if (medicine == null)
                {
                    return BadRequest($"Medicine with ID {item.MedicineId} does not exist.");
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest("Item quantity must be greater than zero.");
                }

                if (medicine.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Not enough stock for medicine '{medicine.Name}'. Available: {medicine.StockQuantity}.");
                }

                item.UnitPrice = medicine.Price;
                item.Medicine = null; // avoid EF duplicate tracking
            }

            invoice.MedicineFee = invoice.Items.Sum(item => item.Quantity * item.UnitPrice);
            invoice.TotalAmount = invoice.ConsultationFee + invoice.MedicineFee;
            invoice.CreatedAt = DateTime.UtcNow;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                _context.Invoices.Add(invoice);
                _context.SaveChanges();

                foreach (var item in invoice.Items)
                {
                    var medicine = _context.Medicines.First(m => m.Id == item.MedicineId);
                    medicine.StockQuantity -= item.Quantity;
                    _context.Medicines.Update(medicine);
                }

                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                return StatusCode(500, "Could not create the invoice.");
            }

            return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
        }

        [HttpPost("consume-prescription")]
        [AllowAnonymous]
        public IActionResult ConsumePrescription([FromBody] PrescriptionRequest request)
        {
            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest("Prescription must contain at least one item.");
            }

            if (string.IsNullOrWhiteSpace(request.PatientName))
            {
                return BadRequest("Patient name is required.");
            }

            if (request.ConsultationFee < 0)
            {
                return BadRequest("Consultation fee must be non-negative.");
            }

            var invoice = new Invoice
            {
                PatientName = request.PatientName,
                ConsultationFee = request.ConsultationFee,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            };

            foreach (var item in request.Items)
            {
                var medicine = _context.Medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                if (medicine == null)
                {
                    return BadRequest($"Medicine with ID {item.MedicineId} does not exist.");
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest("Item quantity must be greater than zero.");
                }

                if (medicine.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Not enough stock for medicine '{medicine.Name}'. Available: {medicine.StockQuantity}.");
                }

                invoice.Items.Add(new InvoiceItem
                {
                    MedicineId = medicine.Id,
                    Quantity = item.Quantity,
                    UnitPrice = medicine.Price
                });
            }

            invoice.MedicineFee = invoice.Items.Sum(item => item.Quantity * item.UnitPrice);
            invoice.TotalAmount = invoice.ConsultationFee + invoice.MedicineFee;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                _context.Invoices.Add(invoice);
                _context.SaveChanges();

                foreach (var item in invoice.Items)
                {
                    var medicine = _context.Medicines.First(m => m.Id == item.MedicineId);
                    medicine.StockQuantity -= item.Quantity;
                    _context.Medicines.Update(medicine);
                }

                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                return StatusCode(500, "Could not consume prescription.");
            }

            return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Nurse")]
        public IActionResult UpdateStatus(int id, [FromBody] UpdateInvoiceStatusRequest request)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
            {
                return NotFound();
            }

            invoice.Status = request.Status;
            _context.SaveChanges();
            return NoContent();
        }
    }

    public class UpdateInvoiceStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class PrescriptionRequest
    {
        public string PatientName { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public List<PrescriptionItemRequest> Items { get; set; } = new();
    }

    public class PrescriptionItemRequest
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }
}
