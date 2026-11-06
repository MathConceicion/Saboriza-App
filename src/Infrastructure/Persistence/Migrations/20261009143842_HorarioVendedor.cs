using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomiaMarketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HorarioVendedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HorarioFuncionamentoJson",
                table: "Vendedores",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HorarioFuncionamentoJson",
                table: "Vendedores");
        }
    }
}
