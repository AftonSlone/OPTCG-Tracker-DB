using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPTCG.Tracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CardSetId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CardName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SetName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SetId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CardType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CardColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rarity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Life = table.Column<int>(type: "integer", nullable: true),
                    CardCost = table.Column<int>(type: "integer", nullable: true),
                    CardPower = table.Column<int>(type: "integer", nullable: true),
                    CounterAmount = table.Column<int>(type: "integer", nullable: true),
                    Attribute = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubTypes = table.Column<string>(type: "text", nullable: true),
                    CardText = table.Column<string>(type: "text", nullable: true),
                    CardImageUrl = table.Column<string>(type: "text", nullable: true),
                    CardImageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cards_CardColor",
                table: "cards",
                column: "CardColor");

            migrationBuilder.CreateIndex(
                name: "IX_cards_CardImageId",
                table: "cards",
                column: "CardImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_CardName",
                table: "cards",
                column: "CardName");

            migrationBuilder.CreateIndex(
                name: "IX_cards_CardSetId",
                table: "cards",
                column: "CardSetId");

            migrationBuilder.CreateIndex(
                name: "IX_cards_CardType",
                table: "cards",
                column: "CardType");

            migrationBuilder.CreateIndex(
                name: "IX_cards_Rarity",
                table: "cards",
                column: "Rarity");

            migrationBuilder.CreateIndex(
                name: "IX_cards_SetId",
                table: "cards",
                column: "SetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cards");
        }
    }
}
