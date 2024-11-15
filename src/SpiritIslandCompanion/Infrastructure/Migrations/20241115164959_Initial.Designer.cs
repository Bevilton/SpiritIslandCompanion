﻿// <auto-generated />
using System;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20241115164959_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Domain.Models.Game.Game", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("StartedAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("Domain.Models.Player.Player", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("Domain.Models.User.User", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("Registered")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Domain.Models.Game.Game", b =>
                {
                    b.OwnsOne("Domain.Models.Game.Difficulty", "Difficulty", b1 =>
                        {
                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("Value")
                                .HasColumnType("int");

                            b1.HasKey("GameId");

                            b1.ToTable("Games");

                            b1.WithOwner()
                                .HasForeignKey("GameId");
                        });

                    b.OwnsOne("Domain.Models.Game.GameNote", "Note", b1 =>
                        {
                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("GameId");

                            b1.ToTable("Games");

                            b1.WithOwner()
                                .HasForeignKey("GameId");
                        });

                    b.OwnsMany("Domain.Models.Game.GamePlayer", "Players", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("Id");

                            b1.HasIndex("GameId");

                            b1.ToTable("Game_GamePlayer", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("GameId");

                            b1.OwnsOne("Domain.Models.User.UserId", "UserId", b2 =>
                                {
                                    b2.Property<Guid>("GamePlayerId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Value")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("GamePlayerId");

                                    b2.ToTable("Game_GamePlayer");

                                    b2.WithOwner()
                                        .HasForeignKey("GamePlayerId");
                                });

                            b1.OwnsOne("Domain.Models.Player.PlayerId", "PlayerId", b2 =>
                                {
                                    b2.Property<Guid>("GamePlayerId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Value")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("GamePlayerId");

                                    b2.ToTable("Game_GamePlayer");

                                    b2.WithOwner()
                                        .HasForeignKey("GamePlayerId");
                                });

                            b1.OwnsOne("Domain.Models.Static.AspectId", "AspectId", b2 =>
                                {
                                    b2.Property<Guid>("GamePlayerId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("GamePlayerId");

                                    b2.ToTable("Game_GamePlayer");

                                    b2.WithOwner()
                                        .HasForeignKey("GamePlayerId");
                                });

                            b1.OwnsOne("Domain.Models.Static.BoardId", "StartingBoard", b2 =>
                                {
                                    b2.Property<Guid>("GamePlayerId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("GamePlayerId");

                                    b2.ToTable("Game_GamePlayer");

                                    b2.WithOwner()
                                        .HasForeignKey("GamePlayerId");
                                });

                            b1.OwnsOne("Domain.Models.Static.SpiritId", "SpiritId", b2 =>
                                {
                                    b2.Property<Guid>("GamePlayerId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("GamePlayerId");

                                    b2.ToTable("Game_GamePlayer");

                                    b2.WithOwner()
                                        .HasForeignKey("GamePlayerId");
                                });

                            b1.Navigation("AspectId");

                            b1.Navigation("PlayerId");

                            b1.Navigation("SpiritId")
                                .IsRequired();

                            b1.Navigation("StartingBoard")
                                .IsRequired();

                            b1.Navigation("UserId");
                        });

                    b.OwnsOne("Domain.Models.Game.GameResult", "Result", b1 =>
                        {
                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<TimeSpan>("Duration")
                                .HasColumnType("time");

                            b1.Property<int>("TerrorLevel")
                                .HasColumnType("int");

                            b1.Property<bool>("Win")
                                .HasColumnType("bit");

                            b1.HasKey("GameId");

                            b1.ToTable("Games");

                            b1.WithOwner()
                                .HasForeignKey("GameId");

                            b1.OwnsOne("Domain.Models.Game.BlightCount", "Blight", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.OwnsOne("Domain.Models.Game.CardsCount", "Cards", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.OwnsOne("Domain.Models.Game.DahanCount", "Dahan", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.OwnsOne("Domain.Models.Game.GameResultId", "Id", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Value")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.OwnsOne("Domain.Models.Game.Score", "Score", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.OwnsOne("Domain.Models.Game.ScoreModifier", "ScoreModifier", b2 =>
                                {
                                    b2.Property<Guid>("GameResultGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("GameResultGameId");

                                    b2.ToTable("Games");

                                    b2.WithOwner()
                                        .HasForeignKey("GameResultGameId");
                                });

                            b1.Navigation("Blight")
                                .IsRequired();

                            b1.Navigation("Cards")
                                .IsRequired();

                            b1.Navigation("Dahan")
                                .IsRequired();

                            b1.Navigation("Id")
                                .IsRequired();

                            b1.Navigation("Score")
                                .IsRequired();

                            b1.Navigation("ScoreModifier")
                                .IsRequired();
                        });

                    b.OwnsMany("Domain.Models.Game.PlayedAdversary", "PlayedAdversaries", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("Id");

                            b1.HasIndex("GameId");

                            b1.ToTable("Game_PlayedAdversary", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("GameId");

                            b1.OwnsOne("Domain.Models.Game.AdversaryLevel", "Level", b2 =>
                                {
                                    b2.Property<Guid>("PlayedAdversaryId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Value")
                                        .HasColumnType("int");

                                    b2.HasKey("PlayedAdversaryId");

                                    b2.ToTable("Game_PlayedAdversary");

                                    b2.WithOwner()
                                        .HasForeignKey("PlayedAdversaryId");
                                });

                            b1.OwnsOne("Domain.Models.Static.AdversaryId", "AdversaryId", b2 =>
                                {
                                    b2.Property<Guid>("PlayedAdversaryId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("PlayedAdversaryId");

                                    b2.ToTable("Game_PlayedAdversary");

                                    b2.WithOwner()
                                        .HasForeignKey("PlayedAdversaryId");
                                });

                            b1.Navigation("AdversaryId")
                                .IsRequired();

                            b1.Navigation("Level")
                                .IsRequired();
                        });

                    b.OwnsOne("Domain.Models.Game.PlayedScenario", "Scenario", b1 =>
                        {
                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("GameId");

                            b1.ToTable("Game_PlayedScenario", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("GameId");

                            b1.OwnsOne("Domain.Models.Game.PlayedAdversaryId", "Id", b2 =>
                                {
                                    b2.Property<Guid>("PlayedScenarioGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Value")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("PlayedScenarioGameId");

                                    b2.ToTable("Game_PlayedScenario");

                                    b2.WithOwner()
                                        .HasForeignKey("PlayedScenarioGameId");
                                });

                            b1.OwnsOne("Domain.Models.Static.ScenarioId", "ScenarioId", b2 =>
                                {
                                    b2.Property<Guid>("PlayedScenarioGameId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("PlayedScenarioGameId");

                                    b2.ToTable("Game_PlayedScenario");

                                    b2.WithOwner()
                                        .HasForeignKey("PlayedScenarioGameId");
                                });

                            b1.Navigation("Id")
                                .IsRequired();

                            b1.Navigation("ScenarioId")
                                .IsRequired();
                        });

                    b.OwnsOne("Domain.Models.Static.IslandSetupId", "IslandSetupId", b1 =>
                        {
                            b1.Property<Guid>("GameId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("GameId");

                            b1.ToTable("Games");

                            b1.WithOwner()
                                .HasForeignKey("GameId");
                        });

                    b.Navigation("Difficulty")
                        .IsRequired();

                    b.Navigation("IslandSetupId")
                        .IsRequired();

                    b.Navigation("Note");

                    b.Navigation("PlayedAdversaries");

                    b.Navigation("Players");

                    b.Navigation("Result");

                    b.Navigation("Scenario");
                });

            modelBuilder.Entity("Domain.Models.Player.Player", b =>
                {
                    b.OwnsOne("Domain.Models.User.UserId", "CreatedBy", b1 =>
                        {
                            b1.Property<Guid>("PlayerId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("Value")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("PlayerId");

                            b1.ToTable("Players");

                            b1.WithOwner()
                                .HasForeignKey("PlayerId");
                        });

                    b.OwnsOne("Domain.Models.Player.PlayerName", "Name", b1 =>
                        {
                            b1.Property<Guid>("PlayerId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("PlayerId");

                            b1.ToTable("Players");

                            b1.WithOwner()
                                .HasForeignKey("PlayerId");
                        });

                    b.Navigation("CreatedBy")
                        .IsRequired();

                    b.Navigation("Name")
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Models.User.User", b =>
                {
                    b.OwnsOne("Domain.Models.User.Email", "Email", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.OwnsOne("Domain.Models.User.Nickname", "Nickname", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.OwnsOne("Domain.Models.User.UserSettings", "UserSettings", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");

                            b1.OwnsMany("Domain.Models.Static.ExpansionId", "Expansions", b2 =>
                                {
                                    b2.Property<Guid>("UserSettingsUserId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("int");

                                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b2.Property<int>("Id"));

                                    b2.Property<string>("Value")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("UserSettingsUserId", "Id");

                                    b2.ToTable("ExpansionId");

                                    b2.WithOwner()
                                        .HasForeignKey("UserSettingsUserId");
                                });

                            b1.OwnsOne("Domain.Models.User.UserSettingsId", "Id", b2 =>
                                {
                                    b2.Property<Guid>("UserSettingsUserId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Value")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("UserSettingsUserId");

                                    b2.ToTable("Users");

                                    b2.WithOwner()
                                        .HasForeignKey("UserSettingsUserId");
                                });

                            b1.Navigation("Expansions");

                            b1.Navigation("Id")
                                .IsRequired();
                        });

                    b.Navigation("Email")
                        .IsRequired();

                    b.Navigation("Nickname")
                        .IsRequired();

                    b.Navigation("UserSettings")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
