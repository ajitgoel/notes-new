using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Problem1
{
    public class PatientService
    {
        private readonly IPatientRepository _repository;

        public PatientService(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Patient>> GetActivePatientsByStateAsync(string state)
        {
            var patients = await _repository.GetAllAsync();
            return patients.Where(p => p.IsActive && p.State == state);
        }

        public async Task DeactivatePatientAsync(int id)
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient != null)
            {
                patient.IsActive = false;
                await _repository.UpdateAsync(patient);
            }
        }
    }
}
