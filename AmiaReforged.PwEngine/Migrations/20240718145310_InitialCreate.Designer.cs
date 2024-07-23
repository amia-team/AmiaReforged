﻿// <auto-generated />
using AmiaReforged.PwEngine.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    [DbContext(typeof(PwEngineContext))]
    [Migration("20240718145310_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AmiaReforged.PwEngine.Database.Entities.WorldCharacter", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("WorldCharacters");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorage", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.HasKey("Id");

                    b.ToTable("StorageContainers");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorageUser", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ItemStorageId")
                        .HasColumnType("bigint");

                    b.Property<long>("WorldCharacterId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ItemStorageId");

                    b.HasIndex("WorldCharacterId");

                    b.ToTable("ItemStorageUsers");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.JobItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<int>("BaseValue")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("DurabilityModifier")
                        .HasColumnType("real");

                    b.Property<string>("IconResRef")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("MagicModifier")
                        .HasColumnType("real");

                    b.Property<int>("Material")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Quality")
                        .HasColumnType("integer");

                    b.Property<string>("ResRef")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<byte[]>("SerializedData")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<long>("WorldCharacterId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("WorldCharacterId");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.StoredJobItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ItemStorageId")
                        .HasColumnType("bigint");

                    b.Property<long>("JobItemId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ItemStorageId");

                    b.HasIndex("JobItemId");

                    b.ToTable("StoredJobItems");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorageUser", b =>
                {
                    b.HasOne("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorage", "ItemStorage")
                        .WithMany()
                        .HasForeignKey("ItemStorageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AmiaReforged.PwEngine.Database.Entities.WorldCharacter", "WorldCharacter")
                        .WithMany()
                        .HasForeignKey("WorldCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ItemStorage");

                    b.Navigation("WorldCharacter");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.JobItem", b =>
                {
                    b.HasOne("AmiaReforged.PwEngine.Database.Entities.WorldCharacter", "MadeBy")
                        .WithMany()
                        .HasForeignKey("WorldCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MadeBy");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.StoredJobItem", b =>
                {
                    b.HasOne("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorage", "ItemStorage")
                        .WithMany("Items")
                        .HasForeignKey("ItemStorageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AmiaReforged.PwEngine.Systems.JobSystem.Entities.JobItem", "JobItem")
                        .WithMany()
                        .HasForeignKey("JobItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ItemStorage");

                    b.Navigation("JobItem");
                });

            modelBuilder.Entity("AmiaReforged.PwEngine.Systems.JobSystem.Entities.ItemStorage", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}
