﻿using Microsoft.EntityFrameworkCore;
using vic_rms_api.Context;
using vic_rms_api.Logs;
using vic_rms_api.Models;

namespace vic_rms_api.Services
{
    public interface IRates_GridService
    {
        Task UpdateRates_Grid_SpecialRateAsync(List<wp_rates_grid> wp_rates_grid, AvailabilityResponse response, int propertyId);
        Task UpdateRates_Grid_RegularRateAsync(List<wp_rates_grid> wp_rates_grid, AvailabilityResponse response, int propertyId);
        Task<List<wp_rates_grid>> GetRates_GridAsync();
        Task<List<wp_rates_grid>> GetRates_GridAsync(int param_PropertyID);
    }

    public class Rates_GridService : IRates_GridService
    {
        private readonly vicweb_2022DbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly object _lock = new object();

        public Rates_GridService(IServiceScopeFactory scopeFactory, vicweb_2022DbContext context)
        {
            _scopeFactory = scopeFactory;
            _context = context;
        }
        public async Task<List<wp_rates_grid>> GetRates_GridAsync()
        {
            // Sử dụng AsNoTracking() để cải thiện hiệu suất, đặc biệt là khi chỉ truy vấn dữ liệu
            // và chuyển đổi ToList() thành ToListAsync() để thực hiện truy vấn một cách bất đồng bộ
            // Ngày hiện tại, lấy chỉ ngày mà không có thời gian
            var today = DateTime.Today;
            return await _context.wp_rates_grid.Where(g => g.Datesell >= today).AsNoTracking().ToListAsync();
        }
        public async Task<List<wp_rates_grid>> GetRates_GridAsync(int param_PropertyID)
        {
            // Sử dụng AsNoTracking() để cải thiện hiệu suất, đặc biệt là khi chỉ truy vấn dữ liệu
            // và chuyển đổi ToList() thành ToListAsync() để thực hiện truy vấn một cách bất đồng bộ
            // Ngày hiện tại, lấy chỉ ngày mà không có thời gian
            var today = DateTime.Today;
            return await _context.wp_rates_grid.Where(g => g.RMS_propertyID == param_PropertyID && g.Datesell >= today).AsNoTracking().ToListAsync();
        }

        public async Task UpdateRates_Grid_SpecialRateAsync(List<wp_rates_grid> wp_rates_grid, AvailabilityResponse response, int propertyId)
        {
            try
            {
                // Tạo dictionary để kiểm tra và cập nhật nhanh hơn
                var keyMap = response.Categories
                    .SelectMany(cat => cat.Rates
                        .Where(rate => rate.RateId == 17) // Filter for rateId = 17
                        .SelectMany(rate => rate.DayBreakdown.Select(day => new
                        {
                            Key = new RateKey
                            {
                                RMS_propertyID = propertyId,
                                RMS_roomtypeID = cat.CategoryId,
                                Datesell = day.TheDate
                            },
                            Value = new { DailyRate = day.DailyRate.HasValue ? day.DailyRate.Value : -1, AvailableRooms = day.AvailableAreas }
                        })))
                    .GroupBy(x => x.Key)
                    .ToDictionary(g => g.Key, g => g.First().Value);  // Assume the first value is sufficient if duplicates exist

                var newRates = new List<wp_rates_grid>();
                var updatedRates = new List<wp_rates_grid>();
                int count = 0;  // Count the number of records processed

                // Iterate through each record in wp_rates_grid
                foreach (var rate in wp_rates_grid)
                {
                    if (keyMap.TryGetValue(new RateKey { RMS_propertyID = rate.RMS_propertyID, RMS_roomtypeID = rate.RMS_roomtypeID, Datesell = rate.Datesell }, out var value))
                    {
                        // Update data from response if corresponding key is found
                        rate.Baserate = (int)value.DailyRate;
                        rate.RoomAvailable = value.AvailableRooms;
                        rate.Updated_Date = DateTime.Now;
                        updatedRates.Add(rate);  // Add to updatedRates list
                        count++;  // Increment the counter after each update
                    }

                    // Check after every 100 records processed
                    if (count > 0 && count % 300 == 0)
                    {
                        await SaveChangesAsync(newRates, updatedRates);
                        newRates.Clear();
                        updatedRates.Clear();
                    }
                }

                // Ensure to save any remaining changes after the final iteration
                if (count % 300 != 0 || updatedRates.Any())
                {
                    await SaveChangesAsync(newRates, updatedRates);
                }
            }
            catch (Exception ex)
            {
                // Log and handle the exception
                Logger.Log($"UpdateRates_Grid_SpecialRateAsync(): An error occurred: {ex.Message}");
            }
        }

