using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 一键给苏州博物馆提取出的 Material 挂上对应的 PBR 贴图。
/// 菜单: Tools > Setup Suzhou Museum Materials
/// </summary>
public class SuzhouMuseumMaterialSetup : EditorWindow
{
    // ── 文件夹路径 ──
    private const string BasePath        = "Assets/CGTrader/SuzhouMuseum";
    private const string ExteriorPath    = "Assets/CGTrader/SuzhouMuseum/Exterior ground";
    private const string GraniteShinyPath= "Assets/CGTrader/SuzhouMuseum/GRANITE/Granite tiles ground shiny";
    private const string TilesPath       = "Assets/CGTrader/SuzhouMuseum/GRANITE/Granite tiles ground shiny/Tiles";
    private const string GreyRectPath    = "Assets/CGTrader/SuzhouMuseum/GRANITE/Grey stone ground rect tiles";
    private const string GreyNoTilePath  = "Assets/CGTrader/SuzhouMuseum/GRANITE/Grey stone no tile";
    private const string GreyTileFloorPath="Assets/CGTrader/SuzhouMuseum/GRANITE/Grey stone tile floor";
    private const string StuccoPath      = "Assets/CGTrader/SuzhouMuseum/White Stucco walls";
    private const string WoodPath        = "Assets/CGTrader/SuzhouMuseum/Wood solar screen";

