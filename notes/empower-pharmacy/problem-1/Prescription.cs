using System.ComponentModel.DataAnnotations;

namespace Problem1
{
    public class CreatePrescriptionDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicationName { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 5000)]
        public double Dosage { get; set; }

        public string? Notes { get; set; }
    }

    public class Prescription
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public double Dosage { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "pending";
    }
}
