using System.ComponentModel.DataAnnotations;

namespace EmailClassification.Application.DTOs.Email;

public class Filter
{
    [Range(1, int.MaxValue)]
    public int PageIndex { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20 ;
    public string? LabelName { get; set; }
    public string? DirectionName { get; set; }

}
