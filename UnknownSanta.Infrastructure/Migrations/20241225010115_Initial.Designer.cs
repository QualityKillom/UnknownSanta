﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnknownSanta.Infrastructure;

#nullable disable

namespace UnknownSanta.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241225010115_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("UnknownSanta.Domain.Entities.Games", b =>
                {
                    b.Property<long>("Game_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("ChatType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("TEXT");

                    b.Property<string>("GameState")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Game_Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("UnknownSanta.Domain.Entities.SendMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("SentAt")
                        .HasColumnType("TEXT");

                    b.Property<long>("Telegram_Id")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("SendMessages");
                });

            modelBuilder.Entity("UnknownSanta.Domain.Entities.Users", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("Game_Id")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Participle")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TagUserName")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<long>("Telegram_Id")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Game_Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("UnknownSanta.Domain.Entities.Users", b =>
                {
                    b.HasOne("UnknownSanta.Domain.Entities.Games", null)
                        .WithMany("Users")
                        .HasForeignKey("Game_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("UnknownSanta.Domain.Entities.Games", b =>
                {
                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
