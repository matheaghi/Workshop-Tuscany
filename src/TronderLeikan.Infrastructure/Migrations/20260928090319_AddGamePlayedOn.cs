using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TronderLeikan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamePlayedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PlayedOn",
                table: "Games",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayedOn",
                table: "Games");
        }
    }
}
