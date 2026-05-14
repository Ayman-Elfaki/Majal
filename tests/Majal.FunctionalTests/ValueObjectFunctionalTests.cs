namespace Majal.FunctionalTests;

public class ValueObjectFunctionalTests
{
    [Fact]
    public void GenericValueObject_Equality_Works()
    {
        var id1 = ProjectId.Create(1);
        var id2 = ProjectId.Create(1);
        var id3 = ProjectId.Create(2);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 == id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void GenericValueObject_Comparison_Works()
    {
        var amount1 = Amount.Create(10.5m);
        var amount2 = Amount.Create(20.0m);

        Assert.True(amount1 < amount2);
        Assert.True(amount2 > amount1);
        Assert.Equal(-1, amount1.CompareTo(amount2));
    }

    [Fact]
    public void ComplexValueObject_Equality_Works()
    {
        var money1 = Money.Create(100, "USD");
        var money2 = Money.Create(100, "USD");
        var money3 = Money.Create(100, "EUR");
        var money4 = Money.Create(200, "USD");

        Assert.Equal(money1, money2);
        Assert.True(money1 == money2);
        Assert.NotEqual(money1, money3);
        Assert.NotEqual(money1, money4);
    }

    [Fact]
    public void ComplexValueObjectWithList_Equality_Works()
    {
        var coupon1 = Coupon.Create(100, [DayOfWeek.Monday, DayOfWeek.Tuesday]);
        var coupon2 = Coupon.Create(100, [DayOfWeek.Monday, DayOfWeek.Tuesday]);
        var coupon3 = Coupon.Create(100, [DayOfWeek.Monday, DayOfWeek.Wednesday]);
        var coupon4 = Coupon.Create(200, [DayOfWeek.Monday, DayOfWeek.Tuesday]);

        Assert.Equal(coupon1, coupon2);
        Assert.True(coupon1 == coupon2);
        Assert.NotEqual(coupon1, coupon3);
        Assert.NotEqual(coupon1, coupon4);
    }

    [Fact]
    public void ComplexValueObjectWithList_HashCode_Works()
    {
        var money1 = Money.Create(100, "USD");
        var money2 = Money.Create(100, "USD");

        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void ComplexValueObjectWithList_ToString_Works()
    {
        var coupon = Coupon.Create(100, [DayOfWeek.Monday, DayOfWeek.Tuesday]);
        var toString = coupon.ToString();

        Assert.Contains("Days = [Monday, Tuesday]", toString);
        Assert.Contains("Discount = 100", toString);
    }

    [Fact]
    public void GenericValueObjectWithList_ToString_Works()
    {
        var tags = Tags.Create(["tag1", "tag2"]);
        var toString = tags.ToString();

        Assert.Equal("{ Values = [tag1, tag2] }", toString);
    }
}

[ValueObject]
public readonly partial struct Tags
{
    public List<string> Values { get; init; }

    public static Tags Create(List<string> values) => new() { Values = values };

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Values;
    }
}

[ValueObject<int>]
public readonly partial struct ProjectId;

[ValueObject<decimal>]
public readonly partial struct Amount;

[ValueObject]
public readonly partial struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public static Money Create(decimal amount, string currency) => new() { Amount = amount, Currency = currency };

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

[ValueObject]
public readonly partial struct Coupon
{
    public decimal Discount { get; init; }
    public List<DayOfWeek> Days { get; init; }

    public static Coupon Create(decimal discount, IEnumerable<DayOfWeek> days)
    {
        return new Coupon
        {
            Days = [..days],
            Discount = discount
        };
    }

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Days;
        yield return Discount;
    }
}