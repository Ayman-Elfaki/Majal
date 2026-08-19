using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Todo.Core.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoListsHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "TodoLists",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "TodoLists");
        }
    }
}
