using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Components.Base;
using Xunit;

namespace Spamma.App.Tests.Components;

public class ModalBaseTests
{
    [Fact]
    public void ModalBase_WhenVisible_ContentContainerDoesNotHaveOverflowHidden()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<ModalBase>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>Content</p>"))));

        var contentContainer = cut.Find(".shadow-xl");

        contentContainer.ClassList.Should().NotContain("overflow-hidden",
            because: "overflow-hidden clips absolutely-positioned dropdown children (e.g. UserTypeahead) when inside a modal");
    }

    [Fact]
    public void ModalBase_WhenNotVisible_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<ModalBase>(parameters => parameters
            .Add(p => p.IsVisible, false));

        cut.Markup.Trim().Should().BeEmpty(because: "modal should not render when IsVisible is false");
    }
}
