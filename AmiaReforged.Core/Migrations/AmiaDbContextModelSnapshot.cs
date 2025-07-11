﻿// <auto-generated />
using System;
using AmiaReforged.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    [DbContext(typeof(AmiaDbContext))]
    partial class AmiaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AmiaReforged.Core.Models.Ban", b =>
                {
                    b.Property<string>("CdKey")
                        .HasColumnType("text")
                        .HasColumnName("cd_key");

                    b.HasKey("CdKey");

                    b.HasIndex(new[] { "CdKey" }, "bans_cd_key_key")
                        .IsUnique();

                    b.ToTable("bans", (string)null);
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Dm", b =>
                {
                    b.Property<string>("CdKey")
                        .HasColumnType("text");

                    b.Property<string>("LoginName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("CdKey");

                    b.ToTable("Dms");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.DmLogin", b =>
                {
                    b.Property<int>("LoginNumber")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("LoginNumber"));

                    b.Property<string>("CdKey")
                        .HasColumnType("text");

                    b.Property<string>("LoginName")
                        .HasColumnType("text");

                    b.Property<int>("PlayTime")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("SessionEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("SessionStart")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("LoginNumber");

                    b.HasIndex("CdKey");

                    b.ToTable("DmLogins");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.DreamcoinRecord", b =>
                {
                    b.Property<string>("CdKey")
                        .HasColumnType("character varying")
                        .HasColumnName("cd_key");

                    b.Property<int?>("Amount")
                        .HasColumnType("integer")
                        .HasColumnName("amount");

                    b.HasKey("CdKey")
                        .HasName("dreamcoin_records_pkey");

                    b.ToTable("dreamcoin_records", (string)null);
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Encounter", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("DmId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("EncounterSize")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("DmId");

                    b.ToTable("Encounters");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.EncounterEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("EncounterId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<byte[]>("SerializedString")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.HasKey("Id");

                    b.HasIndex("EncounterId");

                    b.ToTable("EncounterEntries");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.FactionCharacterRelation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Guid>("CharacterId")
                        .HasColumnType("uuid");

                    b.Property<long>("FactionId")
                        .HasColumnType("bigint");

                    b.Property<int>("Relation")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.HasIndex("FactionId");

                    b.ToTable("FactionCharacterRelations");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.FactionEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Factions");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.FactionRelation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("FactionName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Relation")
                        .HasColumnType("integer");

                    b.Property<string>("TargetFactionName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("FactionRelations");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.PlayerFactionMember", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Guid>("CharacterId")
                        .HasColumnType("uuid");

                    b.Property<long>("FactionId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.HasIndex("FactionId");

                    b.ToTable("PlayerFactionMember");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.InvasionRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaZone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("InvasionPercent")
                        .HasColumnType("integer");

                    b.Property<int>("RealmChaos")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("InvasionRecord");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.LastLocation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaResRef")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("Orientation")
                        .HasColumnType("real");

                    b.Property<string>("PCKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("X")
                        .HasColumnType("real");

                    b.Property<float>("Y")
                        .HasColumnType("real");

                    b.Property<float>("Z")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("LastLocation");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Npc", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("DmCdKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("Public")
                        .HasColumnType("boolean");

                    b.Property<byte[]>("Serialized")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.HasKey("Id");

                    b.ToTable("Npcs");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.PersistPLC", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaResRef")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("Orientation")
                        .HasColumnType("real");

                    b.Property<string>("PLCDescription")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PLCName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PLCResRef")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("Size")
                        .HasColumnType("real");

                    b.Property<float>("X")
                        .HasColumnType("real");

                    b.Property<float>("Y")
                        .HasColumnType("real");

                    b.Property<float>("Z")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("PersistPLC");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Player", b =>
                {
                    b.Property<string>("CdKey")
                        .HasColumnType("character varying")
                        .HasColumnName("cd_key");

                    b.HasKey("CdKey")
                        .HasName("players_pkey");

                    b.ToTable("players", (string)null);
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.PlayerCharacter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasColumnType("character varying");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.SavedQuickslots", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("PlayerCharacterId")
                        .HasColumnType("uuid");

                    b.Property<byte[]>("Quickslots")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.HasKey("Id");

                    b.HasIndex("PlayerCharacterId");

                    b.ToTable("SavedQuickslots");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.SavedSpellbook", b =>
                {
                    b.Property<long>("BookId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("BookId"));

                    b.Property<int>("ClassId")
                        .HasColumnType("integer");

                    b.Property<Guid>("PlayerCharacterId")
                        .HasColumnType("uuid");

                    b.Property<string>("SpellbookJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SpellbookName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("BookId");

                    b.HasIndex("PlayerCharacterId");

                    b.ToTable("SavedSpellbooks");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.StoredItem", b =>
                {
                    b.Property<long>("ItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ItemId"));

                    b.Property<string>("ItemJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("PlayerCharacterId")
                        .HasColumnType("uuid");

                    b.HasKey("ItemId");

                    b.HasIndex("PlayerCharacterId");

                    b.ToTable("PlayerItems");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.DmLogin", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.Dm", "Dm")
                        .WithMany()
                        .HasForeignKey("CdKey");

                    b.Navigation("Dm");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.DreamcoinRecord", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.Player", "CdKeyNavigation")
                        .WithOne("DreamcoinRecord")
                        .HasForeignKey("AmiaReforged.Core.Models.DreamcoinRecord", "CdKey")
                        .IsRequired()
                        .HasConstraintName("dreamcoin_records_cd_key_fkey");

                    b.Navigation("CdKeyNavigation");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Encounter", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.Dm", "Dm")
                        .WithMany()
                        .HasForeignKey("DmId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dm");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.EncounterEntry", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.Encounter", "Encounter")
                        .WithMany("EncounterEntries")
                        .HasForeignKey("EncounterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Encounter");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.FactionCharacterRelation", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.PlayerCharacter", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AmiaReforged.Core.Models.Faction.FactionEntity", "Faction")
                        .WithMany()
                        .HasForeignKey("FactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");

                    b.Navigation("Faction");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.PlayerFactionMember", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.PlayerCharacter", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AmiaReforged.Core.Models.Faction.FactionEntity", "Faction")
                        .WithMany("PlayerMembers")
                        .HasForeignKey("FactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");

                    b.Navigation("Faction");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.PlayerCharacter", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.Player", "Player")
                        .WithMany("PlayerCharacters")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Player");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.SavedQuickslots", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.PlayerCharacter", "PlayerCharacter")
                        .WithMany("Quickslots")
                        .HasForeignKey("PlayerCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlayerCharacter");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.SavedSpellbook", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.PlayerCharacter", "PlayerCharacter")
                        .WithMany("Spellbooks")
                        .HasForeignKey("PlayerCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlayerCharacter");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.StoredItem", b =>
                {
                    b.HasOne("AmiaReforged.Core.Models.PlayerCharacter", "Character")
                        .WithMany("Items")
                        .HasForeignKey("PlayerCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Encounter", b =>
                {
                    b.Navigation("EncounterEntries");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Faction.FactionEntity", b =>
                {
                    b.Navigation("PlayerMembers");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.Player", b =>
                {
                    b.Navigation("DreamcoinRecord");

                    b.Navigation("PlayerCharacters");
                });

            modelBuilder.Entity("AmiaReforged.Core.Models.PlayerCharacter", b =>
                {
                    b.Navigation("Items");

                    b.Navigation("Quickslots");

                    b.Navigation("Spellbooks");
                });
#pragma warning restore 612, 618
        }
    }
}
