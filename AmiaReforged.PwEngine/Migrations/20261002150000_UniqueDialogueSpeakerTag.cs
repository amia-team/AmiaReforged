using Microsoft.EntityFrameworkCore.Migrations;

namespace AmiaReforged.PwEngine.Migrations;

public partial class UniqueDialogueSpeakerTag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT speaker_tag FROM dialogue_trees
                           WHERE speaker_tag IS NOT NULL AND speaker_tag <> ''
                           GROUP BY speaker_tag HAVING COUNT(*) > 1) THEN
                    RAISE EXCEPTION 'Duplicate dialogue speaker tags must be reassigned before applying UniqueDialogueSpeakerTag';
                END IF;
            END $$;
            """);
        migrationBuilder.DropIndex("ix_dialogue_trees_speaker_tag", "dialogue_trees");
        migrationBuilder.CreateIndex("ix_dialogue_trees_speaker_tag", "dialogue_trees", "speaker_tag",
            unique: true, filter: "speaker_tag IS NOT NULL AND speaker_tag <> ''");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("ix_dialogue_trees_speaker_tag", "dialogue_trees");
        migrationBuilder.CreateIndex("ix_dialogue_trees_speaker_tag", "dialogue_trees", "speaker_tag");
    }
}
