using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomiaMarketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FotoLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Vendedores",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Lojas",
                type: "varchar(500)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Lojas");
        }
    }
}
