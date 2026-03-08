namespace ECP.ProductService.Infrastructure.GraphQL.Mutations;

/// <summary>
/// Root Mutation type — HC requires an explicit root type.
/// ProductMutations is registered as an extension that merges into this.
/// </summary>
public sealed class RootMutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);
    }
}