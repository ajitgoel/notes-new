using System.Collections.Generic;
using System.Threading.Tasks;

namespace Problem1
{
    public interface IPatientRepository
    {
        Task<IEnumerable<Patient>> GetAllAsync();
        Task<Patient?> GetByIdAsync(int id);
        Task UpdateAsync(Patient patient);
    }
}
