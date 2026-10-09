using System.Threading.Tasks;

// Keep the existing audit entry point after moving upgrades out of map buttons.
public static class ShelterButtonsAudit
{
    public static Task RunPlay() => ShelterInteractionsAudit.RunPlay();
}
