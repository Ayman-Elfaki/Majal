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

    [Fact]
    public void Entity_GeneratesForeignKeyForNavigationProperty()
    {
        var customer = new Customer { Id = 42, Name = "Alice" };
        var order = new Order
        {
            Id = 1,
            Customer = customer,
            CustomerId = customer.Id,
            OptionalCustomer = null,
            OptionalCustomerId = null
        };

        Assert.Equal(42, order.CustomerId);
        Assert.Null(order.OptionalCustomerId);

        order.OptionalCustomerId = 99;
        Assert.Equal(99, order.OptionalCustomerId);
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

[Entity<int>]
public partial class Order
{
    public Order()
    {
    }

    public Customer Customer { get; set; } = default!;
    public Customer? OptionalCustomer { get; set; }
}

[Entity<int>(GenerateForeignKeys = false)]
public partial class Invoice
{
    public Invoice()
    {
    }

    public Customer Customer { get; set; } = default!;
}