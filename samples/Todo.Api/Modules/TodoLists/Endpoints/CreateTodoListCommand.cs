using FluentValidation;
using Todo.Api.Common.Filters;
using Todo.Core.Common.Persistence;
using Todo.Core.Modules.TodoLists.Entities;
using Todo.Core.Modules.TodoLists.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Todo.Api.Modules.TodoLists.Endpoints;

/// <summary>
/// create a new todoList
/// </summary>
public partial class CreateTodoListCommand
{
    /// <summary>
    /// The TodoList Dto
    /// </summary>
    [DtoFor<PersonalTodoList>]
    [FlattenDtoFor<Capacity>(IsReversed = true)]
    public partial class PersonalTodoListDtos;

    /// <summary>
    /// The Dto Validator
    /// </summary>
    public class Validator : AbstractValidator<PersonalTodoListDtos>
    {
        /// <summary>
        /// the validator constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.Name)
                .NotEmpty()
                .MaximumLength(TodoListName.MaxLength);

            RuleFor(dto => dto.Translations)
                .NotEmpty();

            RuleForEach(dto => dto.Translations).ChildRules(r =>
            {
                r.RuleFor(p => p.DisplayName)
                    .NotEmpty()
                    .MaximumLength(TodoListName.MaxLength);

                r.RuleFor(p => p.Description)
                    .NotEmpty()
                    .MaximumLength(TodoListDescription.MaxLength);

                r.RuleFor(p => p.Locale)
                    .NotEmpty()
                    .Matches("^[a-zA-Z]{2}$");
            });
        }
    }

    /// <summary>
    /// Create New TodoList
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("TodoLists")]
    [WolverinePost("/todo-lists")]
    public static async Task<IResult> Create(PersonalTodoListDtos dto, [FromServices] AppDbContext context,
        CancellationToken ct)
    {
        var todoList = dto.ToPersonalTodoList();

        context.TodoLists.Add(todoList);
        await context.SaveChangesAsync(ct);

        return Results.Ok();
    }
}