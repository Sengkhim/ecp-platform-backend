namespace ECP.ProductService.Infrastructure.GraphQL.Queries;

/// <summary>
/// Root Query type — HC requires an explicit root type.
/// ProductQueries is registered as an extension that merges into this.
/// </summary>
public sealed class RootQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
    }
}