using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <summary>
    /// Migrazione per rimuovere la colonna legacy 'Hour' dalla tabella TaskDaEseguire.
    /// La colonna era ridondante rispetto a TimeTask / CronExpression e non più utilizzata nel dominio.
    /// </summary>
    public partial class RemoveHourFromTaskDaEseguire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rimuove la colonna solo se esiste (compatibilità ambienti già aggiornati)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns 
           WHERE Name = 'Hour' AND Object_ID = OBJECT_ID('dbo.TaskDaEseguire'))
BEGIN
    ALTER TABLE dbo.TaskDaEseguire DROP COLUMN [Hour];
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ripristina la colonna (nullable) per compatibilità rollback
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE Name = 'Hour' AND Object_ID = OBJECT_ID('dbo.TaskDaEseguire'))
BEGIN
    ALTER TABLE dbo.TaskDaEseguire ADD [Hour] int NULL;
END
");
        }
    }
}
