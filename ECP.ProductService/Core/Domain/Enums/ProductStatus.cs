namespace ECP.ProductService.Core.Domain.Enums;

public enum ProductStatus
{
    Draft,       // Created, not published
    Active,      // Live in catalog
    Inactive,    // Hidden from catalog, still exists
    OutOfStock,  // Visible but cannot be purchased
    Archived     // Soft-deleted, never returns to active
}

public enum StockAdjustmentReason
{
    Purchase,
    Return,
    ManualAdjustment,
    DamagedGoods,
    InventoryCount,
    Promotion
}
