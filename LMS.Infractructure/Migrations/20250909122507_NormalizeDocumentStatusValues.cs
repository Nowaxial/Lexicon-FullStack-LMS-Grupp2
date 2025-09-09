using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infractructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDocumentStatusValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Documents]
SET [Status] = 'Pending'
WHERE [Status] IS NULL
   OR LTRIM(RTRIM([Status])) = N''
   OR [Status] = N'Ej bedömd';

UPDATE [Documents] SET [Status] = 'Review'   WHERE [Status] = N'Granskning';
UPDATE [Documents] SET [Status] = 'Approved' WHERE [Status] = N'Godkänd';
UPDATE [Documents] SET [Status] = 'Rejected' WHERE [Status] = N'Underkänd';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optional: revert back to Swedish if you ever roll back
            migrationBuilder.Sql(@"
UPDATE [Documents] SET [Status] = N'Ej bedömd'  WHERE [Status] = 'Pending';
UPDATE [Documents] SET [Status] = N'Granskning' WHERE [Status] = 'Review';
UPDATE [Documents] SET [Status] = N'Godkänd'    WHERE [Status] = 'Approved';
UPDATE [Documents] SET [Status] = N'Underkänd'  WHERE [Status] = 'Rejected';
");
        }
    }
}
