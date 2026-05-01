using ECP.ProductService.Application.Commands;
using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ProductMutations
{
    [GraphQLDescription("Create a new product. Returns the created product with its assigned ID.")]
    public Task<ProductDto> CreateProduct(
        CreateProductInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new CreateProductCommand(
            input.Name, input.Description, input.Price, input.Currency,
            input.CategoryId, input.Brand, input.InitialStock,
            input.Tags,
            input.Images,
            input.Attributes?.ToDictionary(a => a.Key, a => a.Value)), ct);

    [GraphQLDescription("Update product name, description, brand, tags, images, and attributes.")]
    public Task<ProductDto> UpdateProduct(
        UpdateProductInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new UpdateProductCommand(
            input.Id, input.Name, input.Description, input.Brand,
            input.Tags,
            input.Images,
            input.Attributes?.ToDictionary(a => a.Key, a => a.Value)), ct);

    [GraphQLDescription("Update product pricing. Optionally set a sale price below the regular price.")]
    public Task<ProductDto> UpdatePrice(
        UpdatePriceInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new UpdatePriceCommand(
            input.Id, input.Price, input.Currency, input.SalePrice), ct);

    [GraphQLDescription("Adjust stock by a signed delta (positive = restock, negative = consume).")]
    public Task<ProductDto> AdjustStock(
        AdjustStockInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new AdjustStockCommand(input.Id, input.Delta, input.Reason), ct);

    [GraphQLDescription("Reserve stock for a pending order.")]
    public Task<ProductDto> ReserveStock(
        ReserveStockInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ReserveStockCommand(input.Id, input.Quantity), ct);

    [GraphQLDescription("Release previously reserved stock (order cancelled or expired).")]
    public Task<ProductDto> ReleaseStock(
        ReleaseStockInput input,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ReleaseStockCommand(input.Id, input.Quantity), ct);

    [GraphQLDescription("Publish a draft or inactive product to the live catalog.")]
    public Task<ProductDto> PublishProduct(
        Guid id,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new PublishProductCommand(id), ct);

    [GraphQLDescription("Deactivate a product (hidden from catalog but not deleted).")]
    public Task<ProductDto> DeactivateProduct(
        Guid id,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new DeactivateProductCommand(id), ct);

    [GraphQLDescription("Archive a product permanently.")]
    public Task<ProductDto> ArchiveProduct(
        Guid id,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new ArchiveProductCommand(id), ct);

    [GraphQLDescription("Permanently delete a product from the database.")]
    public Task<bool> DeleteProduct(
        Guid id,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new DeleteProductCommand(id), ct);
}