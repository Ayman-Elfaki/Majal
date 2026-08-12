using FluentValidation;
using HotelReservation.Sample.Common.Persistence;
using HotelReservation.Sample.Modules.Hotels.Entities;
using HotelReservation.Sample.Modules.Hotels.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace HotelReservation.Sample.Modules.Hotels.Endpoints;

/// <summary>
/// create a new hotel
/// </summary>
public partial class CreateHotelCommand
{
    /// <summary>
    /// The Hotel Dto
    /// </summary>
    [DtoFor<Hotel>]
    public partial class HotelDtos;

    /// <summary>
    /// The Dto Validator
    /// </summary>
    public class Validator : AbstractValidator<HotelDtos>
    {
        /// <summary>
        /// the validator constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.Name)
                .NotEmpty()
                .MaximumLength(HotelName.MaxLength);

            RuleFor(dto => dto.Translations)
                .NotEmpty();

            RuleForEach(dto => dto.Translations).ChildRules(r =>
            {
                r.RuleFor(p => p.DisplayName)
                    .NotEmpty()
                    .MaximumLength(HotelName.MaxLength);

                r.RuleFor(p => p.Description)
                    .NotEmpty()
                    .MaximumLength(HotelDescription.MaxLength);

                r.RuleFor(p => p.Locale)
                    .NotEmpty()
                    .Matches("^[a-zA-Z]{2}$");
            });
        }
    }

    /// <summary>
    /// Create a new hotel
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Hotels")]
    [WolverinePost("/hotels")]
    public static async Task<IResult> Create(HotelDtos dto, [FromServices] AppDbContext context, CancellationToken ct)
    {
        var hotel = dto.ToHotel();

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync(ct);

        return Results.Ok();
    }
}
