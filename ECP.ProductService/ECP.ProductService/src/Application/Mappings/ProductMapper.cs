using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Core.Domain.Entities;

namespace ECP.ProductService.Application.Mappings;

public static class ProductMapper
{
    public static ProductDto ToDto(this Product p) => new(
        Id:             p.Id.Value,
        Name:           p.Name,
        Slug:           p.Slug.Value,
        Description:    p.Description,
        Brand:          p.Brand,
        CategoryId:     p.CategoryId.Value,
        Price:          p.Price.Amount,
        Currency:       p.Price.Currency,
        SalePrice:      p.SalePrice?.Amount,
        StockQuantity:  p.Stock.Quantity,
        StockReserved:  p.Stock.Reserved,
        StockAvailable: p.Stock.Available,
        IsLowStock:     p.Stock.IsLowStock,
        Status:         p.Status.ToString(),
        Tags:           p.Tags,
        Images:         p.Images,
        Attributes:     p.Attributes,
        CreatedAt:      p.CreatedAt,
        UpdatedAt:      p.UpdatedAt,
        Version:        p.Version);

    public static ProductSummaryDto ToSummaryDto(this Product p) => new(
        Id:             p.Id.Value,
        Name:           p.Name,
        Slug:           p.Slug.Value,
        Brand:          p.Brand,
        Price:          p.Price.Amount,
        Currency:       p.Price.Currency,
        SalePrice:      p.SalePrice?.Amount,
        Status:         p.Status.ToString(),
        StockAvailable: p.Stock.Available,
        PrimaryImage:   p.Images.FirstOrDefault());
}
