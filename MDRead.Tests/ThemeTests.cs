using MDRead.Constants;

namespace MDRead.Tests;

/// <summary>
/// Verifies theme selection and immutable brush creation.
/// </summary>
public sealed class ThemeTests
{
    [Fact]
    public void Select_ReturnsRequestedPalette()
    {
        Assert.Same(ThemePalette.Dark, ThemePalette.Select(true));
        Assert.Same(ThemePalette.Light, ThemePalette.Select(false));
    }

    [Fact]
    public void Create_ReturnsFrozenBrushWithExpectedColor()
    {
        var brush = ThemeBrushFactory.Create("#123456");

        Assert.True(brush.IsFrozen);
        Assert.Equal(0x12, brush.Color.R);
        Assert.Equal(0x34, brush.Color.G);
        Assert.Equal(0x56, brush.Color.B);
    }
}
