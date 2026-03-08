using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.ValueObjects;

namespace ECP.ProductService.Infrastructure.Persistence;

/// <summary>
/// Reconstitutes a Product aggregate from raw persistence data without going
/// through the domain's Create() factory (which would apply business rules
/// inappropriate for already-persisted data).
/// Uses reflection to set private fields — isolated here so the rest of the
/// codebase never sees reflection.
/// </summary>
public static class ProductFactory
{
    public static Product Reconstitute(
        Guid     id,
        string   name,
        string   slug,
        string   description,
        Money    price,
        Money?   salePrice,
        CategoryId categoryId,
        string   brand,
        StockInfo stock,
        ProductStatus status,
        List<string> tags,
        List<string> images,
        Dictionary<string, string> attributes,
        DateTime createdAt,
        DateTime updatedAt,
        int      version)
    {
        var product = (Product)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(typeof(Product));

        Set(product, nameof(Product.Id),          ProductId.From(id));
        Set(product, nameof(Product.Name),         name);
        Set(product, nameof(Product.Slug),         slug);
        Set(product, nameof(Product.Description),  description);
        Set(product, nameof(Product.Price),        price);
        Set(product, nameof(Product.SalePrice),    salePrice);
        Set(product, nameof(Product.CategoryId),   categoryId);
        Set(product, nameof(Product.Brand),        brand);
        Set(product, nameof(Product.Stock),        stock);
        Set(product, nameof(Product.Status),       status);
        Set(product, nameof(Product.Tags),         tags.AsReadOnly());
        Set(product, nameof(Product.Images),       images.AsReadOnly());
        Set(product, nameof(Product.Attributes),   attributes);
        Set(product, nameof(Product.CreatedAt),    createdAt);
        Set(product, nameof(Product.UpdatedAt),    updatedAt);
        Set(product, nameof(Product.Version),      version);

        return product;
    }

    private static void Set(object obj, string propertyName, object? value)
    {
        var prop = typeof(Product).GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on Product.");

        // Set via backing field for init-only / private-set properties
        var backingField = typeof(Product).GetField(
            $"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (backingField is not null)
            backingField.SetValue(obj, value);
        else
            prop.SetValue(obj, value);
    }
}