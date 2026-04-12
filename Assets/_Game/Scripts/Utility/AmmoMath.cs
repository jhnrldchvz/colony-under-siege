using UnityEngine;

public static class AmmoMath
{
    public static bool CanUseAmmo(int currentAmmo) => currentAmmo > 0;

    public static int AddAmmoToReserve(int current, int amount, int maxReserve)
        => Mathf.Min(current + amount, maxReserve);

    public static (int newMagazine, int newReserve) CalculateReload(
        int currentAmmo, int magazineSize, int reserve)
    {
        int needed   = magazineSize - currentAmmo;
        int transfer = Mathf.Min(needed, reserve);
        return (currentAmmo + transfer, reserve - transfer);
    }
}
