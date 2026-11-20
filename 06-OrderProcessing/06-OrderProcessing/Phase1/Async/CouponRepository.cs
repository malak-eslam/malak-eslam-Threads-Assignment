using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1.Async;

public class CouponRepository
{
    private readonly JsonFileStore _store;

    public CouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
    {
        Log.Info("CouponRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("CouponRepository", $"Executing query for Coupon '{code}'...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Coupon> coupons = await _store.ReadAsync<Coupon>(DataFiles.Coupons, ct);

        foreach (Coupon coupon in coupons)
        {
            if (string.Equals(coupon.Code,code,StringComparison.OrdinalIgnoreCase))
            {
                Log.Success("CouponRepository", $"Coupon '{code}' found.");
                return coupon;
            }
        }

        Log.Error("CouponRepository", $"Coupon '{code}' not found.");
        return null;
    }
}

