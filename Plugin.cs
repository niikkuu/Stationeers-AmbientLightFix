using Assets.Scripts.GridSystem;
using Assets.Scripts;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;

namespace AmbientLightFix;

[BepInPlugin("Nikku.AmbientLightFix", "AmbientLightFix", "1.0.2")]
public class Plugin : BaseUnityPlugin
{
    private static Harmony harmony;

    public static ConfigEntry<float> minAmbientLight;
    public static ConfigEntry<float> maxAmbientLight;

    private void Awake()
    {
        minAmbientLight = Config.Bind("Brightness",
            "MinAmbientLight",
            0.2f,
            "The minimum amount of ambient light visible at night. Recommended value is 0.2, vanilla Stationeers is 0.1");
        maxAmbientLight = Config.Bind("Brightness",
            "MaxAmbientLight",
            0.5f,
            "The maximum amount of ambient light visible during daytime. Recommended value is 0.5, vanilla Stationeers is 0.15");

        harmony = new Harmony("Nikku.AmbientLightFix");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(AtmosphericScattering), nameof(AtmosphericScattering.ManagerUpdate))]
class PatchLightingUpdate
{
    public static int counter = 0;
    static void Postfix()
    {
        if (GameManager.GameState != GameState.Running || WorldManager.IsGamePaused)
        {
            return;
        }

        // There is no sun, exit
        if (!OrbitalSimulation.WorldSun)
        {
            return;
        }

        float sunYpos = OrbitalSimulation.WorldSunVector.y;
        float sunAmount = 0f;

        // If there is no atmosphere, sunAmount should become 0 exactly when the sun goes below the horizon
        if (!WorldManager.AtmosphericScattering)
        {
            sunAmount = sunYpos - OrbitalSimulation.EclipseRatio;
        }
        // If there is an atmospere, we start increasing sunAmount when it is still a bit under the horizon, mimicking atmospheric scattering
        else
        {
            sunAmount = (sunYpos + 0.2f) - OrbitalSimulation.EclipseRatio;
        }

        RenderSettings.ambientIntensity = Mathf.Lerp(Plugin.minAmbientLight.Value, Plugin.maxAmbientLight.Value, sunAmount);

        //Debugging
        //counter++;
        //if(counter > 30)
        //{
        //    counter = 0;
        //    //Debug.Log($"Eclipse Ratio: {OrbitalSimulation.EclipseRatio:F2}");
        //    Debug.Log($"Sun Y: {sunYpos:F1}");
        //    Debug.Log(RenderSettings.ambientIntensity);
        //}
    }
}
