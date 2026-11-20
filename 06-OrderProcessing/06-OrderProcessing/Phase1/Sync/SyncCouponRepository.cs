using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1;

public class SyncCouponRepository
{
    private readonly JsonFileStore _store;

    public SyncCouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Coupon? GetByCode(string code)
    {
        Log.Info("SyncCouponRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCouponRepository",
            $"Executing query for Coupon '{code}'...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        List<Coupon> coupons =
            _store.Read<Coupon>(DataFiles.Coupons);

        foreach (Coupon coupon in coupons)
        {
            if (string.Equals(
                coupon.Code,
                code,
                StringComparison.OrdinalIgnoreCase))
            {
                Log.Success("SyncCouponRepository",
                    $"Coupon '{code}' found.");

                return coupon;
            }
        }

        Log.Error("SyncCouponRepository",
            $"Coupon '{code}' not found.");

        return null;
    }
}