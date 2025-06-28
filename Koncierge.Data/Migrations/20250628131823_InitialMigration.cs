using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koncierge.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KubeConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KubeConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForwardContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    KonciergeKubeConfigId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForwardContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForwardContexts_KubeConfigs_KonciergeKubeConfigId",
                        column: x => x.KonciergeKubeConfigId,
                        principalTable: "KubeConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ForwardNameSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    KonciergeForwardContextId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForwardNameSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForwardNameSpaces_ForwardContexts_KonciergeForwardContextId",
                        column: x => x.KonciergeForwardContextId,
                        principalTable: "ForwardContexts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Forwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetType = table.Column<int>(type: "INTEGER", nullable: false),
                    KonciergeForwardNamespaceId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forwards_ForwardNameSpaces_KonciergeForwardNamespaceId",
                        column: x => x.KonciergeForwardNamespaceId,
                        principalTable: "ForwardNameSpaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ForwardLinkedConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KonciergeForwardId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForwardLinkedConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForwardLinkedConfigs_Forwards_KonciergeForwardId",
                        column: x => x.KonciergeForwardId,
                        principalTable: "Forwards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForwardContexts_KonciergeKubeConfigId",
                table: "ForwardContexts",
                column: "KonciergeKubeConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ForwardLinkedConfigs_KonciergeForwardId",
                table: "ForwardLinkedConfigs",
                column: "KonciergeForwardId");

            migrationBuilder.CreateIndex(
                name: "IX_ForwardNameSpaces_KonciergeForwardContextId",
                table: "ForwardNameSpaces",
                column: "KonciergeForwardContextId");

            migrationBuilder.CreateIndex(
                name: "IX_Forwards_KonciergeForwardNamespaceId",
                table: "Forwards",
                column: "KonciergeForwardNamespaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForwardLinkedConfigs");

            migrationBuilder.DropTable(
                name: "Forwards");

            migrationBuilder.DropTable(
                name: "ForwardNameSpaces");

            migrationBuilder.DropTable(
                name: "ForwardContexts");

            migrationBuilder.DropTable(
                name: "KubeConfigs");
        }
    }
}
