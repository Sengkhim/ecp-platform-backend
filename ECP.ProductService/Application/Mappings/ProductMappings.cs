using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Core.Domain.Entities;

namespace ECP.ProductService.Application.Mappings;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product p) => new(
        Id:             p.Id.Value,
        Name:           p.Name,
        Slug:           p.Slug,
        Description:    p.Description,
        Price:          p.Price.Amount,
        Currency:       p.Price.Currency,
        SalePrice:      p.SalePrice?.Amount,
        CategoryId:     p.CategoryId.Value,
        Brand:          p.Brand,
        StockQuantity:  p.Stock.Quantity,
        StockReserved:  p.Stock.Reserved,
        StockAvailable: p.Stock.Available,
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
        Slug:           p.Slug,
        Price:          p.Price.Amount,
        Currency:       p.Price.Currency,
        SalePrice:      p.SalePrice?.Amount,
        Brand:          p.Brand,
        Status:         p.Status.ToString(),
        StockAvailable: p.Stock.Available,
        PrimaryImage:   p.Images.FirstOrDefault());
}