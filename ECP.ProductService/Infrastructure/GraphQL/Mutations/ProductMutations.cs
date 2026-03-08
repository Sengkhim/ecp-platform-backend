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
    [GraphQLDescription("Create a new product in the catalog.")]
    public async Task<ProductDto> CreateProduct(
        CreateProductInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new CreateProductCommand(
            Name:         input.Name,
            Description:  input.Description,
            Price:        input.Price,
            Currency:     input.Currency,
            CategoryId:   input.CategoryId,
            Brand:        input.Brand,
            InitialStock: input.InitialStock,
            Tags:         input.Tags,
            Images:       input.Images,
            Attributes:   input.Attributes), ct);

    [GraphQLDescription("Update product details (name, description, brand, tags, images, attributes).")]
    public async Task<ProductDto> UpdateProduct(
        UpdateProductInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new UpdateProductCommand(
            Id:          input.Id,
            Name:        input.Name,
            Description: input.Description,
            Brand:       input.Brand,
            Tags:        input.Tags,
            Images:      input.Images,
            Attributes:  input.Attributes), ct);

    [GraphQLDescription("Update product price and optional sale price.")]
    public async Task<ProductDto> UpdateProductPrice(
        UpdatePriceInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new UpdateProductPriceCommand(
            Id:        input.Id,
            Price:     input.Price,
            Currency:  input.Currency,
            SalePrice: input.SalePrice), ct);

    [GraphQLDescription("Adjust stock by a delta (positive = restock, negative = consume).")]
    public async Task<ProductDto> AdjustStock(
        AdjustStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new AdjustStockCommand(input.Id, input.Delta, input.Reason), ct);

    [GraphQLDescription("Reserve stock for a pending order.")]
    public async Task<ProductDto> ReserveStock(
        ReserveStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new ReserveStockCommand(input.Id, input.Quantity), ct);

    [GraphQLDescription("Release previously reserved stock (order cancelled).")]
    public async Task<ProductDto> ReleaseStock(
        ReleaseStockInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new ReleaseStockCommand(input.Id, input.Quantity), ct);

    [GraphQLDescription("Activate an inactive or out-of-stock product.")]
    public async Task<ProductDto> ActivateProduct(Guid id, [Service] ISender mediator, CancellationToken ct)
        => await mediator.Send(new ActivateProductCommand(id), ct);

    [GraphQLDescription("Deactivate a product (hidden from catalog but not deleted).")]
    public async Task<ProductDto> DeactivateProduct(Guid id, [Service] ISender mediator, CancellationToken ct)
        => await mediator.Send(new DeactivateProductCommand(id), ct);

    [GraphQLDescription("Archive a product permanently.")]
    public async Task<ProductDto> ArchiveProduct(Guid id, [Service] ISender mediator, CancellationToken ct)
        => await mediator.Send(new ArchiveProductCommand(id), ct);

    [GraphQLDescription("Delete a product permanently.")]
    public async Task<bool> DeleteProduct(Guid id, [Service] ISender mediator, CancellationToken ct)
        => await mediator.Send(new DeleteProductCommand(id), ct);
}