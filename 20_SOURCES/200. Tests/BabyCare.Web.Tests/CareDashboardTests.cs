using BabyCare.Application;
using BabyCare.Domain;
using BabyCare.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BabyCare.Tests;
public class CareDashboardTests
{
    [Fact] public void AgeUsesCalendarMonthsAndBirthIsDayZero()
    {
        Assert.Equal((0, 0), CareDashboard.Age(new(2026, 1, 31), new(2026, 1, 31)));
        Assert.Equal((28, 1), CareDashboard.Age(new(2026, 1, 31), new(2026, 2, 28)));
        Assert.Equal((365, 12), CareDashboard.Age(new(2024, 2, 29), new(2025, 2, 28)));
        Assert.Null(CareDashboard.Age(null, new(2026, 1, 1)));
        Assert.Null(CareDashboard.Age(new(2026, 1, 2), new(2026, 1, 1)));
    }
    [Fact] public void SleepRangesChangeAtMonthBoundaries()
    {
        Assert.Contains("편차", CareDashboard.SleepReference(3));
        Assert.Contains("12~16", CareDashboard.SleepReference(4));
        Assert.Contains("11~14", CareDashboard.SleepReference(12));
        Assert.Contains("10~13", CareDashboard.SleepReference(36));
    }
    [Fact] public void AverageUsesOnlyMatchingRecordsAndRequiresTwo()
    {
        var a = new CareEntry { StartedAt = DateTimeOffset.UtcNow.AddHours(-3) };
        var b = a with { Id = "second", StartedAt = a.StartedAt.AddMinutes(150) };
        Assert.Equal(150d, CareDashboard.AverageInterval([b, a, new() { Kind = "diaper" }], "feeding"));
        Assert.Null(CareDashboard.AverageInterval([a], "feeding"));
    }
    [Fact] public void ProfileIsSharedAndConcurrentChangesAreRejected()
    {
        var folder = Path.Combine(Path.GetTempPath(), "babycare-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareStore(Path.Combine(folder, "test.db"), new CareCatalog(Path.Combine(AppContext.BaseDirectory, "care-kinds.json")));
            var f = store.Create("mother", "test", "baby"); store.Join("father", store.Invite("mother", f.Id));
            var old = f.Preferences with { };
            store.SavePreferences("mother", f.Id, old with { BirthDate = new(2026, 1, 1), DailyMilkGoalMl = 700, DiaperCheckMinutes = 120 });
            var saved = store.Get("father", f.Id).Preferences;
            Assert.Equal(new DateOnly(2026, 1, 1), saved.BirthDate); Assert.Equal(700, saved.DailyMilkGoalMl);
            Assert.Equal(120, saved.DiaperCheckMinutes); Assert.Equal(20, saved.FeedingMinutes);
            Assert.Throws<CareException>(() => store.SavePreferences("father", f.Id, old));
            Assert.Throws<CareException>(() => CareDashboard.Validate(saved with { BirthDate = new(2099, 1, 1) }, new(2026, 9, 25)));
            Assert.Throws<CareException>(() => CareDashboard.Validate(saved with { DailyMilkGoalMl = 0 }, new(2026, 9, 25)));
            store.SavePreferences("father", f.Id, saved with { BirthDate = null, DailyMilkGoalMl = null, DiaperCheckMinutes = null });
            Assert.Null(store.Get("mother", f.Id).Preferences.BirthDate);
        }
        finally { SqliteConnection.ClearAllPools(); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
