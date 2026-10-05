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

    [Fact]
    public void EnumBasedValueObject_StaticFields_Work()
    {
        Assert.Equal(ProductStatus.Draft, Status.Draft.Value);
        Assert.Equal(ProductStatus.Active, Status.Active.Value);
        Assert.Equal(ProductStatus.Archived, Status.Archived.Value);
        Assert.Equal(Status.Active, Status.Create(ProductStatus.Active));
    }

    [Fact]
    public void EnumBasedValueObject_Equality_Works()
    {
        var active1 = Status.Active;
        var active2 = Status.Create(ProductStatus.Active);
        var draft = Status.Draft;

        Assert.Equal(active1, active2);
        Assert.True(active1 == active2);
        Assert.False(active1 == draft);
        Assert.True(active1 != draft);
        Assert.Equal(active1.GetHashCode(), active2.GetHashCode());
    }

    [Fact]
    public void EnumBasedValueObject_Comparison_Works()
    {
        Assert.True(Status.Draft < Status.Active);
        Assert.True(Status.Active > Status.Draft);
        Assert.Equal(-1, Status.Draft.CompareTo(Status.Active));
        Assert.Equal(1, Status.Archived.CompareTo(Status.Active));
        Assert.Equal(0, Status.Active.CompareTo(Status.Create(ProductStatus.Active)));
    }

    [Fact]
    public void EnumBasedValueObject_Parsing_Works()
    {
        Assert.Equal(Status.Active, Status.Parse("Active"));
        Assert.Equal(Status.Active, Status.Parse("active"));
        Assert.Equal(Status.Active, Status.Parse("ACTIVE"));

        Assert.True(Status.TryParse("Draft", null, out var parsedDraft));
        Assert.Equal(Status.Draft, parsedDraft);

        Assert.False(Status.TryParse("InvalidStatus", null, out _));
        Assert.False(Status.TryParse("999", null, out _));
        Assert.False(Status.TryParse(null, null, out _));
        Assert.False(Status.TryParse("   ", null, out _));
    }

    [Fact]
    public void EnumBasedValueObject_ToString_Works()
    {
        Assert.Equal("Active", Status.Active.ToString());
        Assert.Equal("Draft", Status.Draft.ToString());
    }

    [Fact]
    public void EnumBasedValueObject_JsonRoundtrip_Works()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Status.Active);
        Assert.Equal("\"Active\"", json);

        var fromStringJson = System.Text.Json.JsonSerializer.Deserialize<Status>(json);
        Assert.Equal(Status.Active, fromStringJson);

        var fromNumberJson = System.Text.Json.JsonSerializer.Deserialize<Status>("1");
        Assert.Equal(Status.Active, fromNumberJson);
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

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

[ValueObject<ProductStatus>]
public readonly partial struct Status;