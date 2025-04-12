using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CheeseHealthPacks;

[BepInPlugin("NoSpawnn.CheeseHealthPacks", "CheeseHealthPacks", "1.0")]
public class CheeseHealthPacks : BaseUnityPlugin
{
    internal static CheeseHealthPacks Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private static Mesh _cheesePackMeshSmall = null!;
    private static Mesh _cheesePackMeshMedium = null!;
    private static Mesh _cheesePackMeshLarge = null!;
    private static Material _cheeseMaterial = null!;

    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        gameObject.transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        string assetBundleName = "assets";
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), assetBundleName));
        if (assetBundle is null)
        {
            Logger.LogError($"Failed to load asset bundle '{assetBundleName}'");
            return;
        }

        string cheesePackSmallAssetName = "small";
        GameObject cheesePackSmallAsset = assetBundle.LoadAsset<GameObject>(cheesePackSmallAssetName);
        if (cheesePackSmallAsset is null)
        {
            Logger.LogError($"Failed to load small asset ('{cheesePackSmallAssetName}')");
            return;
        }
        _cheesePackMeshSmall = cheesePackSmallAsset.GetComponentInChildren<MeshFilter>().mesh;

        string cheesePackMediumAssetName = "medium";
        var cheesePackMediumAsset = assetBundle.LoadAsset<GameObject>(cheesePackMediumAssetName);
        if (cheesePackMediumAsset is null)
        {
            Logger.LogError($"Failed to load medium asset ('{cheesePackMediumAssetName}')");
            return;
        }
        _cheesePackMeshMedium = cheesePackMediumAsset.GetComponentInChildren<MeshFilter>().mesh;

        string cheesePackLargeAssetName = "large";
        GameObject cheesePackLargeAsset = assetBundle.LoadAsset<GameObject>(cheesePackLargeAssetName);
        if (cheesePackLargeAsset is null)
        {
            Logger.LogError($"Failed to load large asset ('{cheesePackLargeAssetName}')");
            return;
        }
        _cheesePackMeshLarge = cheesePackLargeAsset.GetComponentInChildren<MeshFilter>().mesh;
        _cheeseMaterial = cheesePackLargeAsset.GetComponentInChildren<MeshRenderer>().sharedMaterial;

        if (_cheeseMaterial is null)
        {
            Logger.LogError("Failed to load cheese material");
            return;
        }

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private static readonly Quaternion _meshRot = Quaternion.Euler(-90f, 0f, 0f);

    /* Large */
    private static readonly Vector3 _colliderSizeLarge = new(1.29f, 0.61f, 2.23f);
    private static readonly Vector3 _meshPosLarge = new(0f, 0.15f, 0f);
    private static readonly Vector3 _meshScaleLarge = new(26f, 26f, 26f);

    /* Medium */
    private static readonly Vector3 _colliderSizeMedium = new(0.56f, 0.57f, 1.53f);
    private static readonly Vector3 _colliderCenterMedium = new(0f, -0.04f, 0.12f);
    private static readonly Vector3 _meshPosMedium = new(0f, 0.111f, -0.052f);
    private static readonly Vector3 _meshScaleMedium = new(15f, 15f, 15f);

    /* Small */
    private static readonly Vector3 _colliderSizeSmall = new(0.92f, 0.15f, 1.6f);
    private static readonly Vector3 _meshPosSmall = new(0, 0.11f, 0f);
    private static readonly Vector3 _meshScaleSmall = new(13f, 13f, 13f);

    public void PatchMeshFor(ItemHealthPack healthPack)
    {
        var meshToModify = healthPack.transform.Find("Mesh");
        if (meshToModify is null)
        {
            Logger.LogError("Could not find mesh in tree, is this a custom health pack?");
            return;
        }

        var boxCollider = healthPack.transform.Find("Semi Box Collider").GetComponent<BoxCollider>();
        if (boxCollider is null)
        {
            Logger.LogError("Could not find box collider in tree, is this a custom health pack?");
            return;
        }

        var filter = meshToModify.GetComponent<MeshFilter>();
        var renderer = meshToModify.GetComponent<MeshRenderer>();

        filter.transform.rotation = _meshRot;
        renderer.material = _cheeseMaterial;

        if (healthPack.healAmount == 100) // Large
        {
            meshToModify.transform.localScale = _meshScaleLarge;
            meshToModify.transform.localPosition = _meshPosLarge;
            filter.mesh = _cheesePackMeshLarge;
            boxCollider.size = _colliderSizeLarge;
        }
        else if (healthPack.healAmount == 50) // Medium
        {
            meshToModify.transform.localScale = _meshScaleMedium;
            meshToModify.transform.localPosition = _meshPosMedium;
            filter.mesh = _cheesePackMeshMedium;
            boxCollider.center = _colliderCenterMedium;
            boxCollider.size = _colliderSizeMedium;
        }
        else if (healthPack.healAmount == 25) // Small
        {
            meshToModify.transform.localScale = _meshScaleSmall;
            meshToModify.transform.localPosition = _meshPosSmall;
            filter.mesh = _cheesePackMeshSmall;
            boxCollider.size = _colliderSizeSmall;
        }
    }
}

// Health Pack <Small | Medium | Large>
[HarmonyPatch(typeof(ItemHealthPack))]
public class PatchHealthPackMesh
{
    [HarmonyPostfix, HarmonyPatch(nameof(ItemHealthPack.Start))]
    public static void Postfix(ItemHealthPack __instance)
    {
        if (__instance is null)
        {
            CheeseHealthPacks.Logger.LogError("ItemHealthPack instance is null");
            return;
        }

        CheeseHealthPacks.Instance.PatchMeshFor(__instance);
    }
}