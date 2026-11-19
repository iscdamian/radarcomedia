using System;

namespace RadarComedy.Domain;

public static class ListingPolicy
{
    public const int SinglePriceCents = 200;
    public const int RecurringPriceCents = 500;
    public const string Currency = "USD";

    public static int PriceFor(ListingPlan plan) => plan switch
    {
        ListingPlan.Single => SinglePriceCents,
        ListingPlan.RecurringThreeMonths => RecurringPriceCents,
        _ => throw new ArgumentOutOfRangeException(nameof(plan))
    };

    // Inclusive tax: Stripe Checkout's actual total must be this number of cents.
    // Do NOT use automatic USD tax_behavior=automatic (USD defaults to exclusive).
    public static void ValidateGrossCharge(ListingPlan plan, int checkoutTotalCents)
    {
        if (checkoutTotalCents != PriceFor(plan))
            throw new InvalidOperationException("Checkout total must be exactly the advertised all-in USD price.");
    }

    public static (DateOnly Start, DateOnly EndExclusive) RecurringWindow(DateOnly firstApprovedLocalPerformance)
        => (firstApprovedLocalPerformance, firstApprovedLocalPerformance.AddMonths(3));

    public static bool CoveredByRecurringTerm(DateOnly localDate, DateOnly start)
        => localDate >= start && localDate < start.AddMonths(3);

    // No arbitrary transfer count limit. A transfer is not a second listing:
    // same underlying production, admin-approved, and original show not yet completed.
    public static bool CanTransferSingle(
        bool isSameProduction, bool adminApproved, bool originalAlreadyCompleted, bool promoterApproved)
        => isSameProduction && adminApproved && !originalAlreadyCompleted && promoterApproved;
}
