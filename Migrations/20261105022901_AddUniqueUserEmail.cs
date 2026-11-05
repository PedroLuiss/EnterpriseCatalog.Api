using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseCatalog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUserEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT LOWER(LTRIM(RTRIM([Email])))
                    FROM [Users]
                    GROUP BY LOWER(LTRIM(RTRIM([Email])))
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    ;THROW 50001, 'Cannot create unique email index: duplicate normalized emails exist in Users.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [Users]
                    WHERE LEN(LTRIM(RTRIM([Email]))) > 254
                )
                BEGIN
                    ;THROW 50002, 'Cannot limit email to 254 characters: an existing email is too long.', 1;
                END;

                UPDATE [Users]
                SET [Email] = LOWER(LTRIM(RTRIM([Email])));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
