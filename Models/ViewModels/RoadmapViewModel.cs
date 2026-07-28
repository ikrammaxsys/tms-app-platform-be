namespace tms_template_net8.Models.ViewModels;

public class RoadmapViewModel
{
    public List<RoadmapItem> Ideas { get; set; } = [];
    public List<RoadmapItem> InProgress { get; set; } = [];
    public List<RoadmapItem> Shipped { get; set; } = [];
}

public class RoadmapItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}
