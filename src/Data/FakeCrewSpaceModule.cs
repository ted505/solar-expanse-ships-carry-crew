using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;

namespace CrewCapacityMod.Data;

internal class FakeCrewSpaceModule : SpaceModuleFake
{
    public FakeCrewSpaceModule(FacilityBaseDescriptor facilityDescriptor, ObjectInfoData objectInfoData, int id)
        : base(facilityDescriptor, objectInfoData, id)
    {
    }

    public override void Scrap(long howMuch = 1, bool addResourceOnScrap = true)
    {
        UnSelectFromAllDropDowns();
    }
}
