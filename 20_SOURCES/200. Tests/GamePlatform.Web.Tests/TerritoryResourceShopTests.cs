using GamePlatform.Domain;
namespace GamePlatform.Web.Tests;
public class TerritoryResourceShopTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void PurchasePersistsAndRetriesNeverChargeTwice(int resource)
    {
        var p=new GameProgress { PremiumCurrency=100 }; var s=p.Territory;
        var before=TerritoryResourceShopRules.Balance(s,resource); var id=Guid.NewGuid().ToString("N");
        Assert.True(TerritoryResourceShopRules.Buy(s,p,resource,0,id).Success);
        Assert.Equal(90,p.PremiumCurrency); Assert.Equal(before+1000,TerritoryResourceShopRules.Balance(s,resource));
        p.Territory=TerritoryRules.Clone(s); p.Territory.Reports.Clear();
        Assert.True(TerritoryResourceShopRules.Buy(p.Territory,p,resource,0,id).Success); Assert.Equal(90,p.PremiumCurrency);
        Assert.False(TerritoryResourceShopRules.Buy(p.Territory,p,resource,1,id).Success); Assert.Equal(90,p.PremiumCurrency);
    }
    [Fact]
    public void InsufficientCoinsCapacityAndInvalidRequestsDoNotDeduct()
    {
        var p=new GameProgress { PremiumCurrency=100 }; var s=p.Territory;
        s.Food=TerritoryRules.StorageCapacity(s)-999;
        Assert.False(TerritoryResourceShopRules.Buy(s,p,0,0,Guid.NewGuid().ToString()).Success); Assert.Equal(100,p.PremiumCurrency);
        Assert.False(TerritoryResourceShopRules.Buy(s,p,-1,0,Guid.NewGuid().ToString()).Success);
        Assert.False(TerritoryResourceShopRules.Buy(s,p,1,9,Guid.NewGuid().ToString()).Success);
        Assert.False(TerritoryResourceShopRules.Buy(s,p,1,0,"invalid").Success);
        p.PremiumCurrency=9; Assert.False(TerritoryResourceShopRules.Buy(s,p,1,0,Guid.NewGuid().ToString()).Success);
        Assert.Equal(9,p.PremiumCurrency); Assert.Empty(s.ResourcePurchaseReceipts);
    }
    [Fact]
    public void ExactCapacityPurchaseSucceeds()
    {
        var p=new GameProgress { PremiumCurrency=10 }; var s=p.Territory; s.Wood=TerritoryRules.StorageCapacity(s)-1000;
        Assert.True(TerritoryResourceShopRules.Buy(s,p,1,0,Guid.NewGuid().ToString()).Success);
        Assert.Equal(TerritoryRules.StorageCapacity(s),s.Wood); Assert.Equal(0,p.PremiumCurrency);
    }
}
