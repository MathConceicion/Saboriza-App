using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomiaMarketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LojaConfiguracoesAlargadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Aberta",
                table: "Vendedores",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "DiasFuncionamentoJson",
                table: "Vendedores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Aberta",
                table: "Lojas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Cnpj",
                table: "Lojas",
                type: "varchar(14)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiasFuncionamentoJson",
                table: "Lojas",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Lojas",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aberta",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "DiasFuncionamentoJson",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "Aberta",
                table: "Lojas");

            migrationBuilder.DropColumn(
                name: "Cnpj",
                table: "Lojas");

            migrationBuilder.DropColumn(
                name: "DiasFuncionamentoJson",
                table: "Lojas");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Lojas");
        }
    }
}
