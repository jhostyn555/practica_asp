using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_asp.Migrations
{
    /// <inheritdoc />
    public partial class AddNombrePublicoToOpinion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombrePublico",
                table: "Opiniones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombrePublico",
                table: "Opiniones");
        }
    }
}