        public async Task UpdateRates_Grid_RegularRateAsync(List<wp_rates_grid> wp_rates_grid, AvailabilityResponse response, int propertyId)
        {
            try
            {
                var keys = response.Categories
                    .SelectMany(cat => cat.Rates
                        .Where(rate => rate.RateId != 17)
                        .SelectMany(rate => rate.DayBreakdown
                            .Select(day => new
                            {
                                RMS_propertyID = propertyId,
                                RMS_rateID = rate.RateId,
                                RMS_roomtypeID = cat.CategoryId,
                                Datesell = day.TheDate,
                                DailyRate = day.DailyRate.HasValue ? day.DailyRate.Value : -1, // Gán giá trị mặc định là -1 nếu không có DailyRate
                                RoomAvailable = day.AvailableAreas
                            })))
                    .Distinct()
                    .ToList();

                var relevantRates = wp_rates_grid
                    .Where(r => r.RMS_propertyID == propertyId && keys.Select(k => k.Datesell).Contains(r.Datesell))
                    .ToList();

                var existingRates = new Dictionary<RateKey, wp_rates_grid>();

                foreach (var rate in relevantRates)
                {
                    var key = new RateKey
                    {
                        RMS_propertyID = rate.RMS_propertyID,
                        RMS_rateID = rate.RMS_rateID,
                        RMS_roomtypeID = rate.RMS_roomtypeID,
                        Datesell = rate.Datesell
                    };

                    if (!existingRates.ContainsKey(key))
                    {
                        existingRates.Add(key, rate);
                    }
                }

                var newRates = new List<wp_rates_grid>();
                var updatedRates = new List<wp_rates_grid>();
                int counter = 0;

                foreach (var category in response.Categories)
                {
                    foreach (var rate in category.Rates)
                    {
                        foreach (var day in rate.DayBreakdown)
                        {
                            var dailyRate = day.DailyRate.HasValue ? day.DailyRate.Value : -1;
                            var key = new RateKey
                            {
                                RMS_propertyID = propertyId,
                                RMS_rateID = rate.RateId,
                                RMS_roomtypeID = category.CategoryId,
                                Datesell = day.TheDate
                            };

                            if (existingRates.TryGetValue(key, out var existingRate))
                            {
                                existingRate.DailyRate = (int)dailyRate;
                                existingRate.RoomAvailable = day.AvailableAreas;
                                existingRate.Updated_Date = DateTime.Now;
                                updatedRates.Add(existingRate);  // Add to updatedRates list
                            }
                            else
                            {
                                var newRateGrid = new wp_rates_grid
                                {
                                    RMS_rateID = rate.RateId,
                                    RMS_roomtypeID = category.CategoryId,
                                    RMS_propertyID = propertyId,
                                    Datesell = day.TheDate,
                                    DailyRate = (int)dailyRate,
                                    RoomAvailable = day.AvailableAreas,
                                    Created_Date = DateTime.Now,
                                    Updated_Date = DateTime.Now
                                };
                                newRates.Add(newRateGrid);
                            }

                            counter++;
                            if (counter % 300 == 0)
                            {
                                await SaveChangesAsync(newRates, updatedRates);
                                newRates.Clear();
                                updatedRates.Clear();
                            }
                        }
                    }
                }

                if (counter % 300 != 0 || newRates.Any() || updatedRates.Any())
                {
                    await SaveChangesAsync(newRates, updatedRates);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateRates_Grid_RegularRateAsync(): An error occurred: {ex.Message}");
            }
        }

        private async Task SaveChangesAsync(List<wp_rates_grid> newRates, List<wp_rates_grid> updatedRates)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
                var executionStrategy = context.Database.CreateExecutionStrategy();

                await executionStrategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            if (newRates.Any())
                            {
                                context.wp_rates_grid.AddRange(newRates);
                            }

                            if (updatedRates.Any())
                            {
                                foreach (var rate in updatedRates)
                                {
                                    context.Entry(rate).State = EntityState.Modified;
                                }
                            }

                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"SaveChangesAsync(): An error occurred: {ex.Message}");
                            throw; // Rethrow the exception to be caught by the outer execution strategy
                        }
                        finally
                        {
                            context.ChangeTracker.Clear();  // Clear change tracker to free up memory
                        }
                    }
                });
            }
        }





    }

    public class RateKey
    {
        public int? RMS_propertyID { get; set; }
        public int? RMS_rateID { get; set; }
        public int? RMS_roomtypeID { get; set; }
        public DateTime? Datesell { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is RateKey key)
            {
                return RMS_propertyID == key.RMS_propertyID &&
                       RMS_rateID == key.RMS_rateID &&
                       RMS_roomtypeID == key.RMS_roomtypeID &&
                       Datesell == key.Datesell;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RMS_propertyID, RMS_rateID, RMS_roomtypeID, Datesell);
        }
    }
}
