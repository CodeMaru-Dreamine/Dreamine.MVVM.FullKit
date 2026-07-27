using WeddingPlatform.Models;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class HeroImageCropTests
{
    [Fact]
    public void NormalizePreservesValidIndependentDesktopAndMobileCrops()
    {
        var config = new TenantConfig();
        config.DesignSettings.HeroImagePresentation = new HeroImagePresentationSettings
        {
            DesktopFit = HeroImagePresentationSettings.Cover,
            MobileFit = HeroImagePresentationSettings.Contain,
            DesktopCrop = new HeroImageCropRegion
            {
                X = 10,
                Y = 20,
                Width = 60,
                Height = 40,
            },
            MobileCrop = new HeroImageCropRegion
            {
                X = 30,
                Y = 5,
                Width = 45,
                Height = 80,
            },
        };

        InvitationDesignCatalog.Normalize(config);

        var presentation = config.DesignSettings.HeroImagePresentation;
        Assert.Equal(HeroImagePresentationSettings.Cover, presentation.DesktopFit);
        Assert.Equal(HeroImagePresentationSettings.Contain, presentation.MobileFit);
        Assert.Equal((10d, 20d, 60d, 40d), CropTuple(presentation.DesktopCrop));
        Assert.Equal((30d, 5d, 45d, 80d), CropTuple(presentation.MobileCrop));
    }

    [Fact]
    public void NormalizeRepairsInvalidFitAndOutOfBoundsCrop()
    {
        var config = new TenantConfig();
        config.DesignSettings.HeroImagePresentation = new HeroImagePresentationSettings
        {
            DesktopFit = "invalid",
            MobileFit = HeroImagePresentationSettings.Cover,
            DesktopCrop = new HeroImageCropRegion
            {
                X = -20,
                Y = 90,
                Width = 140,
                Height = 70,
            },
            MobileCrop = new HeroImageCropRegion
            {
                X = double.NaN,
                Y = double.PositiveInfinity,
                Width = double.NaN,
                Height = double.NegativeInfinity,
            },
        };

        InvitationDesignCatalog.Normalize(config);

        var presentation = config.DesignSettings.HeroImagePresentation;
        Assert.Equal(HeroImagePresentationSettings.Contain, presentation.DesktopFit);
        Assert.Equal((0d, 90d, 100d, 10d), CropTuple(presentation.DesktopCrop));
        Assert.Equal((0d, 0d, 100d, 100d), CropTuple(presentation.MobileCrop));
    }

    private static (double X, double Y, double Width, double Height) CropTuple(
        HeroImageCropRegion crop) =>
        (crop.X, crop.Y, crop.Width, crop.Height);
}
