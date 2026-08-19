using System.Globalization;
using Majal;

[assembly: EntityOptions(DefaultIdType = typeof(int))]
[assembly: AggregateOptions(DefaultDomainEventType = typeof(object))]
[assembly: TranslatableOptions(DefaultLocaleType = typeof(CultureInfo))]