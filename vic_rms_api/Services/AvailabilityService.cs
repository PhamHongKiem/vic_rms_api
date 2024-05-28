using Microsoft.EntityFrameworkCore;
using vic_rms_api.Context;
using vic_rms_api.Models;

namespace vic_rms_api.Services
{
    public interface IAvailabilityService
    {
        void CheckAvailability();
    }

    public class AvailabilityService : IAvailabilityService
    {
        private readonly ITokenService _tokenService;
        private readonly vicweb_2022DbContext _context;

        public AvailabilityService(ITokenService tokenService, vicweb_2022DbContext context)
        {
            _tokenService = tokenService;
            _context = context;
        }

        public void CheckAvailability()
        {
            var rates = GetRates(_context);
            Console.WriteLine("Checking Availability.....");
        }

        private async Task<List<wp_rates>> GetRates(vicweb_2022DbContext context)
        {
            return await context.Wp_Rates.ToListAsync();
        }
    }
}
