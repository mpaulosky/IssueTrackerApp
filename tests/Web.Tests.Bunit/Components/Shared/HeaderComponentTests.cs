using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

public class HeaderComponentTests : BunitTestBase
{
	[Fact]
	public void HeaderComponent_WithHeaderText_RendersHeader()
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, "Test Header"));

		// Assert
		cut.Find("header").Should().NotBeNull();
	}

	[Fact]
	public void HeaderComponent_DefaultLevel_RendersH1()
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, "Test Header"));

		// Assert
		cut.Find("h1").TextContent.Should().Be("Test Header");
	}

	[Theory]
	[InlineData("1", "h1", "text-2xl")]
	[InlineData("2", "h2", "text-xl")]
	[InlineData("3", "h3", "text-lg")]
	[InlineData("4", "h4", "text-base")]
	[InlineData("5", "h5", "text-sm")]
	public void HeaderComponent_WithLevel_RendersCorrectHeadingElement(string level, string tag, string sizeClass)
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, "Header Text")
			.Add(c => c.Level, level));

		// Assert
		var heading = cut.Find(tag);
		heading.TextContent.Should().Be("Header Text");
		heading.GetAttribute("class").Should().Contain("heading-page");
		heading.GetAttribute("class").Should().Contain(sizeClass);
	}

	[Fact]
	public void HeaderComponent_WithDescription_RendersDescriptionParagraph()
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, "Header")
			.Add(c => c.Description, "A description"));

		// Assert
		cut.Find("p").TextContent.Should().Be("A description");
	}

	[Fact]
	public void HeaderComponent_WithoutDescription_DoesNotRenderParagraph()
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, "Header"));

		// Assert
		cut.FindAll("p").Should().BeEmpty();
	}

	[Fact]
	public void HeaderComponent_EmptyHeaderText_RendersNothing()
	{
		// Act
		var cut = Render<HeaderComponent>(p => p
			.Add(c => c.HeaderText, ""));

		// Assert
		cut.FindAll("header").Should().BeEmpty();
	}
}
