using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WatchStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class BrandSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("45d14c06-327d-4ea5-b3a0-043aa75baa1e"), "Casio" },
                    { new Guid("5677f78b-81a0-4029-b202-7b3ea0789d83"), "Citizen" },
                    { new Guid("783f7679-9b09-4273-bdef-0d55eadb4597"), "Seiko" },
                    { new Guid("8c06b0cb-056e-4864-ad4b-2fce9172e4e1"), "Sector" },
                    { new Guid("8c97ac50-c26d-499f-b72a-75d360f8e641"), "Maserati" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("45d14c06-327d-4ea5-b3a0-043aa75baa1e"));

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("5677f78b-81a0-4029-b202-7b3ea0789d83"));

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("783f7679-9b09-4273-bdef-0d55eadb4597"));

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("8c06b0cb-056e-4864-ad4b-2fce9172e4e1"));

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("8c97ac50-c26d-499f-b72a-75d360f8e641"));
        }
    }
}
