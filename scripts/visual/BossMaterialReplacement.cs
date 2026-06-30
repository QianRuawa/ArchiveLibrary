using Godot;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 难度感知材质提供接口，根据当前难度返回材质路径。
/// </summary>
public interface IDifficultyMaterialProvider
{
    /// <summary>根据当前难度返回材质资源路径。</summary>
    string GetMaterialPathForDifficulty();

    /// <summary>返回需要替换材质的 MeshInstance3D 节点名称。</summary>
    string GetMeshNodeName();
}

/// <summary>
/// Boss 材质替换辅助工具，支持难度感知、递归查找、批量替换。
/// </summary>
public static class DifficultyMaterialHelper
{
    private static readonly Dictionary<string, Material> MaterialCache = new();

    /// <summary>延迟执行材质替换（使用 CallDeferred），适合在 _Ready 中调用。</summary>
    public static void ApplyDifficultyMaterial(this Node node, IDifficultyMaterialProvider provider)
    {
        Callable.From(() => ApplyMaterialInternal(node, provider)).CallDeferred();
    }

    private static void ApplyMaterialInternal(Node node, IDifficultyMaterialProvider provider)
    {
        var meshNode = FindMeshInstanceRecursive(node, provider.GetMeshNodeName());
        if (meshNode == null)
        {
            LibraryLogger.Error($"找不到Mesh节点: >{provider.GetMeshNodeName()}");
            return;
        }

        var path = provider.GetMaterialPathForDifficulty();
        var material = GetMaterial(path);
        if (material != null)
        {
            var materialCopy = material.Duplicate() as Material;
            meshNode.SetSurfaceOverrideMaterial(0, materialCopy);
            LibraryLogger.Info($"材质已替换: >{path}");
        }
        else
            LibraryLogger.Error($"无法加载材质: >{path}");
    }

    /// <summary>从缓存加载材质，避免重复加载。</summary>
    private static Material GetMaterial(string path)
    {
        if (MaterialCache.TryGetValue(path, out var cached))
            return cached;
        var mat = ResourceLoader.Load<Material>(path);
        if (mat != null)
            MaterialCache[path] = mat;
        return mat;
    }

    /// <summary>递归查找指定名称的 MeshInstance3D 节点。</summary>
    private static MeshInstance3D FindMeshInstanceRecursive(Node node, string name)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.Name == name)
                return mesh;
            var found = FindMeshInstanceRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>递归获取节点下所有 MeshInstance3D 节点。</summary>
    public static List<MeshInstance3D> GetAllMeshInstancesRecursive(Node node)
    {
        var list = new List<MeshInstance3D>();
        GetAllMeshInstancesRecursiveInternal(node, list);
        return list;
    }

    private static void GetAllMeshInstancesRecursiveInternal(Node node, List<MeshInstance3D> list)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D mesh)
                list.Add(mesh);
            GetAllMeshInstancesRecursiveInternal(child, list);
        }
    }

    /// <summary>替换指定节点下所有 MeshInstance3D 的材质（替换第一个表面）。</summary>
    public static void ApplyMaterialToAllMeshInstances(Node root, string materialPath)
    {
        var meshList = GetAllMeshInstancesRecursive(root);
        var material = GetMaterial(materialPath);
        if (material == null)
        {
            LibraryLogger.Error($"无法加载材质: >{materialPath}");
            return;
        }
        foreach (var mesh in meshList)
        {
            var matCopy = material.Duplicate() as Material;
            mesh.SetSurfaceOverrideMaterial(0, matCopy);
        }
        LibraryLogger.Info($"已替换 {meshList.Count} 个 MeshInstance3D 的材质（路径: {materialPath}）");
    }

    /// <summary>替换指定列表中的 MeshInstance3D 材质（替换第一个表面）。</summary>
    public static void ApplyMaterialToMeshList(List<MeshInstance3D> meshList, string materialPath)
    {
        var material = GetMaterial(materialPath);
        if (material == null)
        {
            LibraryLogger.Error($"无法加载材质: >{materialPath}");
            return;
        }
        foreach (var mesh in meshList)
        {
            var matCopy = material.Duplicate() as Material;
            mesh.SetSurfaceOverrideMaterial(0, matCopy);
        }
        LibraryLogger.Info($"已替换列表中的 {meshList.Count} 个 MeshInstance3D 材质（路径: {materialPath}）");
    }

    /// <summary>替换节点下所有 MeshInstance3D 材质（通过 provider 获取路径）。</summary>
    public static void ApplyMaterialToAllMeshInstances(Node root, IDifficultyMaterialProvider provider)
    {
        ApplyMaterialToAllMeshInstances(root, provider.GetMaterialPathForDifficulty());
    }

    /// <summary>替换指定列表中的 MeshInstance3D 材质（通过 provider 获取路径）。</summary>
    public static void ApplyMaterialToMeshList(List<MeshInstance3D> meshList, IDifficultyMaterialProvider provider)
    {
        ApplyMaterialToMeshList(meshList, provider.GetMaterialPathForDifficulty());
    }
}
