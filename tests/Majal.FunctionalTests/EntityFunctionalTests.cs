namespace Majal.FunctionalTests;

public class EntityFunctionalTests
{
    [Fact]
    public void Entity_Equality_IsBasedOnId()
    {
        var id = 1;
        var product1 = new Product { Id = id, Name = "Laptop" };
        var product2 = new Product { Id = id, Name = "Mouse" };
        var product3 = new Product { Id = 2, Name = "Laptop" };

        Assert.Equal(product1, product2);
        Assert.True(product1 == product2);
        Assert.NotEqual(product1, product3);
        Assert.False(product1 == product3);
    }

    [Fact]
    public void EntityWithGenericId_Works()
    {
        var customer1 = new Customer { Id = 1, Name = "Alice" };
        var customer2 = new Customer { Id = 1, Name = "Bob" };

        Assert.Equal(customer1, customer2);
        Assert.Equal(1, customer1.Id);
    }
}

[Entity]
public partial class Product
{
    // Public constructor for testing
    public Product()
    {
    }

    public string Name { get; set; } = string.Empty;
}

[Entity<int>]
public partial class Customer
{
    // Public constructor for testing
    public Customer()
    {
    }

    public string Name { get; set; } = string.Empty;
}