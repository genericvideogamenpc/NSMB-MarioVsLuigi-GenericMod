using UnityEngine;

[CreateAssetMenu(fileName = "Powerup", menuName = "ScriptableObjects/Powerup", order = 0)]
public class Powerup : ScriptableObject {

    public Enums.PowerupState state;
    public string prefab;
    public Enums.Sounds soundEffect = Enums.Sounds.Player_Sound_PowerupCollect;
    public float spawnChance = 0.1f, losingSpawnBonus = 0f;
    public bool big, vertical, custom, modcustom, lives, forBattle;
    public Sprite reserveSprite;

    public float GetModifiedChance(float starsToWin, float leaderStars, float ourStars) {
        float starDifference = leaderStars - ourStars;
        float bonus = losingSpawnBonus * Mathf.Log(starDifference + 1) * (1f - ((starsToWin - leaderStars) / starsToWin));
        if (GameManager.Instance.gamemodeIndex == 1)
        {
            if (forBattle)
            {
                return Mathf.Max(0, spawnChance + bonus);
            }
            else
            {
                if (name == "CloudFlower") // Disabled in deathmatch
                {
                    return 0;
                }
                return Mathf.Max(0, spawnChance/2.5f + bonus * 1.8f);

            }
        }
        else
        {
            return Mathf.Max(0, spawnChance + bonus);
        }
    }
}