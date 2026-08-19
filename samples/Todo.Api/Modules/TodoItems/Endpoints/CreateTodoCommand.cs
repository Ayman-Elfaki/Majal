using FluentValidation;
using Todo.Core.Common.Persistence;
using Todo.Core.Modules.TodoItems.Entities;
using Todo.Core.Modules.TodoItems.ValueObjects;
using Todo.Core.Modules.TodoLists.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Todo.Api.Modules.TodoItems.Endpoints;

/// <summary>
/// Create a new todo
/// </summary>
public partial record CreateTodoItemCommand
{
    [DtoFor<PendingTodoItem>(Prefix = "")]
    [FlattenDtoFor<Capacity>(IsReversed = true)]
    public partial record PendingTodoItemsDto;
    
    

    /// <summary>
    /// request validator
    /// </summary>
    public class Validator : AbstractValidator<PendingTodoItemsDto>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.Title).NotEmpty().MaximumLength(TodoTitle.MaxLength);
            RuleFor(dto => dto.StoryPoints).InclusiveBetween(0, 10);
            RuleFor(dto => dto.Priority).InclusiveBetween(0, 5);
        }
    }


    /// <summary>
    /// Create a new TodoItem
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("TodoItems")]
    [WolverinePost("/todos")]
    public static async Task<IResult> Create(PendingTodoItemsDto dto, [FromServices] AppDbContext context, CancellationToken ct)
    {
        var todoList = context.TodoLists.FirstOrDefault(p => p.Id == dto.TodoListId);
        
        if (todoList is null) return Results.NotFound();
        
        var todo = PendingTodoItem.Create(
            TodoTitle.Create(dto.Title),
            TodoPriority.Create(dto.StoryPoints),
            TodoStoryPoints.Create(dto.StoryPoints),
            todoList
        );
        
        todoList.TodoItems.Add(todo);
        await context.SaveChangesAsync(ct);
        return Results.Ok(PendingTodoItemsDto.FromPendingTodoItem(todo));
    }
}