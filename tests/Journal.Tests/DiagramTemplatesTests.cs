using Journal.Core.Rendering;

namespace Journal.Tests;

public sealed class DiagramTemplatesTests
{
    [Fact]
    public void All_HasUniqueNamesAndNonEmptySources()
    {
        Assert.Equal(DiagramTemplates.All.Count, DiagramTemplates.All.Select(template => template.Name).Distinct().Count());
        Assert.All(DiagramTemplates.All, template => Assert.False(string.IsNullOrWhiteSpace(template.Source)));
    }

    [Fact]
    public void Default_IsTheFlowchart()
    {
        Assert.Equal("Flowchart", DiagramTemplates.Default.Name);
        Assert.StartsWith("flowchart", DiagramTemplates.Default.Source);
    }

    [Fact]
    public void Find_IgnoresCase()
    {
        Assert.Equal("Pie chart", DiagramTemplates.Find("pie CHART")?.Name);
        Assert.Null(DiagramTemplates.Find("missing"));
    }
}
