using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vic_rms_api.Context;
using vic_rms_api.Models;

namespace vic_rms_api.Services
{
    public class RatesService
    {
        private readonly vicweb_2022DbContext _context;

        public RatesService(vicweb_2022DbContext context)
        {
            _context = context;
        }

        public async Task<List<wp_rates>> GetRatesAsync()
        {
            // Sử dụng AsNoTracking() để cải thiện hiệu suất, đặc biệt là khi chỉ truy vấn dữ liệu
            // và chuyển đổi ToList() thành ToListAsync() để thực hiện truy vấn một cách bất đồng bộ
            return await _context.Wp_Rates.AsNoTracking().ToListAsync();
        }
        public async Task<List<wp_rates>> GetRatesAsync(int param_PropertyID)
        {
            // Sử dụng AsNoTracking() để cải thiện hiệu suất, đặc biệt là khi chỉ truy vấn dữ liệu
            // và chuyển đổi ToList() thành ToListAsync() để thực hiện truy vấn một cách bất đồng bộ
            return await _context.Wp_Rates.Where(x=>x.RMS_propertyID==param_PropertyID).AsNoTracking().ToListAsync();
        }
    }

    public class HotelsService
    {
        private readonly vicweb_2022DbContext _context;

        public HotelsService(vicweb_2022DbContext context)
        {
            _context = context;
        }

        public async Task<List<wp_hotels>> GetHotelsAsync()
        {
            // Sử dụng AsNoTracking() để cải thiện hiệu suất, đặc biệt là khi chỉ truy vấn dữ liệu
            // và chuyển đổi ToList() thành ToListAsync() để thực hiện truy vấn một cách bất đồng bộ
            return await _context.wp_hotels.AsNoTracking().ToListAsync();
        }
        
    }

}
