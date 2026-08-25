using EShop.Modules.Categories.Entities;
using EShop.Modules.Products.Endpoints;
using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Products.Entities;

/// <summary>
/// Base catalog product. Uses the bare <c>[Aggregate]</c> form (assembly default domain-event type)
/// combined with <c>[Archivable]</c>, <c>[Auditable]</c> and <c>[Ordinal]</c>. Has no factory method of its
/// own, so <c>[DtoFor&lt;Product&gt;]</c> triggers polymorphic DTO generation -- an abstract base DTO plus
/// one DTO per concrete subclass with a factory method -- whether used directly as a root
/// <c>[DtoFor]</c> target (see <see cref="ListArchivedProductsQuery"/>) or
/// as a nested parameter type on another entity's factory method.
/// </summary>
[Entity, Aggregate]
[Archivable, Auditable, Ordinal]
public abstract partial class Product
{
    public ProductSku Sku { get; protected init; }
    public Money Price { get; protected init; }
    public Category Category { get; protected init; } = null!;

    /// <summary>
    /// Named differently from the "tags" factory parameter on purpose: <see cref="ProductTags"/> wraps a
    /// collection via a single-parameter factory, a shape the DTO generator can't safely
    /// auto-convert today. This naming routes it through the generator's explicit "supplied argument"
    /// fallback instead of generating invalid code.
    /// </summary>
    public ProductTags TagList { get; protected init; }

    /// <summary>
    /// Named differently from the "translations" factory parameter for the same reason as
    /// <see cref="Categories.Entities.Category.Translations"/>: <see cref="ProductTranslation"/> is <c>[Translatable]</c>,
    /// so its locale is supplied by the translation infrastructure rather than threaded through a
    /// nested-collection forwarding call.
    /// </summary>
    public List<ProductTranslation> Translations { get; protected init; } = [];

    /// <summary>
    /// Current stock on hand. Deliberately distinct from the <c>initialStockQuantity</c> factory
    /// parameter below: once created it changes independently of how the product was first stocked,
    /// so it cannot be reconstructed from current state alone.
    /// </summary>
    public uint StockQuantity { get; protected set; }
}
