using Microsoft.EntityFrameworkCore;

namespace ProductSearch;

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    //Assumptions:
    //IDs not included in tests - adding IDs to DB seed
    //No need to throw error on exceeded page bounds as query would just return no results
    //Used default in SortBy to prevent errors - created is the default as per the model. Changing the type to enum could help prevent mistakes

    //Future Improvements:
    //Change SortBy type to enum
    //More checks for invalid inputs
    //More edge case tests

    public Task<Paged<ProductDto>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {

        //more graceful than throwing an error
        if(query.PageSize > 100)
        {
            query.PageSize = 100;
        }

        //Filter by category first, if provided, to reduce the number of results

        var products = _db.Products.Where(x => (query.Category == null || x.Category.ToLower() == query.Category.ToLower())
        && (query.Search == null || x.Name.ToLower().Contains(query.Search.ToLower()) || x.Sku.ToLower().Contains(query.Search.ToLower())));

        switch (query.SortBy.ToLower())
        {
            case "price":
                products = query.Desc ? products.OrderByDescending(x => x.Price) : products.OrderBy(x => x.Price);
                break;
            case "name":
                products = query.Desc ? products.OrderByDescending(x => x.Name) : products.OrderBy(x => x.Name);
                break;
            default:
                products = query.Desc ? products.OrderByDescending(x => x.CreatedUtc) : products.OrderBy(x => x.CreatedUtc);
                break;
        }

        if(query.Page == 1)
        {
            var productDto = products.Take(query.PageSize).Select(x => new ProductDto(x.Id, x.Sku, x.Name, x.Price));
            return Task.FromResult(new Paged<ProductDto>(productDto.ToList(), productDto.Count(), query.Page, query.PageSize));
        }

        var productDto2 = products.Skip((query.Page-1) * query.PageSize).Take(query.PageSize).Select(x => new ProductDto(x.Id, x.Sku, x.Name, x.Price));
        return Task.FromResult(new Paged<ProductDto>(productDto2.ToList(), productDto2.Count(), query.Page, query.PageSize));
    }

}