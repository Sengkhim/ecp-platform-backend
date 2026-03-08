using ECP.ProductService.Application.Commands;
using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Mutations;

[MutationType]
public sealed class ProductMutations
{
    // ── Catalog ───────────────────────────────────────────────────────────────

    [GraphQLDescription("Create a new product. Returns the created product with its assigned ID.")]
    public Task<ProductDto> CreateProduct(
        CreateProductInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new CreateProductCommand(
            input.Name, input.Description, input.Price, input.Currency,
            input.CategoryId, input.Brand, input.InitialStock,
            input.Tags, input.Images, input.Attributes), ct);

    [GraphQLDescription("Update product name, description, brand, tags, images, and attributes.")]
    public Task<ProductDto> UpdateProduct(
        UpdateProductInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new UpdateProductCommand(
            input.Id, input.Name, input.Description, input.Brand,
            input.Tags, input.Images, input.Attributes), ct);

    [GraphQLDescription("Update product pricing. Optionally set a sale price below the regular price.")]
    public Task<ProductDto> UpdatePrice(
        UpdatePriceInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new UpdatePriceCommand(
            input.Id, input.Price, input.Currency, input.SalePrice), ct);

    // ── Stock ─────────────────────────────────────────────────────────────────

    [GraphQLDescription("Adjust stock by a signed delta (positive = restock, negative = consume). Reason is required for audit trail.")]
    public Task<ProductDto> AdjustStock(
        AdjustStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new AdjustStockCommand(input.Id, input.Delta, input.Reason), ct);

    [GraphQLDescription("Reserve stock for a pending order. Reduces available count without reducing total.")]
    public Task<ProductDto> ReserveStock(
        ReserveStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ReserveStockCommand(input.Id, input.Quantity), ct);

    [GraphQLDescription("Release previously reserved stock (order cancelled or expired).")]
    public Task<ProductDto> ReleaseStock(
        ReleaseStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ReleaseStockCommand(input.Id, input.Quantity), ct);

    // ── Status ────────────────────────────────────────────────────────────────

    [GraphQLDescription("Publish a draft or inactive product to the live catalog.")]
    public Task<ProductDto> PublishProduct(
        Guid id,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new PublishProductCommand(id), ct);

    [GraphQLDescription("Deactivate a product (hidden from catalog but not deleted, can be reactivated).")]
    public Task<ProductDto> DeactivateProduct(
        Guid id,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new DeactivateProductCommand(id), ct);

    [GraphQLDescription("Archive a product permanently. Archived products cannot be modified or reactivated.")]
    public Task<ProductDto> ArchiveProduct(
        Guid id,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ArchiveProductCommand(id), ct);

    // ── Delete ────────────────────────────────────────────────────────────────

    [GraphQLDescription("Permanently delete a product from the database.")]
    public Task<bool> DeleteProduct(
        Guid id,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new DeleteProductCommand(id), ct);
}