    // ── 材质 → 贴图 映射 ──
    // BaseTint: 暖色调 → 苏州博物馆"粉墙黛瓦+暖灰花岗岩"配色
    private static readonly Dictionary<string, MaterialMap> MaterialMappings = new()
    {
        // ═══ 室外地面 — 暖灰色花岗岩 ═══
        ["Exterior_ground_tiles"]  = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },
        ["Exterior_ground_tiles1"] = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },
        ["Exterior_ground_tiles2"] = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },
        ["Exterior_ground_tiles3"] = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },
        ["Exterior_ground_tiles4"] = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },
        ["Exterior_ground_tiles5"] = new() { Folder = ExteriorPath, Diffuse = "exterior diffuse", Normal = "exterior normal", Roughness = "exterior roughness", BaseTint = new(0.97f, 0.96f, 0.93f), Smoothness = 0.22f },

        // ═══ 花岗岩亮面地砖 — 暖灰 ═══
        ["Granite_grey_tiles"]  = new() { Folder = GraniteShinyPath, NormalFolder = TilesPath, Diffuse = "shiny square Diffuse", Normal = "4K_Normal", Roughness = "shiny square Roughness", BaseTint = new(0.96f, 0.94f, 0.91f), Smoothness = 0.30f },
        ["Granite_grey_tiles1"] = new() { Folder = GraniteShinyPath, NormalFolder = TilesPath, Diffuse = "whgjbcdl_4K_Albedo",    Normal = "4K_Normal", Roughness = "whgjbcdl_4K_Roughness",    BaseTint = new(0.96f, 0.94f, 0.91f), Smoothness = 0.28f },
        ["Granite_grey_tiles2"] = new() { Folder = TilesPath,         Diffuse = "4K_Albedo",             Normal = "4K_Normal", Roughness = "4K_Roughness",               BaseTint = new(0.96f, 0.94f, 0.91f), Smoothness = 0.28f },
        ["Dark_shiny_tile"]     = new() { Folder = GraniteShinyPath, NormalFolder = TilesPath, Diffuse = "Shiny darker diffuse",  Normal = "4K_Normal", Roughness = "4K_Roughness",               BaseTint = new(0.78f, 0.76f, 0.73f), Smoothness = 0.35f, DisableNormal = true },

        // ═══ 灰色长方形地砖 — 暖灰 ═══
        ["Grey_ston_rect_tiles"]  = new() { Folder = GreyRectPath, Diffuse = "rectangle Diffuse", Normal = "rectangle Normal", Roughness = "rectangle Roughness darker", BaseTint = new(0.95f, 0.93f, 0.90f), Smoothness = 0.25f },
        ["Grey_ston_rect_tiles1"] = new() { Folder = GreyRectPath, Diffuse = "rectangle Diffuse", Normal = "rectangle Normal", Roughness = "rectangle Roughness darker", BaseTint = new(0.95f, 0.93f, 0.90f), Smoothness = 0.25f },

        // ═══ 灰色无缝石材（深灰褐 — 用于屋顶/框架） ═══
        ["Dark_stone_no_tile"]        = new() { Folder = GreyNoTilePath,   Diffuse = "no tile Diffuse", Normal = "no tile Normal", Roughness = "no tile Roughness",                   BaseTint = new(0.80f, 0.78f, 0.75f), Smoothness = 0.22f, DisableNormal = true },
        ["Grey_stone_no_tile_Shanxi"] = new() { Folder = GreyTileFloorPath, Diffuse = "shan xi tile Diffuse Clearer saturated", Normal = "shan xi tile Normal", Roughness = "shan xi tile Roughness", BaseTint = new(0.94f, 0.92f, 0.89f), Smoothness = 0.28f },

        // ═══ 白色灰泥墙 — 暖白 ═══
        ["White_Stucco_walls"] = new() { Folder = StuccoPath, Diffuse = "Albedo clearer", Normal = "4K_Normal", Roughness = "4K_Roughness", BaseTint = new(1.00f, 0.99f, 0.97f), Smoothness = 0.12f },

        // ═══ 白色填缝地砖 — 暖白 ═══
        ["White_joint_tiles"] = new() { Folder = TilesPath, Diffuse = "Albedo white joints", Normal = "4K_Normal", Roughness = "4K_Roughness", BaseTint = new(0.99f, 0.98f, 0.95f), Smoothness = 0.22f },

        // ═══ 木格栅 ═══
        ["Wood_solar_screen"] = new() { Folder = WoodPath, Diffuse = "4K_Albedo", Normal = "4K_Normal", Roughness = "4K_Roughness", AO = "4K_AO", BaseTint = new(1.00f, 0.96f, 0.88f), Smoothness = 0.18f },
    };

    // ═══════════════════════════════════════════════════════════════
    // 纯色 / 金属 / 玻璃材质 — 基于苏州博物馆真实参考图配色
    // 参考：墙=暖白 #F5F2ED, 屋顶/框=暖深灰 #504B46, 地面=暖灰花岗岩 #8C8A87
    // ═══════════════════════════════════════════════════════════════
    private static readonly Dictionary<string, SimpleMaterial> SimpleMaterials = new()
    {
        // 暖白色（墙体主色，参考贝聿铭粉墙黛瓦的"粉墙"）
        ["FrontColor"]              = new() { Color = new Color(0.96f, 0.95f, 0.93f),    Smoothness = 0.15f, Metallic = 0f },

        // 深灰褐色（屋顶瓦片/结构框架，"黛瓦"色）
        ["Pure_black"]              = new() { Color = new Color(0.28f, 0.27f, 0.25f),    Smoothness = 0.25f, Metallic = 0f },

        // 深灰褐色金属（表面氟碳喷涂铝板/钢框架）
        ["Aluminium_01__inoxydable"]= new() { Color = new Color(0.32f, 0.30f, 0.28f),    Smoothness = 0.40f, Metallic = 0.80f },
        ["Aluminium_grey_metal"]    = new() { Color = new Color(0.36f, 0.34f, 0.32f),    Smoothness = 0.35f, Metallic = 0.75f },
        ["Steel_inox"]              = new() { Color = new Color(0.30f, 0.28f, 0.26f),    Smoothness = 0.45f, Metallic = 0.85f },

        // 暖光内透（展厅内部灯光）
        ["Light_in_display"]        = new() { Color = new Color(0.98f, 0.94f, 0.85f),    Smoothness = 0.25f, Metallic = 0f,
                                              Emission = new Color(0.60f, 0.50f, 0.35f) },

        // 半透明磨砂玻璃（天窗/隔断）
        ["Translucent_glass"]       = new() { Color = new Color(0.78f, 0.85f, 0.90f, 0.30f), Smoothness = 0.98f, Metallic = 0.05f,
                                              SurfaceType = 1, RenderType = "Transparent" },

        // 高反射清玻（入口玻璃幕墙/转门）
        ["Glass"]                   = new() { Color = new Color(0.72f, 0.80f, 0.86f, 0.22f), Smoothness = 1.00f, Metallic = 0.10f,
                                              SurfaceType = 1, RenderType = "Transparent" },
    };

    // ═══════════════════════════════════════════════════════════════
    // EditorWindow UI（可选手动界面）
    // ═══════════════════════════════════════════════════════════════
    [MenuItem("Tools/Setup Suzhou Museum Materials")]
    public static void ShowWindow() => GetWindow<SuzhouMuseumMaterialSetup>("Suzhou Museum 贴图设置");

    private void OnGUI()
    {
        GUILayout.Label("苏州博物馆 — 材质贴图批量设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label($"已配置 {MaterialMappings.Count} 个 PBR 材质 + {SimpleMaterials.Count} 个纯色材质");
        GUILayout.Space(10);
        if (GUILayout.Button("一键挂贴图", GUILayout.Height(40)))
        {
            ApplyAll();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 核心逻辑
    // ═══════════════════════════════════════════════════════════════
    [MenuItem("Tools/Setup Suzhou Museum Materials/Apply All", priority = 1)]
    public static void ApplyAll()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[SuzhouMuseum] ❌ 找不到 URP/Lit Shader！请确认项目使用 Universal Render Pipeline。");
            EditorUtility.DisplayDialog("错误", "找不到 URP/Lit Shader！请确认项目使用 Universal Render Pipeline。", "OK");
            return;
        }

        int done = 0;
        int skipped = 0;
        int created = 0;

        // 确保 SuzhouMuseum 文件夹存在
        EnsureFolderExists(BasePath);

        // 1) 处理 PBR 贴图材质
        foreach (var kv in MaterialMappings)
        {
            string matName = kv.Key;
            MaterialMap map = kv.Value;
            string assetPath = $"{BasePath}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            // 如果材质不存在，创建一个新的
            if (mat == null)
            {
                mat = new Material(urpLit) { name = matName };
                AssetDatabase.CreateAsset(mat, assetPath);
                created++;
                Debug.Log($"[SuzhouMuseum] 🆕 创建材质: {assetPath}");
            }

            // 确保使用 URP/Lit
            if (mat.shader != urpLit)
                mat.shader = urpLit;

            // BaseColor 色调（暖色偏移 → 苏州博物馆粉墙黛瓦配色）
            mat.SetColor("_BaseColor", map.BaseTint);

            // 分配贴图
            var normalFolder = string.IsNullOrEmpty(map.NormalFolder) ? map.Folder : map.NormalFolder;
            AssignTexture(mat, map, "_BaseMap",      map.Diffuse,      map.Folder);
            AssignTexture(mat, map, "_BumpMap",      map.Normal,       normalFolder);
            AssignTexture(mat, map, "_OcclusionMap", map.AO,           map.Folder);
            AssignTexture(mat, map, "_ParallaxMap",  map.Displacement, map.Folder);

            // PBR 反射 / 光滑度
            mat.SetFloat("_Smoothness", map.Smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_GlossyReflections", 1);      // 开启镜面反射
            mat.SetFloat("_EnvironmentReflections", 1); // 开启环境反射
            mat.SetFloat("_SpecularHighlights", 1);     // 开启高光
            mat.SetFloat("_GlossMapScale", 1);
            mat.SetFloat("_BumpScale", map.BumpScale);

            // 屋顶材料禁用法线防止摩尔纹
            if (map.DisableNormal)
            {
                mat.SetTexture("_BumpMap", null);
                mat.DisableKeyword("_NORMALMAP");
            }
            else
            {
                SetKeyword(mat, "_NORMALMAP", mat.GetTexture("_BumpMap") != null);
            }
            SetKeyword(mat, "_OCCLUSIONMAP", mat.GetTexture("_OcclusionMap") != null);
            SetKeyword(mat, "_PARALLAXMAP",  mat.GetTexture("_ParallaxMap") != null);

            // Opaque
            mat.SetFloat("_Surface", 0);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = -1;

            EditorUtility.SetDirty(mat);
            done++;
            Debug.Log($"[SuzhouMuseum] ✓ {matName}");
        }

        // 2) 处理纯色 / 金属 / 玻璃材质
        foreach (var kv in SimpleMaterials)
        {
            string matName = kv.Key;
            SimpleMaterial sm = kv.Value;
            string assetPath = $"{BasePath}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(urpLit) { name = matName };
                AssetDatabase.CreateAsset(mat, assetPath);
                created++;
                Debug.Log($"[SuzhouMuseum] 🆕 创建材质: {assetPath}");
            }

            if (mat.shader != urpLit)
                mat.shader = urpLit;

            mat.SetColor("_BaseColor", sm.Color);
            mat.SetFloat("_Smoothness", sm.Smoothness);
            mat.SetFloat("_Metallic", sm.Metallic);

            SetKeyword(mat, "_EMISSION", sm.Emission != Color.black);
            if (sm.Emission != Color.black)
                mat.SetColor("_EmissionColor", sm.Emission);

            // Surface Type: 0=Opaque, 1=Transparent
            mat.SetFloat("_Surface", sm.SurfaceType);
            mat.SetOverrideTag("RenderType", sm.RenderType);
            mat.renderQueue = sm.SurfaceType == 1 ? 3000 : -1;

            if (sm.SurfaceType == 1)
            {
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
            }
            // 开启反射（玻璃需高反射）
            mat.SetFloat("_GlossyReflections", 1);
            mat.SetFloat("_EnvironmentReflections", 1);
            mat.SetFloat("_SpecularHighlights", 1);
            mat.SetFloat("_GlossMapScale", 1);

            EditorUtility.SetDirty(mat);
            done++;
            Debug.Log($"[SuzhouMuseum] ✓ {matName} (纯色)");
        }

        // 3) 更新 FBX 的材质 remapping（让新建的材质自动挂到模型上）
        RemapFbxMaterials();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"材质设置完成！\n成功: {done}\n跳过: {skipped}";
        if (created > 0) msg += $"\n新建: {created}";
        Debug.Log($"[SuzhouMuseum] ✅ {msg}");
        EditorUtility.DisplayDialog("完成", msg, "OK");
    }

    /// <summary>
    /// 更新苏州博物馆 FBX 的材质映射，让所有 Material 指向 SuzhouMuseum 文件夹下的 .mat 文件
    /// </summary>
    private static void RemapFbxMaterials()
    {
        string fbxPath = $"{BasePath}/uploads_files_4690857_Suzhou+Museum+Ensemble+entrance+hall+solo+VF.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[SuzhouMuseum] ⚠ 找不到 FBX 文件，跳过 remapping");
            return;
        }

        // 收集 SuzhouMuseum 下所有材质（按名称索引）
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { BasePath });
        var matDict = new Dictionary<string, Material>();
        foreach (string guid in matGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) matDict[m.name] = m;
        }

        // 合并 PBR 和 Simple 两个列表的所有材质名
        var allNames = new HashSet<string>();
        foreach (var k in MaterialMappings.Keys) allNames.Add(k);
        foreach (var k in SimpleMaterials.Keys)  allNames.Add(k);

        bool changed = false;
        foreach (string matName in allNames)
        {
            if (!matDict.TryGetValue(matName, out Material mat)) continue;

            var sourceId = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
            importer.AddRemap(sourceId, mat);
            changed = true;
            Debug.Log($"[SuzhouMuseum] 🔗 Remap: {matName} → {mat.name}.mat");
        }

        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log("[SuzhouMuseum] 🔗 FBX 材质 remapping 已更新并重新导入");
        }
    }

    private static void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled) mat.EnableKeyword(keyword);
        else         mat.DisableKeyword(keyword);
    }

    /// <summary>确保 Asset 文件夹存在</summary>
    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    /// <summary>
    /// 按文件名关键词模糊查找贴图，赋给材质的指定属性
    /// </summary>
    private static void AssignTexture(Material mat, MaterialMap map, string propName, string keyword, string folder)
    {
        if (string.IsNullOrEmpty(keyword)) return;

        Texture2D tex = FindTexture(keyword, folder);
        if (tex != null)
        {
            mat.SetTexture(propName, tex);
        }
        else
        {
            Debug.LogWarning($"[SuzhouMuseum]   ⚠ 找不到贴图: '{keyword}' 在 {folder}");
        }
    }

    /// <summary>
    /// 在指定文件夹中按文件名包含关键词查找贴图
    /// </summary>
    private static Texture2D FindTexture(string keyword, string folder)
    {
        string kw = keyword.ToLowerInvariant();

        // 优先精确匹配
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (name.Contains(kw))
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // 数据结构
    // ═══════════════════════════════════════════════════════════════
    private class MaterialMap
    {
        public string Folder;       // Diffuse 的搜索文件夹（默认）
        public string NormalFolder; // Normal 的搜索文件夹（可选，默认同 Folder）
        public string Diffuse;      // Base Color 关键词
        public string Normal;
        public string Roughness;
        public string AO;
        public string Displacement;
        public Color BaseTint = Color.white; // 贴图色调（默认白=不调色）
        public float Smoothness = 0.35f;     // 光滑度
        public float BumpScale = 0.4f;       // 法线强度（屋顶材料应设为0防止摩尔纹）
        public bool DisableNormal;           // 完全禁用Normal Map（用于屋顶等）
    }

    private class SimpleMaterial
    {
        public Color Color = Color.white;
        public float Smoothness = 0.3f;
        public float Metallic = 0f;
        public Color Emission = Color.black;
        public string RenderType = "Opaque";
        public int SurfaceType = 0; // 0=Opaque, 1=Transparent
    }
}
