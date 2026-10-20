using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juleplanlaegning.Migrations
{
    /// <inheritdoc />
    public partial class AddPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Order matters: existing buckets and placements are moved into a first plan
            // before People.BucketId is dropped.
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "Name", "SortOrder" },
                values: new object[] { 1, "Day 1", 0 });

            // Existing buckets belong to the first plan.
            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "Buckets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    BucketId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Buckets_BucketId",
                        column: x => x.BucketId,
                        principalTable: "Buckets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "Assignments" ("PlanId", "PersonId", "BucketId")
                SELECT 1, "Id", "BucketId" FROM "People" WHERE "BucketId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_People_Buckets_BucketId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_BucketId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BucketId",
                table: "People");

            migrationBuilder.CreateIndex(
                name: "IX_Buckets_PlanId",
                table: "Buckets",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_BucketId",
                table: "Assignments",
                column: "BucketId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_PersonId",
                table: "Assignments",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_PlanId_PersonId",
                table: "Assignments",
                columns: new[] { "PlanId", "PersonId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Buckets_Plans_PlanId",
                table: "Buckets",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buckets_Plans_PlanId",
                table: "Buckets");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Buckets_PlanId",
                table: "Buckets");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Buckets");

            migrationBuilder.AddColumn<int>(
                name: "BucketId",
                table: "People",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_BucketId",
                table: "People",
                column: "BucketId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Buckets_BucketId",
                table: "People",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
