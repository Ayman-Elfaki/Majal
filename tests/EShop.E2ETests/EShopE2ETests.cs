using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EShop.Modules.Categories.Entities;
using EShop.Modules.Categories.ValueObjects;
using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.E2ETests;

public class EShopApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath;

    public EShopApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"EShop_Test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}

public class EShopE2ETests : IClassFixture<EShopApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EShopApplicationFactory _factory;

    public EShopE2ETests(EShopApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Categories_CreateAndList_Success()
    {
        var ct = TestContext.Current.CancellationToken;
        var categoryPayload = new
        {
            Name = "Books",
            Translations = new[]
            {
                new { Description = "Printed and digital books", Locale = "en-US" }
            }
        };

        var postResponse = await _client.PostAsJsonAsync("/categories", categoryPayload, ct);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await _client.GetAsync("/categories", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var content = await getResponse.Content.ReadAsStringAsync(ct);
        Assert.Contains("Books", content);
    }

    [Fact]
    public async Task Products_CreateAndGet_UsesFromPolymorphicMapping()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Create category
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EShopDbContext>();

        var category = Category.Create(
            CategoryName.Create("Gadgets"),
            [CategoryTranslation.Create("Modern gadgets", "en-US")]);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // 2. Create physical product
        var physicalPayload = new
        {
            CategoryId = category.Id,
            Sku = "GAD-001",
            PriceAmount = 99.99m,
            PriceCurrency = "USD",
            WeightKg = 1.5m,
            InitialStockQuantity = 10u,
            Tags = new[] { "tech", "gadget" },
            Translations = new[]
            {
                new { Name = "Smart Thermostat", Description = "Connected thermostat", Locale = "en-US" }
            }
        };

        var physicalResponse = await _client.PostAsJsonAsync("/products/physical", physicalPayload, ct);
        Assert.Equal(HttpStatusCode.OK, physicalResponse.StatusCode);

        // 3. Create digital product
        var digitalPayload = new
        {
            CategoryId = category.Id,
            Sku = "DIG-001",
            Price = new { Amount = 19.99m, Currency = "USD" },
            DownloadUrl = "https://downloads.example.com/gadget-manual.pdf",
            InitialStockQuantity = 100u,
            Tags = new[] { "digital", "manual" },
            Translations = new[]
            {
                new { Name = "Thermostat Manual", Description = "PDF User Guide", Locale = "en-US" }
            }
        };

        var digitalResponse = await _client.PostAsJsonAsync("/products/digital", digitalPayload, ct);
        Assert.Equal(HttpStatusCode.OK, digitalResponse.StatusCode);

        // 4. Query /products which uses ProductDto.FromEntity(p)
        var getProductsResponse = await _client.GetAsync("/products", ct);
        Assert.Equal(HttpStatusCode.OK, getProductsResponse.StatusCode);

        var json = await getProductsResponse.Content.ReadFromJsonAsync<JsonArray>(cancellationToken: ct);
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Check polymorphic properties are mapped via From
        var hasPhysical = json.Any(item =>
            item?["product"]?["weightKg"]?.GetValue<decimal>() == 1.5m &&
            item?["product"]?["sku"]?.GetValue<string>() == "GAD-001");
        Assert.True(hasPhysical);

        var hasDigital = json.Any(item =>
            item?["product"]?["downloadUrl"]?.GetValue<string>() == "https://downloads.example.com/gadget-manual.pdf" &&
            item?["product"]?["sku"]?.GetValue<string>() == "DIG-001");
        Assert.True(hasDigital);
    }

    [Fact]
    public async Task Products_ListArchived_UsesGeneratedProjections()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Create category and an archived product directly
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EShopDbContext>();

        var category = Category.Create(
            CategoryName.Create("Archived Category"),
            [CategoryTranslation.Create("Category for archived", "en-US")]);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        var physicalProduct = PhysicalProduct.Create(
            ProductSku.Create("ARCH-PHYS-01"),
            Money.Create(49.99m, "USD"),
            category,
            ProductTags.Create(["clearance"]),
            [ProductTranslation.Create("Old Item", "Discontinued physical item", "en-US")],
            2.0m,
            5u);
        physicalProduct.IsArchived = true;
        physicalProduct.ArchivedOn = DateTimeOffset.UtcNow;

        var digitalProduct = DigitalProduct.Create(
            ProductSku.Create("ARCH-DIG-01"),
            Money.Create(9.99m, "USD"),
            category,
            ProductTags.Create(["clearance", "v1"]),
            [ProductTranslation.Create("Old Ebook", "Discontinued ebook", "en-US")],
            "https://downloads.example.com/old.pdf",
            0u);
        digitalProduct.IsArchived = true;
        digitalProduct.ArchivedOn = DateTimeOffset.UtcNow;

        db.Products.AddRange(physicalProduct, digitalProduct);
        await db.SaveChangesAsync(ct);

        // 2. Query /admin/products/archived which uses ProductDto.ArchivedPhysicalProductDto.Projection
        // and ProductDto.ArchivedDigitalProductDto.Projection
        var response = await _client.GetAsync("/admin/products/archived", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonArray>(cancellationToken: ct);
        Assert.NotNull(json);

        var archivedPhys = json.FirstOrDefault(item => item?["sku"]?.GetValue<string>() == "ARCH-PHYS-01");
        Assert.NotNull(archivedPhys);
        Assert.True(archivedPhys["id"]?.GetValue<int>() > 0);
        Assert.Equal(49.99m, archivedPhys["priceAmount"]?.GetValue<decimal>());
        Assert.Equal("USD", archivedPhys["priceCurrency"]?.GetValue<string>());
        Assert.Equal(2.0m, archivedPhys["weightKg"]?.GetValue<decimal>());
        Assert.Equal(5u, archivedPhys["initialStockQuantity"]?.GetValue<uint>());

        var archivedDig = json.FirstOrDefault(item => item?["sku"]?.GetValue<string>() == "ARCH-DIG-01");
        Assert.NotNull(archivedDig);
        Assert.True(archivedDig["id"]?.GetValue<int>() > 0);
        Assert.Equal(9.99m, archivedDig["priceAmount"]?.GetValue<decimal>());
        Assert.Equal("https://downloads.example.com/old.pdf", archivedDig["downloadUrl"]?.GetValue<string>());
    }

    [Fact]
    public async Task Customers_RegisterAndList_UsesFrom()
    {
        var ct = TestContext.Current.CancellationToken;
        var registerPayload = new
        {
            Name = "Grace Hopper",
            Email = "grace.hopper@example.com"
        };

        var postResponse = await _client.PostAsJsonAsync("/customers", registerPayload, ct);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getResponse = await _client.GetAsync("/customers", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var json = await getResponse.Content.ReadFromJsonAsync<JsonArray>(cancellationToken: ct);
        Assert.NotNull(json);

        var customer = json.FirstOrDefault(c =>
            c?["customer"]?["name"]?.GetValue<string>() == "Grace Hopper" &&
            c?["customer"]?["email"]?.GetValue<string>() == "grace.hopper@example.com");

        Assert.NotNull(customer);
    }

    [Fact]
    public async Task Orders_PlaceAndList_UsesFromWithPolymorphicPayment()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EShopDbContext>();

        // Seed Category, Product, Customer
        var category = Category.Create(
            CategoryName.Create("Order Category"),
            [CategoryTranslation.Create("For ordering", "en-US")]);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        var product = PhysicalProduct.Create(
            ProductSku.Create("ORD-PROD-01"),
            Money.Create(25.00m, "USD"),
            category,
            ProductTags.Create(["shop"]),
            [ProductTranslation.Create("Orderable Item", "Great item", "en-US")],
            1.0m,
            50u);
        db.Products.Add(product);

        var customer = EShop.Modules.Customers.Entities.Customer.Create(
            EShop.Modules.Customers.ValueObjects.CustomerName.Create("Alice Smith"),
            EShop.Modules.Customers.ValueObjects.CustomerEmail.Create("alice@example.com"));
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        // Place order with CreditCardPayment
        var orderPayload = new JsonObject
        {
            ["customerId"] = customer.Id,
            ["lines"] = new JsonArray
            {
                new JsonObject
                {
                    ["productId"] = product.Id,
                    ["quantity"] = 2,
                    ["amountUnitPrice"] = 25.00m,
                    ["currencyUnitPrice"] = "USD"
                }
            },
            ["paymentMethod"] = new JsonObject
            {
                ["$type"] = "creditCardPayment",
                ["cardholderName"] = "Alice Smith",
                ["last4Digits"] = "1234"
            }
        };

        var postResponse = await _client.PostAsJsonAsync("/orders", orderPayload, ct);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var postResult = await postResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
        Assert.NotNull(postResult);
        var orderId = postResult["id"]?.GetValue<Guid>();
        Assert.NotNull(orderId);
        Assert.NotEqual(Guid.Empty, orderId.Value);

        // List orders: uses OrderDto.FromEntity(o)
        var listResponse = await _client.GetAsync("/orders", ct);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonArray>(cancellationToken: ct);
        Assert.NotNull(listJson);

        var orderItem = listJson.FirstOrDefault(o => o?["id"]?.GetValue<Guid>() == orderId.Value);
        Assert.NotNull(orderItem);
        Assert.NotNull(orderItem["order"]);

        // Verify nested CreditCardPayment was mapped via From
        var payment = orderItem["order"]?["paymentMethod"];
        Assert.NotNull(payment);
        Assert.Equal("Alice Smith", payment["cardholderName"]?.GetValue<string>());
        Assert.Equal(orderId.Value, orderItem["order"]?["id"]?.GetValue<Guid>());

        // Get single order: uses OrderDto.FromEntity(order)
        var getSingleResponse = await _client.GetAsync($"/orders/{orderId.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, getSingleResponse.StatusCode);

        var singleJson = await getSingleResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
        Assert.NotNull(singleJson);
        var returnedId = singleJson["id"]?.GetValue<Guid>();
        Assert.Equal(orderId.Value, returnedId);
        Assert.Equal(orderId.Value, singleJson["order"]?["id"]?.GetValue<Guid>());
        Assert.Equal(customer.Id, singleJson["order"]?["customerId"]?.GetValue<Guid>());
    }
}
