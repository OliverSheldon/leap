using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductSearch;
using Xunit;

public class SearchSkeletonTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(opts);

        //Changed dates around to test SortBy created (sort direction is only asc)
        db.Products.AddRange(
            new Product { Id = 1, Sku = "A1", Name = "Apple", Category = "Fruit", Price = 1.0m, CreatedUtc = DateTime.UtcNow.AddDays(-1) },
            new Product { Id = 2, Sku = "B2", Name = "Banana", Category = "Fruit", Price = 0.5m, CreatedUtc = DateTime.UtcNow.AddDays(-2) },
            new Product { Id = 3, Sku = "C3", Name = "Carrot", Category = "Veg", Price = 0.8m, CreatedUtc = DateTime.UtcNow.AddDays(-3) }
        );
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Search_By_Name_Returns_1_Result()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Search = "Apple"
        });

        Assert.True(result.Items[0].Name == "Apple");
        Assert.True(result.Total == 1);
    }

    [Fact]
    public async Task Search_By_Name_Lowercase_Returns_1_Result()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Search = "apple"
        });

        Assert.True(result.Items[0].Name == "Apple");
        Assert.True(result.Total == 1);
    }

    [Fact]
    public async Task Search_By_Sku_Returns_1_Result()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Search = "A1"
        });

        Assert.True(result.Items[0].Name == "Apple");
        Assert.True(result.Total == 1);
    }

    [Fact]
    public async Task Vague_Search_Returns_3_Results()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Search = "A"
        });

        Assert.True(result.Total == 3);
    }

    [Fact]
    public async Task Order_By_Price_Desc()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            SortBy = "price"
        });

        Assert.True(result.Total == 3);

        //Would be better to compare prices directly, in case more data is added to db
        Assert.True(result.Items[0].Name == "Apple");
        Assert.True(result.Items[1].Name == "Carrot");
    }

    [Fact]
    public async Task Order_By_Created_Asc()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            SortBy = "created",
            Desc = false
        });

        Assert.True(result.Total == 3);
        Assert.True(result.Items[0].Name == "Carrot");
        Assert.True(result.Items[1].Name == "Banana");
    }

    [Fact]
    public async Task Page_Not_Found()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Page = 2
        });

        Assert.True(result.Total == 0);
    }

    [Fact]
    public async Task Page_Found()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Page = 1
        });

        Assert.True(result.Total == 3);
    }

    [Fact]
    public async Task Page2_1_Result()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Page = 2,
            PageSize = 2,
            Desc = false
        });

        Assert.True(result.Total == 1);
        Assert.True(result.Items[0].Name == "Apple");
    }

    [Fact]
    public async Task Page1_2_Results()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Page = 1,
            PageSize = 2,
            Desc = false
        });

        Assert.True(result.Total == 2);
        Assert.True(result.Items[0].Name == "Carrot");
        Assert.True(result.Items[1].Name == "Banana");
    }

    [Fact]
    public async Task Search_Category_Fruit()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Category = "Fruit",
            Desc = false
        });

        Assert.True(result.Total == 2);
        Assert.True(result.Items[0].Name == "Banana");
        Assert.True(result.Items[1].Name == "Apple");        
    }

    [Fact]
    public async Task Search_Category_Fruit_Specific()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Category = "Fruit",
            Search = "Banana"
        });

        Assert.True(result.Total == 1);
        Assert.True(result.Items[0].Name == "Banana");
    }

    [Fact]
    public async Task Search_Category_Veg()
    {
        await using var db = CreateDb();
        var svc = new ProductService(db);
        var result = await svc.SearchAsync(new ProductQuery()
        {
            Category = "Veg",
            Desc = false
        });

        Assert.True(result.Total == 1);
        Assert.True(result.Items[0].Name == "Carrot");
    }
}