using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models.DTOs;

/// <summary>Create / update category request (age or distance category for an event).</summary>
public class SaveCategoryDto
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

/// <summary>Category data returned to clients.</summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int EnrolmentCount { get; set; }

    public static CategoryDto From(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        EnrolmentCount = c.Enrolments?.Count ?? 0
    };
}
