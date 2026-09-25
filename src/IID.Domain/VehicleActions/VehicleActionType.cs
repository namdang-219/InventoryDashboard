namespace IID.Domain.VehicleActions;

public enum VehicleActionType
{
    PriceReductionPlanned = 0,
    PriceReductionExecuted = 1,    // NEW: Actual price cut applied
    TransferToWholesale = 2,        // NEW: Sent to wholesale channel
    TradeInCustomer = 3,            // NEW: Acquired from customer trade-in
    MarketingCampaign = 4,          // NEW: Marketing push started
    DealerAuction = 5,              // NEW: Listed on dealer auction
    ManagerReview = 6,
    Relist = 7,
    Other = 8,
    TransferDealership = 9
}
